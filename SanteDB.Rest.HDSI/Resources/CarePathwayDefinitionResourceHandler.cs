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
 * Date: 2024-12-12
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Care pathway definition resource handler
    /// </summary>
    public class CarePathwayDefinitionResourceHandler : HdsiResourceHandlerBase<CarePathwayDefinition>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public CarePathwayDefinitionResourceHandler(ILocalizationService localizationService, IRepositoryService<CarePathwayDefinition> repositoryService, IResourceCheckoutService resourceCheckoutService, ISubscriptionExecutor subscriptionExecutor, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, resourceCheckoutService, subscriptionExecutor, freetextSearchService)
        {
        }

        /// <inheritdoc/>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Get | ResourceCapabilityType.Search | ResourceCapabilityType.Create | ResourceCapabilityType.CreateOrUpdate | ResourceCapabilityType.Update | ResourceCapabilityType.Delete;


        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override object Create(object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override object Delete(object key)
        {
            return base.Delete(key);
        }

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override object Update(object data)
        {
            return base.Update(data);
        }

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }
    }
}
