using SanteDB.Core;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using ZXing;
using RestSrvr;
using System.Globalization;
using SanteDB.Core.Model.Acts;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// A REST API operation which sets up a TFA secret
    /// </summary>
    public class SetupTfaSecretOperation : IApiChildOperation
    {
        private readonly IIdentityProviderService m_identityProviderService;
        private readonly ITfaService m_tfaService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public SetupTfaSecretOperation(IIdentityProviderService identityProviderService, ITfaService tfaService)
        {
            this.m_identityProviderService = identityProviderService;
            this.m_tfaService = tfaService;
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
                                var writer = new BarcodeWriter<Bitmap>()
                                {
                                    Format = BarcodeFormat.QR_CODE,
                                    Options = new ZXing.Common.EncodingOptions()
                                    {
                                        Height = 150,
                                        Width = 150,
                                        NoPadding = true,
                                        PureBarcode = true
                                    },
                                    Renderer = new ZXing.Windows.Compatibility.BitmapRenderer()
                                };

                                using (var bmp = writer.Write(tfaSetupInstruction))
                                {
                                    using (var ms = new MemoryStream())
                                    {
                                        bmp.Save(ms, ImageFormat.Png);
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
