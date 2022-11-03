﻿/*
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
using SanteDB.Core.Configuration;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Rest.AMI.Configuration
{
    /// <summary>
    /// AMI Configuration
    /// </summary>
    [XmlType(nameof(AmiConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class AmiConfigurationSection : IConfigurationSection
    {
        /// <summary>
        /// Ami configuration section
        /// </summary>
        public AmiConfigurationSection()
        {
            this.Endpoints = new List<ServiceEndpointOptions>();
        }

        /// <summary>
        /// Resources on the AMI that are forbidden
        /// </summary>
        [XmlIgnore, Browsable(false)]
        public IEnumerable<Type> ResourceHandlers
        {
            get
            {
                var msb = new ModelSerializationBinder();
                return this.ResourceHandlerXml?.Select(o => msb.BindToType(null, o));
            }
        }

        /// <summary>
        /// Gets or sets the resource in xml format
        /// </summary>
        [XmlArray("resources"), XmlArrayItem("add"), Browsable(false)]
        public List<String> ResourceHandlerXml { get; set; }

        /// <summary>
        /// Gets or sets the public settings
        /// </summary>
        [XmlArray("publicSettings"), XmlArrayItem("add")]
        [DisplayName("Public Settings"), Description("Settings which are to be disclosed to clients which they can use for configuration")]
        public List<AppSettingKeyValuePair> PublicSettings { get; set; }


        /// <summary>
        /// Extra endpoints
        /// </summary>
        [XmlArray("endpoints"), XmlArrayItem("add")]
        [DisplayName("API Endpoints"), Description("The API endpoints which can't be auto-detected by the default AMI (if you're running in a distributed deployment). NOTE: DO NOT MODIFY THIS SETTING IF YOU'RE RUNNING THE ENTIRE INFRASTRUCTURE ON ONE SERVER")]
        public List<ServiceEndpointOptions> Endpoints { get; set; }
    }
}