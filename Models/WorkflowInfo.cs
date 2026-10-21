using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class WorkflowInfo
    {
        public string Name { get; set; }
        public string Type { get; set; } // Workflow / Action / BusinessRule
        public string TriggerType { get; set; } // OnCreate, OnUpdate, OnDelete, Scheduled
        public bool IsRealTime { get; set; }
        public bool IsActive { get; set; }
        public string PrimaryEntity { get; set; }
        public List<string> TriggerFields { get; set; } = new();
        public string Description { get; set; }
    }
}
