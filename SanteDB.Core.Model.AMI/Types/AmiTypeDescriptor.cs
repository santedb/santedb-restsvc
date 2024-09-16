/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 */
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Types
{
    /// <summary>
    /// Represents a type descriptor (name + type)
    /// </summary>
    [XmlType(nameof(AmiTypeDescriptor), Namespace = "http://santedb.org/ami"), JsonObject(nameof(AmiTypeDescriptor))]
    public class AmiTypeDescriptor
    {

        /// <summary>
        /// Serialization CTOR
        /// </summary>
        public AmiTypeDescriptor()
        {

        }

        /// <summary>
        /// Create new type descriptor with the specified metadata from the <paramref name="type"/>
        /// </summary>
        public AmiTypeDescriptor(Type type)
        {
            this.TypeAqn = type.AssemblyQualifiedName;
            this.DisplayName = type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? type.Name;
        }

        /// <summary>
        /// Create new type description with specified <paramref name="type"/> and <paramref name="displayName"/>
        /// </summary>
        public AmiTypeDescriptor(Type type, String displayName)
        {
            this.TypeAqn = type.AssemblyQualifiedName;
            this.DisplayName = displayName;
        }

        /// <summary>
        /// Type AQN
        /// </summary>
        [XmlElement("aqn"), JsonProperty("aqn")]
        public String TypeAqn { get; set; }

        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        [XmlElement("display"), JsonProperty("display")]
        public String DisplayName { get; set; }

    }
}
