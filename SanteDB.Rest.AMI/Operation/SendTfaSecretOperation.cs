/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.Common;
using System;

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
            if (parameters.TryGet<Guid>("mechanism", out var mechainsmId))
            {
                if (parameters.TryGet<String>("userName", out var user))
                {
                    var identity = this.m_identityProvider.GetIdentity(user);
                    return new ParameterCollection(new Parameter("challenge", this.m_tfaService.SendSecret(mechainsmId, identity)));
                }
                else
                {
                    return new ParameterCollection(new Parameter("challenge", this.m_tfaService.SendSecret(mechainsmId, AuthenticationContext.Current.Principal.Identity)));
                }
                return null;
            }
            else
            {
                throw new ArgumentNullException("mechanism required");
            }
        }
    }
}
