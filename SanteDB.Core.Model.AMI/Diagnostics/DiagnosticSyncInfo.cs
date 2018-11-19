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
using System;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Diagnostics
{
	/// <summary>
	/// Remote sync info
	/// </summary>
	[JsonObject("RemoteSyncInfo"), XmlType(nameof(DiagnosticSyncInfo), Namespace = "http://santedb.org/ami/diagnostics")]
	public class DiagnosticSyncInfo
	{
        /// <summary>
        /// Gets or sets the last etag of the sync
        /// </summary>
		[JsonProperty("etag"), XmlAttribute("etag")]
		public String Etag { get; set; }

		/// <summary>
		/// Filter used to sync
		/// </summary>
		[JsonProperty("filter"), XmlText]
		public String Filter { get; set; }

        /// <summary>
        /// Gets or sets the last sync time
        /// </summary>
		[JsonProperty("lastSync"), XmlAttribute("lastSync")]
		public DateTime LastSync { get; set; }

		/// <summary>
		/// Friendly name of the sync
		/// </summary>
		[JsonProperty("name"), XmlAttribute("name")]
		public string Name { get; set; }

        /// <summary>
        /// Gets or sets the resource name
        /// </summary>
		[JsonProperty("resource"), XmlAttribute("resource")]
		public String ResourceName { get; set; }
	}
}