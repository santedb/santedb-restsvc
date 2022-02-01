/*
 * Portions Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2022 SanteSuite Contributors (See NOTICE)
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
 * DatERROR: 2021-8-27
 */
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Applet;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler for applet solution files
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class AppletSolutionResourceHandler : IServiceImplementation, IApiResourceHandler, IChainedApiResourceHandler
    {

        // Property providers
        private ConcurrentDictionary<String, IApiChildResourceHandler> m_propertyProviders = new ConcurrentDictionary<string, IApiChildResourceHandler>();

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AppletSolutionResourceHandler));

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
        public Type Type => typeof(AppletSolution);

        /// <summary>
        /// Child resources
        /// </summary>
        public IEnumerable<IApiChildResourceHandler> ChildResources => this.m_propertyProviders.Values;

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Applet Solution Resource Handler";

        /// <summary>
        /// Constructor for AppletSolutionResourceHandler
        /// </summary>
        /// <param name="localizationService">Localization service</param>
        public AppletSolutionResourceHandler(ILocalizationService localizationService)
        {
            this.m_localizationService = localizationService;
        }

        /// <summary>
        /// Create / install an applet on the server
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public object Create(object data, bool updateIfExists)
        {
            var pkg = AppletPackage.Load((Stream)data) as AppletSolution;
            if (pkg == null)
            {
                this.m_tracer.TraceError("Package does not appear to be a solution");
                throw new InvalidOperationException(this.m_localizationService.GetString("error.rest.ami.packageNotASolution"));
            }
               
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
            var appletData = appletService.Solutions.FirstOrDefault(o => o.Meta.Id == solutionId.ToString());

            if (appletData == null)
            {
                this.m_tracer.TraceError($"File not found: {solutionId}");
                throw new FileNotFoundException(this.m_localizationService.FormatString("error.rest.ami.FileNotFoundParam", new 
                { 
                    param = solutionId.ToString()
                }));
            }
            else
            {
                return new AppletSolutionInfo(appletData, null);
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
            {
                this.m_tracer.TraceError($"File not found: {pkg.Meta.Id}");
                throw new FileNotFoundException(this.m_localizationService.FormatString("error.rest.ami.FileNotFoundParam", new
                {
                    param = pkg.Meta.Id
                }));
            }
               

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



        /// <summary>
        /// Add a child resource
        /// </summary>
        public void AddChildResource(IApiChildResourceHandler property)
        {
            this.m_propertyProviders.TryAdd(property.Name, property);
        }

        /// <summary>
        /// Remove a child object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Remove(typeof(AppletSolution), scopingEntityKey, subItemKey);
            }
            else
            {
                this.m_tracer.TraceError($"{propertyName} not found");
                throw new KeyNotFoundException(this.m_localizationService.FormatString("error.type.KeyNotFoundException.notFound", new 
                { 
                    param = propertyName
                }));
            }
        }

        /// <summary>
        /// Query child objects
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public IEnumerable<object> QueryChildObjects(object scopingEntityKey, string propertyName, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Query(typeof(AppletSolution), scopingEntityKey, filter, offset, count, out totalCount);
            }
            else
            {
                this.m_tracer.TraceError($"{propertyName} not found");
                throw new KeyNotFoundException(this.m_localizationService.FormatString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }

        /// <summary>
        /// Add a child object instance
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerApplet)]
        public object AddChildObject(object scopingEntityKey, string propertyName, object scopedItem)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Add(typeof(AppletSolution), scopingEntityKey, scopedItem);
            }
            else
            {
                this.m_tracer.TraceError($"{propertyName} not found");
                throw new KeyNotFoundException(this.m_localizationService.FormatString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }

        /// <summary>
        /// Get a child object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public object GetChildObject(object scopingEntity, string propertyName, object subItemKey)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntity == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Get(typeof(AppletSolution), scopingEntity, subItemKey);
            }
            else
            {
                this.m_tracer.TraceError($"{propertyName} not found");
                throw new KeyNotFoundException(this.m_localizationService.FormatString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }


        /// <summary>
        /// Try to get a chained resource
        /// </summary>
        public bool TryGetChainedResource(string propertyName, ChildObjectScopeBinding bindingType, out IApiChildResourceHandler childHandler)
        {
            var retVal = this.m_propertyProviders.TryGetValue(propertyName, out childHandler) &&
                childHandler.ScopeBinding.HasFlag(bindingType);
            if (!retVal)
            {
                childHandler = null;//clear in case of lazy programmers like me
            }
            return retVal;
        }
    }
}
