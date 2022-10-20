using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Tickles;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.AppService.Model;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AppService.Resources
{
    internal class TickleResourceHandler : IApiResourceHandler
    {
        readonly ITickleService _TickleService;
        readonly ISecurityRepositoryService _SecurityRepository;

        public TickleResourceHandler()
        {
            _TickleService = ApplicationServiceContext.Current.GetService<ITickleService>();
            _SecurityRepository = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>();
        }

        public string ResourceName => nameof(Tickle);

        public Type Type => typeof(Tickle);

        public Type Scope => typeof(IAppServiceContract);

        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get | ResourceCapabilityType.Create | ResourceCapabilityType.Delete;

        [Demand(PermissionPolicyIdentifiers.Login)]
        public object Create(object data, bool updateIfExists)
        {
            if (data is Tickle tickle)
            {
                _TickleService?.SendTickle(tickle);
                RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                return null;
            }
            else
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = 400;
                return null;
            }
        }

        [Demand(PermissionPolicyIdentifiers.Login)]
        public object Delete(object key)
        {
            if (key is Guid tickleid)
            {
                _TickleService?.DismissTickle(tickleid);
                RestOperationContext.Current.OutgoingResponse.StatusCode = 204;
                return null;
            }
            else
            {
                return null;
            }
        }


        [Demand(PermissionPolicyIdentifiers.Login)]
        public object Get(object id, object versionId)
        {
            //Needed for rest metadata to work corectly.
            throw new NotImplementedException();
        }

        [Demand(PermissionPolicyIdentifiers.Login)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            var user = _SecurityRepository?.GetUser(AuthenticationContext.Current.Principal.Identity);

            if (null != user)
            {
                var tickles = _TickleService?.GetTickles(t => t.Expiry >= DateTime.Now && (t.Target == Guid.Empty || t.Target == user.Key));

                if (null != tickles)
                {
                    return new MemoryQueryResultSet<Tickle>(tickles);
                }
                else
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 204; //TODO: Better HTTP status?
                    return null;
                }
            }
            else
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = 403;
                return null;
            }
        }

        public object Update(object data)
        {
            throw new NotImplementedException();
        }
    }
}
