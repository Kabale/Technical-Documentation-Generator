using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynamics.document.generator.Models
{
    public class FormScriptInfo
    {
        /// <summary>
        /// Nom logique de l'entité (ex: "account")
        /// </summary>
        public string EntityLogicalName { get; set; }

        /// <summary>
        /// Nom du formulaire (ex: "Account Card form", "Information")
        /// </summary>
        public string FormName { get; set; }

        /// <summary>
        /// GUID du formulaire
        /// </summary>
        public string FormId { get; set; }

        /// <summary>
        /// Nom de l'événement (ex: "onsave", "onload", "onchange")
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Indique si l'événement est actif ("true"/"false" tel que défini dans le XML)
        /// </summary>
        public string IsEventActive { get; set; }

        /// <summary>
        /// Id du control associé si l'event est un onchange sur un champ spécifique (null pour onload/onsave global)
        /// </summary>
        public string ControlId { get; set; }

        /// <summary>
        /// Nom de la fonction JS appelée (ex: "clientEntity.Package.OnsaveForm")
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Chemin/nom de la librairie JS (ex: "ainos_/js/ClientEntity/ClientEntity_fs.js")
        /// </summary>
        public string LibraryName { get; set; }

        /// <summary>
        /// Indique si le handler est activé ("true"/"false")
        /// </summary>
        public string Enabled { get; set; }

        /// <summary>
        /// Paramètres passés à la fonction (chaîne brute telle que définie dans le XML)
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// Champs sur lesquels l'événement est déclenché (ex: "name,telephone1" pour un onchange sur plusieurs champs)
        /// </summary>
        public string Attributes { get; set; }  

        /// <summary>
        /// Indique si le contexte d'exécution est passé à la fonction ("true"/"false")
        /// </summary>
        public string PassExecutionContext { get; set; }

        public override string ToString()
        {
            var scope = string.IsNullOrEmpty(ControlId) ? "Form" : $"Control[{ControlId}]";
            return $"{EntityLogicalName} / {FormName} / {EventName} ({scope}) => {LibraryName}::{FunctionName} [Enabled={Enabled}]";
        }
    }
}
