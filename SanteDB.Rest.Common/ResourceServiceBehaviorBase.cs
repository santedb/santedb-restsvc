using RestSrvr;
using RestSrvr.Exceptions;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Patch;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TContract"></typeparam>
    public abstract class ResourceServiceBehaviorBase<TContract>
    {
        protected ResourceHandlerTool ResourceHandler { get; }
        protected Tracer Tracer { get; }

        protected IConfigurationManager ConfigurationManager { get; }
        protected IServiceManager ServiceManager { get; }
        protected IPatchService PatchService { get; }
        protected IPolicyEnforcementService PolicyEnforcementService { get; }

        public ResourceServiceBehaviorBase(ResourceHandlerTool resourceHandler, Tracer tracer, IConfigurationManager configurationManager, IServiceManager serviceManager, IPolicyEnforcementService policyEnforcementService, IPatchService patchService = null)
        {
            ResourceHandler = resourceHandler;
            Tracer = tracer;
            ServiceManager = serviceManager;
            ConfigurationManager = configurationManager;
            PolicyEnforcementService = policyEnforcementService;
            PatchService = patchService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <param name="offset"></param>
        /// <param name="totalCount"></param>
        /// <returns></returns>
        protected abstract RestCollectionBase CreateResultCollection(IEnumerable<object> result, int offset, int totalCount);

        /// <summary>
        /// Gets an etag for the data provided. Implementers are free to override the default implementation if the resources have different tag types and interfaces.
        /// </summary>
        /// <param name="data">The object to derive an etag from</param>
        /// <returns>The etag for the data if one can be derived.</returns>
        /// <remarks><para>The default implementation will search for <see cref="IIdentifiedResource"/>, then <see cref="IdentifiedData"/>. If neither is available, <c>null</c> is returned.</para></remarks>
        protected virtual string GetETagFromData(object data)
        {
            if (data is IIdentifiedResource resource)
            {
                return resource.Tag;
            }

            if (data is IdentifiedData identifiedData)
            {
                return identifiedData.Tag;
            }

            return null;
        }

        /// <summary>
        /// Gets the resource location which will be used in the Location header of operations which do not return the resource directly.
        /// </summary>
        /// <param name="data">The object to get the location for.</param>
        /// <returns>The location that can be inserted into the header.</returns>
        /// <remarks>The default implementation looks for <see cref="IVersionedData"/>, <see cref="IIdentifiedResource"/>, <see cref="IdentifiedData"/> in that order. If none match, <c>null</c> is returned.</remarks>
        protected virtual string GetResourceLocation(object data)
        {
            if (data is IVersionedData versioned)
            {
                return $"{IncomingRequest.Url}/{versioned.Key}/history/{versioned.VersionKey}";
            }
            else if (data is IIdentifiedResource idr)
            {
                return $"{IncomingRequest.Url}/{idr.Key}";
            }
            else if (data is IdentifiedData id)
            {
                return $"{IncomingRequest.Url}/{id.Key}";
            }

            return null;
        }

        /// <summary>
        /// Throws a <see cref="DomainStateException"/> when the service is not ready to process a request. This is interpreted to produce a 503 error to the client.
        /// </summary>
        protected virtual void ThrowIfNotReady()
        {
            if (!ApplicationServiceContext.Current.IsRunning)
            {
                throw new DomainStateException();
            }
        }

        /// <summary>
        /// The incoming request object of the current request.
        /// </summary>
        protected HttpListenerRequest IncomingRequest => RestOperationContext.Current.IncomingRequest;
        /// <summary>
        /// The outgoing response object of the current request.
        /// </summary>
        protected HttpListenerResponse OutgoingResponse => RestOperationContext.Current.OutgoingResponse;

        /// <summary>
        /// Attempts to set the location header on the outgoing response if one is available from <see cref="GetResourceLocation(object)"/>.
        /// </summary>
        /// <param name="data">The object to get the location for.</param>
        private void AddContentLocationHeader(object data)
        {
            var contentlocation = GetResourceLocation(data);

            if (null != contentlocation)
            {
                OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, contentlocation);
            }
        }

        private string AddEtagHeader(object data, bool useGuid = false)
        {
            var tag = GetETagFromData(data);

            if (useGuid && null == tag)
            {
                tag = Guid.NewGuid().ToString();
            }

            if (null != tag)
            {
                OutgoingResponse.SetETag(tag);
            }

            return tag;
        }

        private DateTime? AddLastModifiedHeader(object data, bool useCurrentTime = false)
        {
            if (data is IIdentifiedResource idr)
            {
                OutgoingResponse.SetLastModified(idr.ModifiedOn.DateTime);
                return idr.ModifiedOn.DateTime;
            }
            else if (data is IdentifiedData id)
            {
                OutgoingResponse.SetLastModified(id.ModifiedOn.DateTime);
                return id.ModifiedOn.DateTime;
            }
            else if (useCurrentTime)
            {
                var now = DateTime.Now;
                OutgoingResponse.SetLastModified(now);
                return now;
            }

            return null;
        }

        private bool IsContentNotModified(DateTime? lastModified, string etag)
        {
            if (null != lastModified)
            {
                var ifmodified = IncomingRequest.GetIfModifiedSince();

                if (null != ifmodified)
                {
                    return lastModified <= ifmodified;
                }
            }

            else if (null != etag)
            {
                return IncomingRequest.GetIfNoneMatch()?.Any(inm => inm.Equals(etag)) == true;
            }

            return false;
        }

        /// <summary>
        /// Get demands
        /// </summary>
        protected virtual string[] GetDemands(object handler, string action)
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
        /// Perform an ACL check
        /// </summary>
        protected void AclCheck(object handler, string action)
        {
            foreach (var dmn in this.GetDemands(handler, action))
            {
                PolicyEnforcementService.Demand(dmn);
            }
        }

        /// <summary>
        /// Gets options for the APP service. This is called by the metadata service to document options for the service.
        /// </summary>
        /// <returns>Returns options for the APP service.</returns>
        public virtual ServiceOptions Options()
        {
            ThrowIfNotReady();

            if (null != PatchService)
            {
                OutgoingResponse.Headers.Add("Accept-Patch", "application/xml+sdb-patch");
            }

            // mex configuration
            var mexConfig = ConfigurationManager.GetSection<Configuration.RestConfigurationSection>();
            var boundHostPort = IncomingRequest.Url.GetLeftPart(UriPartial.Authority);
            if (!string.IsNullOrEmpty(mexConfig.ExternalHostPort))
            {
                var tUrl = new Uri(mexConfig.ExternalHostPort);
                boundHostPort = tUrl.GetLeftPart(UriPartial.Authority);
            }

            var serviceOptions = new ServiceOptions
            {
                InterfaceVersion = GetType().Assembly.GetName().Version.ToString(),
                Endpoints = ServiceManager.GetServices().OfType<IApiEndpointProvider>().Select(o =>
                    new ServiceEndpointOptions(o)
                    {
                        BaseUrl = o.Url.Select(url =>
                        {
                            var turi = new Uri(url);
                            return $"{boundHostPort}{turi.AbsolutePath}";
                        }).ToArray()
                    }
                ).ToList()
            };


            // Get the resources which are supported
            if (null != ResourceHandler?.Handlers)
            {
                foreach (var itm in ResourceHandler.Handlers)
                {
                    var svc = this.ResourceOptions(itm.ResourceName);
                    serviceOptions.Resources.Add(svc);
                }
            }

            return serviceOptions;
        }

        /// <summary>
        /// Options resource
        /// </summary>
        public virtual ServiceResourceOptions ResourceOptions(string resourceType)
        {
            var handler = ResourceHandler.GetResourceHandler<TContract>(resourceType);
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
                            return GetDemands(handler, nameof(IApiResourceHandler.Create));

                        case ResourceCapabilityType.Delete:
                            return GetDemands(handler, nameof(IApiResourceHandler.Create));

                        case ResourceCapabilityType.Get:
                        case ResourceCapabilityType.GetVersion:
                            return GetDemands(handler, nameof(IApiResourceHandler.Get));

                        case ResourceCapabilityType.History:
                        case ResourceCapabilityType.Search:
                            return GetDemands(handler, nameof(IApiResourceHandler.Query));

                        case ResourceCapabilityType.Update:
                            return GetDemands(handler, nameof(IApiResourceHandler.Update));

                        default:
                            return new string[] { PermissionPolicyIdentifiers.Login };
                    }
                };

                // Get the resource capabilities
                List<ServiceResourceCapability> caps = handler.Capabilities.ToResourceCapabilityStatement(getCaps).ToList();

                // Patching
                if (PatchService != null &&
                    handler.Capabilities.HasFlag(ResourceCapabilityType.Update))
                {
                    caps.Add(new ServiceResourceCapability(ResourceCapabilityType.Patch, getCaps(ResourceCapabilityType.Update)));
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
                // Associative
                return new ServiceResourceOptions(resourceType, handler.Type, caps, childResources);
            }
        }

        /// <summary>
        /// Perform a head operation
        /// </summary>
        /// <param name="resourceType">The resource type</param>
        /// <param name="id">The id of the resource</param>
        public virtual void Head(string resourceType, string id)
        {
            ThrowIfNotReady();
            Get(resourceType, id);
        }


        /// <inheritdoc />
        public virtual object AssociationCreate(string resourceType, string key, string childResourceType, object body)
        {
            this.ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = ResourceHandler.GetResourceHandler<TContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    AclCheck(handler, nameof(IApiResourceHandler.Get)); //TODO: Validate if we need this.
                    AclCheck(handler, nameof(IChainedApiResourceHandler.AddChildObject));

                    object retVal = null;
                    if (Guid.TryParse(key, out Guid uuid))
                    {
                        retVal = handler.AddChildObject(uuid, childResourceType, body);
                    }
                    else
                    {
                        retVal = handler.AddChildObject(key, childResourceType, body);
                    }

                    AddEtagHeader(retVal);
                    RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/{3}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            key,
                            childResourceType,
                            (retVal as IIdentifiedResource)?.Key ?? (retVal as IdentifiedData)?.Key.ToString()));
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
                Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <inheritdoc />
        public virtual object AssociationGet(string resourceType, string key, string childResourceType, string childKey)
        {
            ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = ResourceHandler.GetResourceHandler<TContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    AclCheck(handler, nameof(IApiResourceHandler.Get)); //TODO: Validate if we need this.
                    AclCheck(handler, nameof(IChainedApiResourceHandler.GetChildObject));

                    object retVal = null;
                    if (Guid.TryParse(key, out Guid uuid) && Guid.TryParse(childKey, out Guid scopedUuid))
                    {
                        retVal = handler.GetChildObject(uuid, childResourceType, scopedUuid);
                    }
                    else
                    {
                        retVal = handler.GetChildObject(key, childResourceType, childKey);
                    }

                    AddEtagHeader(retVal, true);
                    RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, string.Format("{0}/{1}/{2}/{3}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            key,
                            childResourceType,
                            (retVal as IIdentifiedResource)?.Key ?? (retVal as IdentifiedData)?.Key.ToString()));
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
                Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <inheritdoc />
        public virtual object AssociationRemove(string resourceType, string key, string childResourceType, string childKey)
        {
            ThrowIfNotReady();

            try
            {
                var handler = ResourceHandler.GetResourceHandler<TContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    AclCheck(handler, nameof(IApiResourceHandler.Get)); //TODO: Validate if we need this.
                    AclCheck(handler, nameof(IChainedApiResourceHandler.RemoveChildObject));

                    object retVal = null;
                    if (Guid.TryParse(key, out Guid uuid) && Guid.TryParse(childKey, out Guid scopedUuid))
                    {
                        retVal = handler.RemoveChildObject(uuid, childResourceType, scopedUuid);
                    }
                    else
                    {
                        retVal = handler.RemoveChildObject(key, childResourceType, childKey);
                    }



                    //var versioned = retVal as IVersionedData;
                    //RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.OK;
                    AddEtagHeader(retVal, true);
                    RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/{3}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            key,
                            childResourceType,
                            (retVal as IIdentifiedResource)?.Key ?? (retVal as IdentifiedData)?.Key.ToString()));
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
                Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <inheritdoc />
        public virtual object AssociationSearch(string resourceType, string key, string childResourceType)
        {
            ThrowIfNotReady();
            try
            {
                var handler = ResourceHandler.GetResourceHandler<TContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    AclCheck(handler, nameof(IApiResourceHandler.Get)); //TODO: Validate if we need this.
                    AclCheck(handler, nameof(IChainedApiResourceHandler.QueryChildObjects));

                    // Send the query to the resource handler
                    var query = IncomingRequest.Url.Query.ParseQueryString();

                    // Modified on?
                    if (IncomingRequest.GetIfModifiedSince() != null)
                    {
                        query.Add("modifiedOn", ">" + IncomingRequest.GetIfModifiedSince()?.ToString("O"));
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
                    var retVal = results.ApplyResultInstructions(query, out int offset, out int totalCount).OfType<object>();

                    if (null != retVal)
                    {
                        OutgoingResponse.SetLastModified((retVal.OfType<IdentifiedData>().OrderByDescending(o => o.ModifiedOn).FirstOrDefault()?.ModifiedOn.DateTime ?? DateTime.Now));
                    }

                    // Last modification time and not modified conditions
                    if ((RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null ||
                        RestOperationContext.Current.IncomingRequest.GetIfNoneMatch() != null) &&
                        totalCount == 0)
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.NotModified;
                        return null;
                    }
                    else
                    {
                        return CreateResultCollection(retVal, offset, totalCount);
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
                Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <inheritdoc />
        public object CheckIn(string resourceType, string key)
        {
            ThrowIfNotReady();

            try
            {
                var handler = ResourceHandler.GetResourceHandler<TContract>(resourceType) as ICheckoutResourceHandler;
                if (handler != null)
                {
                    AclCheck(handler, nameof(ICheckoutResourceHandler.CheckIn));
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
                Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <inheritdoc />
        public object CheckOut(string resourceType, string key)
        {
            ThrowIfNotReady();

            try
            {
                var handler = ResourceHandler.GetResourceHandler<TContract>(resourceType) as ICheckoutResourceHandler;
                if (handler != null)
                {
                    AclCheck(handler, nameof(ICheckoutResourceHandler.CheckOut));
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
                Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <inheritdoc />
        public virtual object Create(string resourceType, object data)
        {
            ThrowIfNotReady();

            try
            {
                var handler = ResourceHandler.GetResourceHandler<TContract>(resourceType);

                if (null != handler)
                {
                    AclCheck(handler, nameof(IApiResourceHandler.Create));

                    var result = handler.Create(data, false);

                    OutgoingResponse.StatusCode = null == result ? (int)HttpStatusCode.NoContent : (int)HttpStatusCode.Created;

                    AddEtagHeader(result, true);

                    AddContentLocationHeader(result);

                    return result;

                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
            {
                var remoteendpoint = IncomingRequest.RemoteEndPoint;
                Tracer.TraceError("{0} - {1}", remoteendpoint?.Address, ex.ToString());
                throw new Exception($"Error when creating resource type {resourceType}", ex);
            }
        }

        /// <inheritdoc />
        public virtual object CreateUpdate(string resourceType, string key, object data)
        {
            ThrowIfNotReady();
            try
            {
                var handler = ResourceHandler.GetResourceHandler<TContract>(resourceType);
                if (handler != null)
                {
                    if (data is IdentifiedData idd)
                    {
                        idd.Key = Guid.Parse(key);
                    }
                    else if (data is IIdentifiedResource idr)
                    {
                        idr.Key = key;
                    }

                    AclCheck(handler, nameof(IApiResourceHandler.Create));

                    var result = handler.Create(data, true);

                    OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;

                    AddEtagHeader(data, true);

                    AddContentLocationHeader(result);

                    return result;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
            {
                var remoteEndpoint = IncomingRequest.RemoteEndPoint;
                Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, ex.ToString()));
                throw new Exception($"Error when creating resource type {resourceType}", ex);
            }
        }

        /// <inheritdoc />
        public virtual object Delete(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = ResourceHandler.GetResourceHandler<TContract>(resourceType);
                if (handler != null)
                {
                    AclCheck(handler, nameof(IApiResourceHandler.Delete));

                    object result = null;
                    if (Guid.TryParse(key, out Guid uuid))
                    {
                        result = handler.Delete(uuid);
                    }
                    else
                    {
                        result = handler.Delete(key);
                    }

                    OutgoingResponse.StatusCode = (int)HttpStatusCode.Created; //TODO: Validate were doing logical deletes?

                    AddEtagHeader(result, false);

                    AddContentLocationHeader(result);

                    return result;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
            {
                var remoteEndpoint = IncomingRequest.RemoteEndPoint;
                Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, ex.ToString()));
                throw new Exception($"Exception deleting resource type {resourceType}", ex);
            }
        }

        /// <inheritdoc />
        public virtual object Get(string resourceType, string key)
        {
            ThrowIfNotReady();

            try
            {
                var handler = ResourceHandler.GetResourceHandler<TContract>(resourceType);
                if (handler != null)
                {
                    object strongKey = key;
                    Guid guidKey = Guid.Empty;
                    if (Guid.TryParse(key, out guidKey))
                    {
                        strongKey = guidKey;
                    }

                    AclCheck(handler, nameof(IApiResourceHandler.Get));

                    var retVal = handler.Get(strongKey, Guid.Empty);

                    if (retVal == null)
                    {
                        throw new FileNotFoundException(key);
                    }

                    var etag = AddEtagHeader(retVal);

                    var lastmodified = AddLastModifiedHeader(retVal, true);

                    // HTTP IF headers?
                    if (IsContentNotModified(lastmodified, etag))
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.NotModified;
                        return null;
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
                Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }


        /// <summary>
        /// Get a specific version of the resource
        /// </summary>
        public virtual object GetVersion(string resourceType, string key, string versionKey)
        {
            ThrowIfNotReady();
            try
            {
                var handler = ResourceHandler.GetResourceHandler<TContract>(resourceType);
                if (handler != null)
                {
                    object strongKey = key, strongVersionKey = versionKey;
                    Guid guidKey = Guid.Empty;
                    if (Guid.TryParse(key, out guidKey))
                    {
                        strongKey = guidKey;
                    }

                    if (Guid.TryParse(versionKey, out guidKey))
                    {
                        strongVersionKey = guidKey;
                    }

                    AclCheck(handler, nameof(IApiResourceHandler.Get));
                    var retVal = handler.Get(strongKey, strongVersionKey) as IdentifiedData;
                    if (retVal == null)
                    {
                        throw new FileNotFoundException(key);
                    }

                    AddEtagHeader(retVal);
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
                Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <inheritdoc />
        public virtual object InvokeMethod(string resourceType, string operationName, ParameterCollection body)
        {
            return InvokeMethod(resourceType, null, operationName, body);
        }

        /// <inheritdoc />
        public virtual object InvokeMethod(string resourceType, string id, string operationName, ParameterCollection body)
        {
            ThrowIfNotReady();

            try
            {
                var handler = ResourceHandler.GetResourceHandler<TContract>(resourceType) as IOperationalApiResourceHandler;

                if (handler != null)
                {
                    AclCheck(handler, nameof(IOperationalApiResourceHandler.InvokeOperation));

                    var result = handler.InvokeOperation(id, operationName, body);

                    var etag = AddEtagHeader(result);

                    var lastmodified = AddLastModifiedHeader(result);

                    if (IsContentNotModified(lastmodified, etag))
                    {
                        OutgoingResponse.StatusCode = (int)HttpStatusCode.NotModified;
                        return null;
                    }

                    return result;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }

        /// <inheritdoc />
        public virtual object Search(string resourceType)
        {
            ThrowIfNotReady();
            try
            {
                var handler = ResourceHandler.GetResourceHandler<TContract>(resourceType);

                if (null != handler)
                {
                    //AclCheck(handler, nameof(IApiResourceHandler.Query));

                    var query = IncomingRequest.Url.Query.ParseQueryString();

                    var ifmodifiedsince = IncomingRequest.GetIfModifiedSince();
                    var ifnonematch = IncomingRequest.GetIfNoneMatch();

                    if (null != ifmodifiedsince)
                    {
                        query.Add("modifiedOn", $">{ifmodifiedsince?.ToString("O")}");
                    }

                    var results = handler.Query(query);

                    var retval = results.ApplyResultInstructions(query, out int offset, out int totalcount)?.OfType<object>();

                    if (null != retval)
                    {
                        OutgoingResponse.SetLastModified((retval.OfType<IdentifiedData>()?.OrderByDescending(o => o.ModifiedOn)?.FirstOrDefault()?.ModifiedOn.DateTime ?? DateTime.Now));
                    }

                    if ((null != ifmodifiedsince && null != ifnonematch) && totalcount == 0)
                    {
                        OutgoingResponse.StatusCode = (int)HttpStatusCode.NotModified;
                        return null;
                    }
                    else
                    {
                        return CreateResultCollection(retval, offset, totalcount);
                    }
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
            {
                var remoteendpoint = IncomingRequest.RemoteEndPoint;
                Tracer.TraceError("{0} - {1}", remoteendpoint?.Address, ex.ToString());
                throw new Exception($"Error when searching for resource type {resourceType}", ex);
            }
        }

        /// <inheritdoc />
        public virtual object Update(string resourceType, string key, object data)
        {
            ThrowIfNotReady();
            try
            {
                var handler = ResourceHandler.GetResourceHandler<TContract>(resourceType);
                if (handler != null)
                {
                    // Get target of update and ensure
                    switch (data)
                    {
                        case IdentifiedData iddata:
                            if (iddata.Key.HasValue && (!Guid.TryParse(key, out var guidKey) || iddata.Key != guidKey))
                            {
                                throw new FaultException(HttpStatusCode.BadRequest, "Key mismatch");
                            }

                            iddata.Key = guidKey;
                            break;
                    }

                    this.AclCheck(handler, nameof(IApiResourceHandler.Update));
                    var retVal = handler.Update(data);

                    if (retVal == null)
                    {
                        OutgoingResponse.StatusCode = (int)HttpStatusCode.NoContent;
                    }
                    else
                    {
                        OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;
                    }

                    AddEtagHeader(retVal);
                    AddContentLocationHeader(retVal);

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
                Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString().Replace("{", "{{").Replace("}", "}}")));
                throw;
            }
        }


        /// <summary>
        /// Perform a patch on the serviceo
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="id"></param>
        /// <param name="body"></param>
        public virtual void Patch(string resourceType, string id, Patch body)
        {
            ThrowIfNotReady();

            if (null == PatchService)
            {
                throw new NotSupportedException("No patch service is available in the service repository.");
            }

            try
            {
                // First we load
                var handler = ResourceHandler.GetResourceHandler<TContract>(resourceType);

                if (handler == null)
                {
                    throw new FileNotFoundException(resourceType);
                }

                // Validate
                var match = IncomingRequest.GetIfMatch();
                if (match == null && typeof(IVersionedData).IsAssignableFrom(handler.Type))
                {
                    throw new InvalidOperationException("Missing If-Match header for versioned objects");
                }

                // Next we get the current version
                AclCheck(handler, nameof(IApiResourceHandler.Get));

                var rawExisting = handler.Get(Guid.Parse(id), Guid.Empty);
                IdentifiedData existing = /*(rawExisting as ISecurityEntityInfo)?.ToIdentifiedData() ??*/ rawExisting as IdentifiedData;

                // Object cannot be patched
                if (existing == null)
                {
                    throw new NotSupportedException();
                }

                var force = Convert.ToBoolean(IncomingRequest.Headers["X-Patch-Force"] ?? "false");

                if (existing == null)
                {
                    throw new FileNotFoundException($"/{resourceType}/{id}");
                }
                else if (match?.Any(m=> null != existing?.Tag && m == existing?.Tag) != true && !force)
                {
                    Tracer.TraceError("Object {0} ETAG is {1} but If-Match specified {2}", existing.Key, existing.Tag, match);
                    OutgoingResponse.StatusCode = 409;
                    OutgoingResponse.StatusDescription = "Conflict";
                    return;
                }
                else if (body == null)
                {
                    throw new ArgumentNullException(nameof(body));
                }
                else
                {
                    // Force load all properties for existing
                    var applied = PatchService.Patch(body, existing, force);
                    AclCheck(handler, nameof(IApiResourceHandler.Update));
                    var updateResult = handler.Update(applied);
                    var data = /*(updateResult as ISecurityEntityInfo)?.ToIdentifiedData() ?? */ updateResult as IdentifiedData;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.NotModified;
                    AddEtagHeader(data, false);
                    AddLastModifiedHeader(applied);
                    AddContentLocationHeader(data);
                }
            }
            catch (PatchAssertionException e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                Tracer.TraceWarning(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }
    }
}
