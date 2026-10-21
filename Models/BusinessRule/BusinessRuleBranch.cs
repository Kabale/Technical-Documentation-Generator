using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class BusinessRuleBranch
    {
        public string Name { get; set; } = "";
        public string Condition { get; set; } = "";
        public string BranchKind { get; set; } = "IF";
        public List<BusinessRuleAction> Actions { get; } = new List<BusinessRuleAction>();
    }
}
