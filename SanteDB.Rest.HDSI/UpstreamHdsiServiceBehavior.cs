/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-6-21
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
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Serialization;
using SanteDB.Rest.HDSI;
using SanteDB.Rest.HDSI.Model;
using SharpCompress;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
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
                ApplicationServiceContext.Current.GetService<IAuditService>()

                )
        {

        }

        /// <inheritdoc/>
        public UpstreamHdsiServiceBehavior(IDataCachingService dataCache, ILocalizationService localeService, IPatchService patchService, IPolicyEnforcementService pepService, IBarcodeProviderService barcodeService, IResourcePointerService resourcePointerService, IServiceManager serviceManager, IConfigurationManager configurationManager, IRestClientFactory restClientResolver, IUpstreamIntegrationService upstreamIntegrationService, IUpstreamAvailabilityProvider availabilityProvider, IDataPersistenceService<Entity> entityRepository = null, IDataPersistenceService<Act> actRepository = null, IAdhocCacheService adhocCacheService = null, IAuditService auditService = null)
            : base(dataCache, localeService, patchService, pepService, barcodeService, resourcePointerService, serviceManager, configurationManager, auditService)
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
            if (this.m_entityPersistence != null || this.m_actPersistence != null) // No bother since we can't store anything anyways
            {
                if (data is Entity entity &&
                                   !this.m_entityPersistence.Query(o => o.Key == data.Key, AuthenticationContext.SystemPrincipal).Any())
                {
                    entity.AddTag(SystemTagNames.UpstreamDataTag, "true");
                }
                else if (data is Act act &&
                    !this.m_actPersistence.Query(o => o.Key == data.Key, AuthenticationContext.SystemPrincipal).Any())
                {
                    act.AddTag(SystemTagNames.UpstreamDataTag, "true");
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
                        restClient.Responded += this.CopyResponseHeaders;

                        var result = restClient.Invoke<CodeSearchRequest, IdentifiedData>("SEARCH", "_ptr", "application/x-www-form-urlencoded", new CodeSearchRequest(parms));
                        if (result != null)
                        {
                            this.m_dataCachingService?.Add(result);
                            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.SeeOther;
                            if (result is IVersionedData versioned)
                            {
                                RestOperationContext.Current.OutgoingResponse.AddHeader("Location", this.CreateContentLocation(result.GetType().GetSerializationName(), versioned.Key.Value, "_history", versioned.VersionKey.Value) + $"?_upstream=true&_format={Uri.EscapeDataString(restClient.Accept)}");
                            }
                            else
                            {
                                RestOperationContext.Current.OutgoingResponse.AddHeader("Location", this.CreateContentLocation(result.GetType().GetSerializationName(), result.Key.Value) + $"?_upstream=true&_format={Uri.EscapeDataString(restClient.Accept)}");
                            }
                        }
                        else
                        {
                            throw new KeyNotFoundException();
                        }
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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

                        restClient.Responded += this.CopyResponseHeaders;
                        var retVal = restClient.Post<IdentifiedData, IdentifiedData>($"{resourceType}", body);
                        this.m_dataCachingService.Remove(retVal.Key.GetValueOrDefault());
                        if (retVal is IResourceCollection irc)
                        {
                            irc.Item.ForEach(o => this.m_dataCachingService.Remove(o.Key.GetValueOrDefault()));
                        }
                        return retVal;
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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

                        restClient.Responded += this.CopyResponseHeaders;

                        if (Guid.TryParse(id, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }
                        var retVal = restClient.Post<IdentifiedData, IdentifiedData>($"{resourceType}/{id}", body);
                        this.m_dataCachingService.Remove(retVal.Key.GetValueOrDefault());
                        if (retVal is IResourceCollection irc)
                        {
                            irc.Item.ForEach(o => this.m_dataCachingService.Remove(o.Key.GetValueOrDefault()));
                        }
                        return retVal;
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
                        restClient.Responded += this.CopyResponseHeaders;

                        if (Guid.TryParse(id, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }

                        var retVal = restClient.Delete<IdentifiedData>($"{resourceType}/{id}");
                        this.m_dataCachingService.Remove(retVal.Key.GetValueOrDefault());
                        if (retVal is IResourceCollection irc)
                        {
                            irc.Item.ForEach(o => this.m_dataCachingService.Remove(o.Key.GetValueOrDefault()));
                        }
                        return retVal;
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
                        var viewModel = this.GetViewModelFromRequest();

                        IdentifiedData cache = null;
                        if (Guid.TryParse(id, out var idGuid) && !AuthenticationContext.Current.Principal.IsElevatedPrincipal())
                        {
                            cache = this.m_dataCachingService.GetCacheItem(idGuid);
                            if (cache != null && cache.Type == resourceType)
                            {
                                // Only do a head if the ad-hoc cache for excessive HEAD checks is null
                                if (this.m_adhocCache?.TryGet<DateTime>($"{cache.Tag}#{viewModel}", out var lastTimeChecked) == true)
                                {
                                    return cache; // we just got this in the cache
                                }
                                if ("full".Equals(viewModel))
                                {
                                    restClient.Requesting += (o, e) => e.AdditionalHeaders.Add(HttpRequestHeader.IfNoneMatch, cache.Tag);
                                }
                            }
                        }

                        restClient.Responded += this.CopyResponseHeaders;
                        //restClient.Accept = String.Join(",", RestOperationContext.Current.IncomingRequest.AcceptTypes);
                        var retVal = restClient.Get<IdentifiedData>($"{resourceType}/{id}", RestOperationContext.Current.IncomingRequest.QueryString);

                        if (retVal == null)
                        {
                            return cache;
                        }
                        else if(!AuthenticationContext.Current.Principal.IsElevatedPrincipal())
                        {
                            this.m_adhocCache?.Add($"{retVal.Tag}#{viewModel}", DateTime.Now, new TimeSpan(0, 1, 00));
                            this.m_dataCachingService.Add(retVal);
                        }
                        this.TagUpstream(retVal);
                        return retVal;
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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

            //// For read operations - we want to pass the accept up to save re-fetching
            //if (RestOperationContext.Current.IncomingRequest.HttpMethod.Equals("get", StringComparison.InvariantCultureIgnoreCase))
            //{
            //    if (RestOperationContext.Current.IncomingRequest.QueryString["_format"] != null)
            //    {
            //        retVal.Accept = RestOperationContext.Current.IncomingRequest.QueryString["_format"];
            //    }
            //    else
            //    {
            //        retVal.Accept = RestOperationContext.Current.IncomingRequest.AcceptTypes.First();
            //    }
            //}
            //else // For posts - we don't want the ViewModel data going up - we want an XML sync representation going up so delay loading on upbound objects is not performed
            //{
            if (RestOperationContext.Current.IncomingRequest.HttpMethod.Equals("get", StringComparison.InvariantCultureIgnoreCase) || this.m_configuration.PreserveContentType)
            {
                RestOperationContext.Current.Data.Add(RestMessageDispatchFormatter.VIEW_MODEL_BYPASS_DELAY_LOAD, true);
                var accept = RestOperationContext.Current.IncomingRequest.AcceptTypes.FirstOrDefault();
                switch (accept)
                {
                    case SanteDBExtendedMimeTypes.JsonRimModel:
                        retVal.Accept = SanteDBExtendedMimeTypes.JsonRimModel;
                        break;
                    case SanteDBExtendedMimeTypes.JsonPatch:
                    case SanteDBExtendedMimeTypes.XmlPatch:
                    case SanteDBExtendedMimeTypes.XmlRimModel:
                    case "application/json":
                    case "application/xml": // We want to use the XML format for serialization
                        retVal.Accept = "application/xml";
                        break;
                    case SanteDBExtendedMimeTypes.JsonViewModel:
                    case "application/json+sdb-viewmodel":
                        retVal.Accept = SanteDBExtendedMimeTypes.JsonViewModel;
                        break;
                    default:
                        retVal.Accept = RestOperationContext.Current.IncomingRequest.AcceptTypes.FirstOrDefault() ?? RestOperationContext.Current.IncomingRequest.ContentType;
                        break;
                }
                //}

                retVal.Requesting += (o, e) =>
                {
                    e.AdditionalHeaders.Add(ExtendedHttpHeaderNames.ViewModelHeaderName, this.GetViewModelFromRequest());
                    e.AdditionalHeaders.Add(ExtendedHttpHeaderNames.ThrowOnPrivacyViolation, this.GetPrivacyViolationFromRequest());
                };
            }
            return retVal;
        }

        /// <summary>
        /// Get the desired view model definition from the request
        /// </summary>
        private string GetViewModelFromRequest()
        {
            if (!String.IsNullOrEmpty(RestOperationContext.Current.IncomingRequest.Headers[ExtendedHttpHeaderNames.ViewModelHeaderName]))
            {
                return RestOperationContext.Current.IncomingRequest.Headers[ExtendedHttpHeaderNames.ViewModelHeaderName];
            }
            else if (!String.IsNullOrEmpty(RestOperationContext.Current.IncomingRequest.QueryString[QueryControlParameterNames.HttpViewModelParameterName]))
            {
                return RestOperationContext.Current.IncomingRequest.QueryString[QueryControlParameterNames.HttpViewModelParameterName];
            }
            else
            {
                return null;
            }
        }

         /// <summary>
        /// Get the desired view model definition from the request
        /// </summary>
        private string GetPrivacyViolationFromRequest()
        {
            if (!String.IsNullOrEmpty(RestOperationContext.Current.IncomingRequest.Headers[ExtendedHttpHeaderNames.ThrowOnPrivacyViolation]))
            {
                return RestOperationContext.Current.IncomingRequest.Headers[ExtendedHttpHeaderNames.ThrowOnPrivacyViolation];
            }
            else
            {
                return null;
            }
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

                    this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(nameof(UpstreamHdsiServiceBehavior), 0.0f, UserMessages.FETCH_FROM_UPSTREAM));
                    var remote = this.m_upstreamIntegrationService.Get(handler.Type, idGuid, null);
                    ApplicationServiceContext.Current.GetService<IDataCachingService>().Remove(remote.Key.Value);
                    this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(nameof(UpstreamHdsiServiceBehavior), 0.25f, UserMessages.FETCH_FROM_UPSTREAM));

                    Bundle insertBundle = new Bundle() { CorrelationKey = idGuid };
                    insertBundle.Add(remote);

                    // Fetch all missing relationships
                    if (remote is Entity entity)
                    {
                        var targetKeys = entity.Relationships.Where(t=>t.RelationshipTypeKey != EntityRelationshipTypeKeys.Duplicate).Select(t => t.TargetEntityKey.Value).Where(s => !this.m_entityPersistence.Query(e => e.Key == s, AuthenticationContext.SystemPrincipal).Any()).ToArray(); // Related entities which are not in this
                        insertBundle.AddRange(this.m_upstreamIntegrationService.Query<Entity>(o => targetKeys.Contains(o.Key.Value), new UpstreamIntegrationQueryControlOptions()
                        {
                            Count = targetKeys.Length,
                            Offset = 0, 
                            IncludeRelatedInformation = false
                        }).Item.OfType<IdentifiedData>());

                        if (remote is Patient patient)
                        {
                            var localKeys = this.m_actPersistence.Query(a => a.Participations.Any(p => p.PlayerEntityKey == patient.Key), AuthenticationContext.SystemPrincipal).Select(k => k.Key.Value).ToArray();
                            // Download all the acts which are not included
                            var queryOptions = new UpstreamIntegrationQueryControlOptions()
                            {
                                Count = 100,
                                Offset = 0,
                                QueryId = Guid.NewGuid()
                            };
                            while(true)
                            {
                                var upstreamResults = this.m_upstreamIntegrationService.Query<Act>(o => o.Participations.Any(p => p.PlayerEntityKey == patient.Key) && !localKeys.Contains(o.Key.Value), queryOptions);
                                if(upstreamResults.Item.Any())
                                {
                                    insertBundle.Item.AddRange(upstreamResults.Item.OfType<IdentifiedData>());
                                    queryOptions.Offset += upstreamResults.Item.Count();
                                }
                                else
                                {
                                    break;
                                }
                            }

                            // Handle MDM just in case
                            insertBundle.Item.AddRange(this.m_upstreamIntegrationService.Query<Act>(o => o.Participations.Any(p => p.PlayerEntity.Relationships.Where(r => r.RelationshipType.Mnemonic == "MDM-Master").Any(r => r.SourceEntityKey == patient.Key))).Item.OfType<IdentifiedData>());
                        }
                    }
                    else if (remote is Act act)
                    {
                        var targetKeys = act.Relationships.Where(t=>t.RelationshipTypeKey != ActRelationshipTypeKeys.Duplicate).Select(t => t.TargetActKey.Value).Where(s => !this.m_actPersistence.Query(e => e.Key == s, AuthenticationContext.SystemPrincipal).Any()).ToArray(); // Related acts which are not in this act
                        insertBundle.AddRange(this.m_upstreamIntegrationService.Query<Act>(o => targetKeys.Contains(o.Key.Value)).Item.OfType<IdentifiedData>());
                        targetKeys = act.Participations.Select(t => t.PlayerEntityKey.Value).Where(s => !this.m_entityPersistence.Query(e => e.Key == s, AuthenticationContext.SystemPrincipal).Any()).ToArray(); // Related players which are not in this act
                        insertBundle.AddRange(this.m_upstreamIntegrationService.Query<Entity>(o => targetKeys.Contains(o.Key.Value)).Item.OfType<IdentifiedData>());
                    }

                    this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(nameof(UpstreamHdsiServiceBehavior), 0.5f, UserMessages.FETCH_FROM_UPSTREAM));

                    // Insert
                    ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>()?.Insert(insertBundle, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                    this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(nameof(UpstreamHdsiServiceBehavior), 1f, UserMessages.FETCH_FROM_UPSTREAM));
                    
                    // Clear cache
                    this.m_dataCachingService.Clear();
                    return remote;
                }
                catch(WebException e) when (e is IRestException)
                {
                    throw;
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
                        restClient.Responded += this.CopyResponseHeaders;
                        return restClient.Get<IdentifiedData>($"{resourceType}/{id}/_history/{versionId}");
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
                        restClient.Responded += this.CopyResponseHeaders;
                        return restClient.Get<IdentifiedData>($"{resourceType}/{id}/_history");
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
        public override void PatchAll(PatchCollection body)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
                        var patchId = restClient.Patch<PatchCollection>($"/", "application/xml", null, body);
                        RestOperationContext.Current.OutgoingResponse.SetETag(patchId);

                        body.Patches.ForEach(p => this.m_dataCachingService.Remove(p.AppliesTo.Key.Value));
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
               base.PatchAll(body);
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
                        var patchId = restClient.Patch<Patch>($"/{resourceType}/{id}", "application/xml", RestOperationContext.Current.IncomingRequest.Headers["If-Match"], body);
                        RestOperationContext.Current.OutgoingResponse.SetETag(patchId);

                        if (Guid.TryParse(id, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
                        restClient.Responded += this.CopyResponseHeaders;
                        //restClient.Accept = String.Join(",", RestOperationContext.Current.IncomingRequest.AcceptTypes);
                        // This NVC is UTF8 compliant
                        var nvc = RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString();
                        var retVal = restClient.Get<IdentifiedData>($"/{resourceType}", nvc);
                        this.TagUpstream(retVal);

                        return retVal;
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
                        restClient.Responded += this.CopyResponseHeaders;

                        if (Guid.TryParse(id, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }

                        return restClient.Put<IdentifiedData, IdentifiedData>($"/{resourceType}/{id}", body);
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
                        restClient.Responded += this.CopyResponseHeaders;
                        // This NVC is UTF8 compliant
                        var nvc = RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString();
                        var retVal = restClient.Get<Object>($"/{resourceType}/{key}/{childResourceType}", nvc) as IdentifiedData;
                        this.TagUpstream(retVal);

                        return retVal;
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
                        restClient.Responded += this.CopyResponseHeaders;

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
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
                        restClient.Responded += this.CopyResponseHeaders;
                        var retVal = restClient.Get<IdentifiedData>($"{resourceType}/{key}/{childResourceType}/{scopedEntityKey}", RestOperationContext.Current.IncomingRequest.QueryString);
                        this.TagUpstream(retVal);
                        return retVal;
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
                        restClient.Responded += this.CopyResponseHeaders;

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
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
                        restClient.Responded += this.CopyResponseHeaders;
                        var retVal = restClient.Post<object, object>($"{resourceType}/${operationName}", body);
                        if (retVal is byte[] ba)
                        {
                            retVal = new MemoryStream(ba);
                        }
                        return retVal;
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
        public override Stream GetDataset(string resourceType, string id)
        {

            if (this.ShouldForwardRequest())
            {
                if (ApplicationServiceContext.Current.GetService<INetworkInformationService>().IsNetworkAvailable)
                {
                    try
                    {
                        var restClient = this.CreateProxyClient();
                        restClient.Responded += this.CopyResponseHeaders;
                        if (String.IsNullOrEmpty(id))
                        {
                            return new MemoryStream(restClient.Get($"{resourceType}/_export", RestOperationContext.Current.IncomingRequest.QueryString));
                        }
                        else
                        {
                            return new MemoryStream(restClient.Get($"{resourceType}/{id}/_export", RestOperationContext.Current.IncomingRequest.QueryString));
                        }
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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
                return base.GetDataset(resourceType, id);
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
                        restClient.Responded += this.CopyResponseHeaders;

                        if (Guid.TryParse(id, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }

                        return restClient.Post<object, object>($"{resourceType}/{id}/${operationName}", body);
                    }
                    catch (WebException e) when (e is IRestException)
                    {
                        throw;
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

        /// <summary>
        /// Copy response headers
        /// </summary>
        private void CopyResponseHeaders(object sender, RestResponseEventArgs e)
        {
            RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
            if (e.Headers?.ContainsKey("Content-Disposition") == true)
            {
                RestOperationContext.Current.OutgoingResponse.AddHeader("Content-Disposition", e.Headers["Content-Disposition"]);
            }
        }
    }

}