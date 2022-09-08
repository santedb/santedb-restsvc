/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 2022-5-30
 */
using Newtonsoft.Json;
using RestSrvr;
using SanteDB.Core.Model.Attributes;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Configuration
{
    /// <summary>
    /// Represents a single behavior configuration element
    /// </summary>
    [XmlType(nameof(RestBehaviorConfiguration), Namespace = "http://santedb.org/configuration")]
    [JsonObject]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public class RestBehaviorConfiguration
    {
        /// <summary>
        /// Creates a new rest behavior configuration
        /// </summary>
        public RestBehaviorConfiguration()
        {

        }

        /// <summary>
        /// AGS Behavior Configuration
        /// </summary>
        public RestBehaviorConfiguration(Type behaviorType)
        {
            this.Type = behaviorType;
        }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [XmlAttribute("type"), JsonProperty("type"), Browsable(false)]
        public string XmlType { get; set; }

        /// <summary>
        /// Gets the type of the binding
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public virtual Type Type
        {
            get
            {
                if (this.XmlType != null)
                    return Type.GetType(this.XmlType);
                else
                    return null;
            }
            set
            {
                this.XmlType = value?.AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Gets or sets the special configuration for the binding
        /// </summary>
        [XmlElement("configuration"), JsonProperty("configuration")]
        [Browsable(false)]
        public XElement Configuration { get; set; }

        /// <summary>
        /// Configuration string
        /// </summary>
        [DisplayName("Behavior Configuration"), Description("XML Configuration for the Behavior")]
        [XmlIgnore, JsonIgnore]
        [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
        //[Editor("System.Web.UI.Design.XmlFileEditor, System.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
        public String ConfigurationString
        {
            get => this.Configuration?.ToString();
            set
            {
                if (!String.IsNullOrEmpty(value))
                    this.Configuration = XElement.Parse(value);
                else
                    this.Configuration = null;
            }
        }

        /// <summary>
        /// Get the name of the behavior
        /// </summary>
        /// <returns></returns>
        public override string ToString() => this.Type?.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? this.Type?.Name;

    }

    /// <summary>
    /// Represents a single behavior configuration element with validation that the type is a IServiceBehavior
    /// </summary>
    [XmlType(nameof(RestServiceBehaviorConfiguration), Namespace = "http://santedb.org/configuration")]
    [JsonObject]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public class RestServiceBehaviorConfiguration : RestBehaviorConfiguration
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public RestServiceBehaviorConfiguration()
        {

        }

        /// <summary>
        /// Create a new behavior configuration with specified type
        /// </summary>
        public RestServiceBehaviorConfiguration(Type behaviorType) : base(behaviorType)
        {

        }

        /// <summary>
        /// Configuration copy constructor
        /// </summary>
        public RestServiceBehaviorConfiguration(RestServiceBehaviorConfiguration configuration)
        {
            this.Configuration = configuration.Configuration;
            this.Type = configuration.Type;
        }

        /// <summary>
        /// Gets the type
        /// </summary>
        [XmlIgnore, JsonIgnore, Editor("SanteDB.Configuration.Editors.TypeSelectorEditor, SanteDB.Configuration", "System.Drawing.Design.UITypeEditor, System.Drawing"),
            TypeConverter("SanteDB.Configuration.Converters.TypeDisplayConverter, SanteDB.Configuration"), BindingAttribute(typeof(IServiceBehavior))]
        public override Type Type { get => base.Type; set => base.Type = value; }


    }

    /// <summary>
    /// Represents a single behavior configuration element with validation that the type is a IServiceBehavior
    /// </summary>
    [XmlType(nameof(RestEndpointBehaviorConfiguration), Namespace = "http://santedb.org/configuration")]
    [JsonObject]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public class RestEndpointBehaviorConfiguration : RestBehaviorConfiguration
    {
        /// <summary>
        /// Default ctor 
        /// </summary>
        public RestEndpointBehaviorConfiguration()
        {

        }

        /// <summary>
        /// REST endpoint configuration
        /// </summary>
        public RestEndpointBehaviorConfiguration(RestEndpointBehaviorConfiguration configuration)
        {
            this.Configuration = configuration.Configuration;
            this.Type = configuration.Type;
        }

        /// <summary>
        /// Create a new endpoint behavior
        /// </summary>
        public RestEndpointBehaviorConfiguration(Type type) : base(type)
        {
        }

        /// <summary>
        /// Gets the type
        /// </summary>
        [XmlIgnore, JsonIgnore, Editor("SanteDB.Configuration.Editors.TypeSelectorEditor, SanteDB.Configuration", "System.Drawing.Design.UITypeEditor, System.Drawing"),
            TypeConverter("SanteDB.Configuration.Converters.TypeDisplayConverter, SanteDB.Configuration"), BindingAttribute(typeof(IEndpointBehavior))]
        public override Type Type { get => base.Type; set => base.Type = value; }
    }
}