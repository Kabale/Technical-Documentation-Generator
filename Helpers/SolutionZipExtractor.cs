using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace dynamics.document.generator.Helpers
{
    public class SolutionZipExtractor
    {
        public string ExtractedPath { get; private set; }
        public XDocument CustomizationsXml { get; private set; }
        public XDocument SolutionXml { get; private set; }

        public void Extract(string zipPath, string outputFolder)
        {
            ExtractedPath = outputFolder;

            if (Directory.Exists(outputFolder))
                Directory.Delete(outputFolder, true);

            ZipFile.ExtractToDirectory(zipPath, outputFolder);

            var customizationsPath = Path.Combine(outputFolder, "customizations.xml");
            var solutionPath = Path.Combine(outputFolder, "solution.xml");

            CustomizationsXml = XDocument.Load(customizationsPath);
            SolutionXml = XDocument.Load(solutionPath);

            Console.WriteLine($"Solution extraite dans : {outputFolder}");
        }

        public string GetWebResourceContent(string webResourceFileName)
        {
            var wrPath = Path.Combine(ExtractedPath, "WebResources", webResourceFileName);
            return File.Exists(wrPath) ? File.ReadAllText(wrPath) : null;
        }
    }
}
