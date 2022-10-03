using Newtonsoft.Json;
using SanteDB.Core.Configuration;
using System.ComponentModel;
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
        /// Default asset to render
        /// </summary>
        public WwwServiceConfigurationSection()
        {
            this.DefaultApplet = "org.santedb.uicore";
        }

        /// <summary>
        /// For deployments with multiple solutions on the server, this specifies the default solution.
        /// </summary>
        [XmlElement("solution"), JsonProperty("solution"), DisplayName("Solution"), Description("For SanteDB deployments which have multiple solutions, this specifies the solution which serve as the base for web pages. When not set the default system applet is used")]
        public string Solution { get; set; }

        /// <summary>
        /// Gets or sets the startup asset
        /// </summary>
        [XmlElement("default"), JsonProperty("default"), DisplayName("Default Asset"), Description("Normally, asset paths in the package are rendered via /app-id/page.html however for requests on the root the default needs to be specified (default is org.santedb.uicore)")]
        public string DefaultApplet { get; set; }

        /// <summary>
        /// Permit caching
        /// </summary>
        [XmlElement("allowCaching"), JsonProperty("allowCaching"), DisplayName("Allow Caching"), Description("When true, emits and adheres to caching instructions from browsers. False is recommended for debugging environemnts")]
        public bool AllowClientCaching { get; set; }
    }
}
