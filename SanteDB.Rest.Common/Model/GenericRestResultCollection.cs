/*
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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Model
{
    /// <summary>
    /// Generic rest result which has a wrapped simple type
    /// </summary>
    [XmlType(nameof(GenericRestResultCollection), Namespace = "http://santedb.org/model")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public class GenericRestResultCollection
    {
        /// <summary>
        /// Gets the values in the result collection
        /// </summary>
        [XmlElement("int", typeof(Int32)),
            XmlElement("string", typeof(String)),
            XmlElement("bool", typeof(bool)),
            XmlElement("float", typeof(float)),
            XmlElement("guid", typeof(Guid)),
            JsonProperty("values")]
        public List<object> Values { get; set; }
    }
}