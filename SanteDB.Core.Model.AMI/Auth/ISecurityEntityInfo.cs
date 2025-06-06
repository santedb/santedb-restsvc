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
using SanteDB.Core.Model.Interfaces;
using System.Collections.Generic;

namespace SanteDB.Core.Model.AMI.Auth
{

    /// <summary>
    /// Security entity information
    /// </summary>
    public interface ISecurityEntityInfo
    {
        /// <summary>
        /// Gets the entity
        /// </summary>
        IdentifiedData ToIdentifiedData();
    }

    /// <summary>
    /// Represents an object that provide a wrapper for a security info
    /// </summary>
    public interface ISecurityEntityInfo<TSecurityObject> : IAmiIdentified, IIdentifiedResource, ISecurityEntityInfo
        where TSecurityObject : NonVersionedEntityData
    {


        /// <summary>
        /// Gets or sets the security object entity
        /// </summary>
        TSecurityObject Entity { get; set; }

        /// <summary>
        /// Gets the policies for the object
        /// </summary>
        List<SecurityPolicyInfo> Policies { get; set; }
    }
}
