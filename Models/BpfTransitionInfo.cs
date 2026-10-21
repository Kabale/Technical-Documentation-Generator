using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class BpfTransitionInfo
    {
        public string BranchName { get; set; }
        public bool IsElse { get; set; }
        public string Condition { get; set; }        // texte lisible, ex. "ainos_asincharge Equal false"
        public string TargetStageId { get; set; }
        public string TargetStageName { get; set; }
    }
}
