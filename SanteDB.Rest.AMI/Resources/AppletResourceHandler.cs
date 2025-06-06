﻿/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Applet;
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
    /// Represents a resource handler that handles applets
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class AppletResourceHandler : IServiceImplementation, IApiResourceHandler
    {
        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AppletResourceHandler));

        // Localization service
        private readonly ILocalizationService m_localizationService;

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
        /// Get the service name
        /// </summary>
        public string ServiceName => "Applet Resource Handler";

        /// <summary>
        /// Initializes the applet resource handler.
        /// </summary>
        /// <param name="localizationService">Localization service</param>
        public AppletResourceHandler(ILocalizationService localizationService)
        {
            this.m_localizationService = localizationService;
        }

        /// <summary>
        /// Create / install an applet on the server
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public object Create(object data, bool updateIfExists)
        {
            var pkg = AppletPackage.Load((Stream)data);
            ApplicationServiceContext.Current.GetService<IAppletManagerService>().Install(pkg);
            return new AppletManifestInfo(pkg);
        }

        /// <summary>
        /// Gets the contents of the applet with the specified ID
        /// </summary>
        /// <param name="appletId">The identifier of the applet to be loaded</param>
        /// <param name="versionId">The version of the applet</param>
        /// <returns></returns>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public object Get(Object appletId, Object versionId)
        {
            var appletService = ApplicationServiceContext.Current.GetService<IAppletManagerService>();
            var appletData = appletService.GetPackage(appletId.ToString());

            if (appletData == null)
            {
                this.m_tracer.TraceError($"File not found: {appletId}");
                throw new FileNotFoundException(this.m_localizationService.GetString("error.rest.ami.FileNotFoundParam", new
                {
                    param = appletId.ToString()
                }));
            }
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
        public object Delete(object appletId)
        {
            ApplicationServiceContext.Current.GetService<IAppletManagerService>().UnInstall(appletId.ToString());
            return null;
        }

        /// <summary>
        /// Perform a query of applets with restrictions
        /// </summary>
        /// <param name="queryParameters">The filter to apply</param>
        /// <returns>The applet manifests</returns>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            var query = QueryExpressionParser.BuildLinqExpression<AppletManifest>(queryParameters);
            var applets = ApplicationServiceContext.Current.GetService<IAppletManagerService>().Applets.Where(query.Compile()).Select(o => new AppletManifestInfo(o.CreatePackage()));
            return new MemoryQueryResultSet(applets);
        }

        /// <summary>
        /// Remove an associated entity
        /// </summary>
        public object RemoveAssociatedEntity(object scopingEntityKey, string propertyName, object subItemKey)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotImplementedException"));
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
            {
                this.m_tracer.TraceError($"File not found: {pkg.Meta.Id}");
                throw new FileNotFoundException(this.m_localizationService.GetString("error.rest.ami.FileNotFoundParam", new
                {
                    param = pkg.Meta.Id
                }));
            }

            ApplicationServiceContext.Current.GetService<IAppletManagerService>().Install(pkg, true);
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
            return new AppletManifestInfo(pkg);
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
    }
}