using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Send a TFA secret
    /// </summary>
    public class SendTfaSecretOperation : IApiChildOperation
    {
        private readonly ITfaService m_tfaService;
        private readonly IIdentityProviderService m_identityProvider;

        /// <summary>
        /// DI constructor
        /// </summary>
        public SendTfaSecretOperation(ITfaService tfaService, IIdentityProviderService identityProvider)
        {
            this.m_tfaService = tfaService;
            this.m_identityProvider = identityProvider;
        }

        /// <summary>
        /// Scope of the operation
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <summary>
        /// Parent types
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(TfaMechanismInfo), typeof(SecurityUserInfo) };

        /// <summary>
        /// Get the name
        /// </summary>
        public string Name => "send";

        /// <summary>
        /// Invoke the method
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(parameters.TryGet<Guid>("mechanism", out var mechainsmId))
            {
                if (parameters.TryGet<String>("userName", out var user))
                {
                    var identity = this.m_identityProvider.GetIdentity(user);
                    this.m_tfaService.SendSecret(mechainsmId, identity);
                }
                else
                {
                    this.m_tfaService.SendSecret(mechainsmId, AuthenticationContext.Current.Principal.Identity);
                }
                return null;
            }
            else
            {
                throw new ArgumentNullException("mechanism and userName required");
            }
        }
    }
}
