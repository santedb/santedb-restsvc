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
 * 
 * User: fyfej
 * Date: 2023-6-21
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using System;
using System.Collections.Specialized;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Represents a resource handler.
    /// </summary>
    public interface IApiResourceHandler
    {


        /// <summary>
        /// Gets the name of the resource which the resource handler supports.
        /// </summary>
        string ResourceName { get; }

        /// <summary>
        /// Gets the type which the resource handler supports.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Get the scope of this resource handler (the service to which the resources are bound)
        /// </summary>
        Type Scope { get; }

        /// <summary>
        /// Gets the capabilities of this service
        /// </summary>
        ResourceCapabilityType Capabilities { get; }

        /// <summary>
        /// Creates a resource.
        /// </summary>
        /// <param name="data">The resource data to be created.</param>
        /// <param name="updateIfExists">Updates the resource if the resource exists.</param>
        /// <returns>Returns the created resource.</returns>
        Object Create(Object data, bool updateIfExists);

        /// <summary>
        /// Gets a specific resource instance.
        /// </summary>
        /// <param name="id">The id of the resource.</param>
        /// <param name="versionId">The version id of the resource.</param>
        /// <returns>Returns the resource.</returns>
        Object Get(Object id, Object versionId);

        /// <summary>
        /// Delete a resource.
        /// </summary>
        /// <param name="key">The key of the resource to delete.</param>
        /// <returns>Returns the deleted resource.</returns>
        /// <remarks>This functionality has changed since SanteDB 2.x</remarks>
        Object Delete(Object key);

        /// <summary>
        /// Queries for a resource.
        /// </summary>
        /// <param name="queryParameters">The query parameters of the resource.</param>
        /// <returns>Returns a collection of resources.</returns>
        IQueryResultSet Query(NameValueCollection queryParameters);

        /// <summary>
        /// Updates a resource.
        /// </summary>
        /// <param name="data">The resource data to be updated.</param>
        /// <returns>Returns the updated resource.</returns>
        Object Update(Object data);
    }
}