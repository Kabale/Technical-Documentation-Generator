using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class BusinessRuleAction
    {
        public string Type { get; set; } = "";
        public string Field { get; set; } = "";
        public string Value { get; set; } = "";
        public string Entity { get; set; } = "";
        public string Branch { get; set; } = "";
        public Dictionary<string, string> RawProperties { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public override string ToString() => $"{Type}: {Field} = {Value}";
    }
}
