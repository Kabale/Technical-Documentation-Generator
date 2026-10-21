using dynamics.document.generator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace dynamics.document.generator.Parsers
{
    public class PluginParser
    {
        private readonly XDocument _customizations;

        public PluginParser(XDocument customizations)
        {
            _customizations = customizations ?? throw new ArgumentNullException(nameof(customizations));
        }

        public List<PluginInfo> ParsePlugins(string entityLogicalName)
        {
            if (string.IsNullOrWhiteSpace(entityLogicalName))
                throw new ArgumentException(
                    "Le nom logique de l'entité est obligatoire.",
                    nameof(entityLogicalName));

            var result = new List<PluginInfo>();

            var steps = _customizations
                .Descendants()
                .Where(x => IsElement(x, "SdkMessageProcessingStep"))
                .Where(x => IsEntityStep(x, entityLogicalName))
                .ToList();

            foreach (var step in steps)
            {
                var pluginType = FindPluginType(step);

                var pluginAssembly = pluginType == null
                    ? null
                    : FindPluginAssembly(pluginType);

                var plugin = result.FirstOrDefault(x =>
                    string.Equals(
                        x.AssemblyId,
                        GetValue(pluginAssembly, "PluginAssemblyId"),
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    string.Equals(
                        x.TypeId,
                        GetValue(pluginType, "PluginTypeId"),
                        StringComparison.OrdinalIgnoreCase));

                if (plugin == null)
                {
                    plugin = new PluginInfo
                    {
                        AssemblyId = GetValue(pluginAssembly, "PluginAssemblyId"),
                        AssemblyName = GetValue(pluginAssembly, "FullName"),
                        Version = GetValue(pluginAssembly, "Version"),
                        Culture = GetValue(pluginAssembly, "Culture"),
                        PublicKeyToken = GetValue(pluginAssembly, "PublicKeyToken"),

                        TypeId = GetValue(pluginType, "PluginTypeId"),
                        TypeName = GetValue(pluginType, "Name")
                    };

                    result.Add(plugin);
                }

                plugin.Steps.Add(new PluginStepInfo
                {
                    Name = GetValue(step, "Name"),
                    StepId = GetValue(step, "SdkMessageProcessingStepId"),
                    PrimaryEntity = GetValue(step, "PrimaryEntity"),
                    SecondaryEntity = GetValue(step, "SecondaryEntity"),
                    Stage = Convert.ToInt32(GetValue(step, "Stage")),
                    Mode = Convert.ToInt32(GetValue(step, "Mode")),
                    Rank = Convert.ToInt32(GetValue(step, "Rank")),
                    FilteringAttributes = GetValue(step, "FilteringAttributes"),
                    ImpersonatingUserIdName = GetValue(step, "ImpersonatingUserIdName")
                });
            }

            return result;
        }

        private bool IsEntityStep(XElement step, string entityLogicalName)
        {
            var primaryEntity = GetValue(step, "PrimaryEntity");

            return string.Equals(
                       primaryEntity,
                       entityLogicalName,
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   string.Equals(
                       primaryEntity,
                       "none",
                       StringComparison.OrdinalIgnoreCase);
        }

        private XElement FindPluginType(XElement step)
        {
            var pluginTypeId = GetValue(step, "PluginTypeId");

            return _customizations
                .Descendants()
                .FirstOrDefault(x =>
                    IsElement(x, "PluginType") &&
                    string.Equals(
                        GetValue(x, "PluginTypeId"),
                        pluginTypeId,
                        StringComparison.OrdinalIgnoreCase));
        }

        private XElement FindPluginAssembly(XElement pluginType)
        {
            var assemblyId = GetValue(pluginType, "PluginAssemblyId");

            return _customizations
                .Descendants()
                .FirstOrDefault(x =>
                    IsElement(x, "PluginAssembly") &&
                    string.Equals(
                        GetValue(x, "PluginAssemblyId"),
                        assemblyId,
                        StringComparison.OrdinalIgnoreCase));
        }

        private static string GetRelatedValue(
            XElement element,
            string relationName,
            string valueName)
        {
            var relation = element?
                .Descendants()
                .FirstOrDefault(x => IsElement(x, relationName));

            return GetValue(relation, valueName);
        }

        private static string GetValue(XElement element, string name)
        {
            if (element == null)
                return null;

            var child = element
                .Elements()
                .FirstOrDefault(x =>
                    x.Name.LocalName.Equals(
                        name,
                        StringComparison.OrdinalIgnoreCase));

            if (child != null)
                return child.Value.Trim();

            return element
                .Attributes()
                .FirstOrDefault(x =>
                    x.Name.LocalName.Equals(
                        name,
                        StringComparison.OrdinalIgnoreCase))
                ?.Value
                ?.Trim();
        }

        private static bool IsElement(XElement element, string name)
        {
            return element.Name.LocalName.Equals(
                name,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
