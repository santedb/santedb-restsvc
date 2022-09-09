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
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Security;
using SanteDB.Rest.Common;
using SanteDB.Rest.HDSI;
using System;
using System.Diagnostics;
using System.Security.Permissions;
using SanteDB.Core.Services;
using SanteDB.Core.Security.Services;
using System.Collections.Specialized;
using RestSrvr;
using SanteDB.Core.Http;
using SanteDB.Rest.HDSI.Model;
using SanteDB.Core.Model.Interfaces;
using System.Net;
using System.Collections.Generic;
using SanteDB.Core.i18n;
using RestSrvr.Exceptions;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using System.Linq;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Parameters;

namespace SanteDB.Messaging.HDSI.Wcf
{
    /// <summary>
    /// Health Data Service Interface (HDSI) which supports upstreaming calls
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Model classes - ignored
    public class UpstreamHdsiServiceBehavior : HdsiServiceBehavior, IReportProgressChanged
    {

      
        private readonly IRestClientFactory m_restClientResolver;
        private readonly IDataPersistenceService<Entity> m_entityPersistence;
        private readonly IDataPersistenceService<Act> m_actPersistence;
        private readonly IUpstreamIntegrationService m_upstreamIntegrationService;

        /// <summary>
        /// Fired when progress changes
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <inheritdoc/>
        public UpstreamHdsiServiceBehavior(IDataCachingService dataCache, ILocalizationService localeService, IPatchService patchService, IPolicyEnforcementService pepService, IBarcodeProviderService barcodeService, IResourcePointerService resourcePointerService, IServiceManager serviceManager, IRestClientFactory restClientResolver, IUpstreamIntegrationService upstreamIntegrationService, IDataPersistenceService<Entity> entityRepository = null, IDataPersistenceService<Act> actRepository = null) : base(dataCache, localeService, patchService, pepService, barcodeService, resourcePointerService, serviceManager)
        {
            this.m_restClientResolver = restClientResolver;
            this.m_entityPersistence = entityRepository;
            this.m_actPersistence = actRepository;
            this.m_upstreamIntegrationService = upstreamIntegrationService;
        }


        /// <summary>
        /// Tag the object if it is only upstream or if it exists locally 
        /// </summary>
        private void TagUpstream(IdentifiedData data)
        {
            if (data is Entity entity &&
                               this.m_entityPersistence?.Query(o => o.Key == data.Key, AuthenticationContext.SystemPrincipal).Any() != true)
                entity.AddTag("$upstream", "true");
            else if (data is Act act &&
                this.m_actPersistence?.Query(o => o.Key == data.Key, AuthenticationContext.SystemPrincipal).Any() != true)
                act.AddTag("$upstream", "true");
            else if (data is Bundle bundle)
            {
                bundle.Item
                    .Select(o =>
                    {
                        this.TagUpstream(o);
                        return o;
                    }).ToList();
            }
        }

        /// <summary>
        /// Resolve the specified code
        /// </summary>
        /// <param name="parms"></param>
        public override void ResolvePointer(NameValueCollection parms)
        {
            // create only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                parms["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);

                        var result = restClient.Invoke<CodeSearchRequest, IdentifiedData>("SEARCH", "_ptr", "application/x-www-form-urlencoded", new CodeSearchRequest(parms));
                        if (result != null)
                        {
                            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.SeeOther;
                            if (result is IVersionedData versioned)
                                RestOperationContext.Current.OutgoingResponse.AddHeader("Location", this.CreateContentLocation(result.GetType().GetSerializationName(), versioned.Key.Value, "_history", versioned.VersionKey.Value) + "?_upstream=true");
                            else
                                RestOperationContext.Current.OutgoingResponse.AddHeader("Location", this.CreateContentLocation(result.GetType().GetSerializationName(), result.Key.Value) + "?_upstream=true");
                        }
                        else
                            throw new KeyNotFoundException();
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                base.ResolvePointer(parms);
            }
        }

        /// <summary>
        /// Create the specified resource
        /// </summary>
        public override IdentifiedData Create(string resourceType, IdentifiedData body)
        {
            // create only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);

                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        return restClient.Post<IdentifiedData, IdentifiedData>($"{resourceType}", body);
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.Create(resourceType, body);
            }
        }

        /// <summary>
        /// Create or udpate with upstream
        /// </summary>
        public override IdentifiedData CreateUpdate(string resourceType, string id, IdentifiedData body)
        {
            // create only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);

                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);

                        if (Guid.TryParse(id, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }
                        return restClient.Post<IdentifiedData, IdentifiedData>($"{resourceType}/{id}", body);
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.CreateUpdate(resourceType, id, body);
            }
        }

        /// <summary>
        /// Delete the specified object on the server
        /// </summary>
        public override IdentifiedData Delete(string resourceType, string id)
        {
            // Only on the remote server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);

                        restClient.Requesting += (o, e) => e.AdditionalHeaders.Add("X-Delete-Mode", RestOperationContext.Current.IncomingRequest.Headers["X-Delete-Mode"] ?? "OBSOLETE");
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);

                        if (Guid.TryParse(id, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }

                        return restClient.Delete<IdentifiedData>($"{resourceType}/{id}");
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.Delete(resourceType, id);
            }
        }

        /// <summary>
        /// Get the specified resource
        /// </summary>
        public override IdentifiedData Get(string resourceType, string id)
        {
            // Delete only on the external server
            if ((RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true") &&
                Guid.TryParse(id, out var idGuid))
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);

                        var cache = this.m_dataCachingService.GetCacheItem(idGuid);
                        if (cache != null)
                        {
                            restClient.Requesting += (o, e) => e.AdditionalHeaders.Add(HttpRequestHeader.IfNoneMatch, cache.Tag);
                        }

                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        var retVal = restClient.Get<IdentifiedData>($"{resourceType}/{id}", RestOperationContext.Current.IncomingRequest.QueryString.ToList().ToArray());
                        this.TagUpstream(retVal);
                        return retVal;
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw new Exception("Error performing online operation", e);
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.Get(resourceType, id);
            }
        }

        /// <summary>
        /// Copy the specified resource from the remote to this instance
        /// </summary>
        public override IdentifiedData Copy(string resourceType, string id)
        {
            if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable &&
                this.m_upstreamIntegrationService.IsAvailable() &&
                Guid.TryParse(id, out var idGuid))
                try
                {
                    var handler = this.GetResourceHandler(resourceType);

                    this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(0.0f, UserMessages.FETCH_FROM_UPSTREAM));
                    var remote = this.m_upstreamIntegrationService.Get(handler.Type, idGuid, null);
                    ApplicationServiceContext.Current.GetService<IDataCachingService>().Remove(remote.Key.Value);
                    this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(0.25f, UserMessages.FETCH_FROM_UPSTREAM));

                    Bundle insertBundle = new Bundle();
                    insertBundle.Add(remote);

                    // Fetch all missing relationships
                    if (remote is Entity entity)
                    {
                        var targetKeys = entity.Relationships.Select(t => t.TargetEntityKey.Value).Where(s => !this.m_entityPersistence.Query(e => e.Key == s, AuthenticationContext.SystemPrincipal).Any()).ToArray(); // Related entities which are not in this
                        insertBundle.AddRange(this.m_upstreamIntegrationService.Find<Entity>(o => targetKeys.Contains(o.Key.Value)));

                        if (remote is Patient patient)
                        {
                            var localKeys = this.m_actPersistence.Query(a => a.Participations.Any(p => p.PlayerEntityKey == patient.Key), AuthenticationContext.SystemPrincipal).Select(k => k.Key.Value).ToArray();
                            // We want to update the patient so that this SDL is linked
                            insertBundle.Item.AddRange(this.m_upstreamIntegrationService.Find<Act>(o => o.Participations.Any(p => p.PlayerEntityKey == patient.Key) && !localKeys.Contains(o.Key.Value)));
                            // Handle MDM just in case
                            insertBundle.Item.AddRange(this.m_upstreamIntegrationService.Find<Act>(o => o.Participations.Any(p => p.PlayerEntity.Relationships.Where(r => r.RelationshipType.Mnemonic == "MDM-Master").Any(r => r.SourceEntityKey == patient.Key))));
                        }

                    }
                    else if (remote is Act act)
                    {
                        var targetKeys = act.Relationships.Select(t => t.TargetActKey.Value).Where(s => !this.m_actPersistence.Query(e => e.Key == s, AuthenticationContext.SystemPrincipal).Any()).ToArray(); // Related acts which are not in this act
                        insertBundle.AddRange(this.m_upstreamIntegrationService.Find<Act>(o => targetKeys.Contains(o.Key.Value)));
                        targetKeys = act.Participations.Select(t => t.PlayerEntityKey.Value).Where(s => !this.m_entityPersistence.Query(e => e.Key == s, AuthenticationContext.SystemPrincipal).Any()).ToArray(); // Related players which are not in this act
                        insertBundle.AddRange(this.m_upstreamIntegrationService.Find<Entity>(o => targetKeys.Contains(o.Key.Value)));
                    }

                    this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(0.5f, UserMessages.FETCH_FROM_UPSTREAM));

                    // Now we want to fetch all participations which have a relationship with the downloaded object if the object is a patient

                    // Insert
                    ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>()?.Insert(insertBundle, TransactionMode.Commit, AuthenticationContext.Current.Principal);

                    // Clear cache
                    ApplicationServiceContext.Current.GetService<IDataCachingService>().Clear();
                    return remote;
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                    throw;
                }
            else
                throw new FaultException(502);
        }

        /// <summary>
        /// Get a specific version of the object
        /// </summary>
        public override IdentifiedData GetVersion(string resourceType, string id, string versionId)
        {
            // Delete only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        return restClient.Get<IdentifiedData>($"{resourceType}/{id}/_history/{versionId}");
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.GetVersion(resourceType, id, versionId);
            }
        }

        /// <summary>
        /// Get history of the object
        /// </summary>
        public override IdentifiedData History(string resourceType, string id)
        {
            // Delete only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        return restClient.Get<IdentifiedData>($"{resourceType}/{id}/history");
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.History(resourceType, id);
            }
        }

        /// <summary>
        /// Perform options
        /// </summary>
        public override ServiceOptions Options()
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);
                        return restClient.Options<ServiceOptions>("/");
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.Options();
            }
        }

        /// <summary>
        /// Perform a patch
        /// </summary>
        public override void Patch(string resourceType, string id, Patch body)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);
                        var patchId = restClient.Patch<Patch>($"/{resourceType}/{id}", "application/xml+sdb-patch", RestOperationContext.Current.IncomingRequest.Headers["If -Match"], body);
                        RestOperationContext.Current.OutgoingResponse.SetETag(patchId);
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                base.Patch(resourceType, id, body);
            }
        }

        /// <summary>
        /// Get options for resource
        /// </summary>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public override ServiceResourceOptions ResourceOptions(string resourceType)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);
                        return restClient.Options<ServiceResourceOptions>($"/{resourceType}");
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.ResourceOptions(resourceType);
            }
        }

        /// <summary>
        /// Perform search
        /// </summary>
        public override IdentifiedData Search(string resourceType)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        // This NVC is UTF8 compliant
                        var nvc = RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString();
                        var retVal = restClient.Get<IdentifiedData>($"/{resourceType}", nvc.ToDictionary().ToArray().ToDictionary(o=>o.Key, o=>(object)o.Value).ToArray());
                        this.TagUpstream(retVal);

                        return retVal;
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.Search(resourceType);
            }
        }

        /// <summary>
        /// Update data
        /// </summary>
        public override IdentifiedData Update(string resourceType, string id, IdentifiedData body)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);

                        if (Guid.TryParse(id, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }

                        return restClient.Put<IdentifiedData, IdentifiedData>($"/{resourceType}/{id}", body);
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.Update(resourceType, id, body);
            }
        }

        /// <summary>
        /// Associated object search
        /// </summary>
        public override object AssociationSearch(string resourceType, string key, string childResourceType)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        // This NVC is UTF8 compliant
                        var nvc = RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString();
                        var retVal = restClient.Get<Object>($"/{resourceType}/{key}/{childResourceType}", nvc.ToDictionary().ToArray().ToDictionary(o => o.Key, o => (object)o.Value).ToArray()) as IdentifiedData;
                        this.TagUpstream(retVal);
                        
                        return retVal;
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.AssociationSearch(resourceType, key, childResourceType);
            }
        }

        /// <summary>
        /// Remove associated object
        /// </summary>
        public override object AssociationRemove(string resourceType, string key, string childResourceType, string scopedEntityKey)
        {
            // Only on the remote server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);
                        restClient.Requesting += (o, e) => e.AdditionalHeaders.Add("X-Delete-Mode", RestOperationContext.Current.IncomingRequest.Headers["X-Delete-Mode"] ?? "OBSOLETE");
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);

                        if (Guid.TryParse(key, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }
                        if (Guid.TryParse(scopedEntityKey, out uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }

                        return restClient.Delete<object>($"{resourceType}/{key}/{childResourceType}/{scopedEntityKey}");
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.AssociationRemove(resourceType, key, childResourceType, scopedEntityKey);
            }
        }

        /// <summary>
        /// Get associated object
        /// </summary>
        public override object AssociationGet(string resourceType, string key, string childResourceType, string scopedEntityKey)
        {
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        var retVal = restClient.Get<IdentifiedData>($"{resourceType}/{key}/{childResourceType}/{scopedEntityKey}", RestOperationContext.Current.IncomingRequest.QueryString.ToList().ToArray());
                        this.TagUpstream(retVal);
                        return retVal;
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.AssociationGet(resourceType, key, childResourceType, scopedEntityKey);
            }
        }

        /// <summary>
        /// Association based create
        /// </summary>
        public override object AssociationCreate(string resourceType, string key, string childResourceType, object body)
        {
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);

                        if (Guid.TryParse(key, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }
                        if (body is IIdentifiedData ide)
                        {
                            this.m_dataCachingService.Remove(ide.Key.Value);
                        }

                        return restClient.Post<object, object>($"{resourceType}/{key}/{childResourceType}", body);
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.AssociationCreate(resourceType, key, childResourceType, body);
            }
        }

        /// <summary>
        /// Invoke a method
        /// </summary>
        public override object InvokeMethod(string resourceType, string operationName, ParameterCollection body)
        {
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        return restClient.Post<object, object>($"{resourceType}/${operationName}", body);
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.InvokeMethod(resourceType, operationName, body);
            }
        }

        /// <summary>
        /// Invoke the specified operation on a specific instance
        /// </summary>
        public override object InvokeMethod(string resourceType, string id, string operationName, ParameterCollection body)
        {
            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.HealthDataService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);

                        if (Guid.TryParse(id, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }

                        return restClient.Post<object, object>($"{resourceType}/{id}/${operationName}", body);
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                else
                    throw new FaultException(502);
            }
            else
            {
                return base.InvokeMethod(resourceType, id, operationName, body);
            }
        }
    }

}