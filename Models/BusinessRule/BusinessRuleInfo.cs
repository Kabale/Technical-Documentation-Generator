using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public sealed class BusinessRuleInfo
    {
        public string Name { get; set; } = "";
        public string UniqueId { get; set; } = "";
        public bool IsActive { get; set; }
        public string Scope { get; set; } = "";
        public string EntityName { get; set; } = "";
        public string RawXaml { get; set; } = "";
        public BusinessRuleDetailedInfo Detail { get; set; } = new BusinessRuleDetailedInfo();
        public List<string> ConditionFields => Detail.ConditionFields;
        public List<string> ActionFields => Detail.ActionFields;
        public List<string> ActionsSummary => Detail.Actions.Select(a => a.ToString()).ToList();
    }
}
