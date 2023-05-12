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
 * Date: 2023-3-10
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents an alert resource handler which can store / retrieve alerts
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class MailboxMessageHandler : ChainedResourceHandlerBase
    {
        private readonly IMailMessageService m_mailMessageService;
        private readonly ISecurityRepositoryService m_securityRepository;
        private readonly IPolicyEnforcementService m_pepService;

        /// <summary>
        /// DI Constructor
        /// </summary>
        public MailboxMessageHandler(ILocalizationService localizationService, IMailMessageService mailMessageService, IPolicyEnforcementService pepService, ISecurityRepositoryService securityRepositoryService) : base(localizationService)
        {
            this.m_mailMessageService = mailMessageService;
            this.m_securityRepository = securityRepositoryService;
            this.m_pepService = pepService;
        }

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public override string ResourceName => "Mailbox";

        /// <summary>
        /// Get the type
        /// </summary>
        public override Type Type => typeof(Mailbox);

        /// <summary>
        /// Get the scope
        /// </summary>
        public override Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Get capabilities
        /// </summary>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get | ResourceCapabilityType.Create | ResourceCapabilityType.Delete;

        /// <summary>
        /// Create the mailbox
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageMail)] // Login only since the user can manage their own mailboxes
        public override object Create(object data, bool updateIfExists)
        {
            if (!(data is Mailbox mailbox))
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }

            var ownerKey = mailbox.OwnerKey == Guid.Empty ?
                this.m_securityRepository.GetSid(AuthenticationContext.Current.Principal.Identity) : mailbox.OwnerKey;
            return this.m_mailMessageService.CreateMailbox(mailbox.Name, ownerKey);
        }

        /// <summary>
        /// Delete mailbox
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageMail)]
        public override object Delete(object key)
        {
            return this.m_mailMessageService.DeleteMailbox(key.ToString());
        }

        /// <summary>
        /// Get the mailbox
        /// </summary>
        public override object Get(object id, object versionId)
        {
            if (id is String str)
            {
                return this.m_mailMessageService.GetMailbox(str);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
        }

        /// <summary>
        /// Query mailboxes
        /// </summary>
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {

            var query = QueryExpressionParser.BuildLinqExpression<Mailbox>(queryParameters);
            if (queryParameters.TryGetValue("owner", out var values) && Guid.TryParse(values.Single(), out var ownerKey))
            {
                return this.m_mailMessageService.GetMailboxes(ownerKey).Where(query);
            }
            else
            {
                return this.m_mailMessageService.GetMailboxes().Where(query);
            }
        }

        /// <summary>
        /// Update the mailbox
        /// </summary>
        public override object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}