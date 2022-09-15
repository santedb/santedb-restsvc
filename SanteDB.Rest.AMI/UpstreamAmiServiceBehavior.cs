using RestSrvr;
using RestSrvr.Exceptions;
using SanteDB.Core;
using SanteDB.Core.Http;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        private readonly INetworkInformationService m_networkInformationService;
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
            this.m_networkInformationService = ApplicationServiceContext.Current.GetService<INetworkInformationService>();
            this.m_dataCachingService = ApplicationServiceContext.Current.GetService<IDataCachingService>();
            this.m_entityRepository = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Entity>>();
            this.m_actRepository = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Act>>();
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
                    entity.AddTag("$upstream", "true");
                else if (data is Act act &&
                    this.m_actRepository?.Query(o => o.Key == act.Key, AuthenticationContext.SystemPrincipal).Any() != true)
                    act.AddTag("$upstream", "true");
                else if (data is Bundle bundle)
                    this.TagUpstream(bundle.Item.ToArray());
                else if (data is AmiCollection coll)
                    this.TagUpstream(coll.CollectionItem.ToArray());
            }
        }

        /// <summary>
        /// Create the specified resource
        /// </summary>
        public override object Create(string resourceType, object data)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        return restClient.Post<Object, Object>($"{resourceType}", data);
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
                return base.Create(resourceType, data);
            }
        }

        /// <summary>
        /// Create or update the specified resource
        /// </summary>
        public override object CreateUpdate(string resourceType, string key, object data)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        if (Guid.TryParse(key, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }
                        return restClient.Post<Object, Object>($"{resourceType}/{key}", data);
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
                return base.CreateUpdate(resourceType, key, data);
            }
        }

        /// <summary>
        /// Delete the specified object
        /// </summary>
        public override object Delete(string resourceType, string key)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        if (Guid.TryParse(key, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }
                        return restClient.Delete<Object>($"{resourceType}/{key}");
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
                return base.Delete(resourceType, key);
            }
        }

        /// <summary>
        /// Get the specified object
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public override object Get(string resourceType, string key)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        return restClient.Get<Object>($"{resourceType}/{key}");
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
                return base.Get(resourceType, key);
            }
        }

        /// <summary>
        /// Get version
        /// </summary>
        public override object GetVersion(string resourceType, string key, string versionKey)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        return restClient.Get<Object>($"{resourceType}/{key}/history/{versionKey}");
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
                return base.GetVersion(resourceType, key, versionKey);
            }
        }

        /// <summary>
        /// Get the history of the object
        /// </summary>
        public override AmiCollection History(string resourceType, string key)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        return restClient.Get<AmiCollection>($"{resourceType}/{key}/history");
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
                return base.History(resourceType, key);
            }
        }

        /// <summary>
        /// Get resource options
        /// </summary>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public override ServiceResourceOptions ResourceOptions(string resourceType)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);
                        return restClient.Options<ServiceResourceOptions>($"{resourceType}");
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
        /// Perform a search on the AMI
        /// </summary>
        public override AmiCollection Search(string resourceType)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);
                        return restClient.Get<AmiCollection>($"{resourceType}", RestOperationContext.Current.IncomingRequest.QueryString.ToList().ToArray());
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
        /// Update the object
        /// </summary>
        public override object Update(string resourceType, string key, object data)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        if (Guid.TryParse(key, out Guid uuid))
                        {
                            this.m_dataCachingService.Remove(uuid);
                        }
                        return restClient.Put<Object, Object>($"{resourceType}/{key}", data);
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
                return base.Update(resourceType, key, data);
            }
        }

        /// <summary>
        /// Lock the specified object
        /// </summary>
        public override object Lock(string resourceType, string key)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        return restClient.Lock<Object>($"{resourceType}/{key}");
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
                return base.Lock(resourceType, key);
            }
        }

        /// <summary>
        /// Unlock the resource
        /// </summary>
        public override object UnLock(string resourceType, string key)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        return restClient.Unlock<Object>($"{resourceType}/{key}");
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
                return base.UnLock(resourceType, key);
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
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);
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
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);
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

        /// <summary>
        /// Associated object search
        /// </summary>
        public override AmiCollection AssociationSearch(string resourceType, string key, string childResourceType)
        {
            // Perform only on the external server
            if (RestOperationContext.Current.IncomingRequest.QueryString["_upstream"] == "true" ||
                RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-Upstream"] == "true")
            {
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(Core.Interop.ServiceEndpointType.AdministrationIntegrationService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        // This NVC is UTF8 compliant
                        var nvc = RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString();
                        var retVal = restClient.Get<object>($"/{resourceType}/{key}/{childResourceType}", nvc.ToArray());

                        this.TagUpstream(retVal);
                        return retVal as AmiCollection;
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
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.AdministrationIntegrationService);
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
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.AdministrationIntegrationService);
                        restClient.Responded += (o, e) => RestOperationContext.Current.OutgoingResponse.SetETag(e.ETag);
                        var retVal = restClient.Get<object>($"{resourceType}/{key}/{childResourceType}/{scopedEntityKey}");
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
                if (this.m_networkInformationService.IsNetworkAvailable)
                    try
                    {
                        var restClient = this.m_restClientResolver.GetRestClientFor(ServiceEndpointType.AdministrationIntegrationService);
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
    }
}
