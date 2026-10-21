using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class BpfStepInfo
    {
        public string StepName { get; set; }     // ControlDisplayName
        public string FieldName { get; set; }     // DataFieldName
        public string ControlId { get; set; }
        public bool IsSystemControl { get; set; }
        public string SystemStepType { get; set; }
        public bool IsRequired { get; set; }
    }
}
