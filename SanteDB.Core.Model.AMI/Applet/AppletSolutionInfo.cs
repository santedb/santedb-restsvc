/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using Newtonsoft.Json;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Model.AMI.Security;
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
		public AppletSolutionInfo(AppletSolution soln, X509Certificate2Info publisher) : base(soln.Meta, publisher)
        {
            this.Include = soln.Include.Select(s => new AppletManifestInfo(s.Meta, null)).ToList();
        }

        /// <summary>
        /// Gets the data this includes
        /// </summary>
        [XmlElement("include"), JsonProperty("include")]
        public List<AppletManifestInfo> Include { get; set; }
    }
}
