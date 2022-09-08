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
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler for concept reference terms.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class ReferenceTermResourceHandler : HdsiResourceHandlerBase<ReferenceTerm>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public ReferenceTermResourceHandler(ILocalizationService localizationService, IFreetextSearchService freetextSearchService, IRepositoryService<ReferenceTerm> repositoryService) : base(localizationService, freetextSearchService, repositoryService)
        {
        }

        /// <summary>
        /// Gets the scope
        /// </summary>
        public override Type Scope => typeof(IHdsiServiceContract);

        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Create(object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }

        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Delete(object key)
        {
            return base.Delete(key);
        }

        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }

        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Update(object data)
        {
            return base.Update(data);
        }
    }
}