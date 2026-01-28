/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using SanteDB.Core.i18n;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents an alert resource handler which can store / retrieve alerts
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class MailSyncResourceHandler : ResourceHandlerBase<MailMessage>
    {
        private readonly IPolicyEnforcementService m_pepService;

        /// <summary>
        /// DI Constructor
        /// </summary>
        public MailSyncResourceHandler(ILocalizationService localizationService, IRepositoryService<MailMessage> repositoryService, IPolicyEnforcementService pepService, ISubscriptionExecutor subscriptionExecutor = null, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, subscriptionExecutor, freetextSearchService)
        {
            this.m_pepService = pepService;
        }

        /// <summary>
        /// Synchronize the mail
        /// </summary>
        public override string ResourceName => typeof(MailMessage).GetSerializationName();

        /// <summary>
        /// Create the mail
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageMail)]
        public override object Create(object data, bool updateIfExists)
        {
            this.ThrowIfNotDeviceOrApplication();
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Only devices and applications are allowed to sync the mail
        /// </summary>
        private void ThrowIfNotDeviceOrApplication()
        {
            if (!(AuthenticationContext.Current.Principal.Identity is IDeviceIdentity ||
                                AuthenticationContext.Current.Principal.Identity is IApplicationIdentity))
            {
                throw new InvalidOperationException(ErrorMessages.PRINCIPAL_NOT_APPROPRIATE);
            }
        }

        /// <summary>
        /// Query for mail messages should default to my messages
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageMail)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            this.ThrowIfNotDeviceOrApplication();
            return base.Query(queryParameters);
        }

        /// <summary>
        /// Manage mail
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageMail)]
        public override object Delete(object key)
        {
            this.ThrowIfNotDeviceOrApplication();
            return base.Delete(key);
        }

        /// <summary>
        /// Update mail message
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageMail)]
        public override object Update(object data)
        {
            this.ThrowIfNotDeviceOrApplication();
            return base.Update(data);
        }

        /// <summary>
        /// Get a specific mail message
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }
    }
}