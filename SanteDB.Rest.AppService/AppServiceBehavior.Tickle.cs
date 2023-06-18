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
 * Date: 2023-5-19
 */
using RestSrvr;
using SanteDB.Client.Tickles;
using SanteDB.Core.Security;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior for tickles
    /// </summary>
    public partial class AppServiceBehavior
    {

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public void CreateTickle(Tickle data)
        {
            if (null == this.m_tickleService)
            {
                throw new NotSupportedException();
            }

            this.m_tickleService.SendTickle(data);
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public void DismissTickle(Guid id)
        {
            if (null == this.m_tickleService)
            {
                throw new NotSupportedException();
            }

            this.m_tickleService.DismissTickle(id);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public List<Tickle> GetTickles()
        {
            if (null == this.m_tickleService)
            {
                throw new NotSupportedException();
            }

            var userSid = this.m_securityRepositoryService.GetSid(AuthenticationContext.Current.Principal.Identity);
            return this.m_tickleService.GetTickles(o => o.Expiry > DateTime.Now && (o.Target == userSid || o.Target == null)).OrderByDescending(o => o.Created).Take(10).ToList();
        }

    }
}
