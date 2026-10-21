using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class PluginStepInfo
    {
        public string Name { get; set; }
        public string StepId { get; set; }  
        public string PrimaryEntity { get; set; }
        public string SecondaryEntity { get; set; }
        public int Stage { get; set; }
        public int Mode { get; set; }
        public int Rank { get; set; }
        public string FilteringAttributes { get; set; }
        public string ImpersonatingUserIdName { get; set; }

        public string GetName()
        {
            return ExtractName(this.Name);
        }
        public string GetMessageName()
        {
            return ExtractMessageName(this.Name);
        }
        private static string ExtractName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var colonIndex = value.IndexOf(':');
            if (colonIndex < 0)
                return null;
            var name = value.Substring(0, colonIndex);
            return name.Trim();
        }
        private static string ExtractMessageName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var colonIndex = value.IndexOf(':');
            var ofIndex = value.IndexOf(
                "of",
                colonIndex >= 0 ? colonIndex + 1 : 0,
                StringComparison.OrdinalIgnoreCase);

            if (colonIndex < 0 || ofIndex < 0 || ofIndex <= colonIndex)
                return null;

            var message = value.Substring(
                colonIndex + 1,
                ofIndex - colonIndex - 1);

            return message.Trim();
        }
    } 
}
