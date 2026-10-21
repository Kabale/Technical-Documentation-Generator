using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models.Parser
{
    public class RoleComponent
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public bool isCustom { get; set; }
        public List<RolePrivilege> RolePrivileges { get; set; } = new List<RolePrivilege>();
    }
}
