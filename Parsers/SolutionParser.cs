using dynamics.document.generator.Models;
using dynamics.document.generator.Models.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace dynamics.document.generator.Parsers
{
    public class SolutionParser
    {
        private readonly XDocument _solutionXml;
        private readonly XDocument _customizationsXml;
        private readonly int _language;

        public SolutionParser(XDocument solutionXml, XDocument customizationsXml)
        {
            _language = 1033;
            _solutionXml = solutionXml;
            _customizationsXml = customizationsXml;
        }
        public Solution ParseSolution() 
        { 
            Solution solution = new Solution();
            solution.RootComponents = ParseRootComponents();
            solution.Entities = ParseEntityComponents(solution.RootComponents);
            return solution;
        }



        private List<Models.Parser.RootComponent> ParseRootComponents()
        {
            var rootComponents = _solutionXml
                .Descendants("RootComponent")
                .Select(rc => new RootComponent
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

        private List<EntityComponent> ParseEntityComponents(List<RootComponent> rootComponents)
        {
            var listEntities = new List<EntityComponent>();

            var entityNodes = _customizationsXml.Descendants("Entity");

            foreach (var entityNode in entityNodes)
            {
                var logicalName = entityNode.Element("Name")?.Value?.ToLower();
                var displayName = entityNode.Element("Name")?.Attribute("LocalizedName")?.Value;
                var description = entityNode.Element("EntityInfo")?.Element("entity")?.Elements("Descriptions")?.FirstOrDefault(dn => dn.Element("Description")?.Attribute("languagecode")?.Value == $"{_language}")?.Element("Description")?.Attribute("description")?.Value;
                description = (description != null ? description : entityNode.Element("EntityInfo")?.Element("entity")?.Elements("Descriptions")?.FirstOrDefault()?.Element("Description")?.Attribute("description")?.Value);

                EntityComponent entity = new EntityComponent()
                {
                    LogicalName = logicalName,
                    DisplayName = displayName,
                    Description = description
                };

                var attributes = entityNode?.Descendants("attribute");
                entity.fields = ParseEntityAttributeComponent(attributes);

                var forms = entityNode?.Descendants("forms");
                entity.forms = ParseEntityFormComponent(forms);
                
                listEntities.Add(entity);
            }

            return listEntities;
        }

        private List<EntityAttributeComponent> ParseEntityAttributeComponent(IEnumerable<XElement>? attributes)
        {
            List<EntityAttributeComponent> fields = new List<EntityAttributeComponent>();
            foreach (var attribute in attributes)
            {
                if (attribute.Element("LogicalName") != null)
                {
                    int maxLenght = 0;
                    string logicalName = attribute.Element("LogicalName")?.Value;
                    string type = attribute.Element("Type")?.Value;
                    string displayName = attribute.Elements("displaynames")?.FirstOrDefault(dn => dn.Element("displayname")?.Attribute("languagecode")?.Value == $"{_language}")?.Attribute("description")?.Value != null ? attribute.Elements("displaynames")?.FirstOrDefault(dn => dn.Element("displayname")?.Attribute("languagecode")?.Value == $"{_language}")?.Element("displayname")?.Attribute("description")?.Value : attribute.Elements("displaynames")?.FirstOrDefault()?.Element("displayname")?.Attribute("description")?.Value;
                    string description = attribute.Elements("Descriptions")?.FirstOrDefault(dn => dn.Element("Description")?.Attribute("languagecode")?.Value == $"{_language}")?.Attribute("Description")?.Value != null ? attribute.Elements("Descriptions")?.FirstOrDefault(dn => dn.Element("Description")?.Attribute("languagecode")?.Value == $"{_language}")?.Element("Description")?.Attribute("description")?.Value : attribute.Elements("Descriptions")?.FirstOrDefault()?.Element("Description")?.Attribute("description")?.Value;
                    bool isAuditEnbaled = attribute.Element("IsAuditEnabled")?.Value == "1" ? true : false;
                    bool isSecured = attribute.Element("IsSecured")?.Value == "1" ? true : false;
                    bool isCustomField = attribute.Element("IsCustomField")?.Value == "1" ? true : false;
                    int.TryParse(attribute.Element("MaxLength")?.Value, out maxLenght);

                    EntityAttributeComponent entityAttributeComponent = new EntityAttributeComponent()
                    {
                        LogicalName = logicalName,
                        Type = type,
                        DisplayName = displayName,
                        Description = description,
                        IsAuditEnbaled = isAuditEnbaled,
                        IsSecured = isSecured,
                        MaxLength = maxLenght,
                        IsCustomField = isCustomField
                    };

                    fields.Add(entityAttributeComponent);
                }
            }
            return fields;
        }

        private List<EntityFormComponent> ParseEntityFormComponent(IEnumerable<XElement>? forms)
        {
            List<EntityFormComponent> listFoms = new List<EntityFormComponent>();
            foreach (var form in forms)
            {
                if (form.Attribute("type") != null)
                {
                    var systemForm = form.Element("systemform");
                    string? formId = CleanID(systemForm.Element("formid")?.Value);
                    string? name = systemForm.Elements("LocalizedNames")?.FirstOrDefault(dn => dn.Element("LocalizedName")?.Attribute("languagecode")?.Value == $"{_language}")?.Attribute("LocalizedName")?.Value != null ? systemForm.Elements("LocalizedNames")?.FirstOrDefault(dn => dn.Element("LocalizedName")?.Attribute("languagecode")?.Value == $"{_language}")?.Element("LocalizedName")?.Attribute("description")?.Value : systemForm.Elements("LocalizedNames")?.FirstOrDefault()?.Element("LocalizedName")?.Attribute("description")?.Value;
                    string? type = form.Attribute("type")?.Value;
                    string? description = systemForm.Elements("Descriptions")?.FirstOrDefault(dn => dn.Element("Description")?.Attribute("languagecode")?.Value == $"{_language}")?.Attribute("Description")?.Value != null ? systemForm.Elements("Descriptions")?.FirstOrDefault(dn => dn.Element("Description")?.Attribute("languagecode")?.Value == $"{_language}")?.Element("Description")?.Attribute("description")?.Value : systemForm.Elements("Descriptions")?.FirstOrDefault()?.Element("Description")?.Attribute("description")?.Value;

                    EntityFormComponent entityAttributeComponent = new EntityFormComponent()
                    {
                        Id = formId,
                        Name = name,
                        Type = type,
                        Description = description

                    };
                    listFoms.Add(entityAttributeComponent);
                }
            }
            return listFoms;
        }

        public string CleanID(string id)
        {
            return id.Replace("{", "").Replace("}", "").ToUpper();
        }
    }
}
