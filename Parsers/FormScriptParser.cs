using dynamics.document.generator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace dynamics.document.generator.Parsers
{
    public class FormScriptParser
    {
        private readonly XDocument _customizations;

        public FormScriptParser(XDocument customizations)
        {
            _customizations = customizations;
        }
        public List<FormScriptInfo> ParseFormScripts(string entityLogicalName)
        {
            var results = new List<FormScriptInfo>();

            var entityNode = _customizations
                .Descendants()
                .FirstOrDefault(e =>
                    e.Name.LocalName.Equals("Entity", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        e.Elements()
                            .FirstOrDefault(x => x.Name.LocalName == "Name")
                            ?.Value,
                        entityLogicalName,
                        StringComparison.OrdinalIgnoreCase));

            if (entityNode == null)
                return results;

            var formXmlNodes = entityNode
                .Descendants()
                .Where(x => x.Name.LocalName == "FormXml");

            foreach (var formXml in formXmlNodes)
            {
                foreach (var form in formXml
                             .Descendants()
                             .Where(x => x.Name.LocalName == "form"))
                {
                    ParseForm(
                        form,
                        entityLogicalName,
                        results);
                }
            }

            return results;
        }

        private static void ParseForm(
            XElement form,
            string entityLogicalName,
            List<FormScriptInfo> results)
        {
            var systemForm = form
                .Ancestors()
                .FirstOrDefault(x => x.Name.LocalName == "systemform");

            var formName =
                GetLocalizedFormName(systemForm)
                ?? GetAttribute(systemForm, "name")
                ?? GetAttribute(form, "name")
                ?? GetElementValue(systemForm, "Name")
                ?? GetElementValue(form, "Name");

            var formId =
                GetAttribute(systemForm, "id")
                ?? GetAttribute(systemForm, "formid")
                ?? GetElementValue(systemForm, "FormId");

            var libraries = form
                .Descendants()
                .Where(x => x.Name.LocalName == "Library")
                .ToList();

            var libraryById = libraries
                .Where(x => !string.IsNullOrWhiteSpace(
                    GetAttribute(x, "libraryUniqueId")))
                .GroupBy(x => GetAttribute(x, "libraryUniqueId")!,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => GetAttribute(g.First(), "name"),
                    StringComparer.OrdinalIgnoreCase);

            // Événements globaux du formulaire
            foreach (var eventsNode in form
                         .Elements()
                         .Where(x => x.Name.LocalName == "events"))
            {
                ParseEvents(
                    eventsNode,
                    entityLogicalName,
                    formName,
                    formId,
                    controlId: null,
                    libraryById,
                    results);
            }

            // Événements associés aux contrôles/champs
            foreach (var cell in form
                         .Descendants()
                         .Where(x => x.Name.LocalName == "cell"))
            {
                var controlId =
                    GetAttribute(cell, "id")
                    ?? GetElementValue(cell, "control");

                var eventsNode = cell
                    .Elements()
                    .FirstOrDefault(x => x.Name.LocalName == "events");

                if (eventsNode == null)
                    continue;

                ParseEvents(
                    eventsNode,
                    entityLogicalName,
                    formName,
                    formId,
                    controlId,
                    libraryById,
                    results);
            }
        }

    private static string GetLocalizedFormName(
        XElement systemForm,
        int languageCode = 1033)
        {
            if (systemForm == null)
                return null;

            var localizedName = systemForm
                .Descendants()
                .FirstOrDefault(x =>
                    x.Name.LocalName.Equals(
                        "LocalizedName",
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    string.Equals(
                        GetAttribute(x, "languagecode"),
                        languageCode.ToString(),
                        StringComparison.OrdinalIgnoreCase));

            return GetAttribute(localizedName, "description");
        }

        private static void ParseEvents(
            XElement eventsNode,
            string entityLogicalName,
            string formName,
            string formId,
            string controlId,
            Dictionary<string, string> libraryById,
            List<FormScriptInfo> results)
        {
            foreach (var eventNode in eventsNode
                         .Elements()
                         .Where(x => x.Name.LocalName == "event"))
            {
                var eventName = GetAttribute(eventNode, "name");

                var isEventActive =
                    GetAttribute(eventNode, "active")
                    ?? GetAttribute(eventNode, "enabled");

                var attribute = GetAttribute(eventNode, "attribute");

                foreach (var handler in eventNode
                             .Descendants()
                             .Where(x => x.Name.LocalName == "Handler"))
                {
                    var libraryName = GetAttribute(handler, "libraryName");

                    // Certains exports utilisent libraryUniqueId
                    if (string.IsNullOrWhiteSpace(libraryName))
                    {
                        var libraryId = GetAttribute(handler, "libraryUniqueId");

                        if (!string.IsNullOrWhiteSpace(libraryId) &&
                            libraryById.TryGetValue(libraryId, out var resolvedLibrary))
                        {
                            libraryName = resolvedLibrary;
                        }
                    }

                    results.Add(new FormScriptInfo
                    {
                        EntityLogicalName = entityLogicalName,
                        FormName = formName,
                        FormId = formId,
                        EventName = eventName,
                        IsEventActive = isEventActive,
                        ControlId = controlId,
                        Attributes = attribute,

                        FunctionName =
                            GetAttribute(handler, "functionName")
                            ?? GetAttribute(handler, "function"),

                        LibraryName = libraryName,

                        Enabled =
                            GetAttribute(handler, "enabled")
                            ?? "true",

                        Parameters =
                            GetAttribute(handler, "parameters")
                            ?? string.Empty,

                        PassExecutionContext =
                            GetAttribute(handler, "passExecutionContext")
                            ?? "false"
                    });
                }
            }
        }

        private static string GetAttribute(XElement element, string name)
        {
            return element?
                .Attributes()
                .FirstOrDefault(a =>
                    a.Name.LocalName.Equals(
                        name,
                        StringComparison.OrdinalIgnoreCase))
                ?.Value;
        }

        private static string GetElementValue(XElement element, string name)
        {
            return element?
                .Elements()
                .FirstOrDefault(e =>
                    e.Name.LocalName.Equals(
                        name,
                        StringComparison.OrdinalIgnoreCase))
                ?.Value;
        }
        public List<FormScriptInfo> ParseFormScriptsClaude(string entityLogicalName)
        {
            var results = new List<FormScriptInfo>();

            var entityNode = _customizations
                .Descendants("Entity")
                .FirstOrDefault(e => string.Equals(
                    e.Element("Name")?.Value,
                    entityLogicalName,
                    StringComparison.OrdinalIgnoreCase));

            var formXml = entityNode.Descendants("FormXml").FirstOrDefault();
            // 1. Ne garder que les formulaires de type "main"
            var mainFormsContainers = formXml
                .Descendants()
                .Where(e => string.Equals(e.Name.LocalName, "forms", StringComparison.OrdinalIgnoreCase)
                         && string.Equals(e.Attribute("type")?.Value, "main", StringComparison.OrdinalIgnoreCase));

            var formNodes = mainFormsContainers
                .Elements()
                .Where(e => string.Equals(e.Name.LocalName, "systemform", StringComparison.OrdinalIgnoreCase));
                         // && string.Equals(e.Element("objecttypecode")?.Value, entityLogicalName, StringComparison.OrdinalIgnoreCase));

            foreach (var formNode in formNodes)
            {
                var formName = formNode.Element("name")?.Value ?? "(Sans nom)";
                var formIdRaw = formNode.Element("formid")?.Value;

                // 2. Le form réel est un élément XML natif <form>, pas du texte échappé
                var formElement = formNode.Elements()
                    .FirstOrDefault(x => string.Equals(x.Name.LocalName, "form", StringComparison.OrdinalIgnoreCase));

                if (formElement == null)
                {
                    Console.WriteLine($"  ⚠ Form '{formName}' : pas d'élément <form> trouvé");
                    continue;
                }

                // 3. Récupération des formLibraries déclarées (optionnel, utile pour le mapping)
                var formLibraries = formElement
                    .Descendants()
                    .Where(e => string.Equals(e.Name.LocalName, "formLibrary", StringComparison.OrdinalIgnoreCase))
                    .Select(lib => lib.Value?.Trim())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .Distinct()
                    .ToList();

                // 4. Récupération de tous les Event (insensible à la casse)
                var eventNodes = formElement
                    .Descendants()
                    .Where(e => string.Equals(e.Name.LocalName, "event", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                Console.WriteLine($"Form '{formName}' : {eventNodes.Count} event(s) trouvé(s)");

                foreach (var evt in eventNodes)
                {
                    var eventName = evt.Attributes()
                        .FirstOrDefault(a => string.Equals(a.Name.LocalName, "name", StringComparison.OrdinalIgnoreCase))
                        ?.Value ?? "(inconnu)";

                    var isActive = evt.Attributes()
                        .FirstOrDefault(a => string.Equals(a.Name.LocalName, "active", StringComparison.OrdinalIgnoreCase))
                        ?.Value;

                    // Champ sur lequel l'event est déclenché (si c'est un onchange sur un control)
                    var parentControl = evt.Ancestors()
                        .FirstOrDefault(a => string.Equals(a.Name.LocalName, "control", StringComparison.OrdinalIgnoreCase));

                    var controlId = parentControl?.Element("id")?.Value
                                     ?? parentControl?.Attribute("id")?.Value;

                    // 5. Récupération des Handlers (insensible à la casse)
                    var handlerNodes = evt
                        .Descendants()
                        .Where(h => string.Equals(h.Name.LocalName, "Handler", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var handler in handlerNodes)
                    {
                        string GetAttr(string attrName) =>
                            handler.Attributes()
                                .FirstOrDefault(a => string.Equals(a.Name.LocalName, attrName, StringComparison.OrdinalIgnoreCase))
                                ?.Value;

                        var functionName = GetAttr("functionName");
                        var libraryName = GetAttr("libraryName");
                        var enabled = GetAttr("enabled");
                        var parameters = GetAttr("parameters");
                        var passExecutionContext = GetAttr("passExecutionContext");

                        results.Add(new FormScriptInfo
                        {
                            EntityLogicalName = entityLogicalName,
                            FormName = formName,
                            FormId = formIdRaw,
                            EventName = eventName,
                            IsEventActive = isActive,
                            ControlId = controlId,
                            FunctionName = functionName,
                            LibraryName = libraryName,
                            Enabled = enabled,
                            Parameters = parameters,
                            PassExecutionContext = passExecutionContext
                        });
                    }
                }
            }

            return results;
        }
    }
}
