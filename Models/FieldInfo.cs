using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class FieldInfo
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public bool IsCustom { get; set; }
        public string Description { get; set; }
        public string RequiredLevel { get; set; }
        public int? MaxLength { get; set; }
        public List<string> OptionSetValues { get; set; } = new(); // pour picklists
    }
}
