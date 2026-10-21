using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class EntityDocumentation
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<FieldInfo> Fields { get; set; } = new();
        public List<BusinessRuleInfo> BusinessRules { get; set; } = new();
        public List<FormScriptInfo> FormScripts { get; set; } = new();
        public List<BpfInfo> BusinessProcessFlows { get; set; } = new();
        public List<WorkflowInfo> Workflows { get; set; } = new();
        public List<PluginInfo> Plugins { get; set; } = new();
        public List<PluginStepInfo> PluginSteps { get; set; } = new();
        public List<string> Forms { get; set; } = new();
        public List<string> Views { get; set; } = new();
        public List<RelationshipInfo> Relationships { get; set; } = new();
    }
}
