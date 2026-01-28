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
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using ZXing;
using ZXing.ImageSharp;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// A REST API operation which sets up a TFA secret
    /// </summary>
    public class SetupTfaSecretOperation : IApiChildOperation
    {
        private readonly IIdentityProviderService m_identityProviderService;
        private readonly ITfaService m_tfaService;
        private readonly ISecurityRepositoryService m_securityRepositoryService;
        private readonly IRepositoryService<SecurityUser> m_securityUserRepository;

        /// <summary>
        /// DI constructor
        /// </summary>
        public SetupTfaSecretOperation(IIdentityProviderService identityProviderService, 
            ITfaService tfaService, 
            ISecurityRepositoryService securityRepositoryService,
            IRepositoryService<SecurityUser> securityUserRepository)
        {
            this.m_identityProviderService = identityProviderService;
            this.m_tfaService = tfaService;
            this.m_securityRepositoryService = securityRepositoryService;
            this.m_securityUserRepository = securityUserRepository;
        }

        /// <inheritdoc/>
        public string Name => "setup";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(TfaMechanismInfo) };

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (scopingKey is Guid mechanismId || Guid.TryParse(scopingKey.ToString(), out mechanismId))
            {
                var mechanism = this.m_tfaService.Mechanisms.FirstOrDefault(o => o.Id == mechanismId);
                if (mechanism == null)
                {
                    throw new KeyNotFoundException(mechanismId.ToString());
                }

                if (parameters.TryGet("code", out String validationCode) && !String.IsNullOrEmpty(validationCode))
                {
                    if (!mechanism.EndSetup(AuthenticationContext.Current.Principal.Identity, validationCode))
                    {
                        throw new ArgumentException("code");
                    }
                    var user = this.m_securityRepositoryService.GetUser(AuthenticationContext.Current.Principal.Identity);
                    user.TwoFactorEnabled = true;
                    user.TwoFactorMechnaismKey = mechanism.Id;
                    this.m_securityUserRepository.Save(user);
                    return null;
                }
                else
                {
                    var tfaSetupInstruction = mechanism.BeginSetup(AuthenticationContext.Current.Principal.Identity);
                    Narrative retVal = null;
                    switch (mechanism.Classification)
                    {
                        case TfaMechanismClassification.Application:
                            {
                                // Now generate the token
                                var writer = new ZXing.ImageSharp.BarcodeWriter<Rgba32>()
                                {
                                    Format = BarcodeFormat.QR_CODE,
                                    Options = new ZXing.Common.EncodingOptions()
                                    {
                                        Height = 150,
                                        Width = 150,
                                        NoPadding = true,
                                        PureBarcode = true
                                    }
                                };

                                using (var bmp = writer.Write(tfaSetupInstruction))
                                {
                                    using (var ms = new MemoryStream())
                                    {
                                        bmp.Save(ms, new PngEncoder());
                                        ms.Seek(0, SeekOrigin.Begin);
                                        retVal = Narrative.DocumentFromStream("TFA", CultureInfo.CurrentCulture.TwoLetterISOLanguageName, "image/png", ms);
                                        break;
                                    }
                                }
                            }
                        case TfaMechanismClassification.Message:
                            retVal = Narrative.DocumentFromString("TFA", CultureInfo.CurrentCulture.TwoLetterISOLanguageName, "text/plain", tfaSetupInstruction);
                            break;
                        default:
                            throw new InvalidOperationException(String.Format(ErrorMessages.ARGUMENT_OUT_OF_RANGE, mechanism.Classification));
                    }
                    return retVal;
                }
            }
            else
            {
                throw new ArgumentNullException("mechanism");
            }
        }
    }
}
