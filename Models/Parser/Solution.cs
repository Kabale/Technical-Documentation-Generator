using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models.Parser
{
    public class Solution
    {
        public List<RootComponent> RootComponents { get; set; } = new List<RootComponent>();
        public List<EntityComponent> Entities { get; set; } = new List<EntityComponent>();
        public List<RoleComponent> Roles { get; set; } = new List<RoleComponent>();
        public List<WorkflowComponent> Workflows { get; set; } = new List<WorkflowComponent>();
        public List<FieldSecurityProfileComponent> FieldSecurityProfiles { get; set; } = new List<FieldSecurityProfileComponent>();
        public List<TemplateComponent> Templates { get; set; } = new List<TemplateComponent>();
        public List<RibbonDiffXmlComponent> RibbonDiffXmls { get; set; } = new List<RibbonDiffXmlComponent>();
        public List<SiteMapComponent> SiteMaps { get; set; } = new List<SiteMapComponent>();
        public List<EntityMapComponent> EntityMaps { get; set; } = new List<EntityMapComponent>();
        public List<EntityRelationshipComponent> EntityRelationships { get; set; } = new List<EntityRelationshipComponent>();
        public List<OrganizationSettingComponent> OrganizationSettings { get; set; } = new List<OrganizationSettingComponent>();
        public List<OptionSetComponent> OptionSets { get; set; } = new List<OptionSetComponent>();
        public List<ReportComponent> Reports { get; set; } = new List<ReportComponent>();
        public List<DashboardComponent> Dashboards { get; set; } = new List<DashboardComponent>();
        public List<InteractionCentricDashboardComponent> InteractionCentricDashboards { get; set; } = new List<InteractionCentricDashboardComponent>();
        public List<WebResourceComponent> WebResources { get; set; } = new List<WebResourceComponent>();
        public List<CustomControlComponent> CustomControls { get; set; } = new List<CustomControlComponent>();
        public List<SolutionPluginAssemblieComponent> SolutionPluginAssemblies { get; set; } = new List<SolutionPluginAssemblieComponent>();
        public List<SdkMessageProcessingStepComponent> SdkMessageProcessingSteps { get; set; } = new List<SdkMessageProcessingStepComponent>();
        public List<AppModuleSiteMapComponent> AppModuleSiteMaps { get; set; } = new List<AppModuleSiteMapComponent>();
        public List<AppModuleComponent> AppModules { get; set; } = new List<AppModuleComponent>();
        public List<EntityDataProviderComponent> EntityDataProviders { get; set; } = new List<EntityDataProviderComponent>();
        public List<int> Languages { get; set; } = new List<int>();
    }
}
