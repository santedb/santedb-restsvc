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

namespace SanteDB.Core.Model.AMI.Security
{
	/// <summary>
	/// Submission request
	/// </summary>
	[XmlType(nameof(SubmissionRequest), Namespace = "http://santedb.org/ami")]
	[XmlRoot(nameof(SubmissionRequest), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(SubmissionRequest))]
	public class SubmissionRequest
	{
		/// <summary>
		/// Gets or sets the admin address
		/// </summary>
		[XmlElement("address"), JsonProperty("address")]
		public String AdminAddress { get; set; }

		/// <summary>
		/// Gets or sets the contact name
		/// </summary>
		[XmlElement("contact"), JsonProperty("contact")]
		public String AdminContactName { get; set; }

		/// <summary>
		/// Gets or sets the cmc request
		/// </summary>
		[XmlElement("cmc"), JsonProperty("cmc")]
		public String CmcRequest { get; set; }
	}
}