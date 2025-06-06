﻿/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-6-21
 */
using Newtonsoft.Json;
using SanteDB.Core.Security.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Configuration
{
    /// <summary>
    /// Represents an endpoint configuration
    /// </summary>
    [XmlType(nameof(RestEndpointConfiguration), Namespace = "http://santedb.org/configuration")]
    [JsonObject]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public class RestEndpointConfiguration
    {

        // Address
        private string m_address;
        private Type m_contractType;

        /// <summary>
        /// AGS Endpoint CTOR
        /// </summary>
        public RestEndpointConfiguration()
        {
            this.Behaviors = new List<RestEndpointBehaviorConfiguration>();
            this.CertificateBinding = new X509ConfigurationElement();
        }

        /// <summary>
        /// Rest endpoint configuration copy constructor
        /// </summary>
        public RestEndpointConfiguration(RestEndpointConfiguration configuration) : this()
        {
            this.Behaviors = new List<RestEndpointBehaviorConfiguration>(configuration.Behaviors.Select(o => new RestEndpointBehaviorConfiguration(o)));
            if (configuration.CertificateBinding != null)
            {
                this.CertificateBinding = new X509ConfigurationElement()
                {
                    FindType = configuration.CertificateBinding.FindType,
                    StoreLocation = configuration.CertificateBinding.StoreLocation,
                    FindTypeSpecified = configuration.CertificateBinding.FindTypeSpecified,
                    StoreLocationSpecified = configuration.CertificateBinding.StoreLocationSpecified,
                    StoreName = configuration.CertificateBinding.StoreName,
                    StoreNameSpecified = configuration.CertificateBinding.StoreNameSpecified,
                    FindValue = configuration.CertificateBinding.FindValue
                };
            }
            this.Address = configuration.Address;
            this.Contract = configuration.Contract;
        }

        /// <summary>
        /// Gets or sets the contract type
        /// </summary>
        [XmlAttribute("contract"), JsonProperty("contract"), Browsable(false)]
        public String ContractXml { get; set; }

        /// <summary>
        /// Gets or sets the Contract type
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [DisplayName("Service Contract"), Description("The service contract this endpoint implements")]
        [Browsable(true)]
        public Type Contract
        {
            get
            {
                if (this.m_contractType == null)
                {
                    this.m_contractType = Type.GetType(this.ContractXml) ?? AppDomain.CurrentDomain.GetAllTypes().FirstOrDefault(o => o.AssemblyQualifiedNameWithoutVersion().Equals(this.ContractXml));
                }
                return this.m_contractType;
            }
            set
            {
                this.ContractXml = value?.AssemblyQualifiedNameWithoutVersion();
            }
        }

        /// <summary>
        /// Gets or sets the address
        /// </summary>
        [XmlAttribute("address"), JsonProperty("address")]
        [DisplayName("Address"), Description("The address where the endpoint should accept messages")]
        public String Address
        {
            get => this.m_address;
            set
            {
                this.m_address = value;
                if (Uri.TryCreate(value, UriKind.Absolute, out Uri uri) && uri.Scheme == "https" && this.CertificateBinding == null)
                {
                    this.CertificateBinding = new X509ConfigurationElement();
                }
            }
        }

        /// <summary>
        /// Gets the bindings 
        /// </summary>
        [XmlArray("behaviors"), XmlArrayItem("add"), JsonProperty("behaviors")]
        [DisplayName("Endpoint Behaviors"), Description("The behaviors to attach to the endpoint")]
        public List<RestEndpointBehaviorConfiguration> Behaviors { get; set; }

        /// <summary>
        /// Gets or sets the certificate binding
        /// </summary>
        [XmlElement("certificate"), JsonProperty("certificate")]
        //[Editor("SanteDB.Configuration.Editors.X509Certificate2Editor, SanteDB.Configuration", "System.Drawing.Design.UITypeEditor, System.Windows.Forms")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [DisplayName("Certificate Binding"), Description("The certificate information to bind to the HTTP endpoint")]
        public X509ConfigurationElement CertificateBinding { get; set; }

        /// <summary>
        /// Client certificate negotiation
        /// </summary>
        [XmlElement("clientCerts"), JsonProperty("clientCerts")]
        [DisplayName("Client Certificates"), Description("When enabled, negotiate client certificates with the endpoint")]
        public bool ClientCertNegotiation { get; set; }

        /// <summary>
        /// Endpoint configuration
        /// </summary>
        public override string ToString() => this.Address;
    }
}