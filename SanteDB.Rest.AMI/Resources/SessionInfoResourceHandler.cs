using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Session information resource handler
    /// </summary>
    public class SessionInfoResourceHandler : IApiResourceHandler
    {
        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName => "SessionInfo";

        /// <summary>
        /// Gets the type that this resource handler returns
        /// </summary>
        public Type Type => typeof(SecuritySessionInfo);

        /// <summary>
        /// Gets the scoped object
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the capabilities for this object
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Get | ResourceCapabilityType.Delete;

        /// <summary>
        /// Create an object
        /// </summary>
        public object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the specified session object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public object Get(object id, object versionId)
        {
            var uuid = (Guid)id;
            var session = ApplicationServiceContext.Current.GetService<ISessionProviderService>().Get(uuid.ToByteArray(), true);
            if (session == null)
                throw new KeyNotFoundException($"Session {uuid} not found");
            return new SecuritySessionInfo(session);
        }

        /// <summary>
        /// Obsolete the specified session
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public object Obsolete(object key)
        {
            var uuid = (Guid)key;
            var session = ApplicationServiceContext.Current.GetService<ISessionProviderService>().Get(uuid.ToByteArray(), false);
            if (session == null)
                throw new KeyNotFoundException($"Session {uuid} not found");
            ApplicationServiceContext.Current.GetService<ISessionProviderService>().Abandon(session);
            return null;
        }

        /// <summary>
        /// Query for sessions
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Query for sessions
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Update the specified session
        /// </summary>
        public object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}
