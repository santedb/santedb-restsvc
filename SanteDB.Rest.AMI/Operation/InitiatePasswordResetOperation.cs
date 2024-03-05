/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-6-21
 */
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Security.Tfa;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Linq;

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
            if (parameters.TryGet("userName", out string userName))
            {
                using (AuthenticationContext.EnterSystemContext())
                {
                    var user = this.m_securityRepository.GetUser(userName);
                    if (user == null)
                    {
                        throw new ArgumentOutOfRangeException(nameof(userName), ErrorMessages.PRINCIPAL_NOT_APPROPRIATE);
                    }

                    // TFA setup?
                    var challengeQuestions = this.m_securityChallenge.Get(user.UserName, AuthenticationContext.Current.Principal);
                    if (this.m_tfaService.Mechanisms.Any() && (user.TwoFactorEnabled || !challengeQuestions.Any()))
                    {
                        var mechanism = user.TwoFactorMechnaismKey.GetValueOrDefault();
                        if (mechanism == Guid.Empty)
                        {
                            mechanism = TfaEmailMechanism.MechanismId;
                        }

                        return new ParameterCollection(
                            new Parameter("text", this.m_tfaService.SendSecret(mechanism, this.m_identityProvider.GetIdentity(userName))),
                            new Parameter("challenge", mechanism)
                        );
                    }
                    else if (challengeQuestions.Any())
                    {
                        var challenge = challengeQuestions.First();
                        return new ParameterCollection(
                            new Parameter("text", challenge.ChallengeText),
                            new Parameter("challenge", challenge.Key.Value)
                        );
                    }
                    else
                    {
                        throw new InvalidOperationException(ErrorMessages.NO_RESET_POSSIBLE);
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
