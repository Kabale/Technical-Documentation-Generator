using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class BpfInfo
    {
        public string? ProcessId { get; set; }
        public string? XamlFile { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public string? XamlHint { get; set; }
        public string Name { get; set; }
        public string UniqueId { get; set; }
        public string PrimaryEntity { get; set; }
        public bool IsActive { get; set; }
        public List<BpfStageInfo> Stages { get; set; } = new();


        public List<string> RelatedEntities =>
            Stages.Select(s => s.EntityName)
                  .Where(e => !string.IsNullOrWhiteSpace(e))
                  .Distinct(StringComparer.OrdinalIgnoreCase)
                  .ToList();
    }

}
