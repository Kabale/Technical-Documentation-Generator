// See https://aka.ms/new-console-template for more information
using dynamics.document.generator.Helpers;
using dynamics.document.generator.Markdown;
using dynamics.document.generator.Models;
using dynamics.document.generator.Parsers;
using System.Xml.Linq;

namespace dynamics.document.generator
{
    public class Program
    {
        static void Main(string[] args)
        {
            // === Configuration ===
            string zipPath = @"D:\Temp\Reporting\GlobalAggregatedSolution_1_0_0_0.zip";
            string extractFolder = @"D:\Temp\Reporting\SolutionExtract_Global";
            string docOutputFolder = @"D:\Temp\Reporting\Documentation_Global";
            string solutionDisplayName = "Arendt Customizations";

            /*
            string zipPath = @"D:\Temp\Repport\GlobalAggregatedSolution_1_0_0_0.zip";
            string extractFolder = @"D:\Temp\Repport\SolutionExtract";
            string docOutputFolder = @"D:\Temp\Repport\Documentation_Global";
            string solutionDisplayName = "Arendt Customizations";

            string zipPath = @"D:\Temp\Repport\RiskMatrixCalculation_1_0_0_1.zip";
            string extractFolder = @"D:\Temp\Repport\RiskMatrix_SolutionExtract";
            string docOutputFolder = @"D:\Temp\Repport\Documentation_RiskMatrix";
            string solutionDisplayName = "Arendt Customizations";
            */
            Console.WriteLine("=== Génération de documentation technique Dynamics 365 ===\n");

            try
            {
                // 1. Extraction du zip
                var extractor = new SolutionZipExtractor();
                extractor.Extract(zipPath, extractFolder);

                // TEST VSC
                var solutionParser = new SolutionParser(extractor.SolutionXml, extractor.CustomizationsXml);
                var solution =solutionParser.ParseSolution(); 

                Console.WriteLine($"Solution '{solutionDisplayName}' analysée avec succès :");
                Console.WriteLine($"   - {solution.RootComponents.Count} composant(s) racine");
                Console.WriteLine($"   - {solution.Entities.Count} entité(s)");
                Console.WriteLine($"   - {solution.Workflows.Count} workflow(s)");


                /*
                // 2. Lister TOUTES les entités de la solution
                var entityLister = new SolutionEntityLister(extractor.SolutionXml, extractor.CustomizationsXml);
                var entityLogicalNames = entityLister.GetEntityLogicalNames();

                Console.WriteLine($"{entityLogicalNames.Count} entité(s) trouvée(s) dans la solution :");
                foreach (var name in entityLogicalNames)
                    Console.WriteLine($"   - {name}");
                Console.WriteLine();

                // 3. Initialiser tous les parsers une seule fois
                var entityParser = new EntityParser(extractor.CustomizationsXml);
                var brParser = new BusinessRuleParser(extractor.CustomizationsXml, extractFolder);
                var scriptParser = new FormScriptParser(extractor.CustomizationsXml);
                var bpfParser = new BpfParser(extractor.CustomizationsXml, $"{extractFolder}\\Workflows");
                var wfParser = new WorkflowParser(extractor.CustomizationsXml);
                var pluginParser = new PluginParser(extractor.CustomizationsXml);

                // 4. Parser le détail (champs, etc.) pour toutes les entités connues de customizations.xml
                var allEntitiesDetails = entityParser.ParseEntities();

                var entitiesDocumentation = new List<EntityDocumentation>();

                // 5. Boucler sur CHAQUE entité de la solution
                foreach (var logicalName in entityLogicalNames)
                {
                    Console.WriteLine($"  → Traitement de {logicalName}...");

                    // Récupérer le détail déjà parsé (champs, display name...) s'il existe
                    var entity = allEntitiesDetails
                        .FirstOrDefault(e => e.LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase));

                    if (entity == null)
                    {
                        // Entité présente dans solution.xml mais sans détail dans customizations.xml
                        // (cas d'une entité standard ajoutée sans customisation de champs, par ex.)
                        entity = new EntityDocumentation
                        {
                            LogicalName = logicalName,
                            DisplayName = logicalName,
                            Description = "Entité incluse dans la solution sans customisation de schéma détaillée."
                        };
                        Console.WriteLine($"    ⚠️ Pas de détail de schéma trouvé pour {logicalName} (entité standard incluse sans modif ?)");
                    }

                    // Enrichir avec tous les composants liés
                    entity.BusinessRules = brParser.ParseBusinessRules(entity.LogicalName);
                    entity.FormScripts = scriptParser.ParseFormScripts(entity.LogicalName);
                    entity.BusinessProcessFlows = bpfParser.ParseBpfs(entity.LogicalName);
                    entity.Workflows = wfParser.ParseWorkflows(entity.LogicalName);
                    entity.Plugins = pluginParser.ParsePlugins(entity.LogicalName);

                    entitiesDocumentation.Add(entity);
                }

                Console.WriteLine($"\n{entitiesDocumentation.Count} entité(s) documentée(s).\n");

                // 6. Génération de la documentation Markdown (1 fichier par entité + 1 index global)
                var mdGenerator = new MarkdownGenerator(docOutputFolder);
                mdGenerator.GenerateFullDocumentation(solutionDisplayName, entitiesDocumentation);

                // 7. Génération du diagramme d'entités (ERD) en Markdown
                var relationshipParser = new RelationshipParser(extractor.CustomizationsXml);
                var allRelationships = relationshipParser.ParseAllRelationships();


                mdGenerator.GenerateEntityDiagram(entitiesDocumentation, allRelationships);

                // take snapshot 
                //https://vmd-dyn-01.dev.as.local/AS360/main.aspx?app=d365default&forceUCI=1&pagetype=entityrecord&etn=account&id=91eed1eb-ff45-ec11-aecf-000c297958dd
                //https://vmd-dyn-01.dev.as.local/AS360/Tools/ProcessControl/UnifiedProcessDesigner.aspx?id=%7b9774572E-7301-421F-A367-39037F56EF2F%7d
                Console.WriteLine($"\n✅ Documentation générée avec succès dans : {docOutputFolder}");
                */
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Erreur : {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nAppuyez sur une touche pour fermer...");
            Console.ReadKey();
        }

        private static void DebugDumpStructure(XDocument customizations, string entityLogicalName)
        {
            var entityNode = customizations.Descendants("Entity")
                .FirstOrDefault(e => e.Element("Name")?.Value == entityLogicalName);

            Console.WriteLine("=== STRUCTURE ENTITY ===");
            Console.WriteLine(entityNode?.ToString().Substring(0, Math.Min(2000, entityNode.ToString().Length)));

            var wfNode = customizations.Descendants("Workflow").FirstOrDefault();
            Console.WriteLine("\n=== STRUCTURE WORKFLOW (premier trouvé) ===");
            Console.WriteLine(wfNode?.ToString().Substring(0, Math.Min(1500, wfNode.ToString().Length)));
        }
    }
}