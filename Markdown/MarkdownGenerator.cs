using dynamics.document.generator.Helpers;
using dynamics.document.generator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace dynamics.document.generator.Markdown
{
    public class MarkdownGenerator
    {
        private readonly string _outputFolder;

        public MarkdownGenerator(string outputFolder)
        {
            _outputFolder = outputFolder;
            Directory.CreateDirectory(_outputFolder);
        }

        public void GenerateFullDocumentation(string solutionName, List<EntityDocumentation> entities)
        {
            // Fichier index
            var indexSb = new StringBuilder();
            indexSb.AppendLine($"# Documentation technique - {solutionName}");
            indexSb.AppendLine();
            indexSb.AppendLine($"*Générée le {DateTime.Now:dd/MM/yyyy à HH:mm}*");
            indexSb.AppendLine();
            indexSb.AppendLine($"## Sommaire ({entities.Count} entités)");
            indexSb.AppendLine();

            foreach (var entity in entities.OrderBy(e => e.LogicalName))
            {
                var fileName = $"{entity.LogicalName}.md";
                indexSb.AppendLine($"- [{entity.DisplayName} (`{entity.LogicalName}`)]({fileName})");

                // Génération du fichier détaillé de l'entité
                GenerateEntityMarkdown(entity, Path.Combine(_outputFolder, fileName));

                Console.WriteLine($"    ✓ {fileName} généré ({entity.Fields.Count} champs, " +
                    $"{entity.BusinessRules.Count} BR, {entity.FormScripts.Count} scripts, " +
                    $"{entity.BusinessProcessFlows.Count} BPF, {entity.Workflows.Count} workflows, " +
                    $"{entity.Plugins.Count} plugins)");
            }

            File.WriteAllText(Path.Combine(_outputFolder, "index.md"), indexSb.ToString());
        }

        #region Entity Documentation Generation

        private void GenerateEntityMarkdown(EntityDocumentation entity, string filePath)
        {
            // ... votre logique existante de génération par entité (fields, BR, scripts, BPF, WF, plugins)
            var sb = new StringBuilder();

            sb.AppendLine($"# {entity.DisplayName} (`{entity.LogicalName}`)");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(entity.Description))
            {
                sb.AppendLine($"> {entity.Description}");
                sb.AppendLine();
            }
            sb.AppendLine("[⬅ Retour à l'index](index.md)");
            sb.AppendLine();

            AppendFieldsSection(sb, entity);
            AppendBusinessRulesSection(sb, entity);
            AppendFormScriptsSection(sb, entity);
            AppendBpfSection(sb, entity.BusinessProcessFlows);
            AppendWorkflowsSection(sb, entity);
            AppendPluginsSection(sb, entity);
            AppendFormsViewsSection(sb, entity);

            File.WriteAllText(filePath, sb.ToString());
        }
        
        private void AppendFieldsSection(StringBuilder sb, EntityDocumentation entity)
        {
            sb.AppendLine("## 1. Champs");
            sb.AppendLine();
            sb.AppendLine($"**{entity.Fields.Count} champs** ({entity.Fields.Count(f => f.IsCustom)} personnalisés)");
            sb.AppendLine();
            sb.AppendLine("| Nom logique | Libellé | Type | Requis | Custom | Description |");
            sb.AppendLine("|-------------|---------|------|--------|--------|-------------|");

            var validFields = entity.Fields.Where(f => !string.IsNullOrEmpty(f.LogicalName));

            foreach (var field in validFields)
            {
                var typeDisplay = field.OptionSetValues.Any()
                    ? $"{field.Type} `[{string.Join(", ", field.OptionSetValues.Take(3))}{(field.OptionSetValues.Count > 3 ? "..." : "")}]`"
                    : field.Type;

                sb.AppendLine($"| `{field.LogicalName}` " +
                    $"| {field.DisplayName} " +
                    $"| {typeDisplay}{(field.MaxLength != null ? "("+field.MaxLength+")" : "")}" +
                    $"| {(field.RequiredLevel)} " +
                    $"| {field.IsCustom} " +
                    $"| {CleanMarkdownCell(field.Description ?? "")} |");
            }
            sb.AppendLine();
        }

        private void AppendBusinessRulesSection(StringBuilder sb, EntityDocumentation entity)
        {
            sb.AppendLine("## 2. Business Rules");
            sb.AppendLine();

            RenderSection(sb, entity.BusinessRules);
        }

        public static string RenderSection(StringBuilder sb, IEnumerable<BusinessRuleInfo> rules)
        {
            var list = (rules ?? Enumerable.Empty<BusinessRuleInfo>()).ToList();
            if (list.Count == 0) { 
                sb.AppendLine("*Aucune Business Rule sur cette entité.*"); 
                sb.AppendLine(); 
                return sb.ToString(); 
            }
            foreach (var r in list) { 
                sb.Append(RenderRule(r)); 
            }
            return sb.ToString();
        }

        public static string RenderRule(BusinessRuleInfo r)
        {
            var sb = new StringBuilder(); sb.AppendLine($"### {r.Name}"); sb.AppendLine();
            sb.AppendLine($"- **Statut** : {(r.IsActive ? "Active" : "Inactive")}");
            sb.AppendLine($"- **Portée** : {r.Scope}");
            var fields = r.Detail.ConditionFields.Concat(r.Detail.ActionFields).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (fields.Count > 0) { 
                sb.AppendLine("- **Champs concernés** : " + String.Join(", ", fields.Select(f => "`" + f + "`")) + "."); 
            }
            if (!String.IsNullOrWhiteSpace(r.EntityName)) sb.AppendLine($"- **Entité** : `{r.EntityName}`");
            sb.AppendLine();
            foreach (var b in r.Detail.Branches) { sb.AppendLine((b.BranchKind == "ELSE" ? "ELSE" : b.BranchKind + (String.IsNullOrWhiteSpace(b.Condition) ? "" : " " + b.Condition))); foreach (var a in b.Actions) sb.AppendLine("  - " + Action(a)); }
           
            sb.AppendLine(); return sb.ToString();
        }

        private static string Action(BusinessRuleAction a) { 
            var v = String.IsNullOrWhiteSpace(a.Value) ? "" : " = " + a.Value; return a.Type + " " + a.Field + v; 
        }

        private void AppendFormScriptsSection(StringBuilder sb, EntityDocumentation entity)
        {
            sb.AppendLine("## 3. Scripts JavaScript (Form Events)");
            sb.AppendLine();

            if (!entity.FormScripts.Any())
            {
                sb.AppendLine("_Aucun script détecté sur cette entité._");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("| Formulaire | Événement | Champ ciblé | Fonction | Librairie | Actif |");
            sb.AppendLine("|---|---|---|---|---|---|");

            foreach (var script in entity.FormScripts)
            {
                sb.AppendLine($"| {script.FormName} | {script.EventName}{(script.Attributes != null ? "(" + script.Attributes + ")" : "")} | {script.ControlId ?? "*(formulaire)*"} | `{script.FunctionName}` | `{script.LibraryName}` | {(script.Enabled /*? "✅" : "❌"*/)} |");
            }

            sb.AppendLine();
        }
        
        private static void AppendBpfSection(StringBuilder sb, List<BpfInfo> bpfs)
        {
            sb.AppendLine("## 4. Business Process Flows\n");

            foreach (var bpf in bpfs)
            {
                sb.AppendLine($"### {bpf.Name}\n");
                sb.AppendLine($"- **Statut** : {(bpf.IsActive ? "🟢 Actif" : "🔴 Inactif")}");
                sb.AppendLine($"- **ProcessId** : `{ParserHelper.NormGuid(bpf.UniqueId)}`");
                sb.AppendLine($"- **Entité primaire du BPF** : `{bpf.PrimaryEntity}`\n");

                // Vue d'ensemble des transitions
                var withTransitions = bpf.Stages.Where(s => s.Transitions.Count > 0 || !string.IsNullOrEmpty(s.NextStageName)).ToList();
                if (withTransitions.Count > 0)
                {
                    sb.AppendLine("#### Synthèse des transitions\n");
                    sb.AppendLine("| Stage source | Condition | Stage cible |");
                    sb.AppendLine("|--------------|-----------|-------------|");
                    foreach (var s in bpf.Stages)
                    {
                        foreach (var t in s.Transitions)
                            sb.AppendLine($"| {ParserHelper.Esc(s.StageName)} (`{ParserHelper.ShortId(s)}`) | {ParserHelper.Esc(t.Condition)} | {ParserHelper.Esc(t.TargetStageName)} |");
                        if (s.Transitions.Count == 0 && !string.IsNullOrEmpty(s.NextStageName))
                            sb.AppendLine($"| {ParserHelper.Esc(s.StageName)} (`{ParserHelper.ShortId(s)}`) | _(séquentiel)_ | {ParserHelper.Esc(s.NextStageName)} |");
                    }
                    sb.AppendLine();
                }

                foreach (var stage in bpf.Stages)
                {
                    sb.AppendLine($"#### Étape : {stage.StageName} — {stage.StageCategory}\n");
                    sb.AppendLine($"- **StageId** : `{stage.StageId}`");
                    if (!string.IsNullOrEmpty(stage.EntityName))
                        sb.AppendLine($"- **Entité** : `{stage.EntityName}`");

                    if (stage.Transitions.Count > 0)
                    {
                        sb.AppendLine("\n**Branchements**\n");
                        sb.AppendLine("| Condition | Stage cible |");
                        sb.AppendLine("|-----------|-------------|");
                        foreach (var t in stage.Transitions)
                            sb.AppendLine($"| {ParserHelper.Esc(t.Condition)} | {ParserHelper.Esc(t.TargetStageName ?? t.TargetStageId)} |");
                    }
                    else if (!string.IsNullOrEmpty(stage.NextStageName))
                    {
                        sb.AppendLine($"- **Étape suivante** : {stage.NextStageName} (`{stage.NextStageId}`)");
                    }
                    sb.AppendLine();

                    if (stage.Steps?.Count > 0)
                    {
                        sb.AppendLine("| Step | Champ | Type système | Contrôle système |");
                        sb.AppendLine("|------|-------|--------------|------------------|");
                        foreach (var st in stage.Steps)
                            sb.AppendLine($"| {ParserHelper.Esc(st.StepName)} | `{st.FieldName}` | {st.SystemStepType} | {(st.IsSystemControl ? "Oui" : "")} |");
                        sb.AppendLine();
                    }
                }
            }
        }

        private void AppendWorkflowsSection(StringBuilder sb, EntityDocumentation entity)
        {
            sb.AppendLine("## 5. Workflows & Processes");
            sb.AppendLine();

            if (!entity.Workflows.Any())
            {
                sb.AppendLine("*Aucun workflow/process sur cette entité.*");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("| Nom | Type | Statut | Mode | Trigger | Trigger Fields |");
            sb.AppendLine("|-----|------|--------|------|-------------|--------------------|");

            foreach (var wf in entity.Workflows)
            {
                sb.AppendLine($"| {wf.Name} " +
                    $"| {wf.Type} " +
                    $"| {(wf.IsActive ? "true" : "false")} " +
                    $"| {(wf.IsRealTime ? "Synchrone" : "Asynchrone")} " +
                    $"| {wf.TriggerType} " +
                    $"| {(wf.TriggerFields.Any() ? string.Join(", ", wf.TriggerFields.Select(f => $"`{f}`")) : "-")} |");
            }
            sb.AppendLine();
        }

        private void AppendPluginsSection(StringBuilder sb, EntityDocumentation entity)
        {
            sb.AppendLine("## Plugins");
            sb.AppendLine();

            if (!entity.Plugins.Any())
            {
                sb.AppendLine("_Aucun plugin détecté sur cette entité._");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("| Plugin | Message | Stage | Mode | Rank | Filtering Attribute | Impersonate |");
            sb.AppendLine("|---|---|---|---|---|---|---|");

            foreach (var plugin in entity.Plugins)
            {
                foreach (var step in plugin.Steps)
                {

                    sb.AppendLine($"| {step.GetName()} | {step.GetMessageName()} | {GetStageLabel(step.Stage)} | {GetModeLabel(step.Mode)} | {step.Rank} | {step.FilteringAttributes} | {step.ImpersonatingUserIdName??"-"} |");
                }
            }

            sb.AppendLine();
        }

        private string GetModeLabel(int mode) => mode switch
        {
            0 => "Synchronous",
            1 => "Asynchronous",
            _ => mode.ToString()
        };

        private string GetStageLabel(int stage) => stage switch
        {
            10 => "Pre-Validation",
            20 => "Pre-Operation",
            40 => "Post-Operation",
            _ => stage.ToString()
        };
       
        private void AppendFormsViewsSection(StringBuilder sb, EntityDocumentation entity)
        {
            sb.AppendLine("## 8. Formulaires & Vues");
            sb.AppendLine();

            sb.AppendLine("### Formulaires");
            sb.AppendLine();
            if (entity.Forms.Any())
            {
                foreach (var form in entity.Forms)
                    sb.AppendLine($"- {form}");
            }
            else
            {
                sb.AppendLine("*Aucun formulaire personnalisé.*");
            }
            sb.AppendLine();

            sb.AppendLine("### Vues");
            sb.AppendLine();
            if (entity.Views.Any())
            {
                foreach (var view in entity.Views)
                    sb.AppendLine($"- {view}");
            }
            else
            {
                sb.AppendLine("*Aucune vue personnalisée.*");
            }
            sb.AppendLine();
        }

        public static string CleanMarkdownCell(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            // Convertit les retours à la ligne, espaces multiples et bullets
            var lines = value
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line =>
                {
                    line = line.Trim();

                    // Supprime les bullets en début de ligne
                    line = Regex.Replace(line, @"^(?:[-*•▪◦‣]\s*)+", "");

                    return line.Trim();
                })
                .Where(line => !string.IsNullOrWhiteSpace(line));

            var result = string.Join(" ", lines);

            // Protection du séparateur Markdown de cellule
            result = result.Replace("|", "\\|");

            // Réduction des espaces multiples
            result = Regex.Replace(result, @"\s+", " ");

            return result.Trim();
        }
        #endregion

        #region Mermaid Generation
        /// <summary>
        /// Génère le fichier _EntityDiagram.md avec un diagramme Mermaid ER
        /// représentant toutes les entités documentées et leurs relations.
        /// </summary>
        public string GenerateEntityDiagram(List<EntityDocumentation> entities, List<RelationshipInfo> relationships)
        {
            var sb = new StringBuilder();

            var dedupedRelationships = DeduplicateRelationships(relationships);

            var entitiesWithRelationships = GetEntitiesWithRelationships(entities, dedupedRelationships);

            var visibleEntities = entities
                .Where(e => entitiesWithRelationships.Contains(e.LogicalName))
                .OrderBy(e => e.DisplayName ?? e.LogicalName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            sb.AppendLine("```mermaid");
            sb.AppendLine("erDiagram");

            var lookupAttributeNames = new HashSet<string>(
                dedupedRelationships
                    .Where(r => !string.IsNullOrWhiteSpace(r.ReferencingAttribute))
                    .Select(r => r.ReferencingAttribute),
                StringComparer.OrdinalIgnoreCase);

            foreach (var entity in visibleEntities)
            {
                sb.AppendLine($"    {SanitizeFieldName(entity.LogicalName)} ");
                /*
                sb.AppendLine($"    {SanitizeFieldName(entity.LogicalName)} {{");

                var relevantFields = entity.Fields
                    .Where(f => !IsSystemField(f.LogicalName))
                    .Where(f => IsRelevantForDiagram(f))
                    .ToList();

                foreach (var field in relevantFields)
                {
                    var pkSuffix = field.LogicalName == entity.LogicalName + "id" ? " PK" : "";
                    sb.AppendLine($"        {MapAttributeType(field)} {SanitizeFieldName(field.LogicalName)}{pkSuffix}");
                }

                sb.AppendLine("    }");
                */
            }

            var visibleNames = new HashSet<string>(visibleEntities.Select(e => e.LogicalName), StringComparer.OrdinalIgnoreCase);

            foreach (var rel in dedupedRelationships)
            {
                var (from, to) = GetRelationshipEntities(rel);

                if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                    continue;

                if (!visibleNames.Contains(from) || !visibleNames.Contains(to))
                    continue;

                if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                    continue;

                sb.AppendLine($"    {SanitizeFieldName(from)} {BuildRelationshipLabel(rel)} {SanitizeFieldName(to)} : \"{rel.SchemaName}\"");
            }

            sb.AppendLine("```");
            sb.AppendLine();

            sb.AppendLine("## Récapitulatif des relations");
            sb.AppendLine();
            sb.AppendLine("| Entité 1 | Cardinalité | Entité 2 | Nom du schéma | Type |");
            sb.AppendLine("|---|---|---|---|---|");

            foreach (var rel in dedupedRelationships)
            {
                var (from, to) = GetRelationshipEntities(rel);

                if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                    continue;

                if (!visibleNames.Contains(from) || !visibleNames.Contains(to))
                    continue;

                sb.AppendLine($"| `{from}` | {BuildRelationshipLabel(rel)} | `{to}` | `{rel.SchemaName}` | {rel.RelationshipType} |");
            }

            var filePath = Path.Combine(_outputFolder, "_EntityDiagram.md");
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

            return sb.ToString();
        }

        private static string BuildRelationshipLabel(RelationshipInfo rel)
        {
            if (rel == null)
                return "||--o{";

            var type = rel.RelationshipType?.Trim().ToLowerInvariant();

            switch (type)
            {
                case "onetomany":
                    // Entity1 = côté "1" (référencé), Entity2 = côté "N" (référençant)
                    return "||--o{";

                case "manytoone":
                    // Entity1 = côté "N", Entity2 = côté "1"
                    return "}o--||";

                case "manytomany":
                    return "}o--o{";

                default:
                    // Type inconnu -> relation générique
                    return "||--o{";
            }
        }
        private static string MapAttributeType(FieldInfo field)
        {
            if (field == null || string.IsNullOrWhiteSpace(field.Type))
                return "string";

            return field.Type.Trim().ToLowerInvariant() switch
            {
                "uniqueidentifier" => "guid",
                "lookup" or "customer" or "owner" => "guid",
                "picklist" or "state" or "status" or "optionsetvalue" => "enum",
                "string" or "memo" => "string",
                "integer" or "bigint" => "int",
                "decimal" or "double" or "money" => "decimal",
                "boolean" => "bool",
                "datetime" => "datetime",
                _ => "string"
            };
        }

        private void AppendRelationshipTable(StringBuilder sb, List<RelationshipInfo> relationships, HashSet<string> entityNames)
        {
            sb.AppendLine("## Détail des relations");
            sb.AppendLine();
            sb.AppendLine("| Type | Entité (1) | Entité (N) | Attribut lié | Cascade Delete |");
            sb.AppendLine("|---|---|---|---|---|");

            var relevant = relationships.Where(r =>
                (r.RelationshipType != "ManyToMany" && entityNames.Contains(r.ReferencedEntity ?? "") && entityNames.Contains(r.ReferencingEntity ?? ""))
                || (r.RelationshipType == "ManyToMany" && entityNames.Contains(r.Entity1 ?? "") && entityNames.Contains(r.Entity2 ?? "")));

            foreach (var rel in relevant.OrderBy(r => r.RelationshipType))
            {
                if (rel.RelationshipType == "ManyToMany")
                {
                    sb.AppendLine($"| N:N | `{rel.Entity1}` | `{rel.Entity2}` | *(table de jonction: `{rel.IntersectEntityName}`)* | - |");
                }
                else
                {
                    sb.AppendLine($"| 1:N | `{rel.ReferencedEntity}` | `{rel.ReferencingEntity}` | `{rel.ReferencingAttribute}` | {rel.CascadeDelete} |");
                }
            }

            sb.AppendLine();
        }

        // --- Helpers de sanitization pour Mermaid ---

        private string SanitizeId(string logicalName)
        {
            // Mermaid n'aime pas certains caractères spéciaux dans les identifiants
            return logicalName?.Replace("-", "_") ?? "unknown";
        }

        private string SanitizeFieldName(string fieldName)
        {
            return fieldName?.Replace(" ", "_") ?? "unknown";
        }

        private string SanitizeType(string dynamicsType)
        {
            // Mermaid ER attend un "type" simple sans espace
            return dynamicsType?.ToLower() switch
            {
                "string" or "memo" => "string",
                "integer" or "bigint" => "int",
                "decimal" or "double" or "money" => "decimal",
                "boolean" => "bool",
                "datetime" => "datetime",
                "lookup" or "customer" or "owner" => "guid",
                "picklist" or "state" or "status" => "enum",
                "uniqueidentifier" => "guid",
                _ => "string"
            };
        }

        private static bool IsRelevantForDiagram(FieldInfo field)
        {
            if (field == null)
                return false;

            var mappedType = MapAttributeType(field);
            return mappedType == "guid";
        }

        private static bool IsSystemField(string fieldLogicalName)
        {
            if (string.IsNullOrWhiteSpace(fieldLogicalName))
                return false;

            switch (fieldLogicalName.Trim().ToLowerInvariant())
            {
                case "createdby":
                case "createdonbehalfby":
                case "modifiedby":
                case "modifiedonbehalfby":
                case "owningteam":
                case "organizationid":
                case "ownerid":
                case "teamid":
                    return true;
                default:
                    return false;
            }
        }

        private static HashSet<string> GetEntitiesWithRelationships(List<EntityDocumentation> entities, List<RelationshipInfo> relationships)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rel in relationships)
            {
                var (from, to) = GetRelationshipEntities(rel);

                if (!string.IsNullOrWhiteSpace(from))
                    result.Add(from);

                if (!string.IsNullOrWhiteSpace(to))
                    result.Add(to);
            }

            return result;
        }

        private static (string From, string To) GetRelationshipEntities(RelationshipInfo rel)
        {
            // Cas 1:N et N:1 → basé sur Referencing/Referenced
            if (!string.IsNullOrWhiteSpace(rel.ReferencingEntity) && !string.IsNullOrWhiteSpace(rel.ReferencedEntity))
            {
                return (rel.ReferencingEntity, rel.ReferencedEntity);
            }

            // Cas N:N → basé sur Entity1LogicalName/Entity2LogicalName si disponibles
            if (!string.IsNullOrWhiteSpace(rel.Entity1) && !string.IsNullOrWhiteSpace(rel.Entity2))
            {
                return (rel.Entity1, rel.Entity2);
            }

            // Fallback historique (Entity1/Entity2)
            return (rel.Entity1, rel.Entity2);
        }

        private static List<RelationshipInfo> DeduplicateRelationships(List<RelationshipInfo> relationships)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<RelationshipInfo>();

            foreach (var rel in relationships)
            {
                var key = BuildRelationshipKey(rel);

                if (seen.Add(key))
                {
                    result.Add(rel);
                }
            }

            return result;
        }

        private static string BuildRelationshipKey(RelationshipInfo rel)
        {
            // Clé primaire : le SchemaName est normalement unique par relation dans Dataverse
            if (!string.IsNullOrWhiteSpace(rel.SchemaName))
            {
                return rel.SchemaName.Trim().ToLowerInvariant();
            }

            // Fallback si SchemaName absent : construire une clé à partir des entités/attributs
            var (from, to) = GetRelationshipEntities(rel);
            return $"{from}|{to}|{rel.ReferencingAttribute}|{rel.ReferencedAttribute}|{rel.RelationshipType}"
                .Trim()
                .ToLowerInvariant();
        }
        #endregion
    }
}