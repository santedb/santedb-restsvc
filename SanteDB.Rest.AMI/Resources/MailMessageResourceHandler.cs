/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2021-8-5
 */
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using System.Collections.Generic;
using SanteDB.Core.Services;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents an alert resource handler which can store / retrieve alerts
    /// </summary>
    public class MailMessageResourceHandler : ResourceHandlerBase<MailMessage>
    {

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

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="localizationService"></param>
        public MailMessageResourceHandler(ILocalizationService localizationService) : base(localizationService)
        {
        }
    }
}
