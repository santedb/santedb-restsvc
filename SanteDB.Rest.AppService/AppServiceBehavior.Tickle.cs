using System.Linq;
using RestSrvr;
using SanteDB.Client.Tickles;
using SanteDB.Core.Security;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

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
            this.m_tickleService?.SendTickle(data);
            RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public void DismissTickle(Guid id)
        {
            this.m_tickleService?.DismissTickle(id);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public List<Tickle> GetTickles()
        {
            var userSid = this.m_identityProvider.GetSid(AuthenticationContext.Current.Principal.Identity.Name);
            return this.m_tickleService.GetTickles(o => o.Expiry > DateTime.Now && (o.Target == userSid || o.Target == null)).OrderByDescending(o => o.Created).Take(10).ToList();
        }

    }
}
