/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-5-19
 */
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler for entities.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class EntityResourceHandler : EntityResourceHandlerBase<Entity>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public EntityResourceHandler(ILocalizationService localizationService, IRepositoryService<Entity> repositoryService, IResourceCheckoutService resourceCheckoutService, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, resourceCheckoutService, freetextSearchService)
        {
        }

        /// <summary>
        /// Creates an entity.
        /// </summary>
        /// <param name="data">The entity to be created.</param>
        /// <param name="updateIfExists">Whether to update the entity if it exits.</param>
        /// <returns>Returns the created entity.s</returns>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public override Object Create(Object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Gets an entity by id and version id.
        /// </summary>
        /// <param name="id">The id of the entity.</param>
        /// <param name="versionId">The version id of the entity.</param>
        /// <returns>Returns the entity.</returns>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public override Object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }

        /// <summary>
        /// Obsoletes an entity.
        /// </summary>
        /// <param name="key">The key of the entity to be obsoleted.</param>
        /// <returns>Returns the obsoleted entity.</returns>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public override Object Delete(object key)
        {
            return base.Delete(key);
        }

        /// <summary>
        /// Queries for an entity.
        /// </summary>
        /// <param name="queryParameters">The query parameters to use to search for the entity.</param>
        /// <returns>Returns a list of entities.</returns>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }

        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="data">The entity to be updated.</param>
        /// <returns>Returns the updated entity.</returns>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public override Object Update(Object data)
        {
            return base.Update(data);
        }
    }
}