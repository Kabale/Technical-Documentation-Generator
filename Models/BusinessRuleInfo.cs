using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public sealed class BusinessRuleInfo
    {
        public string Name { get; set; }
        public string UniqueId { get; set; }
        public bool IsActive { get; set; }
        public string Scope { get; set; } // Entity / Form specifique
        public string RawXaml {get;set;}
        public List<string> ActionsSummary { get; } = new List<string>();
        public List<ConditionInfo> Conditions { get; } = new List<ConditionInfo>();
        public List<string> ConditionFields { get; } = new List<string>();
        public List<ActionInfo> Actions { get; } = new List<ActionInfo>();
        public List<string> ActionFields { get; } = new List<string>();
    }
}
