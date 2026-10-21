using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models.Parser
{
    public class WorkflowComponent
    {
        public string WorkflowId { get; set; }
        public string Name { get; set; }
        public string XamlFileName { get; set; }
        public EnumWorkflowType WorkflowType { get; set; }
        public EnumWorkflowCategory WorkflowCategory { get; set; }
        public EnumWorkflowMode WorkflowMode { get; set; }
        public EnumWorkflowScope WorkflowScope { get; set; }
    }
}
