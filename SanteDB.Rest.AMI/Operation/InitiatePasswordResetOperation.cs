using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Initiate password reset
    /// </summary>
    public class InitiatePasswordResetOperation : IApiChildOperation
    {
  
        private readonly ITfaService m_tfaService;
        private readonly ISecurityRepositoryService m_securityRepository;
        private readonly ISecurityChallengeService m_securityChallenge;
        private readonly IIdentityProviderService m_identityProvider;

        /// <summary>
        /// DI constructor
        /// </summary>
        public InitiatePasswordResetOperation(ITfaService tfaService, ISecurityRepositoryService securityRepository, IIdentityProviderService identityProvider, ISecurityChallengeService securityChallengeService)
        {
            this.m_tfaService = tfaService;
            this.m_securityRepository = securityRepository;
            this.m_securityChallenge = securityChallengeService;
            this.m_identityProvider = identityProvider;
        }

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(SecurityUser) };

        /// <inheritdoc/>
        public string Name => "reset";

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(parameters.TryGet("userName", out string userName))
            {
                using (AuthenticationContext.EnterSystemContext())
                {
                    var user = this.m_securityRepository.GetUser(userName);
                    if (user == null)
                    {
                        throw new ArgumentOutOfRangeException(nameof(userName), ErrorMessages.PRINCIPAL_NOT_APPROPRIATE);
                    }

                    // TFA setup?
                    if (user.TwoFactorEnabled || !this.m_securityChallenge.Get(user.UserName, AuthenticationContext.Current.Principal).Any())
                    {
                        return new ParameterCollection(
                            new Parameter("text", this.m_tfaService.SendSecret(user.TwoFactorMechnaismKey, this.m_identityProvider.GetIdentity(userName))),
                            new Parameter("challenge", user.TwoFactorMechnaismKey)
                        );
                    }
                    else
                    {
                        var challenge = this.m_securityChallenge.Get(user.UserName, AuthenticationContext.Current.Principal).First();
                        return new ParameterCollection(
                            new Parameter("text", challenge.ChallengeText),
                            new Parameter("challenge", challenge.Key.Value)
                        );
                    }
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(userName));
            }
        }
    }
}
