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
 * Date: 2017-11-21
 */

using Newtonsoft.Json;
using System;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Diagnostics
{
	/// <summary>
	/// Diagnostic thread information
	/// </summary>
	[JsonObject(nameof(DiagnosticThreadInfo)), XmlType(nameof(DiagnosticThreadInfo), Namespace = "http://santedb.org/ami/diagnostics")]
	public class DiagnosticThreadInfo
	{
		/// <summary>
		/// Gets or sets the time the CPU has been running
		/// </summary>
		[XmlElement("cpuTime"), JsonProperty("cpuTime")]
		public TimeSpan CpuTime { get; set; }

		/// <summary>
		/// Gets or sets the name of the thread
		/// </summary>
		[XmlAttribute("name"), JsonProperty("name")]
		public string Name { get; set; }

		/// <summary>
		/// Get or set the state
		/// </summary>
		[XmlElement("state"), JsonProperty("state")]
		public string State { get; set; }

		/// <summary>
		/// Gets the task information
		/// </summary>
		[XmlAttribute("taskInfo"), JsonProperty("taskInfo")]
		public string TaskInfo { get; set; }

		/// <summary>
		/// Get or sets the wait reason
		/// </summary>
		[XmlAttribute("waitReason"), JsonProperty("waitReason")]
		public string WaitReason { get; set; }
	}
}