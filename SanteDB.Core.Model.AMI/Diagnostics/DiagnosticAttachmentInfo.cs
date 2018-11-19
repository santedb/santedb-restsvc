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
	/// Runtime file inforamtion
	/// </summary>
	[JsonObject(nameof(DiagnosticAttachmentInfo)), XmlType(nameof(DiagnosticAttachmentInfo), Namespace = "http://santedb.org/ami/diagnostics")]
	public class DiagnosticAttachmentInfo
	{
		/// <summary>
		/// Description
		/// </summary>
		[JsonProperty("description"), XmlElement("description")]
		public String FileDescription { get; set; }

		/// <summary>
		/// Gets or sets the file name
		/// </summary>
		[JsonProperty("file"), XmlAttribute("file")]
		public String FileName { get; set; }

		/// <summary>
		/// Size of the file
		/// </summary>
		[JsonProperty("size"), XmlAttribute("size")]
		public long FileSize { get; set; }

		/// <summary>
		/// Gets or sets the identiifer
		/// </summary>
		[XmlAttribute("id"), JsonProperty("id")]
		public String Id { get; set; }

		/// <summary>
		/// Last write date
		/// </summary>
		[JsonProperty("lastWrite"), XmlAttribute("lastWrite")]
		public DateTime LastWriteDate { get; set; }
	}
}