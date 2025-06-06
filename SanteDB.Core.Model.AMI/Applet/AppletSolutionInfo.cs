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
using SanteDB.Core.Applets.Model;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Applet
{
    /// <summary>
    /// Represents meta information about a solution
    /// </summary>
    [XmlType(nameof(AppletSolutionInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(AppletSolutionInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(AppletSolutionInfo))]
    public class AppletSolutionInfo : AppletManifestInfo
    {

        /// <summary>
        /// Applet solution info
        /// </summary>
        public AppletSolutionInfo()
        {

        }

        /// <summary>
		/// Initializes a new instance of the <see cref="AppletSolutionInfo"/> class
		/// with a specific applet manifest instance.
		/// </summary>
		public AppletSolutionInfo(AppletSolution soln) : base(soln)
        {
            this.Include = soln.Include.Select(s => new AppletManifestInfo(s)).ToList();
        }

        /// <summary>
        /// Gets the data this includes
        /// </summary>
        [XmlElement("include"), JsonProperty("include")]
        public List<AppletManifestInfo> Include { get; set; }
    }
}
