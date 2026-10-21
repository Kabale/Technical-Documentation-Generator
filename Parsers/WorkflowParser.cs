using dynamics.document.generator.Helpers;
using dynamics.document.generator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace dynamics.document.generator.Parsers
{
    public class WorkflowParser
    {
        private readonly XDocument _customizations;

        public WorkflowParser(XDocument customizations)
        {
            _customizations = customizations;
        }

        public List<WorkflowInfo> ParseWorkflows(string entityLogicalName)
        {
            var workflows = new List<WorkflowInfo>();

            // Category : 0 = Workflow, 1 = Dialog, 3 = Action, 5 = Business rule (parfois), 6 = Modern flow ref
            var workflowNodes = _customizations.Descendants("Workflow")
                .Where(w => w.Element("PrimaryEntity")?.Value == entityLogicalName
                         && w.Element("Category")?.Value != "2" // exclut Business Rules
                         && w.Element("Category")?.Value != "4"); // exclut BPF

            foreach (var wf in workflowNodes)
            {
                var categoryValue = wf.Element("Category")?.Value;
                var type = categoryValue switch
                {
                    "0" => "Workflow classique",
                    "1" => "Dialog",
                    "3" => "Action",
                    "5" => "Business Rule (legacy)",
                    _ => $"Autre (catégorie {categoryValue})"
                };

                var mode = wf.Element("Mode")?.Value; // 0 = Background(Async), 1 = Real-time(Sync)
                var triggerOnCreate = wf.Element("TriggerOnCreate")?.Value == "1";
                var triggerOnDelete = wf.Element("TriggerOnDelete")?.Value == "1";
                var triggerOnUpdateAttributeList = wf.Element("TriggerOnUpdateAttributeList")?.Value;

                var workflow = new WorkflowInfo
                {
                    Name = ParserHelper.GetWorkflowDisplayName(wf),
                    Type = type,
                    IsActive = wf.Element("StateCode")?.Value == "1",
                    IsRealTime = mode == "1",
                    PrimaryEntity = entityLogicalName,
                    Description = wf.Element("Description")?.Value ?? ""
                };

                var triggers = new List<string>();
                if (triggerOnCreate) triggers.Add("Création");
                if (triggerOnDelete) triggers.Add("Suppression");
                if (!string.IsNullOrEmpty(triggerOnUpdateAttributeList))
                {
                    triggers.Add("Modification");
                    workflow.TriggerFields = triggerOnUpdateAttributeList.Split(',').ToList();
                }

                workflow.TriggerType = string.Join(" / ", triggers);

                workflows.Add(workflow);
            }

            return workflows;
        }
    }
}
