using Newtonsoft.Json;
using SanteDB.Core.Templates.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Rest.AppService.Model
{

    /// <summary>
    /// Template definition view model
    /// </summary>
    [JsonObject(nameof(TemplateDefinitionViewModel))]
    public class TemplateDefinitionViewModel
    {

        public TemplateDefinitionViewModel()
        {
            
        }
        
        /// <summary>
        /// Template definition view model
        /// </summary>
        /// <param name="dataTemplateDefinition"></param>
        public TemplateDefinitionViewModel(DataTemplateDefinition dataTemplateDefinition)
        {
            this.Uuid = dataTemplateDefinition.Uuid;
            this.Public = dataTemplateDefinition.Public;
            this.Description = dataTemplateDefinition.Description;
            this.Name = dataTemplateDefinition.Name;
            this.Oid = dataTemplateDefinition.Oid;
            this.Scope = dataTemplateDefinition.Scopes?.Select(o => o.ToString()).ToList();
            this.Icon = dataTemplateDefinition.Metadata.Icon;
            this.Mnemonic = dataTemplateDefinition.Mnemonic;
            this.Guard = dataTemplateDefinition.Guard;
        }

        /// <summary>
        /// Gets or sets the uuid
        /// </summary>
        [JsonProperty("uuid")]
        public Guid Uuid { get; set; }

        [JsonProperty("public")]
        public bool Public { get; set; }

        [JsonProperty("mnemonic")]
        public string Mnemonic { get; set; }

        [JsonProperty("oid")]
        public string Oid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("guard")]
        public string Guard { get; set; }

        [JsonProperty("scope")]
        public List<String> Scope { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("definition")]
        public string Definition
        {
            get => $"/app/Template/{this.Mnemonic}/definition.json";
            set { }
        }

        [JsonProperty("form")]
        public string Form
        {
            get => $"/app/Template/{this.Mnemonic}/view/{DataTemplateViewType.Entry}";
            set { }
        }

        [JsonProperty("backEntry")]
        public string BackEntry
        {
            get => $"/app/Template/{this.Mnemonic}/view/{DataTemplateViewType.BackEntry}";
            set { }
        }

        [JsonProperty("view")]
        public string View
        {
            get => $"/app/Template/{this.Mnemonic}/view/{DataTemplateViewType.DetailView}";
            set { }
        }

        [JsonProperty("summaryView")]
        public string SummaryView
        {
            get => $"/app/Template/{this.Mnemonic}/view/{DataTemplateViewType.SummaryView}";
            set { }
        }
    }
}
