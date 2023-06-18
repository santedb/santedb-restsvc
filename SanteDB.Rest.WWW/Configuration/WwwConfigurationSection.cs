﻿/*
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
using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
