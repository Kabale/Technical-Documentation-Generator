using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class UnknownAction
    {
        public string ActivityName { get; set; } = "";
        public string Branch { get; set; } = "";
        public Dictionary<string, string> RawProperties { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
