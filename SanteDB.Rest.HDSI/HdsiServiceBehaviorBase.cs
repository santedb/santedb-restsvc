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
 * Date: 2021-8-27
 */
using RestSrvr;
using RestSrvr.Attributes;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SanteDB.Rest.HDSI
{
    /// <summary>
    /// Health Data Service Interface (HDSI)
    /// </summary>
    /// <remarks>Represents generic implementation of the the Health Data Service Interface (HDSI) contract</remarks>
    [ServiceBehavior(Name = "HDSI", InstanceMode = ServiceInstanceMode.Singleton)]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public abstract class HdsiServiceBehaviorBase : IHdsiServiceContract
    {
        /// <summary>
        /// The trace source for HDSI based implementations
        /// </summary>
        protected readonly Tracer m_traceSource = Tracer.GetTracer(typeof(HdsiServiceBehaviorBase));

        /// <summary>
        /// Get resource handler
        /// </summary>
        protected abstract ResourceHandlerTool GetResourceHandler();

        /// <summary>
        /// Ad-hoc cache method
        /// </summary>
        protected readonly IDataCachingService m_dataCache;

        /// <summary>
        /// Locale service
        /// </summary>
        protected readonly ILocalizationService m_localeService;
        private readonly IPolicyEnforcementService m_pepService;
        private readonly IPatchService m_patchService;
        private readonly IBarcodeProviderService m_barcodeService;

        public IResourcePointerService m_resourcePointerService { get; }

        /// <summary>
        /// HDSI Service Behavior
        /// </summary>
        public HdsiServiceBehaviorBase(IDataCachingService dataCache, ILocalizationService localeService, IPatchService patchService, IPolicyEnforcementService pepService, IBarcodeProviderService barcodeService, IResourcePointerService resourcePointerService)
        {
            this.m_dataCache = dataCache;
            this.m_localeService = localeService;
            this.m_pepService = pepService;
            this.m_patchService = patchService;
            this.m_barcodeService = barcodeService;
            this.m_resourcePointerService = resourcePointerService;
        }

        /// <summary>
        /// Create content location
        /// </summary>
        protected String CreateContentLocation(params Object[] parts)
        {
            var requestUri = RestOperationContext.Current.IncomingRequest.Url;
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = requestUri.Scheme;
            uriBuilder.Host = requestUri.Host;
            uriBuilder.Port = requestUri.Port;
            uriBuilder.Path = RestOperationContext.Current.ServiceEndpoint.Description.ListenUri.AbsolutePath;
            if (!uriBuilder.Path.EndsWith("/"))
                uriBuilder.Path += "/";
            uriBuilder.Path += String.Join("/", parts);
            return uriBuilder.ToString();
        }

        /// <summary>
        /// Perform an ACL check
        /// </summary>
        private void AclCheck(Object handler, String action)
        {
            foreach (var dmn in this.GetDemands(handler, action))
                this.m_pepService.Demand(dmn);
        }

        /// <summary>
        /// Get demands
        /// </summary>
        private String[] GetDemands(object handler, string action)
        {
            var demands = handler.GetType().GetMethods().Where(o => o.Name == action).SelectMany(method => method.GetCustomAttributes<DemandAttribute>());
            if (demands.Any(o => o.Override))
                return demands.Where(o => o.Override).Select(o => o.PolicyId).ToArray();
            else
                return demands.Select(o => o.PolicyId).ToArray();
        }

        /// <summary>
        /// Ping the server
        /// </summary>
        public void Ping()
        {
            this.ThrowIfNotReady();
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Create the specified resource
        /// </summary>
        public virtual IdentifiedData Create(string resourceType, IdentifiedData body)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Create));
                    var retVal = handler.Create(body, false) as IdentifiedData;
                    var versioned = retVal as IVersionedData;

                    if (retVal == null)
                        return null;
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                        if (versioned != null)
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, retVal.Key, "_history", versioned.VersionKey));
                        else
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, retVal.Key));

                        return retVal;
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error creating {body}", e);
            }
        }

        /// <summary>
        /// Create or update the specified object
        /// </summary>
        public virtual IdentifiedData CreateUpdate(string resourceType, string id, IdentifiedData body)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Create));
                    var retVal = handler.Create(body, true) as IdentifiedData;
                    var versioned = retVal as IVersionedData;

                    if (retVal == null)
                        return null;
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);

                        if (versioned != null)
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned.VersionKey));
                        else
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));

                        return retVal;
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error creating/updating {body}", e);
            }
        }

        /// <summary>
        /// Get the specified object
        /// </summary>
        public virtual IdentifiedData Get(string resourceType, string id)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    if (handler is IChainedApiResourceHandler chainedHandler && chainedHandler.TryGetChainedResource(id, ChildObjectScopeBinding.Class, out IApiChildResourceHandler childHandler))
                    {
                        return this.AssociationSearch(resourceType, id) as IdentifiedData;
                    }

                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));

                    Guid objectId = Guid.Parse(id);
                    var ifModifiedHeader = RestOperationContext.Current.IncomingRequest.GetIfModifiedSince();
                    var ifNoneMatchHeader = RestOperationContext.Current.IncomingRequest.GetIfNoneMatch();

                    // HTTP IF headers? - before we go to the DB lets check the cache for them
                    if (ifNoneMatchHeader?.Any() == true || ifModifiedHeader.HasValue)
                    {
                        var cacheResult = this.m_dataCache.GetCacheItem(objectId);

                        if (cacheResult != null && (ifNoneMatchHeader?.Contains(cacheResult.Tag) == true ||
                                cacheResult.ModifiedOn <= ifModifiedHeader))
                        {
                            if (cacheResult is ITaggable tagged)
                            {
                                if (tagged.GetTag(SanteDBConstants.DcdrRefetchTag) == null)
                                {
                                    RestOperationContext.Current.OutgoingResponse.StatusCode = 304;
                                    return null;
                                }
                                else
                                {
                                    tagged.RemoveTag(SanteDBConstants.DcdrRefetchTag);
                                }
                            }
                            else
                            {
                                RestOperationContext.Current.OutgoingResponse.StatusCode = 304;
                                return null;
                            }
                        }
                    }

                    var retVal = handler.Get(objectId, Guid.Empty) as IdentifiedData;
                    if (retVal == null)
                        throw new FileNotFoundException(id);

                    RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(retVal.ModifiedOn.DateTime);

                    // HTTP IF headers?
                    if (ifModifiedHeader.HasValue &&
                        retVal.ModifiedOn <= RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() ||
                        ifNoneMatchHeader?.Any(o => retVal.Tag == o) == true)
                    {
                        if (!(retVal is ITaggable tagged) || tagged.GetTag(SanteDBConstants.DcdrRefetchTag) == null)
                        {
                            RestOperationContext.Current.OutgoingResponse.StatusCode = 304;
                            return null;
                        }
                    }
                    return retVal;
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error getting {resourceType}/{id}", e);
            }
        }

        /// <summary>
        /// Gets a specific version of a resource
        /// </summary>
        public virtual IdentifiedData GetVersion(string resourceType, string id, string versionId)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));
                    var retVal = handler.Get(Guid.Parse(id), Guid.Parse(versionId)) as IdentifiedData;
                    if (retVal == null)
                        throw new FileNotFoundException(id);

                    if (RestOperationContext.Current.IncomingRequest.QueryString["_bundle"] == "true")
                        return Bundle.CreateBundle(retVal);
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                        return retVal;
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error getting version {resourceType}/{id}/history/{versionId}", e);
            }
        }

        /// <summary>
        /// Get the schema which defines this service
        /// </summary>
        public XmlSchema GetSchema(int schemaId)
        {
            this.ThrowIfNotReady();
            try
            {
                XmlSchemas schemaCollection = new XmlSchemas();

                XmlReflectionImporter importer = new XmlReflectionImporter("http://santedb.org/model");
                XmlSchemaExporter exporter = new XmlSchemaExporter(schemaCollection);

                foreach (var cls in this.GetResourceHandler().Handlers.Where(o => o.Scope == typeof(IHdsiServiceContract)).Select(o => o.Type))
                    exporter.ExportTypeMapping(importer.ImportTypeMapping(cls, "http://santedb.org/model"));

                if (schemaId > schemaCollection.Count)
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 404;
                    return null;
                }
                else
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 200;
                    RestOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
                    return schemaCollection[schemaId];
                }
            }
            catch (Exception e)
            {
                //                RestOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                return null;
            }
        }

        /// <summary>
        /// Gets the recent history an object
        /// </summary>
        [UrlParameter("_since", typeof(Guid), "The last version of the object that should be returned")]
        public virtual IdentifiedData History(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType);

                if (handler != null)
                {
                    String since = RestOperationContext.Current.IncomingRequest.QueryString["_since"];
                    Guid sinceGuid = since != null ? Guid.Parse(since) : Guid.Empty;

                    // Query
                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));
                    var retVal = handler.Get(Guid.Parse(id), Guid.Empty) as IVersionedData;
                    List<IVersionedData> histItm = new List<IVersionedData>() { retVal };
                    while (retVal.PreviousVersionKey.HasValue)
                    {
                        retVal = handler.Get(Guid.Parse(id), retVal.PreviousVersionKey.Value) as IVersionedData;
                        if (retVal != null)
                            histItm.Add(retVal);
                        // Should we stop fetching?
                        if (retVal?.VersionKey == sinceGuid)
                            break;
                    }

                    // Lock the item
                    return new Bundle(histItm.OfType<IdentifiedData>(), 0, histItm.Count);
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error getting history for {resourceType}/{id}");
            }
        }

        /// <summary>
        /// Perform a search on the specified resource type
        /// </summary>
        [UrlParameter("_offset", typeof(int), "The offet of the first result to return")]
        [UrlParameter("_count", typeof(int), "The count of items to return in this result set")]
        [UrlParameter("_orderBy", typeof(string), "Instructs the result set to be ordered")]
        public virtual IdentifiedData Search(string resourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Query));

                    // Send the query to the resource handler
                    var query = NameValueCollection.ParseQueryString(RestOperationContext.Current.IncomingRequest.Url.Query);

                    // Modified on?
                    if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null)
                        query.Add("modifiedOn", ">" + RestOperationContext.Current.IncomingRequest.GetIfModifiedSince()?.ToString("o"));

                    // Query for results
                    var results = handler.Query(query);

                    // Now apply controls
                    var retVal = results.ApplyResultInstructions(query, out int offset, out int totalCount).OfType<IdentifiedData>();

                    RestOperationContext.Current.OutgoingResponse.SetLastModified((retVal.OrderByDescending(o => o.ModifiedOn).FirstOrDefault()?.ModifiedOn.DateTime ?? DateTime.Now));

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
                        return new Bundle(retVal, offset, totalCount);
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
                throw new Exception($"Error searching {resourceType}", e);
            }
        }

        /// <summary>
        /// Get the server's current time
        /// </summary>
        public DateTime Time()
        {
            this.ThrowIfNotReady();
            return DateTime.Now;
        }

        /// <summary>
        /// Update the specified resource
        /// </summary>
        public virtual IdentifiedData Update(string resourceType, string id, IdentifiedData body)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Update));

                    var retVal = handler.Update(body) as IdentifiedData;
                    var versioned = retVal as IVersionedData;

                    if (retVal == null)
                        return null;
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);

                        if (versioned != null)
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned.VersionKey));
                        else
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));

                        return retVal;
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error updating {resourceType}/{id}", e);
            }
        }

        /// <summary>
        /// Obsolete the specified data
        /// </summary>
        public virtual IdentifiedData Delete(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    if (RestOperationContext.Current.IncomingRequest.Headers.AllKeys.Contains("X-Delete-Mode"))
                    {
                        throw new NotSupportedException(this.m_localeService.GetString(ErrorMessageStrings.OBSOLETE_FUNCTION, new { name = "X-Delete-Mode" }));
                    }

                    this.AclCheck(handler, nameof(IApiResourceHandler.Delete));
                    var retVal = handler.Delete(Guid.Parse(id)) as IdentifiedData;

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    if (retVal is IVersionedData versioned)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned.VersionKey));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));

                    return retVal;
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error deleting {resourceType}/{id}", e);
            }
        }

        /// <summary>
        /// Perform the search but only return the headers
        /// </summary>
        public virtual void HeadSearch(string resourceType)
        {
            this.ThrowIfNotReady();
            RestOperationContext.Current.IncomingRequest.QueryString.Add("_count", "1");
            this.Search(resourceType);
        }

        /// <summary>
        /// Get just the headers
        /// </summary>
        public virtual void Head(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            this.Get(resourceType, id);
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
                // Validate
                var match = RestOperationContext.Current.IncomingRequest.Headers["If-Match"];
                if (match == null)
                    throw new InvalidOperationException("Missing If-Match header");

                // First we load
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType);

                if (handler == null)
                    throw new FileNotFoundException(resourceType);

                // Next we get the current version
                this.AclCheck(handler, nameof(IApiResourceHandler.Get));

                var existing = handler.Get(Guid.Parse(id), Guid.Empty) as IdentifiedData;
                var force = Convert.ToBoolean(RestOperationContext.Current.IncomingRequest.Headers["X-Patch-Force"] ?? "false");

                if (existing == null)
                    throw new FileNotFoundException($"/{resourceType}/{id}");
                else if (existing.Tag != match && !force)
                {
                    this.m_traceSource.TraceError("Object {0} ETAG is {1} but If-Match specified {2}", existing.Key, existing.Tag, match);
                    throw new PatchAssertionException(match, existing.Tag, null);
                }
                else if (body == null)
                    throw new ArgumentNullException(nameof(body));
                else
                {
                    // Force load all properties for existing
                    var applied = this.m_patchService.Patch(body, existing, force);
                    this.AclCheck(handler, nameof(IApiResourceHandler.Update));
                    var data = handler.Update(applied) as IdentifiedData;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 204;
                    RestOperationContext.Current.OutgoingResponse.SetETag(data.Tag);
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(applied.ModifiedOn.DateTime);
                    var versioned = (data as IVersionedData)?.VersionKey;
                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));
                }
            }
            catch (PatchAssertionException e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceWarning(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Assertion failed while patching {resourceType}/{id}", e);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error patching {resourceType}/{id}", e);
            }
        }

        /// <summary>
        /// Get options
        /// </summary>
        public virtual ServiceOptions Options()
        {
            this.ThrowIfNotReady();
            try
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = 200;
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Allow", $"GET, PUT, POST, OPTIONS, HEAD, DELETE{(this.m_patchService != null ? ", PATCH" : null)}");
                if (this.m_patchService != null)
                    RestOperationContext.Current.OutgoingResponse.Headers.Add("Accept-Patch", "application/xml+sdb-patch");

                // Service options
                var retVal = new ServiceOptions()
                {
                    InterfaceVersion = "1.0.0.0",
                    Resources = new List<ServiceResourceOptions>()
                };

                // Get the resources which are supported
                foreach (var itm in this.GetResourceHandler().Handlers)
                {
                    var svc = this.ResourceOptions(itm.ResourceName);
                    retVal.Resources.Add(svc);
                }

                return retVal;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception("Error gathering service options", e);
            }
        }

        /// <summary>
        /// Throw if the service is not ready
        /// </summary>
        public abstract void ThrowIfNotReady();

        /// <summary>
        /// Options resource
        /// </summary>
        public virtual ServiceResourceOptions ResourceOptions(string resourceType)
        {
            var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType);
            if (handler == null)
                throw new FileNotFoundException(resourceType);
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
                if (this.m_patchService != null &&
                    handler.Capabilities.HasFlag(ResourceCapabilityType.Update))
                    caps.Add(new ServiceResourceCapability(ResourceCapabilityType.Patch, this.GetDemands(handler, nameof(IApiResourceHandler.Update))));

                // To expose associated objects
                var childResources = new List<ChildServiceResourceOptions>();
                if (handler is IChainedApiResourceHandler associative)
                {
                    childResources.AddRange(associative.ChildResources.Select(r => new ChildServiceResourceOptions(r.Name, r.PropertyType, r.Capabilities.ToResourceCapabilityStatement(getCaps).ToList(), r.ScopeBinding, ChildObjectClassification.Resource)));
                }
                if (handler is IOperationalApiResourceHandler operational)
                {
                    childResources.AddRange(operational.Operations.Select(o => new ChildServiceResourceOptions(o.Name, typeof(Object), ResourceCapabilityType.Create.ToResourceCapabilityStatement(getCaps).ToList(), o.ScopeBinding, ChildObjectClassification.RpcOperation)));
                }
                // Associateive
                return new ServiceResourceOptions(resourceType, handler.Type, caps, childResources);
            }
        }

        /// <summary>
        /// Perform a search on the specified entity
        /// </summary>
        [UrlParameter("_offset", typeof(int), "The offet of the first result to return")]
        [UrlParameter("_count", typeof(int), "The count of items to return in this result set")]
        public virtual Object AssociationSearch(string resourceType, string key, string childResourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                if (!Guid.TryParse(key, out Guid keyGuid))
                {
                    throw new ArgumentException(nameof(key));
                }

                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Query));

                    // Send the query to the resource handler
                    var query = NameValueCollection.ParseQueryString(RestOperationContext.Current.IncomingRequest.Url.Query);

                    // Modified on?
                    if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null)
                        query.Add("modifiedOn", ">" + RestOperationContext.Current.IncomingRequest.GetIfModifiedSince()?.ToString("o"));

                    // Query for results
                    var results = handler.QueryChildObjects(keyGuid, childResourceType, query);

                    // Now apply controls
                    var retVal = results.ApplyResultInstructions(query, out int offset, out int totalCount).OfType<IdentifiedData>();

                    RestOperationContext.Current.OutgoingResponse.SetLastModified((retVal.OrderByDescending(o => o.ModifiedOn).FirstOrDefault()?.ModifiedOn.DateTime ?? DateTime.Now));

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
                        return new Bundle(retVal, offset, totalCount);
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);
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
                IChainedApiResourceHandler handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.AddChildObject));
                    var retVal = handler.AddChildObject(Guid.Parse(key), childResourceType, body) as IdentifiedData;

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    if (retVal != null)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, key, childResourceType, retVal.Key));
                    }

                    return retVal;
                }
                else
                    throw new FileNotFoundException(resourceType);
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
                IChainedApiResourceHandler handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.RemoveChildObject));

                    var retVal = handler.RemoveChildObject(Guid.Parse(key), childResourceType, Guid.Parse(scopedEntityKey)) as IdentifiedData;

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, key, childResourceType, retVal.Key));
                    return retVal;
                }
                else
                    throw new FileNotFoundException(resourceType);
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
                IChainedApiResourceHandler handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.GetChildObject));

                    var retVal = handler.GetChildObject(Guid.Parse(key), childResourceType, Guid.Parse(scopedEntityKey));

                    if (retVal is IdentifiedData idData)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(idData.Tag);
                        RestOperationContext.Current.OutgoingResponse.SetLastModified(idData.ModifiedOn.DateTime);

                        // HTTP IF headers?
                        if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null &&
                            idData.ModifiedOn <= RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() ||
                            RestOperationContext.Current.IncomingRequest.GetIfNoneMatch()?.Any(o => idData.Tag == o) == true)
                        {
                            RestOperationContext.Current.OutgoingResponse.StatusCode = 304;
                            return null;
                        }
                        else
                        {
                            return retVal;
                        }
                    }
                    else
                    {
                        return retVal;
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Generate the barcode for the specified object with specified authority
        /// </summary>
        public virtual Stream GetBarcode(string resourceType, string id, string authority)
        {
            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    if (this.m_barcodeService == null)
                        throw new InvalidOperationException("Cannot find barcode generator service");

                    Guid objectId = Guid.Parse(id);
                    var data = handler.Get(objectId, Guid.Empty) as IdentifiedData;
                    if (data == null)
                        throw new KeyNotFoundException($"{resourceType} {id}");
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.ContentType = "image/png";

                        // Specific ID
                        if (Guid.TryParse(authority, out Guid authorityId))
                        {
                            if (data is Entity entity)
                                return this.m_barcodeService.Generate(entity.Identifiers.Where(o => o.AuthorityKey == authorityId));
                            else if (data is Act act)
                                return this.m_barcodeService.Generate(act.Identifiers.Where(o => o.AuthorityKey == authorityId));
                            else
                                return null;
                        }
                        else
                        {
                            if (data is Entity entity)
                                return this.m_barcodeService.Generate(entity.Identifiers);
                            else if (data is Act act)
                                return this.m_barcodeService.Generate(act.Identifiers);
                            else
                                return null;
                        }
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Error generating barcode for {0} - {1}", resourceType, e);
                throw new Exception($"Could not generate visual code for {resourceType}/{id}", e);
            }
        }

        /// <summary>
        /// Touches the specified data object
        /// </summary>
        public virtual IdentifiedData Touch(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler is IApiResourceHandlerEx exResourceHandler)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Update));

                    var retVal = exResourceHandler.Touch(Guid.Parse(id)) as IdentifiedData;

                    var versioned = retVal as IVersionedData;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);

                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned.VersionKey));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));

                    return retVal;
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error updating {resourceType}/{id}", e);
            }
        }

        /// <summary>
        /// Resolve a code
        /// </summary>
        /// <param name="body">The content of the barcode/image obtained from the user interface</param>
        public virtual void ResolvePointer(System.Collections.Specialized.NameValueCollection parms)
        {
            try
            {
                if (this.m_resourcePointerService == null)
                    throw new InvalidOperationException("Cannot find pointer service");

                bool validate = true;
                if (String.IsNullOrEmpty(parms["code"]))
                    throw new ArgumentException("SEARCH have url-form encoded payload with parameter code");
                else if (!String.IsNullOrEmpty(parms["validate"]))
                    Boolean.TryParse(parms["validate"], out validate);

                var result = this.m_resourcePointerService.ResolveResource(parms["code"], validate);

                // Create a 303 see other
                if (result != null)
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.SeeOther;
                    RestOperationContext.Current.OutgoingResponse.AddHeader("Location", this.CreateContentLocation(result.GetType().GetSerializationName(), result.Key.Value));
                }
                else
                    throw new KeyNotFoundException($"Object not found");
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error searching by pointer", e);
            }
        }

        /// <summary>
        /// Get pointer to the specified resource
        /// </summary>
        public virtual Stream GetPointer(string resourceType, string id, string authority)
        {
            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    if (this.m_resourcePointerService == null)
                        throw new InvalidOperationException("Cannot find resource pointer service");

                    Guid objectId = Guid.Parse(id);
                    Guid authorityId = Guid.Parse(authority);
                    var data = handler.Get(objectId, Guid.Empty) as IdentifiedData;
                    if (data == null)
                        throw new KeyNotFoundException($"{resourceType} {id}");
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.ContentType = "application/jose";
                        if (data is Entity entity)
                        {
                            return new MemoryStream(Encoding.UTF8.GetBytes(this.m_resourcePointerService.GeneratePointer(entity.Identifiers.Where(o => o.AuthorityKey == authorityId))));
                        }
                        else if (data is Act act)
                        {
                            return new MemoryStream(Encoding.UTF8.GetBytes(this.m_resourcePointerService.GeneratePointer(act.Identifiers.Where(o => o.AuthorityKey == authorityId))));
                        }
                        else
                            return null;
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Error fetching pointer for {0} - {1}", resourceType, e);
                throw new Exception($"Could fetching pointer code for {resourceType}/{id}", e);
            }
        }

        /// <summary>
        /// Copy (download) a remote object to this instance
        /// </summary>
        public abstract IdentifiedData Copy(String reosurceType, String id);

        /// <summary>
        /// Invoke the specified method on the API
        /// </summary>
        public virtual object InvokeMethod(string resourceType, string id, string operationName, ParameterCollection body)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType) as IOperationalApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IOperationalApiResourceHandler.InvokeOperation));

                    var retValRaw = handler.InvokeOperation(Guid.Parse(id), operationName, body);

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
                    throw new FileNotFoundException(resourceType);
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
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType) as ICheckoutResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(ICheckoutResourceHandler.CheckIn));
                    return handler.CheckIn(Guid.Parse(key));
                }
                else
                    throw new FileNotFoundException(resourceType);
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
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType) as ICheckoutResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(ICheckoutResourceHandler.CheckIn));
                    return handler.CheckOut(Guid.Parse(key));
                }
                else
                    throw new FileNotFoundException(resourceType);
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
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType) as IOperationalApiResourceHandler;
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
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Perform a sub-get on a child resource and key without parent instance (exmaple: GET /Patient/extendedProperty/UUID)
        /// </summary>
        public virtual object AssociationGet(string resourceType, string childResourceType, string childResourceKey)
        {
            this.ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.GetChildObject));

                    var retVal = handler.GetChildObject(null, childResourceType, Guid.Parse(childResourceKey)) as IdentifiedData;

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
                    else
                    {
                        return retVal;
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Operates a DELETE on the instance
        /// </summary>
        public virtual object AssociationRemove(string resourceType, string childResourceType, string childResourceKey)
        {
            this.ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.RemoveChildObject));

                    var retVal = handler.RemoveChildObject(null, childResourceType, Guid.Parse(childResourceKey)) as IdentifiedData;

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, childResourceType, retVal.Key));
                    return retVal;
                }
                else
                    throw new FileNotFoundException(resourceType);
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
        [UrlParameter("_offset", typeof(int), "The offet of the first result to return")]
        [UrlParameter("_count", typeof(int), "The count of items to return in this result set")]
        [UrlParameter("_all", typeof(bool), "True if all properties should be expanded")]
        [UrlParameter("_expand", typeof(string), "The names of the properties which should be forced loaded", Multiple = true)]
        [UrlParameter("_exclude", typeof(string), "The names of the properties which should removed from the bundle", Multiple = true)]
        public virtual Object AssociationSearch(string resourceType, string childResourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IHdsiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Query));

                    // Send the query to the resource handler
                    var query = NameValueCollection.ParseQueryString(RestOperationContext.Current.IncomingRequest.Url.Query);

                    // Modified on?
                    if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null)
                        query.Add("modifiedOn", ">" + RestOperationContext.Current.IncomingRequest.GetIfModifiedSince()?.ToString("o"));

                    // Query for results
                    var results = handler.QueryChildObjects(null, childResourceType, query);

                    // Now apply controls
                    var retVal = results.ApplyResultInstructions(query, out int offset, out int totalCount).OfType<IdentifiedData>();

                    RestOperationContext.Current.OutgoingResponse.SetLastModified((retVal.OrderByDescending(o => o.ModifiedOn).FirstOrDefault()?.ModifiedOn.DateTime ?? DateTime.Now));

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
                        return new Bundle(retVal, offset, totalCount);
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
    }
}