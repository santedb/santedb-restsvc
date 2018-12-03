/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * User: justin
 * Date: 2018-11-20
 */
using Newtonsoft.Json;
using SanteDB.Core.Services;
using System;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Diagnostics
{

    /// <summary>
    /// Represets the type of service classes
    /// </summary>
    [XmlType(nameof(ServiceClass), Namespace = "http://santedb.org/ami/diagnostics")]
    public enum ServiceClass
    {
        [XmlEnum("daemon")]
        Daemon = 1,
        [XmlEnum("data")]
        Data = 2,
        [XmlEnum("repo")]
        Repository = 3,
        [XmlEnum("other")]
        Passive = 4
    }

    /// <summary>
    /// Represents diagnostic service info
    /// </summary>
    [JsonObject(nameof(DiagnosticServiceInfo)), XmlType(nameof(DiagnosticServiceInfo), Namespace = "http://santedb.org/ami/diagnostics")]
    public class DiagnosticServiceInfo
    {
        /// <summary>
        /// Creates new diagnostic service info
        /// </summary>
		public DiagnosticServiceInfo()
        {
        }

        /// <summary>
        /// Create the service info
        /// </summary>
        public DiagnosticServiceInfo(TypeInfo daemonType)
        {
            this.Description = daemonType.GetCustomAttribute<ServiceProviderAttribute>()?.Name ??
                daemonType.FullName;
            this.IsRunning = false;
            this.Type = daemonType.AssemblyQualifiedName;
            this.Class = typeof(IDaemonService).GetTypeInfo().IsAssignableFrom(daemonType) ? ServiceClass.Daemon :
                typeof(IDataPersistenceService).GetTypeInfo().IsAssignableFrom(daemonType) ? ServiceClass.Data :
                daemonType.ImplementedInterfaces.Any(o => o.Name.Contains("IRepositoryService")) ? ServiceClass.Repository :
                ServiceClass.Passive;
        }

        /// <summary>
        /// Create the service info
        /// </summary>
        public DiagnosticServiceInfo(object daemon) : this(daemon.GetType().GetTypeInfo())
        {
            this.IsRunning = (bool)(daemon.GetType().GetRuntimeProperty("IsRunning")?.GetValue(daemon) ?? false);
        }

        /// <summary>
        /// Indicates whether the service is installed (active) or not
        /// </summary>
        [XmlElement("active"), JsonProperty("active")]
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        [XmlElement("class"), JsonProperty("class")]
        public ServiceClass Class { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        [XmlElement("description"), JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets whether the service is running
        /// </summary>
        [XmlElement("running"), JsonProperty("running")]
        public bool IsRunning { get; set; }

        /// <summary>
        /// Gets or sets the type
        /// </summary>
        [XmlElement("type"), JsonProperty("type")]
        public string Type { get; set; }
    }
}