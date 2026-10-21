using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class RelationshipInfo
    {
        public string SchemaName { get; set; }
        public string RelatedEntity { get; set; }
        public string Entity1 { get; set; }
        public string Entity2 { get; set; }
        public string RelationshipType { get; set; } // OneToMany, ManyToOne, ManyToMany
        public string ReferencedEntity { get; set; }  // côté "1"
        public string ReferencingEntity { get; set; } // côté "N"
        public string ReferencedAttribute { get; set; }
        public string ReferencingAttribute { get; set; }
        public string CascadeDelete { get; set; }

        // Pour N:N
        public string IntersectEntityName { get; set; }
        public string Entity1LogicalName { get; set; }
        public string Entity2LogicalName { get; set; }
    }
}
