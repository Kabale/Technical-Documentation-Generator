using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models.Parser
{
    public enum EnumWorkflowScope
    {
        User = 1,
        BusinessUnit = 2,
        BusinessUnitAndChildBusinessUnits = 3,
        Organization = 4
    }
}
