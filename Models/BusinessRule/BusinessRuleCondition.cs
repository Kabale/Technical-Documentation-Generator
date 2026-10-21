using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class BusinessRuleCondition 
    {
        public string Expression { get; set; } = "";
        public string Branch { get; set; } = "";
        public override string ToString() => Expression;
    }
}
