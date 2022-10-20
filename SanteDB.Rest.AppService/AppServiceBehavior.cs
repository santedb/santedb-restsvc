using RestSrvr;
using RestSrvr.Attributes;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.AppService.Model;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml.XPath;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior (APP)
    /// </summary>
    [ServiceBehavior(Name = "APP", InstanceMode = ServiceInstanceMode.Singleton)]
    public class AppServiceBehavior : IAppServiceContract
    {
        /// <summary>
        /// Tracer for diagnostics information.
        /// </summary>
        protected readonly Tracer _Tracer;
        readonly IPatchService _PatchService;
        readonly IConfigurationManager _ConfigurationManager;
        readonly IServiceManager _ServiceManager;
        readonly IPolicyEnforcementService _PolicyEnforcementService;
        readonly ResourceHandlerTool _ResourceHandler;

        /// <summary>
        /// Instantiates a new instance of the behavior.
        /// </summary>
        public AppServiceBehavior()
            : this(
                  ApplicationServiceContext.Current.GetService<IConfigurationManager>(),
                  ApplicationServiceContext.Current.GetService<IServiceManager>(),
                  ApplicationServiceContext.Current.GetService<IPatchService>(),
                  ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>()
                  )
        { }
        

        /// <summary>
        /// DI constructor.
        /// </summary>
        /// <param name="configurationManager"></param>
        /// <param name="serviceManager"></param>
        /// <param name="patchService"></param>
        public AppServiceBehavior(IConfigurationManager configurationManager, IServiceManager serviceManager, IPatchService patchService, IPolicyEnforcementService policyEnforcementService)
        {
            _Tracer = new Tracer(nameof(AppServiceBehavior));
            _ConfigurationManager = configurationManager;
            _ServiceManager = serviceManager;
            _PatchService = patchService;
            _PolicyEnforcementService = policyEnforcementService;
            _ResourceHandler = AppServiceMessageHandler.ResourceHandler;
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
        /// Perform an ACL check
        /// </summary>
        private void AclCheck(Object handler, String action)
        {
            foreach (var dmn in this.GetDemands(handler, action))
            {
                _PolicyEnforcementService.Demand(dmn);
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

        /// <inheritdoc />
        public object AssociationCreate(string resourceType, string id, string childResourceType, object body)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object AssociationGet(string resourceType, string id, string childResourceType, string childKey)
        {
            
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object AssociationRemove(string resourceType, string id, string childResourceType, string childKey)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object AssociationSearch(string resourceType, string id, string childResourceType)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object CheckIn(string resourceType, string id)
        {
            throw new NotImplementedException();
        }

        
        /// <inheritdoc />
        public object CheckOut(string resourceType, string id)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Create(string resourceType, object data)
        {
            ThrowIfNotReady();

            try
            {
                var handler = _ResourceHandler.GetResourceHandler<IAppServiceContract>(resourceType);

                if (null != handler)
                {
                    //AclCheck(handler, nameof(IApiResourceHandler.Create));

                    var result = handler.Create(data, false);

                    OutgoingResponse.StatusCode = null == result ? (int)HttpStatusCode.NoContent : (int)HttpStatusCode.Created;
                    OutgoingResponse.SetETag((result as IdentifiedData)?.Tag ?? Guid.NewGuid().ToString()); //TODO: Some sort of IAmiIdentified interface here?
                    if (result is IVersionedData versioned)
                    {
                        OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, $"{IncomingRequest.Url}/{versioned.Key}/history/{versioned.VersionKey}");
                    }
                    else if (result is IIdentifiedData identified)
                    {
                        OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, $"{IncomingRequest.Url}/{identified.Key}");
                    }


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
                _Tracer.TraceError("{0} - {1}", remoteendpoint?.Address, ex.ToString());
                throw new Exception($"Error when creating resource type {resourceType}", ex);
            }
        }

        /// <inheritdoc />
        public object CreateUpdate(string resourceType, string key, object data)
        {
            ThrowIfNotReady();
            try
            {
                var handler = _ResourceHandler.GetResourceHandler<IAppServiceContract>(resourceType);
                if (handler != null)
                {
                    if (data is IdentifiedData)
                    {
                        (data as IdentifiedData).Key = Guid.Parse(key);
                    }

                    //AclCheck(handler, nameof(IApiResourceHandler.Create));
                    
                    var retVal = handler.Create(data, true);

                    OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;
                    OutgoingResponse.SetETag((retVal as IdentifiedData)?.Tag ?? Guid.NewGuid().ToString()); //TODO: Some sort of IAmiIdentified interface here?

                    if (retVal is IVersionedData versioned)
                    {
                        OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/history/{2}",
                            IncomingRequest.Url,
                            versioned.Key,
                            versioned.VersionKey));
                    }
                    else if (retVal is IIdentifiedData identified)
                    {
                        OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}",
                            IncomingRequest.Url,
                            identified.Key.ToString()));
                    }

                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
            {
                var remoteEndpoint = IncomingRequest.RemoteEndPoint;
                _Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, ex.ToString()));
                throw new Exception($"Error when creating resource type {resourceType}", ex);
            }
        }

        /// <inheritdoc />
        public object Delete(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = _ResourceHandler.GetResourceHandler<IAppServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Delete));

                    object retVal = null;
                    if (Guid.TryParse(key, out Guid uuid))
                    {
                        retVal = handler.Delete(uuid);
                    }
                    else
                    {
                        retVal = handler.Delete(key); //TODO: Really? 
                    }

                    {
                        if (retVal is IdentifiedData identified)
                        {
                            OutgoingResponse.SetETag(identified.Tag);
                        }
                        else if (retVal is IVersionedData versioned)
                        {
                            OutgoingResponse.SetETag($"{versioned.Key}.{versioned.VersionKey}");
                        }
                    }

                    OutgoingResponse.StatusCode = (int)HttpStatusCode.Created; //TODO: Validate were doing logical deletes?

                    {
                        if (retVal is IVersionedData versioned)
                        {
                            OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/history/{2}",
                                IncomingRequest.Url,
                                versioned.Key,
                                versioned.VersionKey));
                        }
                        else if (retVal is IIdentifiedData identified)
                        {
                            OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}",
                                IncomingRequest.Url,
                                identified.Key.ToString()));
                        }
                    }

                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
            {
                var remoteEndpoint = IncomingRequest.RemoteEndPoint;
                _Tracer.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, ex.ToString()));
                throw new Exception($"Exception deleting resource type {resourceType}", ex);
            }
        }

        /// <inheritdoc />
        public object Get(string resourceType, string key)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object InvokeMethod(string resourceType, string operationName, ParameterCollection body)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object InvokeMethod(string resourceType, string id, string operationName, ParameterCollection body)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Search(string resourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = _ResourceHandler.GetResourceHandler<IAppServiceContract>(resourceType);
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

                    OutgoingResponse.SetLastModified((retval.OfType<IdentifiedData>().OrderByDescending(o => o.ModifiedOn).FirstOrDefault()?.ModifiedOn.DateTime ?? DateTime.Now));

                    if ((null != ifmodifiedsince && null != ifnonematch) && totalcount == 0)
                    {
                        OutgoingResponse.StatusCode = (int)HttpStatusCode.NotModified;
                        return null;
                    }
                    else
                    {
                        return new AppServiceCollection(retval, offset, totalcount);
                    }
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch(Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
            {
                var remoteendpoint = IncomingRequest.RemoteEndPoint;
                _Tracer.TraceError("{0} - {1}", remoteendpoint?.Address, ex.ToString());
                throw new Exception($"Error when searching for resource type {resourceType}", ex);
            }
        }

        /// <inheritdoc />
        public object Update(string resourceType, string key, object data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets options for the APP service. This is called by the metadata service to document options for the service.
        /// </summary>
        /// <returns>Returns options for the APP service.</returns>
        public virtual ServiceOptions Options()
        {
            this.ThrowIfNotReady();

            if (null != _PatchService)
            {
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Accept-Patch", "application/xml+sdb-patch");
            }

            // mex configuration
            var mexConfig = _ConfigurationManager.GetSection<SanteDB.Rest.Common.Configuration.RestConfigurationSection>();
            String boundHostPort = IncomingRequest.Url.GetLeftPart(UriPartial.Authority);
            if (!string.IsNullOrEmpty(mexConfig.ExternalHostPort))
            {
                var tUrl = new Uri(mexConfig.ExternalHostPort);
                boundHostPort = $"{tUrl.Scheme}://{tUrl.Host}:{tUrl.Port}";
            }

            var serviceOptions = new ServiceOptions
            {
                InterfaceVersion = typeof(AppServiceBehavior).Assembly.GetName().Version.ToString(),
                Endpoints = _ServiceManager.GetServices().OfType<IApiEndpointProvider>().Select(o =>
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
            if (null != _ResourceHandler?.Handlers)
            {
                foreach (var itm in _ResourceHandler.Handlers)
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
            var handler = _ResourceHandler.GetResourceHandler<IAppServiceContract>(resourceType);
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
                if (_PatchService != null &&
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
                // Associateive
                return new ServiceResourceOptions(resourceType, handler.Type, caps, childResources);
            }
        }

        /// <summary>
        /// Get demands
        /// </summary>
        private string[] GetDemands(object handler, string action)
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
    }
}
