﻿/*
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
 * User: khannan
 * Date: 2017-9-1
 */

using Newtonsoft.Json;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Model.AMI.Security;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Applet
{
	/// <summary>
	/// Represents a wrapper for the <see cref="Applets.Model.AppletManifest"/> class.
	/// </summary>
	[XmlType(nameof(AppletManifestInfo), Namespace = "http://santedb.org/ami")]
	[XmlRoot(nameof(AppletManifestInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(AppletManifestInfo))]
	public class AppletManifestInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AppletManifestInfo"/> class.
		/// </summary>
		public AppletManifestInfo()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AppletManifestInfo"/> class
		/// with a specific applet manifest instance.
		/// </summary>
		/// <param name="info">The applet manifest metadata instance.</param>
        /// <param name="publisher">The publisher of the applet</param>
		public AppletManifestInfo(AppletInfo info, X509Certificate2Info publisher)
		{
			this.AppletInfo = info;
			this.PublisherData = publisher;
		}

		/// <summary>
		/// Gets the applet information name
		/// </summary>
		[XmlElement("applet"), JsonProperty("applet")]
		public AppletInfo AppletInfo { get; set; }

		/// <summary>
		/// Publisher information if available
		/// </summary>
		[XmlElement("publisher"), JsonProperty("publisher")]
		public X509Certificate2Info PublisherData { get; set; }
	}
}