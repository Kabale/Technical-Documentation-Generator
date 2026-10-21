using dynamics.document.generator.Models;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace dynamics.document.generator.Parsers
{
    public class EntityParser
    {
        private readonly XDocument _customizations;

        public EntityParser(XDocument customizations)
        {
            _customizations = customizations;
        }

        public List<EntityDocumentation> ParseEntities()
        {
            var entities = new List<EntityDocumentation>();

            var entityNodes = _customizations.Descendants("Entity");

            foreach (var entityNode in entityNodes)
            {
                var logicalName = entityNode.Element("Name")?.Value;
                if (string.IsNullOrEmpty(logicalName)) continue;

                var displayName = entityNode
                    .Element("EntityInfo")?
                    .Element("entity")?
                    .Element("displaynames")?
                    .Elements("displayname")
                    .FirstOrDefault(dn => dn.Attribute("languagecode")?.Value == "1036")
                    ?.Attribute("description")?.Value ?? logicalName;

                var entity = new EntityDocumentation
                {
                    LogicalName = logicalName,
                    DisplayName = displayName,
                    Fields = ParseFields(entityNode)   // ✅ chemin corrigé
                };

                entities.Add(entity);
            }

            return entities;
        }

        private string GetDisplayName(XElement entityNode)
        {
            var localizedLabel = entityNode
                .Descendants("EntityInfo")
                .Descendants("entity")
                .Descendants("LocalizedNames")
                .Descendants("LocalizedName")
                .FirstOrDefault(x => x.Attribute("languagecode")?.Value == "1036"
                                   || x.Attribute("languagecode")?.Value == "1033");

            return localizedLabel?.Attribute("description")?.Value ?? entityNode.Element("Name")?.Value;
        }

        private string GetDescription(XElement entityNode)
        {
            var desc = entityNode.Descendants("Descriptions")
                .Descendants("Description")
                .FirstOrDefault(x => x.Attribute("languagecode")?.Value == "1036"
                                   || x.Attribute("languagecode")?.Value == "1033");

            return desc?.Attribute("description")?.Value ?? "";
        }

        private List<FieldInfo> ParseFields(XElement entityNode)
        {
            var fields = new List<FieldInfo>();
            var entityName = entityNode.Element("Name")?.Value ?? "?";

            var attributeNodes = entityNode
                .Element("EntityInfo")?
                .Element("entity")?
                .Element("attributes")?
                .Elements("attribute")
                .ToList();

            if (attributeNodes == null || !attributeNodes.Any())
            {
                Console.WriteLine($"    ⚠️ [{entityName}] Aucun attribut trouvé");
                return fields;
            }

            foreach (var attr in attributeNodes)
            {
                var logicalName = attr.Element("LogicalName")?.Value;
                var physicalName = attr.Attribute("PhysicalName")?.Value;

                if (string.IsNullOrEmpty(logicalName))
                {
                    Console.WriteLine($"    ⚠️ [{entityName}] Attribut sans LogicalName trouvé (PhysicalName={physicalName})");
                }

                var displayName = attr
                    .Element("displaynames")?
                    .Elements("displayname")
                    .FirstOrDefault(dn => dn.Attribute("languagecode")?.Value == "1033")
                    ?.Attribute("description")?.Value;

                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = attr
                        .Element("displaynames")?
                        .Elements("displayname")
                        .FirstOrDefault()
                        ?.Attribute("description")?.Value;
                }

                var type = attr.Element("Type")?.Value;
                var requiredLevel = attr.Element("RequiredLevel")?.Value;

                // Log de debug pour comprendre pourquoi une ligne serait vide
                if (string.IsNullOrEmpty(logicalName) && string.IsNullOrEmpty(displayName) && string.IsNullOrEmpty(type))
                {
                    Console.WriteLine($"    ❌ [{entityName}] Attribut totalement vide détecté : {attr}");
                    continue; // on skip cette ligne plutôt que de générer une ligne vide dans le markdown
                }

                fields.Add(new FieldInfo
                {
                    LogicalName = logicalName ?? physicalName ?? "(inconnu)",
                    DisplayName = string.IsNullOrEmpty(displayName) ? (logicalName ?? physicalName) : displayName,
                    Type = type ?? "(inconnu)",
                    IsCustom = bool.TryParse(attr.Element("IsCustomField")?.Value, out var isCustom),
                    RequiredLevel = requiredLevel ?? "none",
                    MaxLength = int.TryParse(attr.Element("MaxLength")?.Value, out var ml) ? ml : (int?)null,
                    Description = GetDescription(attr)
                });
            }

            return fields;
        }

        private List<string> ParseFormNames(XElement entityNode)
        {
            return entityNode.Descendants("systemform")
                .Select(f => f.Element("name")?.Value)
                .Where(n => n != null)
                .ToList();
        }

        private List<string> ParseViewNames(XElement entityNode)
        {
            return entityNode.Descendants("savedquery")
                .Select(v => v.Element("name")?.Value)
                .Where(n => n != null)
                .ToList();
        }

        private List<RelationshipInfo> ParseRelationships(XElement entityNode)
        {
            var relationships = new List<RelationshipInfo>();
            var entityRelationships = entityNode.Parent?.Descendants("EntityRelationship")
                .Where(r => r.Element("EntityName1")?.Value == entityNode.Element("Name")?.Value
                         || r.Element("EntityName2")?.Value == entityNode.Element("Name")?.Value);

            if (entityRelationships != null)
            {
                foreach (var rel in entityRelationships)
                {
                    relationships.Add(new RelationshipInfo
                    {
                        SchemaName = rel.Element("SchemaName")?.Value ?? rel.Attribute("Name")?.Value,
                        RelatedEntity = rel.Element("EntityName1")?.Value == entityNode.Element("Name")?.Value
                            ? rel.Element("EntityName2")?.Value
                            : rel.Element("EntityName1")?.Value,
                        RelationshipType = rel.Element("EntityRelationshipType")?.Value ?? "N/A"
                    });
                }
            }

            return relationships;
        }
    }
}