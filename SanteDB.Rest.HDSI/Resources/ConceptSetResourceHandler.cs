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
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Resource handler for concept sets
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class ConceptSetResourceHandler : HdsiResourceHandlerBase<ConceptSet>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public ConceptSetResourceHandler(ILocalizationService localizationService, IFreetextSearchService freetextSearchService, IRepositoryService<ConceptSet> repositoryService, IAuditService auditService) : base(localizationService, freetextSearchService, repositoryService, auditService)
        {
        }

        /// <summary>
        /// Creates the specified data
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override Object Create(Object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Gets the specified conceptset
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override Object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }

        /// <summary>
        /// Obsolete the specified concept set
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override Object Delete(object key)
        {
            return base.Delete((Guid)key);
        }

        /// <summary>
        /// Perform query
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }

        /// <summary>
        /// Update the specified object
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override Object Update(Object data)
        {
            return base.Update(data);
        }
    }
}