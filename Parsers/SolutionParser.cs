using dynamics.document.generator.Models.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace dynamics.document.generator.Parsers
{
    public class SolutionParser
    {
        private readonly XDocument _solutionXml;
        private readonly XDocument _customizationsXml;

        public SolutionParser(XDocument solutionXml, XDocument customizationsXml)
        {
            _solutionXml = solutionXml;
            _customizationsXml = customizationsXml;
        }
        public Solution ParseSolution() 
        { 
            Solution solution = new Solution();
            solution.RootComponents = ParseRootComponents();
            return solution;
        }

        private List<Models.Parser.RootComponent> ParseRootComponents()
        {
            var rootComponents = _solutionXml
                .Descendants("RootComponent")
                .Select(rc => new Models.Parser.RootComponent
                {
                    SchemaName = rc.Attribute("schemaName")?.Value,
                    ComponentType = (Models.Parser.EnumComponentType)Enum.Parse(typeof(Models.Parser.EnumComponentType), rc.Attribute("type")?.Value ?? "0"),
                    ComponentBehavior = (Models.Parser.EnumRootComponentBehavior)Enum.Parse(typeof(Models.Parser.EnumRootComponentBehavior), rc.Attribute("behavior")?.Value ?? "0"),
                    Id = rc.Attribute("id")?.Value,
                    ParentId = rc.Attribute("parentId")?.Value
                })
                .ToList();
            return rootComponents;
        }

    }
}
