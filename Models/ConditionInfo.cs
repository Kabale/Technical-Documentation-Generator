using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public sealed class ConditionInfo
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public string Branch { get; set; }
        public override string ToString() => $"{Field} {Operator} {Value} [{Branch}]";
    }
}
