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
using SanteDB.Core.Http;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.HDSI;
using SanteDB.Rest.HDSI.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;

namespace SanteDB.Messaging.HDSI.Wcf
{
    /// <summary>
    /// Health Data Service Interface (HDSI) which supports upstreaming calls
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Model classes - ignored
    public class UpstreamHdsiServiceBehavior : HdsiServiceBehavior, IReportProgressChanged
    {


        private readonly IDataPersistenceService<Entity> m_entityPersistence;
        private readonly IDataPersistenceService<Act> m_actPersistence;
        private readonly IUpstreamIntegrationService m_upstreamIntegrationService;
        private readonly IUpstreamAvailabilityProvider m_availabilityProvider;
        private readonly IRestClientFactory m_restClientFactory;
        private readonly IAdhocCacheService m_adhocCache;

        /// <summary>
        /// Fired when progress changes
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// For REST service initialization
        /// </summary>
        public UpstreamHdsiServiceBehavior() :
            this(ApplicationServiceContext.Current.GetService<IDataCachingService>(),
                ApplicationServiceContext.Current.GetService<ILocalizationService>(),
                ApplicationServiceContext.Current.GetService<IPatchService>(),
                ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>(),
                ApplicationServiceContext.Current.GetService<IBarcodeProviderService>(),
                ApplicationServiceContext.Current.GetService<IResourcePointerService>(),
                ApplicationServiceContext.Current.GetService<IServiceManager>(),
                ApplicationServiceContext.Current.GetService<IConfigurationManager>(),
                ApplicationServiceContext.Current.GetService<IRestClientFactory>(),
                ApplicationServiceContext.Current.GetService<IUpstreamIntegrationService>(),
                ApplicationServiceContext.Current.GetService<IUpstreamAvailabilityProvider>(),
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Entity>>(),
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Act>>(),
                ApplicationServiceContext.Current.GetService<IAdhocCacheService>(),
                ApplicationServiceContext.Current.GetService<IAuditBuilder>()

                )
        {

        }

        /// <inheritdoc/>
        public UpstreamHdsiServiceBehavior(IDataCachingService dataCache, ILocalizationService localeService, IPatchService patchService, IPolicyEnforcementService pepService, IBarcodeProviderService barcodeService, IResourcePointerService resourcePointerService, IServiceManager serviceManager, IConfigurationManager configurationManager, IRestClientFactory restClientResolver, IUpstreamIntegrationService upstreamIntegrationService, IUpstreamAvailabilityProvider availabilityProvider, IDataPersistenceService<Entity> entityRepository = null, IDataPersistenceService<Act> actRepository = null, IAdhocCacheService adhocCacheService = null, IAuditBuilder auditBuilder = null) 
            : base(dataCache, localeService, patchService, pepService, barcodeService, resourcePointerService, serviceManager, configurationManager, auditBuilder)
        {
            this.m_restClientFactory = restClientResolver;
            this.m_adhocCache = adhocCacheService;
            this.m_entityPersistence = entityRepository;
            this.m_actPersistence = actRepository;
            this.m_upstreamIntegrationService = upstreamIntegrationService;
            this.m_availabilityProvider = availabilityProvider;
        }


        /// <summary>
        /// Tag the object if it is only upstream or if it exists locally 
        /// </summary>
        private void TagUpstream(IdentifiedData data)
        {
            if (data is Entity entity &&
                               this.m_entityPersistence?.Query(o => o.Key == data.Key, AuthenticationContext.SystemPrincipal).Any() != true)
            {
                entity.AddTag("$upstream", "true");
            }
            else if (data is Act act &&
                this.m_actPersistence?.Query(o => o.Key == data.Key, AuthenticationContext.SystemPrincipal).Any() != true)
            {
                act.AddTag("$upstream", "true");
            }
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

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override void ResolvePointer(NameValueCollection parms)
        {
            // create only on the external server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);

                        var result = restClient.Invoke<CodeSearchRequest, IdentifiedData>("SEARCH", "_ptr", "application/x-www-form-urlencoded", new CodeSearchRequest(parms));
                        if (result != null)
                        {
                            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.SeeOther;
                            if (result is IVersionedData versioned)
                            {
                                RestOperationContext.Current.OutgoingResponse.AddHeader("Location", this.CreateContentLocation(result.GetType().GetSerializationName(), versioned.Key.Value, "_history", versioned.VersionKey.Value) + "?_upstream=true");
                            }
                            else
                            {
                                RestOperationContext.Current.OutgoingResponse.AddHeader("Location", this.CreateContentLocation(result.GetType().GetSerializationName(), result.Key.Value) + "?_upstream=true");
                            }
                        }
                        else
                        {
                            throw new KeyNotFoundException();
                        }
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                base.ResolvePointer(parms);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override IdentifiedData Create(string resourceType, IdentifiedData body)
        {
            // create only on the external server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();

                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        return restClient.Post<IdentifiedData, IdentifiedData>($"{resourceType}", body);
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.Create(resourceType, body);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override IdentifiedData CreateUpdate(string resourceType, string id, IdentifiedData body)
        {
            // create only on the external server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();

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
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.CreateUpdate(resourceType, id, body);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override IdentifiedData Delete(string resourceType, string id)
        {
            // Only on the remote server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();

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
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.Delete(resourceType, id);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override IdentifiedData Get(string resourceType, string id)
        {
            // Delete only on the external server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();

                        IdentifiedData cache = null;
                        if (Guid.TryParse(id, out var idGuid))
                        {
                            cache = this.m_dataCachingService.GetCacheItem(idGuid);
                            if (cache != null)
                            {
                                // Only do a head if the ad-hoc cache for excessive HEAD checks is null
                                if (this.m_adhocCache?.TryGet<DateTime>(cache.Tag, out var lastTimeChecked) == true)
                                {
                                    return cache; // we just got this in the cache
                                }
                                restClient.Requesting += (o, e) => e.AdditionalHeaders.Add(HttpRequestHeader.IfNoneMatch, cache.Tag);
                            }
                        }

                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        //restClient.Accept = String.Join(",", RestOperationContext.Current.IncomingRequest.AcceptTypes);
                        var retVal = restClient.Get<IdentifiedData>($"{resourceType}/{id}", RestOperationContext.Current.IncomingRequest.QueryString);

                        if (retVal == null)
                        {
                            return cache;
                        }
                        else
                        {
                            this.m_adhocCache?.Add(retVal.Tag, DateTime.Now, new TimeSpan(0, 1, 00));
                            this.m_dataCachingService.Add(retVal);
                            this.TagUpstream(retVal);
                            return retVal;
                        }
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw new Exception("Error performing online operation", e);
                    }
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.Get(resourceType, id);
            }
        }

        /// <summary>
        /// Create a proxy client with appropriate headers
        /// </summary>
        private IRestClient CreateProxyClient()
        {
            var retVal = this.m_restClientFactory.GetRestClientFor(ServiceEndpointType.HealthDataService);
            retVal.Accept = String.Join(",", RestOperationContext.Current.IncomingRequest.AcceptTypes);
            retVal.Requesting += (o, e) =>
            {
                var inboundHeaders = RestOperationContext.Current.IncomingRequest.Headers;
                if (!String.IsNullOrEmpty(inboundHeaders[ExtendedHttpHeaderNames.ViewModelHeaderName]))
                {
                    e.AdditionalHeaders.Add(ExtendedHttpHeaderNames.ViewModelHeaderName, inboundHeaders[ExtendedHttpHeaderNames.ViewModelHeaderName]);
                }
            };
            return retVal;
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override IdentifiedData Copy(string resourceType, string id)
        {
            if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable &&
                this.m_availabilityProvider.IsAvailable(ServiceEndpointType.HealthDataService) &&
                Guid.TryParse(id, out var idGuid))
            {
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
            }
            else
            {
                throw new FaultException(System.Net.HttpStatusCode.BadGateway);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override IdentifiedData GetVersion(string resourceType, string id, string versionId)
        {
            // Delete only on the external server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        return restClient.Get<IdentifiedData>($"{resourceType}/{id}/_history/{versionId}");
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.GetVersion(resourceType, id, versionId);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override IdentifiedData History(string resourceType, string id)
        {
            // Delete only on the external server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        return restClient.Get<IdentifiedData>($"{resourceType}/{id}/history");
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.History(resourceType, id);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override ServiceOptions Options()
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
                        return restClient.Options<ServiceOptions>("/");
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.Options();
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override void Patch(string resourceType, string id, Patch body)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
                        var patchId = restClient.Patch<Patch>($"/{resourceType}/{id}", "application/xml+sdb-patch", RestOperationContext.Current.IncomingRequest.Headers["If -Match"], body);
                        RestOperationContext.Current.OutgoingResponse.SetETag(patchId);
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                base.Patch(resourceType, id, body);
            }
        }

        /// <summary>
        /// Returns true if the request should be forwarded
        /// </summary>
        private bool ShouldForwardRequest()
        {
            var hasUpstreamParam = Boolean.TryParse(RestOperationContext.Current.IncomingRequest.QueryString[QueryControlParameterNames.HttpUpstreamParameterName], out var upstreamQry);
            var hasUpstreamHeader = Boolean.TryParse(RestOperationContext.Current.IncomingRequest.Headers[ExtendedHttpHeaderNames.UpstreamHeaderName], out var upstreamHdr);
            return upstreamHdr || upstreamQry || this.m_configuration?.AutomaticallyForwardRequests == true && !hasUpstreamHeader && !hasUpstreamParam;
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override ServiceResourceOptions ResourceOptions(string resourceType)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
                        return restClient.Options<ServiceResourceOptions>($"/{resourceType}");
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.ResourceOptions(resourceType);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override IdentifiedData Search(string resourceType)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        //restClient.Accept = String.Join(",", RestOperationContext.Current.IncomingRequest.AcceptTypes);
                        // This NVC is UTF8 compliant
                        var nvc = RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString();
                        var retVal = restClient.Get<IdentifiedData>($"/{resourceType}", nvc);
                        this.TagUpstream(retVal);

                        return retVal;
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.Search(resourceType);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override IdentifiedData Update(string resourceType, string id, IdentifiedData body)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
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
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.Update(resourceType, id, body);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object AssociationSearch(string resourceType, string key, string childResourceType)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        // This NVC is UTF8 compliant
                        var nvc = RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString();
                        var retVal = restClient.Get<Object>($"/{resourceType}/{key}/{childResourceType}", nvc) as IdentifiedData;
                        this.TagUpstream(retVal);

                        return retVal;
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.AssociationSearch(resourceType, key, childResourceType);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object AssociationRemove(string resourceType, string key, string childResourceType, string scopedEntityKey)
        {
            // Only on the remote server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
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
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.AssociationRemove(resourceType, key, childResourceType, scopedEntityKey);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object AssociationGet(string resourceType, string key, string childResourceType, string scopedEntityKey)
        {
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        var retVal = restClient.Get<IdentifiedData>($"{resourceType}/{key}/{childResourceType}/{scopedEntityKey}", RestOperationContext.Current.IncomingRequest.QueryString);
                        this.TagUpstream(retVal);
                        return retVal;
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.AssociationGet(resourceType, key, childResourceType, scopedEntityKey);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object AssociationCreate(string resourceType, string key, string childResourceType, object body)
        {
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);

                        if (Guid.TryParse(key, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }
                        if (body is IAnnotatedResource ide)
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
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.AssociationCreate(resourceType, key, childResourceType, body);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object InvokeMethod(string resourceType, string operationName, ParameterCollection body)
        {
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        return restClient.Post<object, object>($"{resourceType}/${operationName}", body);
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error performing online operation: {0}", e.InnerException);
                        throw;
                    }
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.InvokeMethod(resourceType, operationName, body);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object InvokeMethod(string resourceType, string id, string operationName, ParameterCollection body)
        {
            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
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
                }
                else
                {
                    throw new FaultException(System.Net.HttpStatusCode.BadGateway);
                }
            }
            else
            {
                return base.InvokeMethod(resourceType, id, operationName, body);
            }
        }
    }

}