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
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Resource handler for sbadm
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class SubstanceAdministrationResourceHandler : HdsiResourceHandlerBase<SubstanceAdministration>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public SubstanceAdministrationResourceHandler(ILocalizationService localizationService, IRepositoryService<SubstanceAdministration> repositoryService, IResourceCheckoutService resourceCheckoutService, ISubscriptionExecutor subscriptionExecutor = null, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, resourceCheckoutService, subscriptionExecutor, freetextSearchService)
        {
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public override Object Create(Object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadClinicalData)]
        public override Object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.DeleteClinicalData)]
        public override Object Delete(object key)
        {
            return base.Delete(key);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.QueryClinicalData)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public override Object Update(Object data)
        {
            return base.Update(data);
        }
    }
}