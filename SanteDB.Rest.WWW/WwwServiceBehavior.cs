using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Configuration;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Behaviors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SanteDB.Rest.WWW
{
    /// <summary>
    /// Service behavior which renders WWW content from the the applets installed on the server
    /// </summary>
    public class WwwServiceBehavior : IWwwServiceContract
    {
        // Cached applets
        private readonly ConcurrentDictionary<String, AppletAsset> m_cacheApplets = new ConcurrentDictionary<string, AppletAsset>();
        private readonly AppletConfigurationSection m_configuration;
        private readonly IPolicyEnforcementService m_policyEnforcementSerivce;
        private readonly IAppletSolutionManagerService m_appletSolutionManager;
        private readonly ReadonlyAppletCollection m_serviceApplet;

        /// <summary>
        /// Web page service behavior
        /// </summary>
        public WwwServiceBehavior()
        {
            this.m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AppletConfigurationSection>();
            this.m_policyEnforcementSerivce = ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>();
            this.m_appletSolutionManager = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>();
            if (String.IsNullOrEmpty(this.m_configuration.DefaultApplet) || this.m_appletSolutionManager == null)
            {
                this.m_serviceApplet = ApplicationServiceContext.Current.GetService<IAppletManagerService>().Applets;
            }
            else
            {
                this.m_serviceApplet = this.m_appletSolutionManager.GetApplets(this.m_configuration.DefaultSolution);
            }

            // Set the default 
            this.m_serviceApplet.DefaultApplet = this.m_serviceApplet.FirstOrDefault(o => o.Info.Id == (this.m_configuration.DefaultApplet ?? "org.santedb.uicore"));
        }

        /// <summary>
        /// Throw exception if not running
        /// </summary>
        private void ThrowIfNotRunning()
        {
            if (!ApplicationServiceContext.Current.IsRunning)
            {
                throw new DomainStateException();
            }
        }

        /// <summary>
        /// Get the icon for the SanteDB server
        /// </summary>
        public Stream GetIcon()
        {

            this.ThrowIfNotRunning();

            // Does the applet have a favicon?
            if (this.m_serviceApplet.TryResolveApplet("favicon.ico", out var icon))
            {
                return new MemoryStream(this.m_serviceApplet.RenderAssetContent(icon));
            }
            else
            {
                var ms = new MemoryStream();
                typeof(WwwServiceBehavior).Assembly.GetManifestResourceStream("SanteDB.Rest.WWW.Resources.icon.ico").CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            }
        }

        /// <summary>
        /// Get the asset
        /// </summary>
        public Stream Get()
        {
            this.ThrowIfNotRunning();

            if (RestOperationContext.Current.Data.TryGetValue("lang", out object lang) && lang is String[] langList)
            {
                lang = langList[0];
            }
            else if (AuthenticationContext.Current.Principal is IClaimsPrincipal icp)
            {
                lang = icp.GetClaimValue(SanteDBClaimTypes.Language);
            }
            else
            {
                lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            }

            var etag = $"{String.Join(";", this.m_serviceApplet.Select(o => o.Info.Version))}/{lang}";

            if (RestOperationContext.Current.IncomingRequest.Headers["If-None-Match"] == etag)
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = 304; // not modified
                return null;
            }

            // Navigate asset

            String appletPath = RestOperationContext.Current.IncomingRequest.Url.AbsolutePath.ToLower();
            if (String.IsNullOrEmpty(appletPath) || "/".Equals(appletPath))
            {
                appletPath = "index.html";
            }

            if (!m_cacheApplets.TryGetValue(appletPath, out var navigateAsset) &&
                this.m_serviceApplet.DefaultApplet?.TryGetAsset(appletPath, out navigateAsset) != true &&
                !this.m_serviceApplet.TryResolveApplet(appletPath, out navigateAsset))
            {
                throw new FileNotFoundException(appletPath);
            }
            else
            {
                if (this.m_serviceApplet.CachePages)
                {
                    this.m_cacheApplets.TryAdd(appletPath, navigateAsset);
                }
            }

            // Navigate policy?
            navigateAsset.Policies?.ForEach(o => this.m_policyEnforcementSerivce.Demand(o, AuthenticationContext.Current.Principal));

            // Caching
            RestOperationContext.Current.OutgoingResponse.AddHeader("ETag", etag);
            RestOperationContext.Current.OutgoingResponse.ContentType = navigateAsset.MimeType;

            // Write asset
            var content = this.m_serviceApplet.RenderAssetContent(navigateAsset, lang?.ToString(), bindingParameters: new Dictionary<String, String>()
            {
                { "csp_nonce", RestOperationContext.Current.ServiceEndpoint.Behaviors.OfType<SecurityPolicyHeadersBehavior>().FirstOrDefault()?.Nonce },
#if DEBUG
                { "env_type", "debug" },
#else
                { "env_type", "release" },
#endif
                { "host_type", ApplicationServiceContext.Current.HostType.ToString() }
            });
            return new MemoryStream(content);

        }

    }
}
