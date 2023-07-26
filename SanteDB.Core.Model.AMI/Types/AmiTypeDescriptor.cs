using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Types
{
    /// <summary>
    /// Represents a type descriptor (name + type)
    /// </summary>
    [XmlType(nameof(AmiTypeDescriptor), Namespace = "http://santedb.org/ami"), JsonObject(nameof(AmiTypeDescriptor))]
    public class AmiTypeDescriptor
    {

        /// <summary>
        /// Serialization CTOR
        /// </summary>
        public AmiTypeDescriptor()
        {
            
        }

        /// <summary>
        /// Create new type descriptor with the specified metadata from the <paramref name="type"/>
        /// </summary>
        public AmiTypeDescriptor(Type type)
        {
            this.TypeAqn = type.AssemblyQualifiedName;
            this.DisplayName = type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? type.Name;
        }

        /// <summary>
        /// Create new type description with specified <paramref name="type"/> and <paramref name="displayName"/>
        /// </summary>
        public AmiTypeDescriptor(Type type, String displayName)
        {
            this.TypeAqn = type.AssemblyQualifiedName;
            this.DisplayName = displayName;
        }

        /// <summary>
        /// Type AQN
        /// </summary>
        [XmlElement("aqn"), JsonProperty("aqn")]
        public String TypeAqn { get; set; }

        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        [XmlElement("display"), JsonProperty("display")]
        public String DisplayName { get; set; }

    }
}
