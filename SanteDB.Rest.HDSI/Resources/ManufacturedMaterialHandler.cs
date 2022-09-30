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
    /// Represents a HDSI handler for manufactured materials
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class ManufacturedMaterialHandler : EntityResourceHandlerBase<ManufacturedMaterial>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public ManufacturedMaterialHandler(ILocalizationService localizationService, IFreetextSearchService freetextSearchService, IRepositoryService<ManufacturedMaterial> repositoryService) : base(localizationService, freetextSearchService, repositoryService)
        {
        }

        /// <summary>
        /// Create the specified material
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.WriteMaterials)]
        public override Object Create(Object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Gets the specified manufactured material
        /// </summary>
        /// <returns></returns>
        [Demand(PermissionPolicyIdentifiers.ReadMaterials)]
        public override Object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }

        /// <summary>
        /// Obsoletes the specified material
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.DeleteMaterials)]
        public override Object Delete(object key)
        {
            return base.Delete(key);
        }

        /// <summary>
        /// Query for the specified material
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMaterials)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }

        /// <summary>
        /// Update the specified material
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.WriteMaterials)]
        public override Object Update(Object data)
        {
            return base.Update(data);
        }
    }
}