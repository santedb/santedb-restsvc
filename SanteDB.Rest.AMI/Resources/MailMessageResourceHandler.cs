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
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Services;
using System.Collections.Specialized;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents an alert resource handler which can store / retrieve alerts
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class MailMessageResourceHandler : ResourceHandlerBase<MailMessage>
    {
        /// <summary>
        /// DI Constructor
        /// </summary>
        public MailMessageResourceHandler(ILocalizationService localizationService, IFreetextSearchService freetextSearchService, IRepositoryService<MailMessage> repositoryService, IAuditService auditService) : base(localizationService, freetextSearchService, repositoryService, auditService)
        {
        }

        /// <summary>
        /// Create the mail
        /// </summary>
        public override object Create(object data, bool updateIfExists)
        {
            if (data is MailMessage message)
            {
                if (!(AuthenticationContext.Current.Principal.Identity is IDeviceIdentity ||
                    AuthenticationContext.Current.Principal.Identity is IApplicationIdentity))
                {
                    message.From = AuthenticationContext.Current.Principal.Identity.Name;
                }
            }
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Query for mail messages should default to my messages
        /// </summary>
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            if (!queryParameters.TryGetValue("rcpt.userName", out _) && !queryParameters.TryGetValue("from", out _))
            {
                queryParameters.Add("rcpt.userName", "SYSTEM");
                queryParameters.Add("rcpt.userName", AuthenticationContext.Current.Principal.Identity.Name);
            }
            return base.Query(queryParameters);
        }
    }
}