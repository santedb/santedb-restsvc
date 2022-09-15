using Newtonsoft.Json;
using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Rest.WWW.Configuration
{
    /// <summary>
    /// Service configuration for the web interface
    /// </summary>
    [XmlType(nameof(WwwServiceConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class WwwServiceConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// For deployments with multiple solutions on the server, this specifies the default solution.
        /// </summary>
        [XmlElement("defaultSolution"), JsonProperty("defaultSolution"), DisplayName("Default Solution"), Description("For SanteDB deployments which have multiple solutions, this specifies the solution which serve as the base for web pages. When not set the default system applet is used")]
        public string DefaultSolutionName { get; set; }

        /// <summary>
        /// Permit caching
        /// </summary>
        [XmlElement("allowCaching"), JsonProperty("allowCaching"), DisplayName("Allow Caching"), Description("When true, emits and adheres to caching instructions from browsers. False is recommended for debugging environemnts")]
        public bool AllowClientCaching { get; set; }
    }
}
