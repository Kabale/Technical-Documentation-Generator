using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class BusinessRuleDetailedInfo
    {
        public string RawXaml { get; set; } = "";
        public List<string> Entities { get; } = new List<string>();
        public List<string> Reads { get; } = new List<string>();
        public List<string> ConditionFields { get; } = new List<string>();
        public List<string> ActionFields { get; } = new List<string>();
        public List<BusinessRuleCondition> Conditions { get; } = new List<BusinessRuleCondition>();
        public List<BusinessRuleAction> Actions { get; } = new List<BusinessRuleAction>();
        public List<BusinessRuleBranch> Branches { get; } = new List<BusinessRuleBranch>();
        public List<UnknownAction> UnknownActions { get; } = new List<UnknownAction>();
    }
}
