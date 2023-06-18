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
 * Date: 2023-5-19
 */
using Newtonsoft.Json;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Configuration
{
    /// <summary>
    /// Represents the configuration for the AGS
    /// </summary>
    [XmlType(nameof(RestConfigurationSection), Namespace = "http://santedb.org/configuration")]
    [JsonObject]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public class RestConfigurationSection : IValidatableConfigurationSection
    {

        /// <summary>
        /// Construct the AGS configuration
        /// </summary>
        public RestConfigurationSection()
        {
            this.Services = new List<RestServiceConfiguration>();
        }

        /// <summary>
        /// Gets the base address
        /// </summary>
        [XmlElement("baseAddress"), JsonProperty("baseAddress")]
        [DisplayName("Base Address"), Description("When running SanteDB behind a reverse proxy (like NGINX or OpenHIM), this setting controls the external base address of all REST endpoints")]
        public string ExternalHostPort { get; set; }

        /// <summary>
        /// Gets or sets the service configuration
        /// </summary>
        [XmlElement("service"), JsonProperty("service")]
        [DisplayName("REST Services"), Description("A complete list of REST based services which are exposed within the host context on this server")]
        public List<RestServiceConfiguration> Services { get; set; }

        /// <summary>
        /// Validate the configuration
        /// </summary>
        public IEnumerable<DetectedIssue> Validate()
        {
            foreach (var itm in this.Services)
            {
                if (itm.ServiceType == null)
                {
                    yield return new DetectedIssue(DetectedIssuePriorityType.Error, "rest.service", $"Behavior implementation type {itm.ServiceTypeXml} could not be found!", Guid.Empty);
                }

                if (itm.Endpoints?.Any() != true)
                {
                    yield return new DetectedIssue(DetectedIssuePriorityType.Error, "rest.endpoint", $"Service {itm.ConfigurationName} contains no endpoints!", Guid.Empty);
                }
                else
                {
                    foreach (var itmE in itm.Endpoints)
                    {
                        if (itmE.Contract == null)
                        {
                            yield return new DetectedIssue(DetectedIssuePriorityType.Error, "rest.endpoint.contract", $"Endpoint {itmE.Address} contract {itmE.ContractXml} cannot be found!", Guid.Empty);
                        }

                        if (itmE.Behaviors != null)
                        {
                            foreach (var itmB in itmE.Behaviors)
                            {
                                if (itmB.Type == null)
                                {
                                    yield return new DetectedIssue(DetectedIssuePriorityType.Error, "rest.endpoint.behavior", $"Behavior {itmB.XmlType} on {itmE.Address} cannot be found!", Guid.Empty);
                                }
                            }
                        }
                    }
                }

                if (itm.Behaviors != null)
                {
                    foreach (var itmB in itm.Behaviors)
                    {
                        if (itmB.Type == null)
                        {
                            yield return new DetectedIssue(DetectedIssuePriorityType.Error, "rest.service.behavior", $"Service behavior {itmB.XmlType} on service {itm.ConfigurationName} cannot be found!", Guid.Empty);
                        }

                    }
                }
            }
        }
    }
}
