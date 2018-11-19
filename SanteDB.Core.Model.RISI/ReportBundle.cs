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
 * User: fyfej
 * Date: 2017-9-1
 */
using Newtonsoft.Json;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.RISI
{
	/// <summary>
	/// Represents a report bundle.
	/// </summary>
	/// <seealso cref="SanteDB.Core.Model.BaseEntityData" />
	[JsonObject]
	[XmlType(nameof(ReportBundle), Namespace = "http://santedb.org/risi")]
	[XmlRoot(nameof(ReportBundle), Namespace = "http://santedb.org/risi")]
	public class ReportBundle : BaseEntityData
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ReportBundle"/> class.
		/// </summary>
		public ReportBundle()
		{
		}

		/// <summary>
		/// Gets or sets the parameters.
		/// </summary>
		/// <value>The parameters.</value>
		public RisiCollection<ReportParameter> Parameters { get; set; }
	}
}