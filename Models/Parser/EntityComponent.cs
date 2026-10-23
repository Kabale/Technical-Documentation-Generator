using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models.Parser
{
    public class EntityComponent
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<EntityAttributeComponent> fields { get; set; } = new List<EntityAttributeComponent>();
        public List<EntityFormComponent> forms { get; set; } = new List<EntityFormComponent>();
    }
}
