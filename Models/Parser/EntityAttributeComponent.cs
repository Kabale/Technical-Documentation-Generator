using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models.Parser
{
    public class EntityAttributeComponent
    {
        public string LogicalName { get; set; }
        public string DisplayName {  get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string RequiredLevel { get; set; }
        public bool IsSecured { get; set; }
        public bool IsAuditEnbaled { get; set; }
        public bool IsCustomField { get; set; }
        public int MaxLength { get; set; }
        public OptionSetComponent OptionSet {  get; set; }
    }
}
