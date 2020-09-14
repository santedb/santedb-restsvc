/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using System.Collections.Generic;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Principal;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents an alert resource handler which can store / retrieve alerts
    /// </summary>
    public class MailMessageResourceHandler : ResourceHandlerBase<MailMessage>
    {

        /// <summary>
        /// Create the mail
        /// </summary>
        public override object Create(object data, bool updateIfExists)
        {
            if (data is MailMessage message)
            {
                if(!(AuthenticationContext.Current.Principal.Identity is IDeviceIdentity ||
                    AuthenticationContext.Current.Principal.Identity is IApplicationIdentity))
                    message.From = AuthenticationContext.Current.Principal.Identity.Name;
            }
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Query for mail messages should default to my messages
        /// </summary>
        public override IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            if (!queryParameters.ContainsKey("rcpt.userName") && !queryParameters.ContainsKey("from"))
            {
                queryParameters.Add("rcpt.userName", new List<string>() { "SYSTEM", AuthenticationContext.Current.Principal.Identity.Name });
            }
            return base.Query(queryParameters, offset, count, out totalCount);
        }
    }
}
