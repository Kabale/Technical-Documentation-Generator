using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class BpfConditionInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Field { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public string NodeType { get; set; }
        public string Label { get; set; }
        public string TargetStageId { get; set; }
        public string TargetStageName { get; set; }
        public List<BpfConditionClause> Clauses { get; set; } = new();
        public string RawXml { get; set; }
    }
}
