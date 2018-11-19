using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.AMI.Applet;
using SanteDB.Core.Applets.Model;
using System.IO;
using SanteDB.Core.Applets.Services;
using System.Security.Cryptography.X509Certificates;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Rest.Common.Attributes;
using SanteDB.Core.Security;
using SanteDB.Core;
using RestSrvr;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler that handles applets
    /// </summary>
    public class AppletResourceHandler : IResourceHandler
    {
        /// <summary>
        /// Gets the capabilities of the resource handler
        /// </summary>
        public ResourceCapability Capabilities
        {
            get
            {
                return ResourceCapability.Create | ResourceCapability.Update | ResourceCapability.Get | ResourceCapability.Search;
            }
        }

        /// <summary>
        /// Gets the name of the resource
        /// </summary>
        public string ResourceName
        {
            get
            {
                return "Applet";
            }
        }

        /// <summary>
        /// Get the scope of this object
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Get the type of this
        /// </summary>
        public Type Type => typeof(Stream);

        /// <summary>
        /// Create / install an applet on the server
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public object Create(object data, bool updateIfExists)
        {
            var pkg = AppletPackage.Load((Stream)data);
            ApplicationServiceContext.Current.GetService<IAppletManagerService>().Install(pkg);
            X509Certificate2 cert = null;
            if (pkg.PublicKey != null)
                cert = new X509Certificate2(pkg.PublicKey);
            else if (pkg.Meta.PublicKeyToken != null)
            {
                X509Store store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
                try
                {
                    store.Open(OpenFlags.ReadOnly);
                    var results = store.Certificates.Find(X509FindType.FindByThumbprint, pkg.Meta.PublicKeyToken, false);
                    if (results.Count > 0)
                        cert = results[0];
                }
                finally
                {
                    store.Close();
                }
            }
            return new AppletManifestInfo(pkg.Meta, new X509Certificate2Info(cert?.Issuer, cert?.NotBefore, cert?.NotAfter, cert?.Subject, cert?.Thumbprint));
        }

        /// <summary>
        /// Gets the contents of the applet with the specified ID
        /// </summary>
        /// <param name="appletId">The identifier of the applet to be loaded</param>
        /// <param name="versionId">The version of the applet</param>
        /// <returns></returns>
        public object Get(Object appletId, Object versionId)
        {
            var appletService = ApplicationServiceContext.Current.GetService<IAppletManagerService>();
            var appletData = appletService.GetPackage(appletId.ToString());

            if (appletData == null)
                throw new FileNotFoundException(appletId.ToString());
            else
            {
                var appletManifest = AppletPackage.Load(appletData);
                this.SetAppletHeaders(appletManifest.Meta);
                return new MemoryStream(appletData);
            }
        }

        /// <summary>
        /// Obsoletes the specified applet
        /// </summary>
        /// <param name="appletId">The identifier of the applet to uninstall</param>
        /// <returns>Null</returns>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public object Obsolete(object appletId)
        {
            ApplicationServiceContext.Current.GetService<IAppletManagerService>().UnInstall(appletId.ToString());
            return null;
        }

        /// <summary>
        /// Perform a query of applets
        /// </summary>
        /// <param name="queryParameters">The filter to apply to the applet</param>
        /// <returns>The matching applet manifests</returns>
        public IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            int tc = 0;
            return this.Query(queryParameters, 0, 100, out tc);
        }

        /// <summary>
        /// Perform a query of applets with restrictions
        /// </summary>
        /// <param name="queryParameters">The filter to apply</param>
        /// <param name="offset">The offset of the first result</param>
        /// <param name="count">The count of objects</param>
        /// <param name="totalCount">The total matching results</param>
        /// <returns>The applet manifests</returns>
        public IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            var query = QueryExpressionParser.BuildLinqExpression<AppletManifest>(queryParameters);
            var applets = ApplicationServiceContext.Current.GetService<IAppletManagerService>().Applets.Where(query.Compile()).Select(o => new AppletManifestInfo(o.Info, null));
            totalCount = applets.Count();
            return applets.Skip(offset).Take(count).OfType<Object>();

        }

        /// <summary>
        /// Update the specified applet
        /// </summary>
        /// <param name="data">The data to be update</param>
        /// <returns>The updated applet manifest</returns>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public object Update(object data)
        {
            var appletMgr = ApplicationServiceContext.Current.GetService<IAppletManagerService>();

            var pkg = AppletPackage.Load((Stream)data);
            if (!appletMgr.Applets.Any(o => pkg.Meta.Id == o.Info.Id))
                throw new FileNotFoundException(pkg.Meta.Id);

            ApplicationServiceContext.Current.GetService<IAppletManagerService>().Install(pkg, true);
            X509Certificate2 cert = null;
            if (pkg.PublicKey != null)
                cert = new X509Certificate2(pkg.PublicKey);
            else if (pkg.Meta.PublicKeyToken != null)
            {
                X509Store store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
                try
                {
                    store.Open(OpenFlags.ReadOnly);
                    var results = store.Certificates.Find(X509FindType.FindByThumbprint, pkg.Meta.PublicKeyToken, false);
                    if (results.Count > 0)
                        cert = results[0];
                }
                finally
                {
                    store.Close();
                }
            }
            return new AppletManifestInfo(pkg.Meta, new X509Certificate2Info(cert?.Issuer, cert?.NotBefore, cert?.NotAfter, cert?.Subject, cert?.Thumbprint));
        }

        /// <summary>
        /// Set applet headers
        /// </summary>
        private void SetAppletHeaders(AppletInfo package)
        {
            RestOperationContext.Current.OutgoingResponse.AppendHeader("ETag", package.Version);
            RestOperationContext.Current.OutgoingResponse.Headers.Add("X-SanteDB-PakID", package.Id);
            if (package.Hash != null)
                RestOperationContext.Current.OutgoingResponse.AppendHeader("X-SanteDB-Hash", Convert.ToBase64String(package.Hash));
            RestOperationContext.Current.OutgoingResponse.AppendHeader("Content-Type", "application/octet-stream");
            RestOperationContext.Current.OutgoingResponse.ContentType = "application/octet-stream";
            RestOperationContext.Current.OutgoingResponse.AppendHeader("Content-Disposition", $"attachment; filename=\"{package.Id}.pak.gz\"");
            RestOperationContext.Current.OutgoingResponse.AppendHeader("Location", $"/ami/Applet/{package.Id}");
        }
    }
}
