using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class PluginInfo
    {
        public string AssemblyName { get; set; }
        public string AssemblyId { get; set; }
        public string Version { get; set; }
        public string Culture { get; set; }
        public string PublicKeyToken { get; set; }

        public string TypeName { get; set; }
        public string TypeId { get; set; }

        public List<PluginStepInfo> Steps { get; set; } = new();
    }

}
