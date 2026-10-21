using dynamics.document.generator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace dynamics.document.generator.Helpers
{
    public class ParserHelper
    {
        public static string GetWorkflowDisplayName(XElement workflowNode)
        {
            // Le nom est directement dans l'attribut Name du Workflow
            var name = workflowNode.Attribute("Name")?.Value;
            if (!string.IsNullOrEmpty(name))
                return name;

            // Fallback : LocalizedNames si jamais Name est vide
            return workflowNode
                .Element("LocalizedNames")?
                .Elements("LocalizedName")
                .FirstOrDefault(ln => ln.Attribute("languagecode")?.Value == "1033")
                ?.Attribute("description")?.Value
                ?? "(Nom inconnu)";
        }

        public static void DebugXamlStructure(XDocument xamlDoc)
        {
            Console.WriteLine("=== Namespaces déclarés ===");
            foreach (var attr in xamlDoc.Root.Attributes())
            {
                if (attr.IsNamespaceDeclaration)
                    Console.WriteLine($"  {attr.Name.LocalName} => {attr.Value}");
            }

            Console.WriteLine("=== Tous les éléments distincts (Name local) ===");
            var distinctElements = xamlDoc.Descendants()
                .Select(e => e.Name.LocalName)
                .Distinct()
                .OrderBy(x => x);

            foreach (var name in distinctElements)
                Console.WriteLine($"  {name}");
        }

        public static string ShortId(BpfStageInfo s) => (s.StageId ?? "").Split('-').FirstOrDefault();
        public static string Esc(string s) => (s ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        public static string NormGuid(string g) => string.IsNullOrWhiteSpace(g) ? null : g.Trim('{', '}', ' ').ToLowerInvariant();
    }
}
