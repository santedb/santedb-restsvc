/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 */
using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Exceptions;
using SanteDB.Core;
using SanteDB.Core.Http;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;

namespace SanteDB.Rest.AMI
{
    /// <summary>
    /// Upstream AMI service behavior
    /// </summary>
    public class UpstreamAmiServiceBehavior : AmiServiceBehavior
    {
        // Rest client resolution
        private readonly IRestClientFactory m_restClientResolver;

        // Upstream service
        private readonly IUpstreamIntegrationService m_upstreamService;
        private readonly IUpstreamAvailabilityProvider m_upstreamAvailabilityProvider;
        private readonly IDataCachingService m_dataCachingService;
        private readonly IDataPersistenceService<Entity> m_entityRepository;
        private readonly IDataPersistenceService<Act> m_actRepository;

        /// <summary>
        /// Constructor 
        /// </summary>
        public UpstreamAmiServiceBehavior()
        {
            this.m_restClientResolver = ApplicationServiceContext.Current.GetService<IRestClientFactory>();
            this.m_upstreamService = ApplicationServiceContext.Current.GetService<IUpstreamIntegrationService>();
            this.m_upstreamAvailabilityProvider = ApplicationServiceContext.Current.GetService<IUpstreamAvailabilityProvider>();
            this.m_dataCachingService = ApplicationServiceContext.Current.GetService<IDataCachingService>();
            this.m_entityRepository = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Entity>>();
            this.m_actRepository = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Act>>();
        }

        /// <summary>
        /// Create a proxy client with appropriate headers
        /// </summary>
        private IRestClient CreateProxyClient()
        {
            var retVal = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.AdministrationIntegrationService);

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
            switch (RestOperationContext.Current.IncomingRequest.ContentType)
            {
                case SanteDBExtendedMimeTypes.JsonPatch:
                case SanteDBExtendedMimeTypes.JsonRimModel:
                case SanteDBExtendedMimeTypes.JsonViewModel:
                case SanteDBExtendedMimeTypes.XmlPatch:
                case SanteDBExtendedMimeTypes.XmlRimModel:
                case "application/json":
                case "application/json+sdb-viewmodel":
                case "application/xml": // We want to use the XML format for serialization
                    retVal.Accept = "application/xml";
                    break;
                default:
                    retVal.Accept = RestOperationContext.Current.IncomingRequest.ContentType ?? retVal.Accept;
                    break;
            }
            //}

            retVal.Requesting += (o, e) =>
            {
                var inboundHeaders = RestOperationContext.Current.IncomingRequest.Headers;
                if (!String.IsNullOrEmpty(inboundHeaders[ExtendedHttpHeaderNames.ViewModelHeaderName]))
                {
                    e.AdditionalHeaders.Add(ExtendedHttpHeaderNames.ViewModelHeaderName, inboundHeaders[ExtendedHttpHeaderNames.ViewModelHeaderName]);
                }
                else if (!String.IsNullOrEmpty(RestOperationContext.Current.IncomingRequest.QueryString[QueryControlParameterNames.HttpViewModelParameterName]))
                {
                    e.AdditionalHeaders.Add(ExtendedHttpHeaderNames.ViewModelHeaderName, RestOperationContext.Current.IncomingRequest.QueryString[QueryControlParameterNames.HttpViewModelParameterName]);
                }
                e.Query?.Remove("_upstream"); // Don't cascade the upstream query to the upstream

            };

            retVal.Responded += (o, e) =>
            {
                var responseHeaders = RestOperationContext.Current.OutgoingResponse.Headers;
                if (e.Headers?.ContainsKey("Content-Disposition") == true)
                {
                    responseHeaders.Add("Content-Disposition", e.Headers["Content-Disposition"]);
                    RestOperationContext.Current.OutgoingResponse.ContentType = e.ContentType;
                }
            };
            return retVal;
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

        /// <summary>
        /// Tag the object if it is only upstream or if it exists locally 
        /// </summary>
        private void TagUpstream(params object[] dataObjects)
        {
            foreach (var data in dataObjects)
            {
                if (data is Entity entity &&
                        this.m_entityRepository?.Query(o => o.Key == entity.Key, AuthenticationContext.SystemPrincipal).Any() != true)
                {
                    entity.AddTag(SystemTagNames.UpstreamDataTag, "true");
                }
                else if (data is Act act &&
                    this.m_actRepository?.Query(o => o.Key == act.Key, AuthenticationContext.SystemPrincipal).Any() != true)
                {
                    act.AddTag(SystemTagNames.UpstreamDataTag, "true");
                }
                else if (data is Bundle bundle)
                {
                    this.TagUpstream(bundle.Item.ToArray());
                }
                else if (data is AmiCollection coll)
                {
                    this.TagUpstream(coll.CollectionItem.ToArray());
                }
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object Create(string resourceType, object data)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {

                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            return restClient.Post<Object, Object>($"{resourceType}", data);
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
                return base.Create(resourceType, data);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object CreateUpdate(string resourceType, string key, object data)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            if (Guid.TryParse(key, out Guid uuid))
                            {
                                this.m_dataCachingService.Remove(uuid);
                            }
                            return restClient.Post<Object, Object>($"{resourceType}/{key}", data);
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
                return base.CreateUpdate(resourceType, key, data);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object Delete(string resourceType, string key)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            if (Guid.TryParse(key, out Guid uuid))
                            {
                                this.m_dataCachingService.Remove(uuid);
                            }
                            return restClient.Delete<Object>($"{resourceType}/{key}");
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
                return base.Delete(resourceType, key);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object Get(string resourceType, string key)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            var retVal = restClient.Get<Object>($"{resourceType}/{key}", RestOperationContext.Current.IncomingRequest.QueryString);
                            this.TagUpstream(retVal);
                            return retVal;
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
                return base.Get(resourceType, key);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object GetVersion(string resourceType, string key, string versionKey)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            return restClient.Get<Object>($"{resourceType}/{key}/_history/{versionKey}", RestOperationContext.Current.IncomingRequest.QueryString);
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
                return base.GetVersion(resourceType, key, versionKey);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override AmiCollection History(string resourceType, string key)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            return restClient.Get<AmiCollection>($"{resourceType}/{key}/_history");
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
                return base.History(resourceType, key);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override ServiceResourceOptions ResourceOptions(string resourceType)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            return restClient.Options<ServiceResourceOptions>($"{resourceType}");
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
                return base.ResourceOptions(resourceType);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override AmiCollection Search(string resourceType)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            var retVal = restClient.Get<AmiCollection>($"{resourceType}", RestOperationContext.Current.IncomingRequest.QueryString);
                            this.TagUpstream(retVal);
                            return retVal;
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
                return base.Search(resourceType);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object Update(string resourceType, string key, object data)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            if (Guid.TryParse(key, out Guid uuid))
                            {
                                this.m_dataCachingService.Remove(uuid);
                            }
                            return restClient.Put<Object, Object>($"{resourceType}/{key}", data);
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
                return base.Update(resourceType, key, data);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object Lock(string resourceType, string key)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            return restClient.Lock<Object>($"{resourceType}/{key}");
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
                return base.Lock(resourceType, key);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object UnLock(string resourceType, string key)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            return restClient.Unlock<Object>($"{resourceType}/{key}");
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
                return base.UnLock(resourceType, key);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object CheckIn(string resourceType, string key)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            return restClient.Invoke<Object, Object>("CHECKIN", $"{resourceType}/{key}", null);
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
                return base.CheckIn(resourceType, key);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object CheckOut(string resourceType, string key)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            return restClient.Invoke<Object, Object>("CHECKOUT", $"{resourceType}/{key}", null);
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
                return base.CheckOut(resourceType, key);
            }
        }
        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object InvokeMethod(string resourceType, string operationName, ParameterCollection body)
        {
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            return restClient.Post<object, object>($"{resourceType}/${operationName}", body);
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
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            if (Guid.TryParse(id, out Guid uuid))
                            {
                                this.m_dataCachingService.Remove(uuid);
                            }
                            return restClient.Post<object, object>($"{resourceType}/{id}/${operationName}", body);
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
                return base.InvokeMethod(resourceType, id, operationName, body);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override AmiCollection AssociationSearch(string resourceType, string key, string childResourceType)
        {
            // Perform only on the external server
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            // This NVC is UTF8 compliant
                            var nvc = RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString();
                            var retVal = restClient.Get<object>($"/{resourceType}/{key}/{childResourceType}", nvc);

                            this.TagUpstream(retVal);
                            return retVal as AmiCollection;
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
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
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
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                            var retVal = restClient.Get<object>($"{resourceType}/{key}/{childResourceType}/{scopedEntityKey}");
                            this.TagUpstream(retVal);
                            return retVal;
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
                return base.AssociationGet(resourceType, key, childResourceType, scopedEntityKey);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpUpstreamParameterName, typeof(bool), "When true, forces this API to relay the caller's query to the configured upstream server")]
        public override object AssociationCreate(string resourceType, string key, string childResourceType, object body)
        {
            if (this.ShouldForwardRequest())
            {
                if (this.m_upstreamAvailabilityProvider.IsAvailable(Core.Interop.ServiceEndpointType.AdministrationIntegrationService))
                {
                    try
                    {
                        using (var restClient = this.CreateProxyClient())
                        {
                            restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);

                            if (Guid.TryParse(key, out Guid uuid))
                            {
                                this.m_dataCachingService.Remove(uuid);
                            }
                            if (body is IAnnotatedResource ide && ide.Key.HasValue)
                            {
                                this.m_dataCachingService.Remove(ide.Key.Value);
                            }

                            return restClient.Post<object, object>($"{resourceType}/{key}/{childResourceType}", body);
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
                return base.AssociationCreate(resourceType, key, childResourceType, body);
            }
        }
    }
}
