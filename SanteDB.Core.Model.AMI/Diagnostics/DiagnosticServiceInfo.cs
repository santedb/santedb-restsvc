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
        Daemon,
        [XmlEnum("data")]
        Data,
        [XmlEnum("repo")]
        Repository,
        [XmlEnum("other")]
        Passive
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
        public DiagnosticServiceInfo(object daemon)
        {
            this.Description = daemon.GetType().GetTypeInfo().GetCustomAttribute<ServiceProviderAttribute>()?.Name ??
                (daemon as IServiceImplementation)?.ServiceName ??
                daemon.GetType().FullName;
            this.IsRunning = (bool)(daemon.GetType().GetRuntimeProperty("IsRunning")?.GetValue(daemon) ?? false);
            this.Type = daemon.GetType().FullName;
            this.Class = daemon is IDaemonService ? ServiceClass.Daemon :
                daemon is IDataPersistenceService ? ServiceClass.Data :
                daemon.GetType().GetTypeInfo().ImplementedInterfaces.Any(o => o.Name.Contains("IRepositoryService")) ? ServiceClass.Repository :
                ServiceClass.Passive;
        }

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