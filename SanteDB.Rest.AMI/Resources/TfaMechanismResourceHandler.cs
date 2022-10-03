using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// TFA mechanism resource handler
    /// </summary>
    public class TfaMechanismResourceHandler : IApiResourceHandler
    {
        private readonly ITfaRelayService m_tfaRelayService;

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName => "Tfa";

        /// <summary>
        /// Get the for this resource
        /// </summary>
        public Type Type => typeof(TfaMechanismInfo);

        /// <summary>
        /// Gets the scope of the api
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Get the capabilities
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search;

        /// <summary>
        /// DI CTOR
        /// </summary>
        public TfaMechanismResourceHandler(ITfaRelayService tfaRelay)
        {
            this.m_tfaRelayService = tfaRelay;
        }

        /// <inheritdoc/>
        public object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public object Delete(object key)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public object Get(object id, object versionId)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return this.m_tfaRelayService.Mechanisms.Select(o => new TfaMechanismInfo(o)).AsResultSet();
        }

        /// <inheritdoc/>
        public object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}
