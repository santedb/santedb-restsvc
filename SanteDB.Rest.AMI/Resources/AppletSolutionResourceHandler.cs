/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Applet;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler for applet solution files
    /// </summary>
    public class AppletSolutionResourceHandler : IApiResourceHandler, IAssociativeResourceHandler
    {
        /// <summary>
        /// Gets the capabilities of the resource handler
        /// </summary>
        public ResourceCapabilityType Capabilities
        {
            get
            {
                return ResourceCapabilityType.Create | ResourceCapabilityType.Update | ResourceCapabilityType.Get | ResourceCapabilityType.Search;
            }
        }

        /// <summary>
        /// Gets the name of the resource
        /// </summary>
        public string ResourceName
        {
            get
            {
                return "AppletSolution";
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
        /// Adding solution components not yet supported
        /// </summary>
        public object AddAssociatedEntity(object scopingEntityKey, string propertyName, object scopedItem)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Create / install an applet on the server
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public object Create(object data, bool updateIfExists)
        {
            var pkg = AppletPackage.Load((Stream)data) as AppletSolution;
            if (pkg == null)
                throw new InvalidOperationException($"Package does not appear to be a solution");

            ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>().Install(pkg);
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
            return new AppletSolutionInfo(pkg, new X509Certificate2Info(cert?.Issuer, cert?.NotBefore, cert?.NotAfter, cert?.Subject, cert?.Thumbprint));
        }

        /// <summary>
        /// Gets the contents of the applet with the specified ID
        /// </summary>
        /// <param name="solutionId">The identifier of the applet to be loaded</param>
        /// <param name="versionId">The version of the applet</param>
        /// <returns></returns>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public object Get(Object solutionId, Object versionId)
        {
            var appletService = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>();
            var appletData = appletService.Solutions.FirstOrDefault(o=>o.Meta.Id == solutionId.ToString());

            if (appletData == null)
                throw new FileNotFoundException(solutionId.ToString());
            else
            {
                return new AppletSolutionInfo(appletData, null);
            }
        }
        /// <summary>
        /// Get the associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public object GetAssociatedEntity(object scopingEntity, string propertyName, object subItemKey)
        {
            var appletService = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>();
            var appletData = appletService.GetPackage(scopingEntity.ToString(), subItemKey.ToString());

            if (appletData == null)
                throw new FileNotFoundException(subItemKey.ToString());
            else
            {
                var appletManifest = AppletPackage.Load(appletData);
                this.SetAppletHeaders(appletManifest.Meta);
                return new MemoryStream(appletData);
            }
        }


        /// <summary>
        /// Set applet headers
        /// </summary>
        private void SetAppletHeaders(AppletInfo package)
        {
            RestOperationContext.Current.OutgoingResponse.SetETag(package.Version);
            RestOperationContext.Current.OutgoingResponse.Headers.Add("X-SanteDB-PakID", package.Id);
            if (package.Hash != null)
                RestOperationContext.Current.OutgoingResponse.AppendHeader("X-SanteDB-Hash", Convert.ToBase64String(package.Hash));
            RestOperationContext.Current.OutgoingResponse.AppendHeader("Content-Type", "application/octet-stream");
            RestOperationContext.Current.OutgoingResponse.ContentType = "application/octet-stream";
            RestOperationContext.Current.OutgoingResponse.AppendHeader("Content-Disposition", $"attachment; filename=\"{package.Id}.pak.gz\"");
            RestOperationContext.Current.OutgoingResponse.AppendHeader("Location", $"/ami/Applet/{package.Id}");
        }

        /// <summary>
        /// Get associated entities
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public IEnumerable<object> QueryAssociatedEntities(object scopingEntityKey, string propertyName, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            switch (propertyName)
            {
                case "applet":
                    var query = QueryExpressionParser.BuildLinqExpression<AppletManifest>(filter);
                    var applets = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>().GetApplets(scopingEntityKey.ToString()).Where(query.Compile()).Select(o => new AppletManifestInfo(o.Info, null));
                    totalCount = applets.Count();
                    return applets.Skip(offset).Take(count).OfType<Object>();
                default:
                    throw new KeyNotFoundException($"Don't understand {propertyName}");
            }
        }


        /// <summary>
        /// Obsoletes the specified applet
        /// </summary>
        /// <param name="solutionId">The identifier of the applet to uninstall</param>
        /// <returns>Null</returns>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public object Obsolete(object solutionId)
        {
            ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>().UnInstall(solutionId.ToString());
            return null;
        }

        /// <summary>
        /// Perform a query of applets
        /// </summary>
        /// <param name="queryParameters">The filter to apply to the applet</param>
        /// <returns>The matching applet manifests</returns>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
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
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public IEnumerable<object> Query(Core.Model.Query.NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            var query = QueryExpressionParser.BuildLinqExpression<AppletSolution>(queryParameters);
            var applets = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>().Solutions.Where(query.Compile()).Select(o => new AppletSolutionInfo(o, null));
            totalCount = applets.Count();
            return applets.Skip(offset).Take(count).OfType<Object>();

        }
        
        /// <summary>
        /// Removing sub items not yet supported
        /// </summary>
        public object RemoveAssociatedEntity(object scopingEntityKey, string propertyName, object subItemKey)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Update the specified applet
        /// </summary>
        /// <param name="data">The data to be update</param>
        /// <returns>The updated applet manifest</returns>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public object Update(object data)
        {
            var appletMgr = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>();

            var pkg = AppletPackage.Load((Stream)data) as AppletSolution;
            if (!appletMgr.Solutions.Any(o => pkg.Meta.Id == o.Meta.Id))
                throw new FileNotFoundException(pkg.Meta.Id);

            appletMgr.Install(pkg, true);
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
            return new AppletSolutionInfo(pkg, new X509Certificate2Info(cert?.Issuer, cert?.NotBefore, cert?.NotAfter, cert?.Subject, cert?.Thumbprint));
        }

    }
}
