/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using Newtonsoft.Json;
using RestSrvr.Attributes;
using SanteDB.Core.Model.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Configuration
{

    /// <summary>
    /// Represents configuration of a single AGS service
    /// </summary>
    [XmlType(nameof(RestServiceConfiguration), Namespace = "http://santedb.org/configuration")]
    [XmlRoot(nameof(RestServiceConfiguration), Namespace = "http://santedb.org/configuration")]
    [JsonObject]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public class RestServiceConfiguration
    {
        // Configuration
        private static XmlSerializer s_serializer;

        /// <summary>
        /// AGS Service Configuration
        /// </summary>
        public RestServiceConfiguration()
        {
            this.Behaviors = new List<RestServiceBehaviorConfiguration>();
            this.Endpoints = new List<RestEndpointConfiguration>();
        }

        /// <summary>
        /// AGS Service configuration copy ctor
        /// </summary>
        public RestServiceConfiguration(RestServiceConfiguration configuration)
        {
            if (configuration.Behaviors != null)
            {
                this.Behaviors = new List<RestServiceBehaviorConfiguration>(configuration.Behaviors.Select(o => new RestServiceBehaviorConfiguration(o)));
            }
            if (configuration.Endpoints != null)
            {
                this.Endpoints = new List<RestEndpointConfiguration>(configuration.Endpoints?.Select(o => new RestEndpointConfiguration(o)));
            }
            this.ConfigurationName = configuration.ConfigurationName;
            this.ServiceType = configuration.ServiceType;
        }

        /// <summary>
        /// Creates a service configuration from the specified type
        /// </summary>
        public RestServiceConfiguration(Type implementationType) : this()
        {
            this.ConfigurationName = implementationType.GetCustomAttribute<ServiceBehaviorAttribute>()?.Name ?? implementationType.FullName;
            this.ServiceType = implementationType;
        }

        /// <summary>
        /// Gets or sets the name of the service
        /// </summary>
        [XmlAttribute("name"), JsonProperty("name")]
        [DisplayName("Configuration Name"), Description("Sets the informative name for this service")]
        public string ConfigurationName { get; set; }

        /// <summary>
        /// Gets or sets the behavior
        /// </summary>
        [XmlAttribute("implementationType"), JsonProperty("implementationType")]
        [Browsable(false)]
        public String ServiceTypeXml { get; set; }

        /// <summary>
        /// Service ignore
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [DisplayName("Behavior"), Description("Sets the implementation behavior of this service")]
        [Browsable(false)]
        public Type ServiceType { get => this.ServiceTypeXml != null ? Type.GetType(this.ServiceTypeXml) : null; set => this.ServiceTypeXml = value?.AssemblyQualifiedName; }

        /// <summary>
        /// Gets or sets the behavior of the AGS endpoint
        /// </summary>
        [XmlArray("behaviors"), XmlArrayItem("add"), JsonProperty("behaviors")]
        [DisplayName("Service Behaviors"), Description("Sets the overall behaviors on the service layer")]
        public List<RestServiceBehaviorConfiguration> Behaviors { get; set; }

        /// <summary>
        /// Gets or sets the endpoints 
        /// </summary>
        [XmlElement("endpoint"), JsonProperty("endpoint")]
        [DisplayName("Endpoints"), Description("One or more service endpoints where this service can be reached")]
        public List<RestEndpointConfiguration> Endpoints { get; set; }

        /// <summary>
        /// Load from the specified stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static RestServiceConfiguration Load(Stream stream)
        {
            if (s_serializer == null)
            {
                s_serializer = XmlModelSerializerFactory.Current.CreateSerializer(typeof(RestServiceConfiguration));
            }

            return s_serializer.Deserialize(stream) as RestServiceConfiguration;
        }

        /// <summary>
        /// Gets the string representation of this 
        /// </summary>
        public override string ToString() => $"REST Service: {this.ConfigurationName}";

    }
}