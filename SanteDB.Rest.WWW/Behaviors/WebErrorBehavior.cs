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
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Configuration;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.WWW.Behaviors
{
    /// <summary>
    /// A <see cref="IServiceBehavior"/> which provides error pages via HTML
    /// </summary>
    public class WebErrorBehavior : IServiceBehavior, IServiceErrorHandler
    {

        // Log Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(WebErrorBehavior));

        private readonly ReadonlyAppletCollection m_appletCollection;

        readonly IAuditService _AuditService;

        /// <summary>
        /// Applet collection
        /// </summary>
        public WebErrorBehavior()
        {
            _AuditService = ApplicationServiceContext.Current.GetAuditService();
            var defaultSolution = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AppletConfigurationSection>()?.DefaultSolution;
            if (!String.IsNullOrEmpty(defaultSolution))
            {
                this.m_appletCollection = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>()?.GetApplets(defaultSolution);
            }
            if (defaultSolution == null || this.m_appletCollection == null)
            {
                this.m_appletCollection = ApplicationServiceContext.Current.GetService<IAppletManagerService>().Applets;
            }
        }

        /// <inheritdoc cref="IServiceBehavior.ApplyServiceBehavior(RestService, ServiceDispatcher)"/>
        public void ApplyServiceBehavior(RestService service, ServiceDispatcher dispatcher)
        {
            dispatcher.ErrorHandlers.Clear();
            dispatcher.ErrorHandlers.Add(this);
        }

        /// <inheritdoc cref="IServiceErrorHandler.HandleError(Exception)"/>
        public bool HandleError(Exception error) => true;

        /// <inheritdoc cref="IServiceErrorHandler.ProvideFault(Exception, RestResponseMessage)"/>
        public bool ProvideFault(Exception error, RestResponseMessage response)
        {
            try
            {
                this.m_tracer.TraceWarning("{0} - {1}", RestOperationContext.Current?.EndpointOperation?.Description?.InvokeMethod?.Name, error.Message);

                // The error content stream
                response.StatusCode = error.GetHttpStatusCode();
                var errorAsset = this.m_appletCollection.GetErrorAsset(error.GetHttpStatusCode());
                var errorVariables = new Dictionary<String, String>()
                {
                    { "status", response.StatusCode.ToString() },
                    { "description", response.StatusDescription },
                    { "type", error.GetType().Name },
                    { "message", error.Message },
                    { "details", error.ToString() },
                    { "trace", error.StackTrace }
                };

                Stream errorPageStream = null;
                if (errorAsset != null)
                {
                    errorPageStream = new MemoryStream(this.m_appletCollection.RenderAssetContent(errorAsset, bindingParameters: errorVariables));
                }
                else
                {
                    using (var sr = new StreamReader(typeof(WebErrorBehavior).Assembly.GetManifestResourceStream("SanteDB.Rest.WWW.Resources.GenericError.html")))
                    {
                        var errorContent = sr.ReadToEnd();
                        errorVariables.ToList().ForEach(o => errorContent = errorContent.Replace($"{{{o.Key}}}", o.Value));
                        errorPageStream = new MemoryStream(Encoding.UTF8.GetBytes(errorContent));
                    }
                }

                response.Body = errorPageStream;
                response.ContentType = "text/html";
                _AuditService.Audit().ForNetworkRequestFailure(error, RestOperationContext.Current.IncomingRequest.Url, RestOperationContext.Current.IncomingRequest.Headers, RestOperationContext.Current.OutgoingResponse.Headers).Send();
                return true;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Could not provide web fault: {0}", e);
                throw new InvalidOperationException(ErrorMessages.ERROR_PROVIDING_FAULT, e);
            }
        }
    }
}
