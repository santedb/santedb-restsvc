using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Util;
using SanteDB.Rest.WWW.Configuration;
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

        /// <summary>
        /// Applet collection
        /// </summary>
        public WebErrorBehavior()
        {
            var defaultSolution = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<WwwServiceConfigurationSection>()?.DefaultSolutionName;
            if(!String.IsNullOrEmpty(defaultSolution))
            {
                this.m_appletCollection = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>().GetApplets(defaultSolution);
            }
            if(defaultSolution == null)
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
                this.m_tracer.TraceWarning("{0} - {1}", RestOperationContext.Current?.EndpointOperation.Description?.InvokeMethod?.Name, error.Message);

                // The error content stream
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
                if(errorAsset != null)
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
                AuditUtil.AuditNetworkRequestFailure(error, RestOperationContext.Current.IncomingRequest.Url, RestOperationContext.Current.IncomingRequest.Headers, RestOperationContext.Current.OutgoingResponse.Headers);
                return true;
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Could not provide web fault: {0}", e);
                throw new InvalidOperationException(ErrorMessages.ERROR_PROVIDING_FAULT, e);
            }
        }
    }
}
