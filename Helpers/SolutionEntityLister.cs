using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace dynamics.document.generator.Helpers
{
    public class SolutionEntityLister
    {
        private readonly XDocument _solutionXml;
        private readonly XDocument _customizationsXml;

        public SolutionEntityLister(XDocument solutionXml, XDocument customizationsXml)
        {
            _solutionXml = solutionXml;
            _customizationsXml = customizationsXml;
        }

        /// <summary>
        /// Retourne la liste des logical names d'entités présentes dans la solution.
        /// Combine solution.xml (RootComponents, type=1) et customizations.xml (Entities).
        /// </summary>
        public List<string> GetEntityLogicalNames()
        {
            var namesFromCustomizations = _customizationsXml
                .Descendants("Entity")
                .Select(e => e.Element("Name")?.Value)
                .Where(n => !string.IsNullOrEmpty(n));

            // RootComponents avec type="1" = Entity dans solution.xml
            // Le schemaName y est stocké, généralement identique au logical name mais avec casse d'origine
            var namesFromSolution = _solutionXml
                .Descendants("RootComponent")
                .Where(rc => rc.Attribute("type")?.Value == "1")
                .Select(rc => rc.Attribute("schemaName")?.Value)
                .Where(n => !string.IsNullOrEmpty(n));

            var allNames = namesFromCustomizations
                .Concat(namesFromSolution)
                .Select(n => n.ToLowerInvariant())
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            return allNames;
        }
    }
}
