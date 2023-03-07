/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Exceptions;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.AMI.Configuration;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SanteDB.Rest.AMI
{
    /// <summary>
    /// Administration Management Interface (AMI)
    /// </summary>
    /// <remarks>Represents a generic implementation of the Administrative Management Interface (AMI) contract</remarks>
    [ServiceBehavior(Name = "AMI", InstanceMode = ServiceInstanceMode.Singleton)]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class AmiServiceBehavior : IAmiServiceContract
    {
        /// <summary>
        /// Trace source for logging
        /// </summary>
        protected readonly Tracer m_traceSource = Tracer.GetTracer(typeof(AmiServiceBehavior));
        private readonly ILocalizationService m_localizationService;
        private readonly IPatchService m_patchService;
        private readonly IConfigurationManager m_configurationManager;
        private readonly IServiceManager m_serviceManager;
        private readonly IDataCachingService m_dataCachingService;
        private readonly IPolicyEnforcementService m_pepService;

        /// <summary>
        /// The resource handler tool for executing operations
        /// </summary>
        private ResourceHandlerTool m_resourceHandler;

        /// <summary>
        /// Configuration
        /// </summary>
        protected readonly AmiConfigurationSection m_configuration;

        /// <summary>
        /// Default CTOR for rest creation
        /// </summary>
        public AmiServiceBehavior() :
            this(
                ApplicationServiceContext.Current.GetService<ILocalizationService>(),
                ApplicationServiceContext.Current.GetService<IConfigurationManager>(),
                ApplicationServiceContext.Current.GetService<IServiceManager>(),
                ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>(),
                ApplicationServiceContext.Current.GetService<IPatchService>(),
                ApplicationServiceContext.Current.GetService<IDataCachingService>())
        {
            m_resourceHandler = AmiMessageHandler.ResourceHandler;
        }

        /// <summary>
        /// Get the resource handler for the named resource
        /// </summary>
        protected IApiResourceHandler GetResourceHandler(String resourceTypeName) => this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceTypeName);

        /// <summary>
        /// Gets the resource location which will be used in the Location header of operations which do not return the resource directly.
        /// </summary>
        /// <param name="data">The object to get the location for.</param>
        /// <returns>The location that can be inserted into the header.</returns>
        /// <remarks>The default implementation looks for <see cref="IVersionedData"/>, <see cref="IIdentifiedResource"/>, <see cref="IdentifiedData"/> in that order. If none match, <c>null</c> is returned.</remarks>
        private string GetResourceLocation(object data)
        {
            switch (data)
            {
                case IVersionedData versioned:
                    return $"{RestOperationContext.Current.IncomingRequest.Url}/{versioned.Key}/history/{versioned.VersionKey}";
                case IIdentifiedResource idr:
                    return $"{RestOperationContext.Current.IncomingRequest.Url}/{idr.Key}";
            }
            return null;
        }

        /// <summary>
        /// Attempts to set the location header on the outgoing response if one is available from <see cref="GetResourceLocation(object)"/>.
        /// </summary>
        /// <param name="data">The object to get the location for.</param>
        private void AddContentLocationHeader(object data)
        {
            var contentlocation = GetResourceLocation(data);

            if (null != contentlocation)
            {
                RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, contentlocation);
            }
        }

        /// <summary>
        /// Gets an etag for the data provided. Implementers are free to override the default implementation if the resources have different tag types and interfaces.
        /// </summary>
        /// <param name="data">The object to derive an etag from</param>
        /// <returns>The etag for the data if one can be derived.</returns>
        /// <remarks><para>The default implementation will search for <see cref="IIdentifiedResource"/>, then <see cref="IdentifiedData"/>. If neither is available, <c>null</c> is returned.</para></remarks>
        private string GetETagFromData(object data)
        {
            switch (data)
            {
                case IIdentifiedResource resource:
                    return resource.Tag;
                
                default:
                    return null;
            }
        }

        /// <summary>
        /// Add e-tag header
        /// </summary>
        private string AddEtagHeader(object data, bool useGuid = false)
        {
            var tag = this.GetETagFromData(data);

            if (useGuid && null == tag)
            {
                tag = Guid.NewGuid().ToString();
            }

            if (null != tag)
            {
                RestOperationContext.Current.OutgoingResponse.SetETag(tag);
            }

            return tag;
        }

        /// <summary>
        /// Add last modified header
        /// </summary>
        private DateTime? AddLastModifiedHeader(object data, bool useCurrentTime = false)
        {
            switch (data)
            {
                case IIdentifiedResource idr:
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(idr.ModifiedOn.DateTime);
                    return idr.ModifiedOn.DateTime;
               
                default:
                    if (useCurrentTime)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetLastModified(DateTime.Now);
                        return DateTime.Now;
                    }
                    return null;
            }
        }

        /// <inheritdoc/>
        private string ExtractValidateMatchHeader(Type resourceType, String headerString)
        {
            if (!headerString.Contains("."))
            {
                return headerString;
            }
            else
            {
                var headerParts = headerString.Split('.');
                if (resourceType.IsAssignableFrom(this.GetResourceHandler(headerParts[0]).Type))
                {
                    return headerParts[1];
                }
                else
                {
                    throw new PreconditionFailedException();
                }
            }
        }

        /// <inheritdoc/>
        /// <exception cref="PreconditionFailedException">When the HTTP header pre-conditions fail</exception>
        private void ThrowIfPreConditionFails(IApiResourceHandler handler, String objectId)
        {

            var ifModifiedHeader = RestOperationContext.Current.IncomingRequest.GetIfModifiedSince();
            var ifUnmodifiedHeader = RestOperationContext.Current.IncomingRequest.GetIfUnmodifiedSince();
            var ifNoneMatchHeader = RestOperationContext.Current.IncomingRequest.GetIfNoneMatch()?.Select(o => this.ExtractValidateMatchHeader(handler.Type, o));
            var ifMatchHeader = RestOperationContext.Current.IncomingRequest.GetIfMatch()?.Select(o => this.ExtractValidateMatchHeader(handler.Type, o));

            // HTTP IF headers? - before we go to the DB lets check the cache for them
            if (ifNoneMatchHeader?.Any() == true || ifMatchHeader?.Any() == true || ifModifiedHeader.HasValue || ifUnmodifiedHeader.HasValue)
            {

                IdentifiedData cacheResult = null;
                if (Guid.TryParse(objectId, out var guidKey))
                {
                    cacheResult = this.m_dataCachingService.GetCacheItem(guidKey);
                }

                if (cacheResult != null && (ifNoneMatchHeader?.Contains(cacheResult.Tag) == true ||
                    ifMatchHeader?.Contains(cacheResult.Tag) != true ||
                        ifModifiedHeader.HasValue && cacheResult.ModifiedOn <= ifModifiedHeader ||
                        ifUnmodifiedHeader.HasValue && cacheResult.ModifiedOn >= ifUnmodifiedHeader))
                {
                    if (cacheResult is ITaggable tagged)
                    {
                        if (tagged.GetTag(SanteDBModelConstants.DcdrRefetchTag) == null)
                        {
                            throw new PreconditionFailedException();
                        }
                        else
                        {
                            tagged.RemoveTag(SanteDBModelConstants.DcdrRefetchTag);
                        }
                    }
                    else
                    {
                        throw new PreconditionFailedException();
                    }
                }
                else
                {
                    var checkQuery = $"_id={objectId}".ParseQueryString();
                    if (!handler.Query(checkQuery).Any()) // Object doesn't exist
                    {
                        throw new KeyNotFoundException();
                    }

                    // If-None-Match when used with If-Modified-Since then If-None-Match has priority
                    if (ifNoneMatchHeader?.Any() == true ||
                        ifMatchHeader?.Any() == true)
                    {
                        checkQuery.Add("etag", ifNoneMatchHeader?.Where(c => Guid.TryParse(c, out _)).Select(o => $"{o}").ToArray());
                        checkQuery.Add("etag", ifMatchHeader?.Where(c => Guid.TryParse(c, out _)).Select(o => $"{o}").ToArray());
                        if (typeof(IVersionedData).IsAssignableFrom(handler.Type))
                        {
                            checkQuery.Add("obsoletionTime", "null", "!null");
                        }
                        var matchingTags = handler.Query(checkQuery).Any();
                        if ((ifNoneMatchHeader?.Any() == true && matchingTags) ^
                            (ifMatchHeader?.Any() == true && !matchingTags))
                        {
                            throw new PreconditionFailedException();
                        }
                    }

                    if (ifModifiedHeader.HasValue)
                    {
                        checkQuery.Remove("tag");
                        checkQuery.Add("modifiedOn", $"<{ifModifiedHeader:o}");
                        if (handler.Query(checkQuery).Any())
                        {
                            throw new PreconditionFailedException();
                        }
                    }

                    if (ifUnmodifiedHeader.HasValue)
                    {
                        checkQuery.Remove("tag");
                        checkQuery.Remove("modifiedOn");
                        checkQuery.Add("modifiedOn", $">{ifUnmodifiedHeader:o}");
                        if (handler.Query(checkQuery).Any())
                        {
                            throw new PreconditionFailedException();
                        }
                    }
                }
            }
        }


        /// <summary>
        /// AMI Service Behavior constructor
        /// </summary>
        public AmiServiceBehavior(ILocalizationService localizationService, IConfigurationManager configurationManager, IServiceManager serviceManager, IPolicyEnforcementService pepService, IPatchService patchService = null, IDataCachingService cachingService = null)
        {
            this.m_localizationService = localizationService;
            this.m_patchService = patchService;
            this.m_configurationManager = configurationManager;
            this.m_serviceManager = serviceManager;
            this.m_dataCachingService = cachingService;
            this.m_pepService = pepService;
            m_resourceHandler = AmiMessageHandler.ResourceHandler;
            this.m_configuration = configurationManager.GetSection<AmiConfigurationSection>();
        }

        /// <summary>
        /// Perform an ACL check
        /// </summary>
        private void AclCheck(Object handler, String action)
        {
            foreach (var dmn in this.GetDemands(handler, action))
            {
                ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>().Demand(dmn);
            }
        }

        /// <summary>
        /// Get demands
        /// </summary>
        private String[] GetDemands(object handler, string action)
        {
            var demands = handler.GetType().GetMethods().Where(o => o.Name == action).SelectMany(method => method.GetCustomAttributes<DemandAttribute>());
            if (demands.Any(o => o.Override))
            {
                return demands.Where(o => o.Override).Select(o => o.PolicyId).ToArray();
            }
            else
            {
                return demands.Select(o => o.PolicyId).ToArray();
            }
        }

        /// <summary>
        /// Gets the schema for the administrative interface.
        /// </summary>
        /// <param name="schemaId">The id of the schema to be retrieved.</param>
        /// <returns>Returns the administrative interface schema.</returns>
        public XmlSchema GetSchema(int schemaId)
        {
            try
            {
                XmlSchemas schemaCollection = new XmlSchemas();

                XmlReflectionImporter importer = new XmlReflectionImporter("http://santedb.org/ami");
                XmlSchemaExporter exporter = new XmlSchemaExporter(schemaCollection);

                foreach (var cls in this.m_resourceHandler.Handlers.Select(o => o.Type))
                {
                    exporter.ExportTypeMapping(importer.ImportTypeMapping(cls, "http://santedb.org/ami"));
                }

                if (schemaId > schemaCollection.Count)
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    return null;
                }
                else
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.OK;
                    RestOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
                    return schemaCollection[schemaId];
                }
            }
            catch (Exception e)
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                this.m_traceSource.TraceError(e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Gets options for the AMI service.
        /// </summary>
        /// <returns>Returns options for the AMI service.</returns>
        public virtual ServiceOptions Options()
        {
            this.ThrowIfNotReady();

            if (this.m_patchService != null)
            {
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Accept-Patch", "application/xml+sdb-patch");
            }

            // mex configuration
            var mexConfig = this.m_configurationManager.GetSection<SanteDB.Rest.Common.Configuration.RestConfigurationSection>();
            String boundHostPort = $"{RestOperationContext.Current.IncomingRequest.Url.Scheme}://{RestOperationContext.Current.IncomingRequest.Url.Host}:{RestOperationContext.Current.IncomingRequest.Url.Port}";
            if (!String.IsNullOrEmpty(mexConfig.ExternalHostPort))
            {
                var tUrl = new Uri(mexConfig.ExternalHostPort);
                boundHostPort = $"{tUrl.Scheme}://{tUrl.Host}:{tUrl.Port}";
            }

            var serviceOptions = new ServiceOptions()
            {
                Key = ApplicationServiceContext.Current.ActivityUuid,
                ServerVersion = $"{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product} v{Assembly.GetEntryAssembly().GetName().Version} ({Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion})",
                InterfaceVersion = typeof(AmiServiceBehavior).Assembly.GetName().Version.ToString(),
                Endpoints = this.m_serviceManager.GetServices().OfType<IApiEndpointProvider>().Select(o =>
                    new ServiceEndpointOptions(o)
                    {
                        BaseUrl = o.Url.Select(url =>
                        {
                            var turi = new Uri(url);
                            if (turi.Scheme.StartsWith("http"))
                            {
                                return $"{boundHostPort}{turi.AbsolutePath}";
                            }
                            else
                            {
                                return turi.ToString();
                            }
                        }).ToArray()
                    }
                ).ToList()
            };

            // Get endpoints
            var config = this.m_configurationManager.GetSection<AmiConfigurationSection>();

            if (config?.Endpoints != null)
            {
                serviceOptions.Endpoints.AddRange(config.Endpoints);
            }

            // Get the resources which are supported
            if (null != m_resourceHandler?.Handlers)
            {
                foreach (var itm in this.m_resourceHandler.Handlers)
                {
                    var svc = this.ResourceOptions(itm.ResourceName);
                    serviceOptions.Resources.Add(svc);
                }
            }
            serviceOptions.Settings = config.PublicSettings.ToList();

            if(this.m_pepService.SoftDemand(PermissionPolicyIdentifiers.AccessClientAdministrativeFunction, AuthenticationContext.Current.Principal))
            {
                serviceOptions.Settings.AddRange(this.m_configurationManager.GetSection<SecurityConfigurationSection>().ForDisclosure());
            }
            return serviceOptions;
        }

        /// <summary>
        /// Perform a patch on the serviceo
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="id"></param>
        /// <param name="body"></param>
        public virtual void Patch(string resourceType, string id, Patch body)
        {
            this.ThrowIfNotReady();
            try
            {
                // First we load
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);

                if (handler == null)
                {
                    throw new FileNotFoundException(resourceType);
                }

                // Validate
                var match = RestOperationContext.Current.IncomingRequest.Headers["If-Match"];
                if (match == null && typeof(IVersionedData).IsAssignableFrom(handler.Type))
                {
                    throw new InvalidOperationException("Missing If-Match header for versioned objects");
                }

                // Next we get the current version
                this.AclCheck(handler, nameof(IApiResourceHandler.Get));

                var rawExisting = handler.Get(Guid.Parse(id), Guid.Empty);
                IdentifiedData existing = (rawExisting as ISecurityEntityInfo)?.ToIdentifiedData() ?? rawExisting as IdentifiedData;

                // Object cannot be patched
                if (existing == null)
                {
                    throw new NotSupportedException();
                }

                var force = Convert.ToBoolean(RestOperationContext.Current.IncomingRequest.Headers["X-Patch-Force"] ?? "false");

                if (existing == null)
                {
                    throw new FileNotFoundException($"/{resourceType}/{id}");
                }
                else if (!String.IsNullOrEmpty(match) && (existing as IdentifiedData)?.Tag != match && !force)
                {
                    this.m_traceSource.TraceError("Object {0} ETAG is {1} but If-Match specified {2}", existing.Key, existing.Tag, match);
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 409;
                    RestOperationContext.Current.OutgoingResponse.StatusDescription = "Conflict";
                    return;
                }
                else if (body == null)
                {
                    throw new ArgumentNullException(nameof(body));
                }
                else
                {
                    // Force load all properties for existing
                    var applied = ApplicationServiceContext.Current.GetService<IPatchService>().Patch(body, existing, force);
                    this.AclCheck(handler, nameof(IApiResourceHandler.Update));
                    var updateResult = handler.Update(applied);
                    var data = (updateResult as ISecurityEntityInfo)?.ToIdentifiedData() ?? updateResult as IdentifiedData;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 204;
                    RestOperationContext.Current.OutgoingResponse.SetETag(data.Tag);
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(applied.ModifiedOn.DateTime);
                    var versioned = (data as IVersionedData)?.VersionKey;
                    if (versioned != null)
                    {
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/history/{2}",
                                RestOperationContext.Current.IncomingRequest.Url,
                                id,
                                versioned));
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}",
                                RestOperationContext.Current.IncomingRequest.Url,
                                id));
                    }
                }
            }
            catch (PatchAssertionException e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceWarning(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Perform a ping
        /// </summary>
        public void Ping()
        {
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Creates the specified resource for the AMI service
        /// </summary>
        /// <param name="resourceType">The type of resource being created</param>
        /// <param name="data">The resource data being created</param>
        /// <returns>The created the data</returns>
        public virtual Object Create(string resourceType, Object data)
        {
            this.ThrowIfNotReady();

            try
            {
                IApiResourceHandler handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Create));
                    var retVal = handler.Create(data, false);

                    RestOperationContext.Current.OutgoingResponse.StatusCode = retVal == null ? (int)HttpStatusCode.NoContent : (int)System.Net.HttpStatusCode.Created;
                    this.AddEtagHeader(retVal);
                    this.AddContentLocationHeader(retVal);
                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error creating resource {resourceType}", e);
            }
        }

        /// <summary>
        /// Create or update the specific resource
        /// </summary>
        public virtual Object CreateUpdate(string resourceType, string key, Object data)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    if (data is IdentifiedData)
                    {
                        (data as IdentifiedData).Key = Guid.Parse(key);
                    }
                    else if (data is IAmiIdentified)
                    {
                        (data as IAmiIdentified).Key = key;
                    }

                    this.AclCheck(handler, nameof(IApiResourceHandler.Create));
                    var retVal = handler.Create(data, true);
                    var versioned = retVal as IVersionedData;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;

                    this.AddEtagHeader(retVal);
                    this.AddContentLocationHeader(retVal);

                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Delete the specified resource
        /// </summary>
        public virtual Object Delete(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Delete));

                    this.ThrowIfPreConditionFails(handler, key);
                    var retVal = handler.Delete(Guid.TryParse(key, out var uuid) ? (object)uuid : key);

                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;
                    this.AddEtagHeader(retVal);
                    this.AddContentLocationHeader(retVal);
                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Get the specified resource
        /// </summary>
        public virtual Object Get(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {

                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));
                    this.ThrowIfPreConditionFails(handler, key);
                    var retVal = handler.Get(Guid.TryParse(key, out var guidKey) ? (object)guidKey : key, Guid.Empty);
                    if (retVal == null)
                    {
                        throw new FileNotFoundException(key);
                    }
                    this.AddEtagHeader(retVal);
                    this.AddContentLocationHeader(retVal);
                    this.AddLastModifiedHeader(retVal);
                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Get a specific version of the resource
        /// </summary>
        public virtual Object GetVersion(string resourceType, string key, string versionKey)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {

                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));
                    this.ThrowIfPreConditionFails(handler, key);
                    var retVal = handler.Get(Guid.TryParse(key, out var guidKey) ? (object)guidKey : key, Guid.TryParse(versionKey, out var versionGuid) ? (object)versionGuid : versionKey);
                    if (retVal == null)
                    {
                        throw new FileNotFoundException(key);
                    }
                    this.AddEtagHeader(retVal);
                    this.AddContentLocationHeader(retVal);
                    this.AddLastModifiedHeader(retVal);
                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Get the complete history of a resource
        /// </summary>
        public virtual AmiCollection History(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);

                if (handler != null)
                {
                    String since = RestOperationContext.Current.IncomingRequest.QueryString["_since"];
                    Guid sinceGuid = since != null ? Guid.Parse(since) : Guid.Empty;

                    // Query
                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));

                    this.ThrowIfPreConditionFails(handler, key);
                    var retVal = handler.Get(Guid.TryParse(key, out var guidKey) ? (object)guidKey : key, Guid.Empty) as IVersionedData;
                    List<IVersionedData> histItm = new List<IVersionedData>() { retVal };
                    while (retVal.PreviousVersionKey.HasValue)
                    {
                        retVal = handler.Get(Guid.Parse(key), retVal.PreviousVersionKey.Value) as IVersionedData;
                        if (retVal != null)
                        {
                            histItm.Add(retVal);
                        }
                        // Should we stop fetching?
                        if (retVal?.VersionKey == sinceGuid)
                        {
                            break;
                        }
                    }

                    // Lock the item
                    return new AmiCollection(histItm, 0, histItm.Count);
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Options resource
        /// </summary>
        public virtual ServiceResourceOptions ResourceOptions(string resourceType)
        {
            var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
            if (handler == null)
            {
                throw new FileNotFoundException(resourceType);
            }
            else
            {
                Func<ResourceCapabilityType, String[]> getCaps = (o) =>
                {
                    switch (o)
                    {
                        case ResourceCapabilityType.Create:
                        case ResourceCapabilityType.CreateOrUpdate:
                            return this.GetDemands(handler, nameof(IApiResourceHandler.Create));

                        case ResourceCapabilityType.Delete:
                            return this.GetDemands(handler, nameof(IApiResourceHandler.Create));

                        case ResourceCapabilityType.Get:
                        case ResourceCapabilityType.GetVersion:
                            return this.GetDemands(handler, nameof(IApiResourceHandler.Get));

                        case ResourceCapabilityType.History:
                        case ResourceCapabilityType.Search:
                            return this.GetDemands(handler, nameof(IApiResourceHandler.Query));

                        case ResourceCapabilityType.Update:
                            return this.GetDemands(handler, nameof(IApiResourceHandler.Update));

                        default:
                            return new string[] { PermissionPolicyIdentifiers.Login };
                    }
                };

                // Get the resource capabilities
                List<ServiceResourceCapability> caps = handler.Capabilities.ToResourceCapabilityStatement(getCaps).ToList();

                // Patching
                if (ApplicationServiceContext.Current.GetService<IPatchService>() != null &&
                    handler.Capabilities.HasFlag(ResourceCapabilityType.Update))
                {
                    caps.Add(new ServiceResourceCapability(ResourceCapabilityType.Patch, this.GetDemands(handler, nameof(IApiResourceHandler.Update))));
                }

                // To expose associated objects
                var childResources = new List<ChildServiceResourceOptions>();
                if (handler is IChainedApiResourceHandler associative)
                {
                    childResources = associative.ChildResources.Select(r => new ChildServiceResourceOptions(r.Name, r.PropertyType, r.Capabilities.ToResourceCapabilityStatement(getCaps).ToList(), r.ScopeBinding, ChildObjectClassification.Resource)).ToList();
                }
                if (handler is IOperationalApiResourceHandler operation)
                {
                    childResources = operation.Operations.Select(o => new ChildServiceResourceOptions(o.Name, typeof(Object), ResourceCapabilityType.Create.ToResourceCapabilityStatement(getCaps).ToList(), o.ScopeBinding, ChildObjectClassification.RpcOperation)).ToList();
                }
                // Associateive
                return new ServiceResourceOptions(resourceType, handler.Type, caps, childResources);
            }
        }

        /// <summary>
        /// Performs a search of the specified AMI resource
        /// </summary>
        public virtual AmiCollection Search(string resourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Query));

                    // Send the query to the resource handler
                    var query = RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString();

                    // Modified on?
                    if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null)
                    {
                        query.Add("modifiedOn", ">" + RestOperationContext.Current.IncomingRequest.GetIfModifiedSince()?.ToString("o"));
                    }

                    // Query for results
                    var results = handler.Query(query) as IOrderableQueryResultSet;

                    // Now apply controls
                    var retVal = results.ApplyResultInstructions(query, out int offset, out int totalCount).OfType<Object>();

                    if (typeof(IdentifiedData).IsAssignableFrom(handler.Type) && (this.m_configuration?.IncludeMetadataHeadersOnSearch != false))
                    {
                        var modifiedOnSelector = QueryExpressionParser.BuildPropertySelector(handler.Type, "modifiedOn", convertReturn: typeof(object));
                        var lastModified = (DateTime)results.OrderByDescending(modifiedOnSelector).Select<DateTimeOffset>(modifiedOnSelector).AsResultSet().FirstOrDefault().DateTime;
                    }

                    // Last modification time and not modified conditions
                    if ((RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null ||
                        RestOperationContext.Current.IncomingRequest.GetIfNoneMatch() != null) &&
                        totalCount == 0)
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 304;
                        return null;
                    }
                    else
                    {
                        return new AmiCollection(retVal, offset, totalCount);
                    }
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error searching resource type {resourceType}", e);
            }
        }

        /// <summary>
        /// Updates the specified object on the server
        /// </summary>
        public virtual Object Update(string resourceType, string key, Object data)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    // Get target of update and ensure
                    switch (data)
                    {
                        case IdentifiedData iddata:
                            if ((!Guid.TryParse(key, out var guidKey) || iddata.Key != guidKey) && iddata.Key.HasValue)
                            {
                                throw new FaultException(HttpStatusCode.BadRequest, "Key mismatch");
                            }

                            iddata.Key = guidKey;
                            break;
                        case IIdentifiedResource iir:
                            if((!Guid.TryParse(key, out var guidKey2) || iir.Key != guidKey2) && iir.Key.HasValue)
                            {
                                throw new FaultException(HttpStatusCode.BadRequest, "Key mismatch");
                            }
                            iir.Key = guidKey2;
                            break;
                        case IAmiIdentified amid:
                            if (!String.IsNullOrEmpty(amid.Key?.ToString()) && amid.Key.Equals(key))
                            {
                                throw new FaultException(HttpStatusCode.BadRequest, "Key mismatch");
                            }

                            amid.Key = key;
                            break;
                    }

                    this.AclCheck(handler, nameof(IApiResourceHandler.Update));
                    this.ThrowIfPreConditionFails(handler, key);

                    var retVal = handler.Update(data);
                    if (retVal == null)
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.NoContent;
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;
                    }
                    this.AddEtagHeader(retVal);
                    this.AddContentLocationHeader(retVal);
                    this.AddLastModifiedHeader(retVal);
                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString().Replace("{", "{{").Replace("}", "}}")));
                throw;
            }
        }

        /// <summary>
        /// Perform a head operation
        /// </summary>
        /// <param name="resourceType">The resource type</param>
        /// <param name="id">The id of the resource</param>
        public virtual void Head(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            this.Get(resourceType, id);
        }

        /// <summary>
        /// Service is not ready
        /// </summary>
        protected void ThrowIfNotReady()
        {
            if (!ApplicationServiceContext.Current.IsRunning)
            {
                throw new DomainStateException();
            }
        }

        /// <summary>
        /// Lock resource
        /// </summary>
        public virtual object Lock(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null && handler is ILockableResourceHandler)
                {
                    this.AclCheck(handler, nameof(ILockableResourceHandler.Lock));
                    this.ThrowIfPreConditionFails(handler, key);
                    var retVal = (handler as ILockableResourceHandler).Lock(Guid.TryParse(key, out var guidKey) ? (object)guidKey : key);
                    if (retVal == null)
                    {
                        throw new FileNotFoundException(key);
                    }
                    this.AddEtagHeader(retVal);
                    this.AddLastModifiedHeader(retVal);
                    this.AddContentLocationHeader(retVal);

                    // HTTP IF headers?
                    RestOperationContext.Current.OutgoingResponse.StatusCode = retVal == null ? (int)HttpStatusCode.NoContent : (int)HttpStatusCode.Created;
                    return retVal;
                }
                else if (handler == null)
                {
                    throw new FileNotFoundException(resourceType);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Unlock resource
        /// </summary>
        public virtual object UnLock(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null && handler is ILockableResourceHandler)
                {
                    this.AclCheck(handler, nameof(ILockableResourceHandler.Unlock));
                    this.ThrowIfPreConditionFails(handler, key);
                    var retVal = (handler as ILockableResourceHandler).Unlock(Guid.TryParse(key, out var guidKey) ? (object)guidKey : key);
                    if (retVal == null)
                    {
                        throw new FileNotFoundException(key);
                    }
                    this.AddEtagHeader(retVal);
                    this.AddLastModifiedHeader(retVal);
                    this.AddContentLocationHeader(retVal);

                    // HTTP IF headers?
                    RestOperationContext.Current.OutgoingResponse.StatusCode = retVal == null ? (int)HttpStatusCode.NoContent : (int)HttpStatusCode.Created;
                    return retVal;
                }
                else if (handler == null)
                {
                    throw new FileNotFoundException(resourceType);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Perform a search on the specified entity
        /// </summary>
        public virtual AmiCollection AssociationSearch(string resourceType, string key, string childResourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Query));

                    // Send the query to the resource handler
                    var query = RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString();

                    // Modified on?
                    if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null)
                    {
                        query.Add("modifiedOn", ">" + RestOperationContext.Current.IncomingRequest.GetIfModifiedSince()?.ToString("o"));
                    }

                    // Query for results
                    IQueryResultSet results = null;
                    if (Guid.TryParse(key, out Guid keyUuid))
                    {
                        results = handler.QueryChildObjects(keyUuid, childResourceType, query);
                    }
                    else
                    {
                        results = handler.QueryChildObjects(key, childResourceType, query);
                    }

                    // Now apply controls
                    var retVal = results.ApplyResultInstructions(query, out int offset, out int totalCount).OfType<Object>();

                    if (this.m_configuration?.IncludeMetadataHeadersOnSearch == true)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetLastModified((retVal.OfType<IdentifiedData>().OrderByDescending(o => o.ModifiedOn).FirstOrDefault()?.ModifiedOn.DateTime ?? DateTime.Now));
                    }

                    // Last modification time and not modified conditions
                    if ((RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null ||
                        RestOperationContext.Current.IncomingRequest.GetIfNoneMatch() != null) &&
                        totalCount == 0)
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 304;
                        return null;
                    }
                    else
                    {
                        return new AmiCollection(retVal, offset, totalCount);
                    }
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Create an associated entity
        /// </summary>
        public virtual object AssociationCreate(string resourceType, string key, string childResourceType, object body)
        {
            this.ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.AddChildObject));
                    this.ThrowIfPreConditionFails(handler, key);
                    var retVal = handler.AddChildObject(Guid.TryParse(key, out var guidKey) ? (object)guidKey : key, childResourceType, body);
                    
                    RestOperationContext.Current.OutgoingResponse.StatusCode = retVal == null ? (int)HttpStatusCode.NoContent : (int)System.Net.HttpStatusCode.Created;
                    this.AddEtagHeader(retVal);
                    this.AddContentLocationHeader(retVal);
                    this.AddLastModifiedHeader(retVal);
                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Removes an associated entity from the scoping property path
        /// </summary>
        public virtual object AssociationRemove(string resourceType, string key, string childResourceType, string scopedEntityKey)
        {
            this.ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.RemoveChildObject));
                    this.ThrowIfPreConditionFails(handler, key);
                    var retVal = handler.RemoveChildObject(Guid.TryParse(key, out var uuid) ? (object)uuid : key, childResourceType, Guid.TryParse(scopedEntityKey, out var scopedUuid) ? (object)scopedUuid : scopedEntityKey);

                    var versioned = retVal as IVersionedData;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.OK;
                    this.AddEtagHeader(retVal);
                    this.AddContentLocationHeader(retVal);
                    this.AddLastModifiedHeader(retVal);
                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Removes an associated entity from the scoping property path
        /// </summary>
        public virtual object AssociationGet(string resourceType, string key, string childResourceType, string scopedEntityKey)
        {
            this.ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.GetChildObject));
                    this.ThrowIfPreConditionFails(handler, scopedEntityKey);
                    var retVal = handler.GetChildObject(Guid.TryParse(key, out var uuid) ? (object)uuid : key, childResourceType, Guid.TryParse(scopedEntityKey, out var childUuid) ? (object)childUuid : scopedEntityKey);

                    var versioned = retVal as IVersionedData;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.OK;
                    this.AddEtagHeader(retVal);
                    this.AddContentLocationHeader(retVal);
                    this.AddLastModifiedHeader(retVal);
                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Invoke the specified method on the API
        /// </summary>
        public virtual object InvokeMethod(string resourceType, string id, string operationName, ParameterCollection body)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType) as IOperationalApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IOperationalApiResourceHandler.InvokeOperation));

                    var retValRaw = handler.InvokeOperation(id, operationName, body);

                    if (retValRaw is IdentifiedData retVal)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                        RestOperationContext.Current.OutgoingResponse.SetLastModified(retVal.ModifiedOn.DateTime);

                        // HTTP IF headers?
                        if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null &&
                            retVal.ModifiedOn <= RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() ||
                            RestOperationContext.Current.IncomingRequest.GetIfNoneMatch()?.Any(o => retVal.Tag == o) == true)
                        {
                            RestOperationContext.Current.OutgoingResponse.StatusCode = 304;
                            return null;
                        }
                    }
                    return retValRaw;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Check-in the specified object
        /// </summary>
        public virtual object CheckIn(string resourceType, string key)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType) as ICheckoutResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(ICheckoutResourceHandler.CheckIn));
                    return handler.CheckIn(Guid.Parse(key));
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Check-out the specified object
        /// </summary>
        public virtual object CheckOut(string resourceType, string key)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType) as ICheckoutResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(ICheckoutResourceHandler.CheckIn));
                    return handler.CheckOut(Guid.Parse(key));
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Invoke a method which is not tied to a classifier object
        /// </summary>
        public virtual object InvokeMethod(string resourceType, string operationName, ParameterCollection body)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType) as IOperationalApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IOperationalApiResourceHandler.InvokeOperation));

                    var retValRaw = handler.InvokeOperation(null, operationName, body);

                    if (retValRaw is IdentifiedData retVal)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                        RestOperationContext.Current.OutgoingResponse.SetLastModified(retVal.ModifiedOn.DateTime);

                        // HTTP IF headers?
                        if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null &&
                            retVal.ModifiedOn <= RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() ||
                            RestOperationContext.Current.IncomingRequest.GetIfNoneMatch()?.Any(o => retVal.Tag == o) == true)
                        {
                            RestOperationContext.Current.OutgoingResponse.StatusCode = 304;
                            return null;
                        }
                    }
                    return retValRaw;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }
    }
}