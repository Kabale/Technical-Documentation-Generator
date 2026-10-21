using dynamics.document.generator.Helpers;
using dynamics.document.generator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace dynamics.document.generator.Parsers
{
    /// <summary>Analyse les XAML exportés par les Business Rules Dynamics 365.</summary>
    public class BusinessRuleParser
    {
        private readonly XDocument _customizations;

        public BusinessRuleParser(XDocument customizations)
        {
            _customizations = customizations ?? throw new ArgumentNullException(nameof(customizations));
        }

        /// <summary>API historique conservée. Les champs et le résumé sont maintenant alimentés par le parseur XML.</summary>
        public List<BusinessRuleInfo> ParseBusinessRules(string entityLogicalName)
        {
            var result = new List<BusinessRuleInfo>();
            var workflows = _customizations.Descendants().Where(e => Local(e) == "Workflow");
            foreach (var workflow in workflows)
            {
                if (Value(workflow, "Category") != "2" ||
                    (!string.IsNullOrWhiteSpace(entityLogicalName) && Value(workflow, "PrimaryEntity") != entityLogicalName))
                    continue;

                var rule = new BusinessRuleInfo
                {
                    Name = ParserHelper.GetWorkflowDisplayName(workflow),
                    UniqueId = Attr(workflow, "WorkflowId"),
                    IsActive = Value(workflow, "StateCode") == "1",
                    Scope = Value(workflow, "Scope") == "1" ? "Entité" : "Formulaire spécifique",
                    RawXaml = Value(workflow, "JsonFormXml") ?? Value(workflow, "Xaml")
                };
                if (!string.IsNullOrWhiteSpace(rule.RawXaml))
                {
                    var detail = ParseXaml(rule.RawXaml);
                    foreach (var f in detail.ConditionFields) if (!rule.ConditionFields.Contains(f)) rule.ConditionFields.Add(f);
                    foreach (var f in detail.ActionFields) if (!rule.ActionFields.Contains(f)) rule.ActionFields.Add(f);
                    rule.ActionsSummary.AddRange(detail.Actions.Select(a => a.ToString()));
                }
                result.Add(rule);
            }
            return result;
        }

        /// <summary>Analyse directement le XAML d'une règle, sans exiger un wrapper Customizations.</summary>
        public BusinessRuleDetailedInfo ParseXaml(string xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml)) throw new ArgumentException("Le XAML est vide.", nameof(xaml));
            // Certains exports contiennent des URI CLR avec des espaces. Ils restent des noms
            // XML valides pour Dynamics, mais pas pour tous les lecteurs XML .NET.
            var safe = Regex.Replace(xaml,@"(xmlns(?::[^=]+)?=""[^""]*"")",m => m.Value.Replace(" ", "%20")); var doc = XDocument.Parse(safe, LoadOptions.PreserveWhitespace);
            var info = new BusinessRuleDetailedInfo { RawXaml = xaml };
            var root = doc.Root;
            if (root == null) return info;

            foreach (var e in root.DescendantsAndSelf())
            {
                var entity = Attr(e, "EntityName");
                if (!string.IsNullOrWhiteSpace(entity)) info.Entities.Add(entity);
                var field = Attr(e, "Attribute") ?? Attr(e, "ControlId");
                if (Local(e) == "GetEntityProperty" && !string.IsNullOrWhiteSpace(field)) info.Reads.Add(field);
            }

            var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var read in root.Descendants().Where(e => Local(e) == "GetEntityProperty"))
            {
                var target = Attr(read, "Value");
                var field = Attr(read, "Attribute");
                if (!string.IsNullOrWhiteSpace(target) && !string.IsNullOrWhiteSpace(field)) variables[Unwrap(target)] = field;
            }
            foreach (var expression in root.Descendants().Where(IsActivity("EvaluateExpression")))
            {
                var target = Unwrap(Argument(expression, "Result"));
                if (!string.IsNullOrWhiteSpace(target)) variables[target] = Literal(Argument(expression, "Parameters"));
            }
            foreach (var condition in root.Descendants().Where(IsActivity("EvaluateCondition")))
            {
                var target = Unwrap(Argument(condition, "Result"));
                var operand = Unwrap(Argument(condition, "Operand"));
                var value = Unwrap(Argument(condition, "Parameters"));
                var op = Argument(condition, "ConditionOperator") ?? "";
                if (!string.IsNullOrWhiteSpace(target)) variables[target] = FormatCondition(variables, operand, op, value);
            }
            foreach (var logical in root.Descendants().Where(IsActivity("EvaluateLogicalCondition")))
            {
                var target = Unwrap(Argument(logical, "Result"));
                var left = Unwrap(Argument(logical, "LeftOperand"));
                var right = Unwrap(Argument(logical, "RightOperand"));
                var op = Argument(logical, "LogicalOperator") ?? "";
                if (!string.IsNullOrWhiteSpace(target)) variables[target] = "(" + Resolve(variables, left) + " " + op + " " + Resolve(variables, right) + ")";
            }
            foreach (var branch in root.Descendants().Where(IsActivity("ConditionBranch")))
            {
                var condition = Unwrap(Argument(branch, "Condition"));
                info.Conditions.Add(new BusinessRuleCondition { Expression = Resolve(variables, condition), Branch = BranchOf(branch) });
            }

            foreach (var e in root.Descendants())
            {
                var type = Local(e);
                if (type != "SetVisibility" && type != "SetFieldRequiredLevel" && type != "SetDefaultValue" && type != "SetAttributeValue" && type != "SetEntityProperty" && type != "SetDisplayMode" && type != "SetLock" && type != "LockField") continue;
                var field = Attr(e, "ControlId") ?? Attr(e, "Attribute") ?? Attr(e, "AttributeName");
                var value = FirstAttr(e, "IsVisible", "DisplayMode", "RequiredLevel", "Value", "Message", "Text");
                if (type == "SetAttributeValue" && string.IsNullOrWhiteSpace(field))
                {
                    var child = e.Descendants().FirstOrDefault(x => Local(x) == "SetEntityProperty");
                    field = Attr(child, "Attribute") ?? field; value = Attr(child, "Value") ?? value;
                }
                if (!string.IsNullOrWhiteSpace(field)) info.Actions.Add(new BusinessRuleAction { Type = type, Field = field, Value = value ?? "", Branch = BranchOf(e), Entity = Attr(e, "EntityName") });
            }
            info.ConditionFields.AddRange(info.Conditions.SelectMany(c => FieldsIn(c.Expression)).Distinct(StringComparer.OrdinalIgnoreCase));
            info.ActionFields.AddRange(info.Actions.Select(a => a.Field).Distinct(StringComparer.OrdinalIgnoreCase));
            return info;
        }

        private static Func<XElement, bool> IsActivity(string name) =>
            (XElement e) => Local(e) == "ActivityReference"
                && (Attr(e, "AssemblyQualifiedName") ?? "")
                    .IndexOf("." + name + ",", StringComparison.OrdinalIgnoreCase) >= 0; private static string Local(XElement e) => e.Name.LocalName;
        private static string Attr(XElement e, string name) => e == null ? null : (string)e.Attribute(name);
        private static string Value(XElement e, string name) => e.Elements().FirstOrDefault(x => Local(x) == name)?.Value;
        private static string FirstAttr(XElement e, params string[] names) { foreach (var n in names) { var v = Attr(e, n); if (v != null) return v; } return null; }
        private static string Argument(XElement e, string key)
        {
            var container = e.Elements().FirstOrDefault(x => Local(x) == "ActivityReference.Arguments");
            var arg = container?.Elements().FirstOrDefault(x => Attr(x, "Key") == key);
            return arg == null ? null : arg.Value.Trim();
        }
        private static string Unwrap(string s) { if (string.IsNullOrWhiteSpace(s)) return ""; s = s.Trim(); return s.StartsWith("[") && s.EndsWith("]") ? s.Substring(1, s.Length - 2) : s; }
        private static string Literal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var m = Regex.Match(s, "\\\"((?:\\\\.|[^\\\"])*)\\\"");
            return m.Success ? m.Groups[1].Value : s.Trim('[', ']', ' ');
        }
        private static string Resolve(Dictionary<string, string> vars, string s) { string v; return vars.TryGetValue(Unwrap(s), out v) ? v : Unwrap(s); }
        private static string FormatCondition(Dictionary<string, string> vars, string operand, string op, string value) { return Resolve(vars, operand) + " " + op + " " + Resolve(vars, value); }
        private static string BranchOf(XElement e) { var a = e.Ancestors().FirstOrDefault(x => Attr(x, "Key") == "Then" || Attr(x, "Key") == "Else"); return Attr(a, "Key") ?? ""; }
        private static IEnumerable<string> FieldsIn(string expression) { return Regex.Matches(expression ?? "", @"\b[a-zA-Z_][a-zA-Z0-9_]*\b").Cast<Match>().Select(m => m.Value).Where(x => x.StartsWith("ainos_", StringComparison.OrdinalIgnoreCase) || x.StartsWith("arendt_", StringComparison.OrdinalIgnoreCase) || x == "processid"); }
    }

    public sealed class BusinessRuleDetailedInfo
    {
        public string RawXaml { get; set; }
        public List<string> Entities { get; } = new List<string>();
        public List<string> Reads { get; } = new List<string>();
        public List<BusinessRuleCondition> Conditions { get; } = new List<BusinessRuleCondition>();
        public List<BusinessRuleAction> Actions { get; } = new List<BusinessRuleAction>();
        public List<string> ConditionFields { get; } = new List<string>();
        public List<string> ActionFields { get; } = new List<string>();
        public string ToReadableString() { var b = new StringBuilder(); b.AppendLine("Entités: " + string.Join(", ", Entities.Distinct())); b.AppendLine("Conditions:"); foreach (var c in Conditions) b.AppendLine("- [" + c.Branch + "] " + c.Expression); b.AppendLine("Actions:"); foreach (var a in Actions) b.AppendLine("- [" + a.Branch + "] " + a); return b.ToString(); }
        public string ToMarkdown() => ToReadableString();
    }
    public sealed class BusinessRuleCondition { public string Expression { get; set; } public string Branch { get; set; } }
    public sealed class BusinessRuleAction
    {
        public string Type { get; set; }
        public string Field { get; set; }
        public string Value { get; set; }
        public string Branch { get; set; }
        public string Entity { get; set; }
        public override string ToString() => Type + " → " + Field + (string.IsNullOrWhiteSpace(Value) ? "" : " = " + Value);
    }
}
