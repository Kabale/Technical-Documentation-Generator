using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models.Parser
{
    public class RootComponent
    {
        public string SchemaName { get; set; }
        public EnumComponentType ComponentType { get; set; }
        public EnumRootComponentBehavior ComponentBehavior { get; set; }
        public string Id { get; set; }
        public string ParentId { get; set; }
    }
}
