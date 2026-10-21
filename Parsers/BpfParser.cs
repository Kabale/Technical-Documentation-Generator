using dynamics.document.generator.Helpers;
using dynamics.document.generator.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace dynamics.document.generator.Parsers
{
    public class BpfParser
    {
        static readonly XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        static readonly XNamespace mxswa = "clr-namespace:Microsoft.Xrm.Sdk.Workflow.Activities;assembly=Microsoft.Xrm.Sdk.Workflow";
        static readonly XNamespace mcwo = "clr-namespace:Microsoft.Crm.Workflow.Activities;assembly=Microsoft.Crm.Workflow";
        static readonly XNamespace mcwb = "clr-namespace:Microsoft.Crm.Workflow.Activities;assembly=Microsoft.Crm.Workflow"; // à ajuster selon le vrai préfixe trouvé

        private readonly XDocument _customizations;
        private readonly string _workflowsFolderPath;

        public BpfParser(XDocument customizations, string workflowsFolderPath)
        {
            _customizations = customizations;
            _workflowsFolderPath = workflowsFolderPath;
        }

        private static XDocument LoadXamlSafe(string path)
        {
            var settings = new System.Xml.XmlReaderSettings
            {
                CheckCharacters = false,
                DtdProcessing = System.Xml.DtdProcessing.Ignore
            };
            try
            {
                using var reader = System.Xml.XmlReader.Create(path, settings);
                return XDocument.Load(reader);
            }
            catch (System.Xml.XmlException)
            {
                // Repli : on nettoie les espaces dans les valeurs clr-namespace
                var text = File.ReadAllText(path);
                text = System.Text.RegularExpressions.Regex.Replace(
                    text, @"(xmlns:\w+=""clr-namespace:[^""]*"")",
                    m => m.Value.Replace("; ", ";").Replace(" ", ""));
                return XDocument.Parse(text);
            }
        }

        /// <summary>
        /// Parse tous les BPF (Category=4) liés à une entité donnée,
        /// que ce soit en tant qu'entité primaire OU entité traversée par le BPF.
        /// </summary>
        public List<BpfInfo> ParseBpfs(string entityLogicalName)
        {
            var bpfs = new List<BpfInfo>();

            var bpfNodes = _customizations.Descendants("Workflow")
                .Where(w => (string)w.Element("Category") == "4");

            foreach (var bpfNode in bpfNodes)
            {
                var xamlFileName = (string)bpfNode.Element("XamlFileName");
                if (string.IsNullOrWhiteSpace(xamlFileName))
                    continue;

                var xamlPath = Path.Combine(_workflowsFolderPath, Path.GetFileName(xamlFileName));
                if (!File.Exists(xamlPath))
                {
                    Console.WriteLine($"[Warning] Fichier XAML introuvable : {xamlPath}");
                    continue;
                }

                List<BpfStageInfo> stages;
                try
                {
                    //var xamlDoc = XDocument.Load(xamlPath);
                    var xamlDoc = LoadXamlSafe(xamlPath);
                    stages = ParseStagesFromXaml(xamlDoc);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Warning] Impossible de parser le XAML du BPF '{(string)bpfNode.Element("Name")}' : {ex.Message}");
                    continue;
                }

                // On ne garde le BPF que s'il concerne réellement l'entité demandée
                var relatedEntities = stages
                    .Select(s => s.EntityName)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!relatedEntities.Contains(entityLogicalName, StringComparer.OrdinalIgnoreCase))
                    continue;

                var bpf = new BpfInfo
                {
                    Name = (string)bpfNode.Attribute("Name"),
                    ProcessId = ParserHelper.NormGuid((string)bpfNode.Attribute("WorkflowId")),
                    UniqueId = (string)bpfNode.Attribute("WorkflowId"),
                    IsActive = (string)bpfNode.Element("StateCode") == "1",
                    PrimaryEntity = entityLogicalName,
                    Stages = stages
                };

                bpfs.Add(bpf);
            }

            return bpfs;
        }
        /*
        /// <summary>
        /// Parse tous les BPF de la solution en une seule passe (utile si on documente
        /// beaucoup d'entités : évite de relire chaque .xaml pour chaque entité).
        /// </summary>
        public List<BpfInfo> ParseAllBpfs()
        {
            var bpfs = new List<BpfInfo>();

            var bpfNodes = _customizations.Descendants("Workflow")
                .Where(w => (string)w.Element("Category") == "4");

            foreach (var bpfNode in bpfNodes)
            {
                var xamlFileName = (string)bpfNode.Element("XamlFileName");
                if (string.IsNullOrWhiteSpace(xamlFileName))
                    continue;

                var xamlPath = Path.Combine(_workflowsFolderPath, Path.GetFileName(xamlFileName));
                if (!File.Exists(xamlPath))
                    continue;

                var bpf = new BpfInfo
                {   
                    Name = (string)bpfNode.Element("Name"),
                    UniqueId = (string)bpfNode.Attribute("WorkflowId"),
                    IsActive = (string)bpfNode.Element("StateCode") == "1"
                };

                try
                {
                    //var xamlDoc = XDocument.Load(xamlPath);
                    var xamlDoc = LoadXamlSafe(xamlPath);
                    bpf.Stages = ParseStagesFromXaml(xamlDoc);
                    bpf.PrimaryEntity = bpf.Stages.FirstOrDefault()?.EntityName;
                    bpf.ProcessId = NormGuid((string)bpfNode.Attribute("WorkflowId"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Warning] Impossible de parser le XAML du BPF '{bpf.Name}' : {ex.Message}");
                }

                bpfs.Add(bpf);
            }

            return bpfs;
        }
        */
        /*
        /// <summary>
        /// Filtre des BPF déjà parsés (issus de ParseAllBpfs) pour une entité donnée.
        /// À privilégier si vous documentez plusieurs entités : un seul passage sur les .xaml.
        /// </summary>
        public static List<BpfInfo> GetBpfsForEntity(List<BpfInfo> allBpfs, string entityLogicalName)
        {
            return allBpfs
                .Where(b => b.Stages
                    .Select(s => s.EntityName)
                    .Any(e => string.Equals(e, entityLogicalName, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
        */

        string GetPropByKey(XElement propertiesContainer, string key)
        {
            if (propertiesContainer == null) return null;

            var el = propertiesContainer.Elements()
                .FirstOrDefault(e => e.Attributes()
                    .Any(a => a.Name.LocalName == "Key" && a.Value == key));

            return el?.Value;
        }

        private static readonly XNamespace XamlX = "http://schemas.microsoft.com/winfx/2006/xaml";

        private static string Attr(XElement e, string name) =>
            e?.Attributes().FirstOrDefault(a => a.Name.LocalName == name)?.Value;

        private static string XKey(XElement e) =>
            e.Attributes().FirstOrDefault(a => a.Name.LocalName == "Key" && a.Name.Namespace == XamlX)?.Value;

        private static bool IsActivity(XElement e, string typeName) =>
            e.Name.LocalName == "ActivityReference" &&
            (Attr(e, "AssemblyQualifiedName") ?? "").Contains(typeName + ",", StringComparison.Ordinal);

        // Lit une propriété <String x:Key="..."> appartenant directement à ce stage (hors stages imbriqués)
        private static string StageProp(XElement stageNode, string key) =>
            stageNode.Descendants()
                .Where(e => e.Name.LocalName is "String" or "Boolean" or "Int32")
                .Where(e => XKey(e) == key)
                .FirstOrDefault(e => e.Ancestors().First(a => IsActivity(a, "StageComposite")) == stageNode)
                ?.Value?.Trim();

        private List<BpfStageInfo> ParseStagesFromXaml(XDocument xamlDoc)
        {
            var stages = new List<BpfStageInfo>();

            var stageNodes = xamlDoc.Descendants().Where(e => IsActivity(e, "StageComposite")).ToList();

            foreach (var stageNode in stageNodes)
            {
                var stage = new BpfStageInfo
                {
                    StageId = StageProp(stageNode, "StageId"),
                    StageCategory = StageProp(stageNode, "StageCategory"),
                    NextStageId = StageProp(stageNode, "NextStageId"),
                    StageName = stageNode.Descendants()
                                    .FirstOrDefault(e => e.Name.LocalName == "StepLabel")
                                    ?.Attribute("Description")?.Value
                                ?? Attr(stageNode, "DisplayName"),
                    EntityName = stageNode.Descendants()
                                    .FirstOrDefault(e => e.Name.LocalName == "GetEntityProperty")
                                    ?.Attribute("EntityName")?.Value
                };

                stage.Steps = stageNode.Descendants()
                    .Where(e => IsActivity(e, "StepComposite"))
                    .Where(s => IsDirectStepOfStage(s, stageNode))
                    .Select(ParseStep)
                    .ToList();

                stage.Transitions = ParseTransitions(stageNode, stage.StageId);
                stages.Add(stage);
            }

            ResolveNames(stages);   // une seule fois, après la boucle
            return stages;
        }

        private static List<BpfTransitionInfo> ParseTransitions(XElement stageNode, string stageId)
        {
            var result = new List<BpfTransitionInfo>();

            foreach (var seq in stageNode.Descendants().Where(e => IsActivity(e, "ConditionSequence")))
            {
                // On ne traite que les séquences appartenant à CE stage
                if (seq.Ancestors().First(a => IsActivity(a, "StageComposite")) != stageNode) continue;

                // Index des variables : nom -> (attribut lu, ou expression littérale)
                var props = seq.Descendants().ToList();

                foreach (var branch in seq.Descendants().Where(e => IsActivity(e, "ConditionBranch")))
                {
                    var snsList = branch.Descendants()
                        .Where(e => e.Name.LocalName == "SetNextStage")
                        .Where(e => string.Equals(Attr(e, "ParentStageId"), stageId, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (snsList.Count == 0) continue;

                    var conditionArg = branch.Descendants()
                        .FirstOrDefault(e => e.Name.LocalName == "InArgument" && XKey(e) == "Condition")
                        ?.Value?.Trim();

                    var isElse = string.Equals(conditionArg, "True", StringComparison.OrdinalIgnoreCase);
                    var condText = isElse ? "Sinon (par défaut)" : DescribeCondition(seq, conditionArg);

                    foreach (var sns in snsList)
                    {
                        result.Add(new BpfTransitionInfo
                        {
                            BranchName = Attr(branch, "DisplayName"),
                            IsElse = isElse,
                            Condition = condText,
                            TargetStageId = Attr(sns, "StageId")
                        });
                    }
                }
            }
            return result;
        }

        // Reconstruit un texte lisible à partir des variables ConditionBranchStepNNN_x
        private static string DescribeCondition(XElement seq, string conditionArg)
        {
            var varName = conditionArg?.Trim('[', ']', ' ');
            if (string.IsNullOrEmpty(varName)) return "";

            string Describe(string resultVar, int depth = 0)
            {
                if (depth > 6) return resultVar;

                // EvaluateLogicalCondition (And / Or)
                var logical = seq.Descendants().FirstOrDefault(e => IsActivity(e, "EvaluateLogicalCondition")
                    && ArgByKey(e, "Result")?.Trim('[', ']', ' ') == resultVar);
                if (logical != null)
                {
                    var op = ArgByKey(logical, "LogicalOperator") ?? "And";
                    var operands = ArgByKey(logical, "Parameters") ?? "";
                    var vars = System.Text.RegularExpressions.Regex.Matches(operands, @"ConditionBranchStep\d+_\w+")
                        .Select(m => m.Value).ToList();
                    return "(" + string.Join($" {op.ToUpperInvariant()} ", vars.Select(v => Describe(v, depth + 1))) + ")";
                }

                // EvaluateCondition simple
                var cond = seq.Descendants().FirstOrDefault(e => IsActivity(e, "EvaluateCondition")
                    && ArgByKey(e, "Result")?.Trim('[', ']', ' ') == resultVar);
                if (cond == null) return resultVar;

                var op2 = ArgByKey(cond, "ConditionOperator");
                var operandVar = ArgByKey(cond, "Operand")?.Trim('[', ']', ' ');
                var paramsRaw = ArgByKey(cond, "Parameters") ?? "";

                // Champ : GetEntityProperty dont Value = operandVar
                var field = seq.Descendants().FirstOrDefault(e => e.Name.LocalName == "GetEntityProperty"
                    && Attr(e, "Value")?.Trim('[', ']', ' ') == operandVar);
                var fieldName = Attr(field, "Attribute") ?? operandVar;

                // Valeur : variables ConditionBranchStepNNN_k -> EvaluateExpression(CreateCrmType)
                var valueVars = System.Text.RegularExpressions.Regex.Matches(paramsRaw, @"ConditionBranchStep\d+_\w+")
                    .Select(m => m.Value).ToList();
                var values = valueVars.Select(v => ResolveLiteral(seq, v)).ToList();

                return values.Count > 0
                    ? $"{fieldName} {op2} {string.Join(", ", values)}"
                    : $"{fieldName} {op2}";
            }

            return Describe(varName);
        }

        private static string ResolveLiteral(XElement seq, string variable)
        {
            var expr = seq.Descendants().FirstOrDefault(e => IsActivity(e, "EvaluateExpression")
                && ArgByKey(e, "Result")?.Trim('[', ']', ' ') == variable);
            var p = ArgByKey(expr, "Parameters");
            if (p == null) return variable;

            // New Object() { WorkflowPropertyType.String, "valeur", "String" }
            var m = System.Text.RegularExpressions.Regex.Matches(p, "\"([^\"]*)\"");
            if (m.Count > 0) return $"\"{m[0].Groups[1].Value}\"";

            var n = System.Text.RegularExpressions.Regex.Match(p, @"-?\d{5,}|\bTrue\b|\bFalse\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return n.Success ? n.Value : p.Trim();
        }

        private static string ArgByKey(XElement e, string key) =>
            e?.Descendants().FirstOrDefault(x =>
                (x.Name.LocalName == "InArgument" || x.Name.LocalName == "OutArgument") && XKey(x) == key)
                ?.Value?.Trim();

        private static void ResolveNames(List<BpfStageInfo> stages)
        {
            var byId = stages
                .Where(s => !string.IsNullOrWhiteSpace(s.StageId))
                .GroupBy(s => ParserHelper.NormGuid(s.StageId))
                .ToDictionary(g => g.Key, g => g.First().StageName);

            foreach (var s in stages)
            {
                var nk = ParserHelper.NormGuid(s.NextStageId);
                if (nk != null && byId.TryGetValue(nk, out var n)) s.NextStageName = n;

                foreach (var t in s.Transitions)
                {
                    var tk = ParserHelper.NormGuid(t.TargetStageId);
                    if (tk != null && byId.TryGetValue(tk, out var tn)) t.TargetStageName = tn;
                }
            }
        }

        private BpfStepInfo ParseStep(XElement stepNode)
        {
            var stepProps = stepNode.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "ActivityReference.Properties");

            // Le Control est nested dans Activities > Sequence > Control
            var controlEl = stepNode.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "Control");

            var fieldName = controlEl?.Attribute("DataFieldName")?.Value;
            var controlId = controlEl?.Attribute("ControlId")?.Value;
            var controlDisplayName = controlEl?.Attribute("ControlDisplayName")?.Value;
            var isSystemControl = controlEl?.Attribute("IsSystemControl")?.Value == "True";
            var systemStepType = controlEl?.Attribute("SystemStepType")?.Value;

            // Fallback sur StepLabel si pas de Control (rare, mais possible)
            var stepLabel = stepNode.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "StepLabel");
            var stepName = controlDisplayName
                ?? stepLabel?.Attribute("Description")?.Value
                ?? (string)stepNode.Attribute("DisplayName");

            var isRequired = GetPropByKey(stepProps, "IsProcessRequired") == "True";

            return new BpfStepInfo
            {
                StepName = stepName,
                FieldName = fieldName,
                ControlId = controlId,
                IsSystemControl = isSystemControl,
                SystemStepType = systemStepType,
                IsRequired = isRequired
            };
        }

        private bool IsDirectStepOfStage(XElement stepNode, XElement stageNode)
        {
            var current = stepNode.Parent;
            while (current != null && current != stageNode)
            {
                if (current.Name.LocalName == "ActivityReference" &&
                    ((string)current.Attribute("AssemblyQualifiedName") ?? "").Contains("StageComposite,"))
                {
                    return false;
                }
                current = current.Parent;
            }
            return current == stageNode;
        }

        private static readonly string[] ConditionNodeNames =
        {
            "ConditionBranch", "ConditionStep", "Condition", "Branch", "ConditionExpression"
        };
        /*
        private List<BpfConditionInfo> ParseConditions(XElement stageNode)
        {
            var result = new List<BpfConditionInfo>();

            // 1) ActivityReference dont le type contient "Condition" / "Branch" (ex: ConditionBranchComposite)
            var activityConditions = stageNode.Descendants()
                .Where(e => e.Name.LocalName == "ActivityReference")
                .Where(a =>
                {
                    var aqn = (string)a.Attribute("AssemblyQualifiedName") ?? "";
                    return (aqn.Contains("Condition", StringComparison.OrdinalIgnoreCase)
                         || aqn.Contains("Branch", StringComparison.OrdinalIgnoreCase))
                        && !aqn.Contains("StageComposite,")
                        && !aqn.Contains("StepComposite,");
                });

            // 2) Éléments directs portant un nom de type condition
            var directConditions = stageNode.Descendants()
                .Where(e => ConditionNodeNames.Contains(e.Name.LocalName, StringComparer.OrdinalIgnoreCase));

            var seen = new HashSet<XElement>();
            foreach (var node in activityConditions.Concat(directConditions))
            {
                // évite de traiter un nœud imbriqué dans un nœud déjà traité
                if (node.Ancestors().Any(seen.Contains)) continue;
                seen.Add(node);

                var props = node.Elements().FirstOrDefault(e => e.Name.LocalName == "ActivityReference.Properties");

                var cond = new BpfConditionInfo
                {
                    NodeType = ShortType((string)node.Attribute("AssemblyQualifiedName")) ?? node.Name.LocalName,
                    Label = (string)node.Attribute("DisplayName")
                            ?? node.Descendants().FirstOrDefault(e => e.Name.LocalName == "StepLabel")?.Attribute("Description")?.Value,
                    TargetStageId = GetPropByKey(props, "NextStageId")
                                    ?? GetPropByKey(props, "TargetStageId")
                                    ?? GetPropByKey(props, "StageId"),
                    Clauses = ParseClauses(node),
                    RawXml = Condensed(node)
                };

                result.Add(cond);
            }

            return result;
        }

        private List<BpfConditionClause> ParseClauses(XElement conditionNode)
        {
            var clauses = new List<BpfConditionClause>();

            // Cas 1 : nœuds de type ConditionExpression / Condition avec attributs explicites
            foreach (var e in conditionNode.DescendantsAndSelf()
                .Where(e => e.Name.LocalName is "ConditionExpression" or "Condition" or "ConditionClause"))
            {
                var field = (string)e.Attribute("AttributeName") ?? (string)e.Attribute("Attribute")
                            ?? (string)e.Attribute("FieldName") ?? (string)e.Attribute("DataFieldName");
                var op = (string)e.Attribute("Operator") ?? (string)e.Attribute("ConditionOperator");

                if (field == null && op == null) continue;

                var values = e.Descendants().Where(v => v.Name.LocalName is "Value" or "Values")
                               .Select(v => v.Value.Trim()).Where(v => v.Length > 0).ToList();

                clauses.Add(new BpfConditionClause
                {
                    Entity = (string)e.Attribute("EntityName") ?? (string)e.Attribute("Entity"),
                    Field = field,
                    Operator = op,
                    Value = (string)e.Attribute("Value") ?? (values.Count > 0 ? string.Join(", ", values) : null),
                    LogicalOperator = (string)e.Parent?.Attribute("LogicalOperator")
                                      ?? (string)e.Parent?.Attribute("FilterOperator")
                });
            }

            // Cas 2 : structure type FetchXML (<filter type="and"><condition attribute=".." operator=".." value=".."/>)
            foreach (var c in conditionNode.Descendants().Where(e => e.Name.LocalName == "condition"))
            {
                clauses.Add(new BpfConditionClause
                {
                    Entity = (string)c.Attribute("entityname"),
                    Field = (string)c.Attribute("attribute"),
                    Operator = (string)c.Attribute("operator"),
                    Value = (string)c.Attribute("value")
                            ?? string.Join(", ", c.Elements().Select(v => v.Value.Trim())),
                    LogicalOperator = (string)c.Parent?.Attribute("type")
                });
            }

            return clauses;
        }

        private static string ShortType(string aqn)
        {
            if (string.IsNullOrWhiteSpace(aqn)) return null;
            var typePart = aqn.Split(',')[0];
            return typePart.Split('.').Last();
        }

        private static string Condensed(XElement e, int max = 1500)
        {
            var s = e.ToString(SaveOptions.DisableFormatting);
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+xmlns(:\w+)?=""[^""]+""", "");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
            return s.Length > max ? s[..max] + "…" : s;
        }
        */
        //private static string NormGuid(string g) => string.IsNullOrWhiteSpace(g) ? null : g.Trim('{', '}', ' ').ToLowerInvariant();
    }
}
