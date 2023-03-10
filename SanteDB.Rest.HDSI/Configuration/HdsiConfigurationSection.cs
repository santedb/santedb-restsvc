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
using SanteDB.Core.Configuration;
using SanteDB.Core.Model.Attributes;
using SanteDB.Rest.Common;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Rest.HDSI.Configuration
{
    /// <summary>
    /// Configuration class for HDSI configuration
    /// </summary>
    [XmlType(nameof(HdsiConfigurationSection), Namespace = "http://santedb.org/configuration")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Model classes - ignored
    public class HdsiConfigurationSection : IConfigurationSection
    {
        /// <summary>
        /// HDSI Configuration
        /// </summary>
        public HdsiConfigurationSection()
        {

        }

        /// <summary>
        /// Resources on the HDSI that are allowed
        /// </summary>
        [XmlArray("resources"), XmlArrayItem("add")]
        [DisplayName("Allowed Resources"), Description("When set, instructs the HDSI to only allow these resources to be accessed (can also be used to specify custom resource handlers). LEAVE THIS BLANK IF YOU WANT THE HDSI TO USE THE DEFAULT CONFIGURATION")]
        [Editor("SanteDB.Configuration.Editors.TypeSelectorEditor, SanteDB.Configuration", "System.Drawing.Design.UITypeEditor, System.Drawing"), Binding(typeof(IApiResourceHandler))]
        public List<TypeReferenceConfiguration> ResourceHandlers
        {
            get; set;
        }

        /// <summary>
        /// When set to true in the configuration the service should include metadata headers
        /// </summary>
        [XmlAttribute("includeMetadataHeadersInSearch")]
        [DisplayName("Include Metadata in Search Results"), Description("When set to true, instructs the HDSI to perform a query for LastModifiedTime headers. This can provide additional information to callers, however can introduce additional overhead")]
        public bool IncludeMetadataHeadersOnSearch { get; set; }

        /// <summary>
        /// Auto forward requests to upstream
        /// </summary>
        [XmlAttribute("autoforwardToUpstream")]
        [DisplayName("Auto-Forward Requests"), Description("When true, automatically forwards all requests to the upstream")]
        public bool AutomaticallyForwardRequests { get; set; }
    }
}