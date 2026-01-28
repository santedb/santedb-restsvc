/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Data.Quality;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler that interacts with containers
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class ContainerResourceHandler : EntityResourceHandlerBase<Container>
    {

        /// <summary>
        /// DI constructor
        /// </summary>
        public ContainerResourceHandler(ILocalizationService localizationService, IRepositoryService<Container> repositoryService, IResourceCheckoutService resourceCheckoutService, ISubscriptionExecutor subscriptionExecutor = null, IFreetextSearchService freetextSearchService = null, IPrivacyEnforcementService privacyEnforcement = null) : base(localizationService, repositoryService, resourceCheckoutService, subscriptionExecutor, freetextSearchService, privacyEnforcement)
        {
        }

        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object Create(object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object Update(object data)
        {
            return base.Update(data);
        }

        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object Delete(object key)
        {
            return base.Delete(key);
        }

        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }

        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }
    }
}
