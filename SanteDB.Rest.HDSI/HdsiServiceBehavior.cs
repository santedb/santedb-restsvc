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
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
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
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using SanteDB.Rest.HDSI.Vrp;
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
    public class HdsiServiceBehavior : IHdsiServiceContract
    {
        /// <summary>
        /// The trace source for HDSI based implementations
        /// </summary>
        protected readonly Tracer m_traceSource = Tracer.GetTracer(typeof(HdsiServiceBehavior));

        // Resource handler tool
        private readonly ResourceHandlerTool m_resourceHandlerTool;

        /// <summary>
        /// Ad-hoc cache method
        /// </summary>
        protected readonly IDataCachingService m_dataCachingService;

        /// <summary>
        /// Locale service
        /// </summary>
        protected readonly ILocalizationService m_localeService;
        private readonly IPolicyEnforcementService m_pepService;
        private readonly IPatchService m_patchService;
        private readonly IBarcodeProviderService m_barcodeService;
        private readonly IResourcePointerService m_resourcePointerService;

        /// <summary>
        /// Get the resource handler for the named resource
        /// </summary>
        protected IApiResourceHandler GetResourceHandler(String resourceTypeName) => this.m_resourceHandlerTool.GetResourceHandler<IHdsiServiceContract>(resourceTypeName);

        /// <summary>
        /// For REST service initialization
        /// </summary>
        public HdsiServiceBehavior() :
            this(ApplicationServiceContext.Current.GetService<IDataCachingService>(),
                ApplicationServiceContext.Current.GetService<ILocalizationService>(),
                ApplicationServiceContext.Current.GetService<IPatchService>(),
                ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>(),
                ApplicationServiceContext.Current.GetService<IBarcodeProviderService>(),
                ApplicationServiceContext.Current.GetService<IResourcePointerService>(),
                ApplicationServiceContext.Current.GetService<IServiceManager>()
                )
        {

        }
        /// <summary>
        /// HDSI Service Behavior
        /// </summary>
        public HdsiServiceBehavior(IDataCachingService dataCache, ILocalizationService localeService, IPatchService patchService, IPolicyEnforcementService pepService, IBarcodeProviderService barcodeService, IResourcePointerService resourcePointerService, IServiceManager serviceManager)
        {
            this.m_dataCachingService = dataCache;
            this.m_localeService = localeService;
            this.m_pepService = pepService;
            this.m_patchService = patchService;
            this.m_barcodeService = barcodeService;
            this.m_resourcePointerService = resourcePointerService;
            this.m_resourceHandlerTool = new ResourceHandlerTool(
                        serviceManager.GetAllTypes()
                        .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IApiResourceHandler).IsAssignableFrom(t))
                        .ToList(), typeof(IHdsiServiceContract)
                    );
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
            {
                uriBuilder.Path += "/";
            }

            uriBuilder.Path += String.Join("/", parts);
            return uriBuilder.ToString();
        }

        /// <summary>
        /// Perform an ACL check
        /// </summary>
        private void AclCheck(Object handler, String action)
        {
            foreach (var dmn in this.GetDemands(handler, action))
            {
                this.m_pepService.Demand(dmn);
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

        /// <inheritdoc/>
        public void Ping()
        {
            this.ThrowIfNotReady();
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.NoContent;
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData Create(string resourceType, IdentifiedData body)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Create));
                    var retVal = handler.Create(body, false) as IdentifiedData;
                    var versioned = retVal as IVersionedData;

                    if (retVal == null)
                    {
                        return null;
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                        if (versioned != null)
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, retVal.Key, "_history", versioned.VersionKey));
                        }
                        else
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, retVal.Key));
                        }

                        return retVal;
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

                throw new Exception($"Error creating {body}", e);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData CreateUpdate(string resourceType, string id, IdentifiedData body)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Create));
                    var retVal = handler.Create(body, true) as IdentifiedData;
                    var versioned = retVal as IVersionedData;

                    if (retVal == null)
                    {
                        return null;
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);

                        if (versioned != null)
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned.VersionKey));
                        }
                        else
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));
                        }

                        return retVal;
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
                throw new Exception($"Error creating/updating {body}", e);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpBundleRelatedParameterName, typeof(bool), "True if the server should send related objects to the caller in a bundle")]
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData Get(string resourceType, string id)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    if (handler is IChainedApiResourceHandler chainedHandler && chainedHandler.TryGetChainedResource(id, ChildObjectScopeBinding.Class, out IApiChildResourceHandler childHandler))
                    {
                        return this.AssociationSearch(resourceType, id) as IdentifiedData;
                    }

                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));

                    Guid objectId = Guid.Parse(id);
                    this.ThrowIfPreConditionFails(handler, objectId);

                    IdentifiedData retVal = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = handler.Get(objectId, Guid.Empty) as IdentifiedData;
                    }

                    if (retVal == null)
                    {
                        throw new FileNotFoundException(id);
                    }

                    RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(retVal.ModifiedOn.DateTime);

                    if (Boolean.TryParse(RestOperationContext.Current.IncomingRequest.Headers[QueryControlParameterNames.HttpBundleRelatedParameterName], out var bundle)
                        && bundle)
                    {
                        return Bundle.CreateBundle(retVal);
                    }
                    else
                    {
                        return retVal;
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
                throw new Exception($"Error getting {resourceType}/{id}", e);
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
        private void ThrowIfPreConditionFails(IApiResourceHandler handler, Guid objectId)
        {

            var ifModifiedHeader = RestOperationContext.Current.IncomingRequest.GetIfModifiedSince();
            var ifUnmodifiedHeader = RestOperationContext.Current.IncomingRequest.GetIfUnmodifiedSince();
            var ifNoneMatchHeader = RestOperationContext.Current.IncomingRequest.GetIfNoneMatch()?.Select(o => this.ExtractValidateMatchHeader(handler.Type, o));
            var ifMatchHeader = RestOperationContext.Current.IncomingRequest.GetIfMatch()?.Select(o => this.ExtractValidateMatchHeader(handler.Type, o));

            // HTTP IF headers? - before we go to the DB lets check the cache for them
            if (ifNoneMatchHeader?.Any() == true || ifMatchHeader?.Any() == true || ifModifiedHeader.HasValue || ifUnmodifiedHeader.HasValue)
            {
                var cacheResult = this.m_dataCachingService.GetCacheItem(objectId);

                if (cacheResult != null && (ifNoneMatchHeader?.Contains(cacheResult.Tag) == true ||
                    !ifMatchHeader.Contains(cacheResult.Tag) == true ||
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
                    var checkQuery = $"id={objectId}".ParseQueryString();
                    if (!handler.Query(checkQuery).Any()) // Object doesn't exist
                    {
                        throw new KeyNotFoundException();
                    }

                    // If-None-Match when used with If-Modified-Since then If-None-Match has priority
                    if (ifNoneMatchHeader?.Any() == true ||
                        ifMatchHeader?.Any() == true)
                    {
                        checkQuery.Add("tag", ifNoneMatchHeader?.Where(c => Guid.TryParse(c, out _)).Select(o => $"{o}").ToArray());
                        checkQuery.Add("tag", ifMatchHeader?.Where(c => Guid.TryParse(c, out _)).Select(o => $"{o}").ToArray());
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

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData GetVersion(string resourceType, string id, string versionId)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));

                    var retVal = handler.Get(Guid.Parse(id), Guid.Parse(versionId)) as IdentifiedData;
                    if (retVal == null)
                    {
                        throw new FileNotFoundException(id);
                    }

                    if (Boolean.TryParse(RestOperationContext.Current.IncomingRequest.QueryString[QueryControlParameterNames.HttpBundleRelatedParameterName], out var bundle) && bundle)
                    {
                        return Bundle.CreateBundle(retVal);
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                        return retVal;
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
                throw new Exception($"Error getting version {resourceType}/{id}/history/{versionId}", e);
            }
        }

        /// <inheritdoc/>
        public XmlSchema GetSchema(int schemaId)
        {
            this.ThrowIfNotReady();
            try
            {
                XmlSchemas schemaCollection = new XmlSchemas();

                XmlReflectionImporter importer = new XmlReflectionImporter("http://santedb.org/model");
                XmlSchemaExporter exporter = new XmlSchemaExporter(schemaCollection);

                foreach (var cls in this.m_resourceHandlerTool.Handlers.Where(o => o.Scope == typeof(IHdsiServiceContract)).Select(o => o.Type))
                {
                    exporter.ExportTypeMapping(importer.ImportTypeMapping(cls, "http://santedb.org/model"));
                }

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

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpSinceParameterName, typeof(Guid), "The last version of the object that should be returned")]
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData History(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler(resourceType);

                if (handler != null)
                {


                    String since = RestOperationContext.Current.IncomingRequest.QueryString[QueryControlParameterNames.HttpSinceParameterName];
                    Guid sinceGuid = since != null ? Guid.Parse(since) : Guid.Empty;
                    var objectId = Guid.Parse(id);
                    this.ThrowIfPreConditionFails(handler, objectId);

                    // Query
                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));
                    var retVal = handler.Get(objectId, Guid.Empty) as IVersionedData;
                    List<IVersionedData> histItm = new List<IVersionedData>() { retVal };
                    while (retVal.PreviousVersionKey.HasValue)
                    {
                        retVal = handler.Get(Guid.Parse(id), retVal.PreviousVersionKey.Value) as IVersionedData;
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
                    return new Bundle(histItm.OfType<IdentifiedData>(), 0, histItm.Count);
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
                throw new Exception($"Error getting history for {resourceType}/{id}");
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpOffsetParameterName, typeof(int), "The offet of the first result to return")]
        [UrlParameter(QueryControlParameterNames.HttpCountParameterName, typeof(int), "The count of items to return in this result set")]
        [UrlParameter(QueryControlParameterNames.HttpIncludeTotalParameterName, typeof(bool), "True if the server should count the matching results. May reduce performance")]
        [UrlParameter(QueryControlParameterNames.HttpOrderByParameterName, typeof(string), "Instructs the result set to be ordered (in format: property:(asc|desc))")]
        [UrlParameter(QueryControlParameterNames.HttpQueryStateParameterName, typeof(Guid), "The query state identifier. This allows the client to get a stable result set.")]
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData Search(string resourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler(resourceType);
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
                    var retVal = results.ApplyResultInstructions(query, out int offset, out int totalCount).OfType<IdentifiedData>();

                    // Last modified object
                    var modifiedOnSelector = QueryExpressionParser.BuildPropertySelector(handler.Type, "modifiedOn", convertReturn: typeof(object));
                    var lastModified = (DateTime)results.OrderByDescending(modifiedOnSelector).Select<DateTimeOffset>(modifiedOnSelector).FirstOrDefault().DateTime;

                    if (lastModified != default(DateTime))
                    {
                        RestOperationContext.Current.OutgoingResponse.SetLastModified(lastModified);
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
                        using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                        {
                            return new Bundle(retVal, offset, totalCount);
                        }
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

        /// <inheritdoc/>
        public DateTime Time()
        {
            this.ThrowIfNotReady();
            return DateTime.Now;
        }

        /// <summary>
        /// Update the specified resource
        /// </summary>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData Update(string resourceType, string id, IdentifiedData body)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Update));

                    var objectId = Guid.Parse(id);

                    this.ThrowIfPreConditionFails(handler, objectId);


                    IdentifiedData retVal = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = handler.Update(body) as IdentifiedData;
                    }


                    if (retVal == null)
                    {
                        return null;
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);

                        if (retVal is IVersionedData versioned)
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned.VersionKey));
                        }
                        else
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));
                        }

                        return retVal;
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
                throw new Exception($"Error updating {resourceType}/{id}", e);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData Delete(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Delete));

                    var objectId = Guid.Parse(id);
                    this.ThrowIfPreConditionFails(handler, objectId);

                    IdentifiedData retVal = null;
                    // Adhere to delete mode
                    if (RestOperationContext.Current.IncomingRequest.Headers.TryGetValue(ExtendedHttpHeaderNames.DeleteModeHeaderName, out var deleteModeHeader) &&
                        Enum.TryParse<DeleteMode>(deleteModeHeader[0], out var deleteMode))
                    {
                        using (DataPersistenceControlContext.Create(LoadMode.SyncLoad, deleteMode, false))
                        {
                            retVal = handler.Delete(objectId) as IdentifiedData;
                        }
                    }
                    else
                    {
                        retVal = handler.Delete(objectId) as IdentifiedData;
                    }

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    if (retVal is IVersionedData versioned)
                    {
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned.VersionKey));
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));
                    }

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
                throw new Exception($"Error deleting {resourceType}/{id}", e);
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public virtual void Patch(string resourceType, string id, Patch body)
        {
            this.ThrowIfNotReady();

            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            try
            {
                // Validate
                var match = RestOperationContext.Current.IncomingRequest.Headers["If-Match"];
                if (match == null)
                {
                    throw new InvalidOperationException("Missing If-Match header");
                }

                // First we load
                var handler = this.GetResourceHandler(resourceType);

                if (handler == null)
                {
                    throw new FileNotFoundException(resourceType);
                }

                // Next we get the current version
                this.AclCheck(handler, nameof(IApiResourceHandler.Get));

                var objectId = Guid.Parse(id);
                var force = Convert.ToBoolean(RestOperationContext.Current.IncomingRequest.Headers[ExtendedHttpHeaderNames.ForceApplyPatchHeaderName] ?? "false");

                this.ThrowIfPreConditionFails(handler, objectId);

                using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                {
                    var existing = handler.Get(objectId, Guid.Empty) as IdentifiedData;

                    if (existing == null)
                    {
                        throw new FileNotFoundException($"/{resourceType}/{id}");
                    }
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
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned));
                        }
                        else
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));
                        }
                    }
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

        /// <inheritdoc/>
        public virtual ServiceOptions Options()
        {
            this.ThrowIfNotReady();
            try
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = 200;
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Allow", $"GET, PUT, POST, OPTIONS, HEAD, DELETE{(this.m_patchService != null ? ", PATCH" : null)}");
                if (this.m_patchService != null)
                {
                    RestOperationContext.Current.OutgoingResponse.Headers.Add("Accept-Patch", "application/xml+sdb-patch");
                }

                // Service options
                var retVal = new ServiceOptions()
                {
                    InterfaceVersion = "1.0.0.0",
                    Resources = new List<ServiceResourceOptions>()
                };

                // Get the resources which are supported
                foreach (var itm in this.m_resourceHandlerTool.Handlers)
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
        /// <exception cref="DomainStateException">Thrown if the application context is in a half-started state</exception>
        protected void ThrowIfNotReady()
        {
            if (!ApplicationServiceContext.Current.IsRunning)
            {
                throw new DomainStateException();
            }
        }

        /// <inheritdoc/>
        public virtual ServiceResourceOptions ResourceOptions(string resourceType)
        {
            var handler = this.GetResourceHandler(resourceType);
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
                if (this.m_patchService != null &&
                    handler.Capabilities.HasFlag(ResourceCapabilityType.Update))
                {
                    caps.Add(new ServiceResourceCapability(ResourceCapabilityType.Patch, this.GetDemands(handler, nameof(IApiResourceHandler.Update))));
                }

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

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpOffsetParameterName, typeof(int), "The offet of the first result to return")]
        [UrlParameter(QueryControlParameterNames.HttpCountParameterName, typeof(int), "The count of items to return in this result set")]
        [UrlParameter(QueryControlParameterNames.HttpIncludeTotalParameterName, typeof(bool), "True if the server should count the matching results. May reduce performance")]
        [UrlParameter(QueryControlParameterNames.HttpOrderByParameterName, typeof(string), "Instructs the result set to be ordered (in format: property:(asc|desc))")]
        [UrlParameter(QueryControlParameterNames.HttpQueryStateParameterName, typeof(Guid), "The query state identifier. This allows the client to get a stable result set.")]
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual Object AssociationSearch(string resourceType, string key, string childResourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                if (!Guid.TryParse(key, out Guid keyGuid))
                {
                    throw new ArgumentException(nameof(key));
                }

                var handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
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
                        using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                        {
                            return new Bundle(retVal, offset, totalCount);
                        }
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

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual object AssociationCreate(string resourceType, string key, string childResourceType, object body)
        {
            this.ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.AddChildObject));
                    var objectId = Guid.Parse(key);
                    this.ThrowIfPreConditionFails(handler, objectId);

                    IdentifiedData retVal = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = handler.AddChildObject(objectId, childResourceType, body) as IdentifiedData;
                    }

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    if (retVal != null)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, key, childResourceType, retVal.Key));
                    }

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

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual object AssociationRemove(string resourceType, string key, string childResourceType, string scopedEntityKey)
        {
            this.ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.RemoveChildObject));

                    var objectId = Guid.Parse(key);
                    this.ThrowIfPreConditionFails(handler, objectId);
                    IdentifiedData retVal = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = handler.RemoveChildObject(objectId, childResourceType, Guid.Parse(scopedEntityKey)) as IdentifiedData;
                    }

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, key, childResourceType, retVal.Key));
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

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpBundleRelatedParameterName, typeof(bool), "True if the server should include related objects in a bundle to the caller")]
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual object AssociationGet(string resourceType, string key, string childResourceType, string scopedEntityKey)
        {
            this.ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.GetChildObject));

                    var objectId = Guid.Parse(scopedEntityKey);
                    this.ThrowIfPreConditionFails(handler, objectId);

                    object retVal = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = handler.GetChildObject(Guid.Parse(key), childResourceType, objectId);
                    }

                    if (retVal is IdentifiedData idData)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(idData.Tag);
                        RestOperationContext.Current.OutgoingResponse.SetLastModified(idData.ModifiedOn.DateTime);

                        if (Boolean.TryParse(RestOperationContext.Current.IncomingRequest.Headers[QueryControlParameterNames.HttpBundleRelatedParameterName], out var bundle) && bundle)
                        {
                            return Bundle.CreateBundle(idData);
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

        /// <inheritdoc/>
        [UrlParameter("_format", typeof(String), "The format of the barcode (santedb-vrp, code39, etc.)")]
        public virtual Stream GetBarcode(string resourceType, string id)
        {
            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    if (this.m_barcodeService == null)
                    {
                        throw new InvalidOperationException("Cannot find barcode generator service");
                    }

                    Guid objectId = Guid.Parse(id);
                    var data = handler.Get(objectId, Guid.Empty) as IHasIdentifiers;
                    if (data == null)
                    {
                        throw new KeyNotFoundException($"{resourceType} {id}");
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.ContentType = "image/png";

                        // Get the generator
                        IBarcodeGenerator barcodeGenerator = this.m_barcodeService.GetBarcodeGenerator(
                            RestOperationContext.Current.IncomingRequest.QueryString["_format"] ?? VrpBarcodeProvider.AlgorithmName);

                        if (barcodeGenerator == null)
                        {
                            throw new ArgumentException($"Barcode format unknown");
                        }
                        else
                        {
                            return barcodeGenerator.Generate(data);
                        }
                    }
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Error generating barcode for {0} - {1}", resourceType, e);
                throw new Exception($"Could not generate visual code for {resourceType}/{id}", e);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData Touch(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler is IApiResourceHandlerEx exResourceHandler)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Update));

                    var objectId = Guid.Parse(id);
                    this.ThrowIfPreConditionFails(handler, objectId);
                    IdentifiedData retVal = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = exResourceHandler.Touch(objectId) as IdentifiedData;
                    }

                    var versioned = retVal as IVersionedData;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);

                    if (versioned != null)
                    {
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned.VersionKey));
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));
                    }

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
                throw new Exception($"Error updating {resourceType}/{id}", e);
            }
        }

        /// <inheritdoc/>
        public virtual void ResolvePointer(System.Collections.Specialized.NameValueCollection parms)
        {
            try
            {
                if (this.m_resourcePointerService == null)
                {
                    throw new InvalidOperationException("Cannot find pointer service");
                }

                bool validate = true;
                if (String.IsNullOrEmpty(parms["code"]))
                {
                    throw new ArgumentException("SEARCH have url-form encoded payload with parameter code");
                }
                else if (!String.IsNullOrEmpty(parms["validate"]))
                {
                    Boolean.TryParse(parms["validate"], out validate);
                }

                var result = this.m_resourcePointerService.ResolveResource(parms["code"], validate);

                // Create a 303 see other
                if (result != null)
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.SeeOther;
                    RestOperationContext.Current.OutgoingResponse.AddHeader("Location", this.CreateContentLocation(result.GetType().GetSerializationName(), result.Key.Value));
                }
                else
                {
                    throw new KeyNotFoundException($"Object not found");
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error searching by pointer", e);
            }
        }

        /// <inheritdoc/>
        public virtual Stream GetVrpPointerData(string resourceType, string id)
        {
            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    if (this.m_resourcePointerService == null)
                    {
                        throw new InvalidOperationException("Cannot find resource pointer service");
                    }

                    Guid objectId = Guid.Parse(id);

                    var data = handler.Get(objectId, Guid.Empty) as IHasIdentifiers;
                    if (data == null)
                    {
                        throw new KeyNotFoundException($"{resourceType} {id}");
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.ContentType = "application/jose";
                        return new MemoryStream(Encoding.UTF8.GetBytes(this.m_resourcePointerService.GeneratePointer(data)));
                    }
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Error fetching pointer for {0} - {1}", resourceType, e);
                throw new Exception($"Could fetching pointer code for {resourceType}/{id}", e);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData Copy(String reosurceType, String id)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual object InvokeMethod(string resourceType, string id, string operationName, ParameterCollection body)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.GetResourceHandler(resourceType) as IOperationalApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IOperationalApiResourceHandler.InvokeOperation));

                    var objectId = Guid.Parse(id);
                    this.ThrowIfPreConditionFails(handler, objectId);
                    object retValRaw = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retValRaw = handler.InvokeOperation(objectId, operationName, body);
                    }

                    if (retValRaw is IdentifiedData retVal)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                        RestOperationContext.Current.OutgoingResponse.SetLastModified(retVal.ModifiedOn.DateTime);

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

        /// <inheritdoc/>
        public virtual object CheckIn(string resourceType, string key)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.GetResourceHandler(resourceType) as ICheckoutResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(ICheckoutResourceHandler.CheckIn));
                    var objectId = Guid.Parse(key);
                    this.ThrowIfPreConditionFails(handler, objectId);
                    return handler.CheckIn(objectId);
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

        /// <inheritdoc/>
        public virtual object CheckOut(string resourceType, string key)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.GetResourceHandler(resourceType) as ICheckoutResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(ICheckoutResourceHandler.CheckIn));
                    var objectId = Guid.Parse(key);
                    this.ThrowIfPreConditionFails(handler, objectId);
                    return handler.CheckOut(objectId);
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

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual object InvokeMethod(string resourceType, string operationName, ParameterCollection body)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.GetResourceHandler(resourceType) as IOperationalApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IOperationalApiResourceHandler.InvokeOperation));

                    object retValRaw = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retValRaw = handler.InvokeOperation(null, operationName, body);
                    }

                    if (retValRaw is IdentifiedData retVal)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                        RestOperationContext.Current.OutgoingResponse.SetLastModified(retVal.ModifiedOn.DateTime);

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

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpBundleRelatedParameterName, typeof(bool), "True if the server should include related objects")]
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual object AssociationGet(string resourceType, string childResourceType, string childResourceKey)
        {
            this.ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.GetChildObject));
                    var objectId = Guid.Parse(childResourceKey);
                    this.ThrowIfPreConditionFails(handler, objectId);

                    IdentifiedData retVal = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = handler.GetChildObject(null, childResourceType, objectId) as IdentifiedData;
                    }

                    RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(retVal.ModifiedOn.DateTime);

                    if (Boolean.TryParse(RestOperationContext.Current.IncomingRequest.Headers[QueryControlParameterNames.HttpBundleRelatedParameterName], out var bundle) && bundle)
                    {
                        return Bundle.CreateBundle(retVal);
                    }
                    else
                    {
                        return retVal;
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

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual object AssociationRemove(string resourceType, string childResourceType, string childResourceKey)
        {
            this.ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.RemoveChildObject));
                    var objectId = Guid.Parse(childResourceKey);
                    this.ThrowIfPreConditionFails(handler, objectId);
                    IdentifiedData retVal = null;

                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = handler.RemoveChildObject(null, childResourceType, objectId) as IdentifiedData;
                    }

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, childResourceType, retVal.Key));
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

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpOffsetParameterName, typeof(int), "The offet of the first result to return")]
        [UrlParameter(QueryControlParameterNames.HttpCountParameterName, typeof(int), "The count of items to return in this result set")]
        [UrlParameter(QueryControlParameterNames.HttpIncludeTotalParameterName, typeof(bool), "True if the server should count the matching results. May reduce performance")]
        [UrlParameter(QueryControlParameterNames.HttpOrderByParameterName, typeof(string), "Instructs the result set to be ordered (in format: property:(asc|desc))")]
        [UrlParameter(QueryControlParameterNames.HttpQueryStateParameterName, typeof(Guid), "The query state identifier. This allows the client to get a stable result set.")]
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual Object AssociationSearch(string resourceType, string childResourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
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