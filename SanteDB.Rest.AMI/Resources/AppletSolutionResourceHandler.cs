/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-5-19
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
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler for applet solution files
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class AppletSolutionResourceHandler : ChainedResourceHandlerBase
    {

        /// <summary>
        /// Gets the capabilities of the resource handler
        /// </summary>
        public override ResourceCapabilityType Capabilities
        {
            get
            {
                return ResourceCapabilityType.Create | ResourceCapabilityType.Update | ResourceCapabilityType.Get | ResourceCapabilityType.Search;
            }
        }

        /// <summary>
        /// Gets the name of the resource
        /// </summary>
        public override string ResourceName => "AppletSolution";
            
        /// <summary>
        /// Get the scope of this object
        /// </summary>
        public override Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Get the type of this
        /// </summary>
        public override Type Type => typeof(AppletSolution);

        /// <summary>
        /// Constructor for AppletSolutionResourceHandler
        /// </summary>
        /// <param name="localizationService">Localization service</param>
        public AppletSolutionResourceHandler(ILocalizationService localizationService) : base(localizationService)
        {
        }

        /// <summary>
        /// Create / install an applet on the server
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public override object Create(object data, bool updateIfExists)
        {
            var pkg = AppletPackage.Load((Stream)data) as AppletSolution;
            if (pkg == null)
            {
                this.m_tracer.TraceError("Package does not appear to be a solution");
                throw new InvalidOperationException(this.LocalizationService.GetString("error.rest.ami.packageNotASolution"));
            }

            ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>().Install(pkg);
            X509Certificate2 cert = null;
            if (pkg.PublicKey != null)
            {
                cert = new X509Certificate2(pkg.PublicKey);
            }
            else if (pkg.Meta.PublicKeyToken != null)
            {
                X509Store store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
                try
                {
                    store.Open(OpenFlags.ReadOnly);
                    var results = store.Certificates.Find(X509FindType.FindByThumbprint, pkg.Meta.PublicKeyToken, false);
                    if (results.Count > 0)
                    {
                        cert = results[0];
                    }
                }
                finally
                {
                    store.Close();
                }
            }
            return new AppletSolutionInfo(pkg);
        }

        /// <summary>
        /// Gets the contents of the applet with the specified ID
        /// </summary>
        /// <param name="solutionId">The identifier of the applet to be loaded</param>
        /// <param name="versionId">The version of the applet</param>
        /// <returns></returns>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override object Get(Object solutionId, Object versionId)
        {
            var appletService = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>();
            var appletData = appletService.Solutions.FirstOrDefault(o => o.Meta.Id == solutionId.ToString());

            if (appletData == null)
            {
                this.m_tracer.TraceError($"File not found: {solutionId}");
                throw new FileNotFoundException(this.LocalizationService.GetString("error.rest.ami.FileNotFoundParam", new
                {
                    param = solutionId.ToString()
                }));
            }
            else
            {
                return new AppletSolutionInfo(appletData);
            }
        }

        /// <summary>
        /// Set applet headers
        /// </summary>
        private void SetAppletHeaders(AppletInfo package)
        {
            RestOperationContext.Current.OutgoingResponse.SetETag(package.Version);
            RestOperationContext.Current.OutgoingResponse.Headers.Add(ExtendedHttpHeaderNames.PackageIdentifierHeaderName, package.Id);
            if (package.Hash != null)
            {
                RestOperationContext.Current.OutgoingResponse.AppendHeader(ExtendedHttpHeaderNames.PackageHashHeaderName, Convert.ToBase64String(package.Hash));
            }

            RestOperationContext.Current.OutgoingResponse.AppendHeader("Content-Type", "application/octet-stream");
            RestOperationContext.Current.OutgoingResponse.ContentType = "application/octet-stream";
            RestOperationContext.Current.OutgoingResponse.AppendHeader("Content-Disposition", $"attachment; filename=\"{package.Id}.pak.gz\"");
            RestOperationContext.Current.OutgoingResponse.AppendHeader("Location", $"/ami/Applet/{package.Id}");
        }

        /// <summary>
        /// Obsoletes the specified applet
        /// </summary>
        /// <param name="solutionId">The identifier of the applet to uninstall</param>
        /// <returns>Null</returns>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public override object Delete(object solutionId)
        {
            ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>().UnInstall(solutionId.ToString());
            return null;
        }

        /// <summary>
        /// Perform a query of applets with restrictions
        /// </summary>
        /// <param name="queryParameters">The filter to apply</param>
        /// <returns>The applet manifests</returns>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            var query = QueryExpressionParser.BuildLinqExpression<AppletSolution>(queryParameters);
            var applets = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>().Solutions.Where(query.Compile()).Select(o => new AppletSolutionInfo(o));
            return new MemoryQueryResultSet(applets);
        }

        /// <summary>
        /// Update the specified applet
        /// </summary>
        /// <param name="data">The data to be update</param>
        /// <returns>The updated applet manifest</returns>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public override object Update(object data)
        {
            var appletMgr = ApplicationServiceContext.Current.GetService<IAppletSolutionManagerService>();

            var pkg = AppletPackage.Load((Stream)data) as AppletSolution;
            if (!appletMgr.Solutions.Any(o => pkg.Meta.Id == o.Meta.Id))
            {
                this.m_tracer.TraceError($"File not found: {pkg.Meta.Id}");
                throw new FileNotFoundException(this.LocalizationService.GetString("error.rest.ami.FileNotFoundParam", new
                {
                    param = pkg.Meta.Id
                }));
            }

            appletMgr.Install(pkg, true);
            X509Certificate2 cert = null;
            if (pkg.PublicKey != null)
            {
                cert = new X509Certificate2(pkg.PublicKey);
            }
            else if (pkg.Meta.PublicKeyToken != null)
            {
                X509Store store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
                try
                {
                    store.Open(OpenFlags.ReadOnly);
                    var results = store.Certificates.Find(X509FindType.FindByThumbprint, pkg.Meta.PublicKeyToken, false);
                    if (results.Count > 0)
                    {
                        cert = results[0];
                    }
                }
                finally
                {
                    store.Close();
                }
            }
            return new AppletSolutionInfo(pkg);
        }

        /// <summary>
        /// Query child objects
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IQueryResultSet QueryChildObjects(object scopingEntityKey, string propertyName, NameValueCollection filter)
        {
            return base.QueryChildObjects(scopingEntityKey, propertyName, filter);
        }

        /// <summary>
        /// Add a child object instance
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public override object AddChildObject(object scopingEntityKey, string propertyName, object scopedItem)
        {
            return base.AddChildObject(scopingEntityKey, propertyName, scopedItem);
        }

        /// <summary>
        /// Get a child object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override object GetChildObject(object scopingEntity, string propertyName, object subItemKey)
        {
            return base.GetChildObject(scopingEntity, propertyName, subItemKey);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public override object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            return base.RemoveChildObject(scopingEntityKey, propertyName, subItemKey);
        }
    }
}