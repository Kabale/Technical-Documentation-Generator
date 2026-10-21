using dynamics.document.generator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace dynamics.document.generator.Parsers
{
    public class RelationshipParser
    {
        private readonly XDocument _customizations;

        public RelationshipParser(XDocument customizations)
        {
            _customizations = customizations;
        }

        public List<RelationshipInfo> ParseAllRelationships()
        {
            var results = new List<RelationshipInfo>();

            var relNodes = _customizations.Descendants("EntityRelationship");

            foreach (var rel in relNodes)
            {
                var type = rel.Element("EntityRelationshipType")?.Value;

                if (type == "OneToMany" || type == "ManyToOne")
                {
                    results.Add(new RelationshipInfo
                    {
                        RelationshipType = type,
                        SchemaName = rel.Element("SchemaName")?.Value,
                        ReferencedEntity = rel.Element("ReferencedEntityName")?.Value,
                        ReferencingEntity = rel.Element("ReferencingEntityName")?.Value,
                        ReferencedAttribute = rel.Element("ReferencedAttributeName")?.Value,
                        ReferencingAttribute = rel.Element("ReferencingAttributeName")?.Value,
                        CascadeDelete = rel.Element("CascadeDelete")?.Value
                    });
                }
                else if (type == "ManyToMany")
                {
                    results.Add(new RelationshipInfo
                    {
                        RelationshipType = type,
                        SchemaName = rel.Element("SchemaName")?.Value,
                        Entity1 = rel.Element("Entity1LogicalName")?.Value,
                        Entity2 = rel.Element("Entity2LogicalName")?.Value,
                        IntersectEntityName = rel.Element("IntersectEntityName")?.Value
                    });
                }
            }

            return results;
        }

        // Filtre les relations impliquant au moins une entité de la liste fournie
        public List<RelationshipInfo> FilterByEntities(List<RelationshipInfo> all, IEnumerable<string> entityLogicalNames)
        {
            var set = new HashSet<string>(entityLogicalNames, StringComparer.OrdinalIgnoreCase);

            return all.Where(r =>
                set.Contains(r.ReferencedEntity ?? "") ||
                set.Contains(r.ReferencingEntity ?? "") ||
                set.Contains(r.Entity1 ?? "") ||
                set.Contains(r.Entity2 ?? "")
            ).ToList();
        }
    }
}
