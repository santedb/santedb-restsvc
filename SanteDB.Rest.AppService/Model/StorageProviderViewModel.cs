using Newtonsoft.Json;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AppService.Model
{
    /// <summary>
    /// View model for provider
    /// </summary>
    [JsonObject]
    public class StorageProviderViewModel
    {
        /// <summary>
        /// Default ctor for serialization
        /// </summary>
        public StorageProviderViewModel()
        {
        }

        /// <summary>
        /// Creates a new storage provider
        /// </summary>
        public StorageProviderViewModel(IDataConfigurationProvider o)
        {
            this.Invariant = o.Invariant;
            this.Name = o.Name;
            this.Options = o.Options;
        }

        /// <summary>
        /// The invariant name
        /// </summary>
        [JsonProperty("invariant")]
        public string Invariant { get; set; }

        /// <summary>
        /// The property name
        /// </summary>
        [JsonProperty("name")]
        public String Name { get; set; }

        /// <summary>
        /// Gets or sets the options
        /// </summary>
        [JsonProperty("options")]
        public IDictionary<String, ConfigurationOptionType> Options { get; set; }
    }
}
