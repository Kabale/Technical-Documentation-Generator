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
        private readonly string _extractFolderPath;


        public BusinessRuleParser(XDocument customizations, string extractFolder)
        {
            _customizations = customizations ?? throw new ArgumentNullException(nameof(customizations));
            _extractFolderPath = extractFolder;
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

                var XamlFileName = Value(workflow, "XamlFileName") ?? Value(workflow, "Xaml");
                XDocument RawXaml = XDocument.Load($"{_extractFolderPath}{XamlFileName}");

                var rule = new BusinessRuleInfo
                {
                    Name = ParserHelper.GetWorkflowDisplayName(workflow),
                    UniqueId = Attr(workflow, "WorkflowId"),
                    IsActive = Value(workflow, "StateCode") == "1",
                    Scope = Value(workflow, "Scope") == "1" ? "Entité" : "Formulaire spécifique",
                    RawXaml = RawXaml.ToString()
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
            if (String.IsNullOrWhiteSpace(xaml)) throw new ArgumentException("XAML is empty.", nameof(xaml));
            var doc = XDocument.Parse(xaml, LoadOptions.PreserveWhitespace);
            var result = new BusinessRuleDetailedInfo { RawXaml = xaml };
            var root = doc.Root;
            if (root == null) return result;
            var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var sourceVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var v in root.Descendants().Where(e => Local(e) == "Variable"))
                if (Attr(v, "Name") != null && Attr(v, "Default") != null) vars[Attr(v, "Name")] = Attr(v, "Default");
            foreach (var e in root.DescendantsAndSelf())
            {
                var entity = Attr(e, "EntityName"); if (!String.IsNullOrWhiteSpace(entity)) AddDistinct(result.Entities, entity);
                var t = Local(e);
                if (t == "GetEntityProperty")
                {
                    var field = FirstAttr(e, "Attribute", "AttributeName"); var target = Unwrap(Attr(e, "Value"));
                    if (!String.IsNullOrWhiteSpace(field)) { AddDistinct(result.Reads, field); if (!String.IsNullOrWhiteSpace(target)) sourceVars[target] = field; }
                }
            }
            foreach (var e in root.Descendants().Where(IsActivity("EvaluateExpression")))
            {
                var target = Unwrap(Argument(e, "Result")); if (target.Length > 0) vars[target] = ParseLiteral(Argument(e, "Parameters"));
            }
            foreach (var e in root.Descendants().Where(IsActivity("EvaluateCondition")))
            {
                var target = Unwrap(Argument(e, "Result")); var operand = Unwrap(Argument(e, "Operand"));
                var p = Argument(e, "Parameters"); var op = Argument(e, "ConditionOperator") ?? "?";
                var right = ParseLiteral(p);
                var expr = Resolve(vars, sourceVars, operand) + " " + op + " " + Resolve(vars, sourceVars, right);
                if (target.Length > 0) vars[target] = expr;
            }
            foreach (var e in root.Descendants().Where(IsActivity("EvaluateLogicalCondition")))
            {
                var target = Unwrap(Argument(e, "Result")); var left = Resolve(vars, sourceVars, Unwrap(Argument(e, "LeftOperand")));
                var right = Resolve(vars, sourceVars, Unwrap(Argument(e, "RightOperand"))); var op = Argument(e, "LogicalOperator") ?? "AND";
                if (target.Length > 0) vars[target] = "(" + left + " " + op.ToUpperInvariant() + " " + right + ")";
            }
            var branches = root.Descendants().Where(IsActivity("ConditionBranch")).ToList();
            foreach (var b in branches)
            {
                var id = Attr(b, "DisplayName") ?? "ConditionBranch"; var c = Unwrap(Argument(b, "Condition"));
                var condition = Resolve(vars, sourceVars, c); var isElse = c.Equals("True", StringComparison.OrdinalIgnoreCase) || (!vars.ContainsKey(c) && !root.Descendants().Any(x => Local(x) == "EvaluateCondition" && Unwrap(Argument(x, "Result")) == c));
                var branch = new BusinessRuleBranch { Name = id, BranchKind = isElse ? "ELSE" : (result.Branches.Count == 0 ? "IF" : "ELSE IF"), Condition = isElse ? "" : condition };
                var holder = b.Descendants().FirstOrDefault(x => Attr(x, "Key") == "Then");
                if (holder != null) foreach (var a in holder.Descendants().Where(IsAction)) { var action = ReadAction(a); action.Branch = id; branch.Actions.Add(action); result.Actions.Add(action); }
                result.Branches.Add(branch);
                if (!isElse) result.Conditions.Add(new BusinessRuleCondition { Expression = condition, Branch = id });
            }
            // Be tolerant of action nodes not nested under a branch (or activities unknown to this parser).
            foreach (var a in root.Descendants().Where(IsAction)) if (!result.Actions.Contains(result.Actions.FirstOrDefault(x => SameAction(x, a)))) { var x = ReadAction(a); x.Branch = BranchOf(a); result.Actions.Add(x); }
            foreach (var e in root.Descendants().Where(x => Local(x) == "ActivityReference" && !IsKnownActivity(x))) { var u = new UnknownAction { ActivityName = Attr(e, "DisplayName") ?? Attr(e, "AssemblyQualifiedName") ?? "UnknownActivity", Branch = BranchOf(e) }; foreach (var p in e.Attributes()) u.RawProperties[p.Name.LocalName] = p.Value; result.UnknownActions.Add(u); }
            foreach (var f in result.Reads) AddDistinct(result.ConditionFields, f);
            foreach (var a in result.Actions) { AddDistinct(result.ActionFields, a.Field); if (!String.IsNullOrWhiteSpace(a.Entity)) AddDistinct(result.Entities, a.Entity); }
            return result;
        }
        public static Func<XElement, bool> IsActivity(string name) => e => Local(e) == "ActivityReference" && (Attr(e, "AssemblyQualifiedName") ?? "").IndexOf("." + name + ",", StringComparison.OrdinalIgnoreCase) >= 0;
        private static bool IsKnownActivity(XElement e) => new[] { "ConditionSequence", "ConditionBranch", "EvaluateExpression", "EvaluateCondition", "EvaluateLogicalCondition", "Composite" }.Any(n => IsActivity(n)(e)) || IsAction(e);
        private static bool IsAction(XElement e) => new[] { "SetVisibility", "SetFieldRequiredLevel", "SetRequiredLevel", "SetDisplayMode", "SetMessage", "SetDefaultValue", "SetAttributeValue", "SetEntityProperty", "LockField", "SetLock" }.Contains(Local(e));
        private static BusinessRuleAction ReadAction(XElement e) { var a = new BusinessRuleAction { Type = Local(e), Field = FirstAttr(e, "ControlId", "Attribute", "AttributeName") ?? "", Value = FirstAttr(e, "IsVisible", "DisplayMode", "RequiredLevel", "Message", "Text", "Value") ?? "", Entity = Attr(e, "EntityName") ?? "" }; foreach (var p in e.Attributes()) a.RawProperties[p.Name.LocalName] = p.Value; if (a.Type == "SetAttributeValue" && a.Field == "") { var n = e.Descendants().FirstOrDefault(x => Local(x) == "SetEntityProperty"); if (n != null) { a.Field = Attr(n, "Attribute") ?? ""; a.Value = Attr(n, "Value") ?? a.Value; } } return a; }
        private static string BranchOf(XElement e) => e.Ancestors().FirstOrDefault(x => Attr(x, "Key") == "Then" || Attr(x, "Key") == "Else") is XElement a ? Attr(a, "Key") ?? "" : "";
        private static bool SameAction(BusinessRuleAction a, XElement e) => a.Type == Local(e) && (a.Field == FirstAttr(e, "ControlId", "Attribute", "AttributeName"));
        private static string Resolve(Dictionary<string, string> v, Dictionary<string, string> s, string x) { x = Unwrap(x); if (v.TryGetValue(x, out var r)) return Resolve(v, s, r); if (s.TryGetValue(x, out r)) return r; return FormatLiteral(x); }
        private static string ParseLiteral(string s) { var m = Regex.Match(s ?? "", "\\\"([^\\\"]*)\\\""); return m.Success ? m.Groups[1].Value : VariableRefs(s ?? "").FirstOrDefault() ?? FormatLiteral(s); }
        private static string FormatLiteral(string s) => String.IsNullOrWhiteSpace(s) ? "null" : (s.Equals("True", StringComparison.OrdinalIgnoreCase) || s.Equals("False", StringComparison.OrdinalIgnoreCase) ? s : s);
        private static IEnumerable<string> VariableRefs(string s) => Regex.Matches(s ?? "", @"(?<![\w])([A-Za-z_]\w*)(?![\w])").Cast<Match>().Select(m => m.Groups[1].Value).Where(x => !x.Equals("New", StringComparison.OrdinalIgnoreCase) && !x.Equals("Object", StringComparison.OrdinalIgnoreCase));
        private static string Unwrap(string s) { s = (s ?? "").Trim(); return s.StartsWith("[") && s.EndsWith("]") ? s.Substring(1, s.Length - 2).Trim() : s; }
        private static string Argument(XElement e, string key) => e.Elements().FirstOrDefault(x => Local(x) == "ActivityReference.Arguments")?.Elements().FirstOrDefault(x => Attr(x, "Key") == key)?.Value.Trim() ?? "";
        private static string Local(XElement e) => e.Name.LocalName; private static string Attr(XElement e, string n) => e?.Attribute(n)?.Value; private static string FirstAttr(XElement e, params string[] n) { foreach (var x in n) { var v = Attr(e, x); if (v != null) return v; } return null; }
        private static string Value(XElement e, string n) => e.Elements().FirstOrDefault(x => Local(x) == n)?.Value; private static void AddDistinct(List<string> l, string v) { if (!l.Any(x => String.Equals(x, v, StringComparison.OrdinalIgnoreCase))) l.Add(v); }
    }
}
