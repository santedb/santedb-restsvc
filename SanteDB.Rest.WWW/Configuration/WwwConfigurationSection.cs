using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Rest.WWW.Configuration
{
    /// <summary>
    /// Web configuration section
    /// </summary>
    [XmlType(nameof(WwwConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class WwwConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Gets or sets the extensions of the objects which are allowed on all endpoint
        /// </summary>
        [DisplayName("Extensions"), Description("Sets the global list of file extensions which clients may cache (those for which etags and cache instructions are sent)")]
        [XmlArray("cache"), XmlArrayItem("extension")]
        public List<String> CacheExtensions { get; set; }

        /// <summary>
        /// The maximum age of cache items
        /// </summary>
        [DisplayName("Max Age"), Description("The time to live for cache items (the max-age)")]
        [XmlElement("maxAge")]
        public int? MaxAge { get; set; }
    }
}
