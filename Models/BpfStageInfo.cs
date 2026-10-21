using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class BpfStageInfo
    {
        public string StageId { get; set; }
        public string StageName { get; set; }
        public string EntityName { get; set; }
        public string StageCategory { get; set; } // 0=Qualify,1=Develop,2=Propose,3=Close
        public string NextStageId { get; set; }
        public string NextStageName { get; set; }
        public List<BpfStepInfo> Steps { get; set; } = new();
        public List<BpfTransitionInfo> Transitions { get; set; } = new();

    }

}
