using RestSrvr;
using RestSrvr.Attributes;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SanteDB.Rest.HDSI
{
    /// <summary>
    /// Data implementation
    /// </summary>
    [ServiceBehavior(Name = "HDSI", InstanceMode = ServiceInstanceMode.PerCall)]
    [Description("Health Data Service Interface")]
    public abstract class HdsiServiceBehaviorBase : IHdsiServiceContract
    {
        // Trace source
        protected Tracer m_traceSource = Tracer.GetTracer(typeof(HdsiServiceBehaviorBase));
        // Resource Handler
        protected ResourceHandlerTool m_resourceHandler;

        /// <summary>
        /// HDSI Service Behavior
        /// </summary>
        public HdsiServiceBehaviorBase(ResourceHandlerTool handler)
        {
            this.m_resourceHandler = handler;
        }

        /// <summary>
        /// Callers provide the demand method for access control
        /// </summary>
        protected abstract void Demand(String policyId);

        /// <summary>
        /// Perform an ACL check
        /// </summary>
        private void AclCheck(Object handler, String action)
        {
            foreach (var dmn in handler.GetType().GetMethods().Where(o => o.Name == action).SelectMany(method => method.GetCustomAttributes<DemandAttribute>()))
                this.Demand(dmn.PolicyId);
        }

        /// <summary>
        /// Ping the server
        /// </summary>
        public void Ping()
        {
            this.ThrowIfNotReady();
            RestOperationContext.Current.OutgoingResponse.StatusCode = 204;
        }

        /// <summary>
        /// Create the specified resource
        /// </summary>
        public virtual IdentifiedData Create(string resourceType, IdentifiedData body)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {

                    this.AclCheck(handler, nameof(IResourceHandler.Create));
                    var retVal = handler.Create(body, false) as IdentifiedData;

                    var versioned = retVal as IVersionedEntity;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/history/{3}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            retVal.Key,
                            versioned.Key));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            retVal.Key));

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
        /// Create or update the specified object
        /// </summary>
        public virtual IdentifiedData CreateUpdate(string resourceType, string id, IdentifiedData body)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IResourceHandler.Create));
                    var retVal = handler.Create(body, true) as IdentifiedData;
                    var versioned = retVal as IVersionedEntity;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);

                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/history/{3}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            retVal.Key,
                            versioned.Key));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            retVal.Key));

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
        /// Get the specified object
        /// </summary>
        public virtual IdentifiedData Get(string resourceType, string id)
        {
            this.ThrowIfNotReady();

            try
            {


                var handler = this.m_resourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IResourceHandler.Get));
                    var retVal = handler.Get(Guid.Parse(id), Guid.Empty) as IdentifiedData;
                    if (retVal == null)
                        throw new FileNotFoundException(id);

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

                    else if (RestOperationContext.Current.IncomingRequest.QueryString["_bundle"] == "true" ||
                        RestOperationContext.Current.IncomingRequest.QueryString["_all"] == "true")
                    {
                        retVal = retVal.GetLocked();
                        ObjectExpander.ExpandProperties(retVal, SanteDB.Core.Model.Query.NameValueCollection.ParseQueryString(RestOperationContext.Current.IncomingRequest.Url.Query));
                        ObjectExpander.ExcludeProperties(retVal, SanteDB.Core.Model.Query.NameValueCollection.ParseQueryString(RestOperationContext.Current.IncomingRequest.Url.Query));
                        return Bundle.CreateBundle(retVal);
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
        /// Gets a specific version of a resource
        /// </summary>
        public virtual IdentifiedData GetVersion(string resourceType, string id, string versionId)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IResourceHandler.Get));
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
                throw;

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


                foreach (var cls in this.m_resourceHandler.Handlers.Where(o => o.Scope == typeof(IHdsiServiceContract)).Select(o => o.Type))
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
        public virtual IdentifiedData History(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);

                if (handler != null)
                {
                    String since = RestOperationContext.Current.IncomingRequest.QueryString["_since"];
                    Guid sinceGuid = since != null ? Guid.Parse(since) : Guid.Empty;

                    // Query 
                    this.AclCheck(handler, nameof(IResourceHandler.Get));
                    var retVal = handler.Get(Guid.Parse(id), Guid.Empty) as IVersionedEntity;
                    List<IVersionedEntity> histItm = new List<IVersionedEntity>() { retVal };
                    while (retVal.PreviousVersionKey.HasValue)
                    {
                        retVal = handler.Get(Guid.Parse(id), retVal.PreviousVersionKey.Value) as IVersionedEntity;
                        if (retVal != null)
                            histItm.Add(retVal);
                        // Should we stop fetching?
                        if (retVal?.VersionKey == sinceGuid)
                            break;

                    }

                    // Lock the item
                    return BundleUtil.CreateBundle(histItm.OfType<IdentifiedData>(), histItm.Count, 0, false);
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
        /// Perform a search on the specified resource type
        /// </summary>
        public virtual IdentifiedData Search(string resourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    String offset = RestOperationContext.Current.IncomingRequest.QueryString["_offset"],
                        count = RestOperationContext.Current.IncomingRequest.QueryString["_count"];

                    var query = RestOperationContext.Current.IncomingRequest.QueryString.ToQuery();

                    // Modified on?
                    if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null)
                        query.Add("modifiedOn", ">" + RestOperationContext.Current.IncomingRequest.GetIfModifiedSince()?.ToString("o"));

                    // No obsoletion time?
                    if (typeof(BaseEntityData).IsAssignableFrom(handler.Type) && !query.ContainsKey("obsoletionTime"))
                        query.Add("obsoletionTime", "null");

                    int totalResults = 0;

                    // Lean mode
                    var lean = RestOperationContext.Current.IncomingRequest.QueryString["_lean"];
                    bool parsedLean = false;
                    bool.TryParse(lean, out parsedLean);

                    this.AclCheck(handler, nameof(IResourceHandler.Query));

                    var retVal = handler.Query(query, Int32.Parse(offset ?? "0"), Int32.Parse(count ?? "100"), out totalResults).OfType<IdentifiedData>().Select(o => o.GetLocked()).ToList();
                    RestOperationContext.Current.OutgoingResponse.SetLastModified((retVal.OrderByDescending(o => o.ModifiedOn).FirstOrDefault()?.ModifiedOn.DateTime ?? DateTime.Now));


                    // Last modification time and not modified conditions
                    if ((RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null ||
                        RestOperationContext.Current.IncomingRequest.GetIfNoneMatch() != null) &&
                        totalResults == 0)
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 304;
                        return null;
                    }
                    else
                    {
                        if (query.ContainsKey("_all") || query.ContainsKey("_expand") || query.ContainsKey("_exclude"))
                        {
                            var wtp = ApplicationServiceContext.Current.GetService<IThreadPoolService>();
                            retVal.AsParallel().Select((itm) =>
                            {
                                try
                                {
                                    var i = itm as IdentifiedData;
                                    ObjectExpander.ExpandProperties(i, query);
                                    ObjectExpander.ExcludeProperties(i, query);
                                    return true;
                                }
                                catch (Exception e)
                                {
                                    this.m_traceSource.TraceError("Error setting properties: {0}", e);
                                    return false;
                                }
                            }).ToList();
                        }

                        return BundleUtil.CreateBundle(retVal, totalResults, Int32.Parse(offset ?? "0"), parsedLean);
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
                var handler = this.m_resourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IResourceHandler.Update));

                    var retVal = handler.Update(body) as IdentifiedData;

                    var versioned = retVal as IVersionedEntity;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 200;
                    RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);

                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/history/{3}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            retVal.Key,
                            versioned.Key));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            retVal.Key));

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
        /// Obsolete the specified data
        /// </summary>
        public virtual IdentifiedData Delete(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IResourceHandler.Obsolete));
                    var retVal = handler.Obsolete(Guid.Parse(id)) as IdentifiedData;

                    var versioned = retVal as IVersionedEntity;

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/history/{3}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            retVal.Key,
                            versioned.Key));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            retVal.Key));

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

                // Match bin
                var versionId = Guid.ParseExact(match, "N");

                // First we load
                var handler = this.m_resourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);

                if (handler == null)
                    throw new FileNotFoundException(resourceType);

                // Next we get the current version
                this.AclCheck(handler, nameof(IResourceHandler.Get));

                var existing = handler.Get(Guid.Parse(id), Guid.Empty) as IdentifiedData;
                var force = Convert.ToBoolean(RestOperationContext.Current.IncomingRequest.Headers["X-Patch-Force"] ?? "false");

                if (existing == null)
                    throw new FileNotFoundException($"/{resourceType}/{id}/history/{versionId}");
                else if (existing.Tag != match && !force)
                {
                    this.m_traceSource.TraceError("Object {0} ETAG is {1} but If-Match specified {2}", existing.Key, existing.Tag, match);
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 409;
                    RestOperationContext.Current.OutgoingResponse.StatusDescription = "Conflict";
                    return;
                }
                else if (body == null)
                    throw new ArgumentNullException(nameof(body));
                else
                {
                    // Force load all properties for existing
                    var applied = ApplicationServiceContext.Current.GetService<IPatchService>().Patch(body, existing, force);
                    this.AclCheck(handler, nameof(IResourceHandler.Update));
                    var data = handler.Update(applied) as IdentifiedData;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 204;
                    RestOperationContext.Current.OutgoingResponse.SetETag(data.Tag);
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(applied.ModifiedOn.DateTime);
                    var versioned = (data as IVersionedEntity)?.VersionKey;
                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/history/{3}",
                                RestOperationContext.Current.IncomingRequest.Url,
                                id,
                                versioned));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}",
                                RestOperationContext.Current.IncomingRequest.Url,
                                id));
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
        /// Gets the specifieed patch id
        /// </summary>
        public virtual Patch GetPatch(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            throw new NotImplementedException();
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
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Allow", $"GET, PUT, POST, OPTIONS, HEAD, DELETE{(ApplicationServiceContext.Current.GetService<IPatchService>() != null ? ", PATCH" : null)}");
                if (ApplicationServiceContext.Current.GetService<IPatchService>() != null)
                    RestOperationContext.Current.OutgoingResponse.Headers.Add("Accept-Patch", "application/xml+oiz-patch");

                // Service options
                var retVal = new ServiceOptions()
                {
                    InterfaceVersion = "1.0.0.0",
                    Services = new List<ServiceResourceOptions>()
                    {
                        new ServiceResourceOptions()
                        {
                            ResourceName = null
                        },
                        new ServiceResourceOptions()
                        {
                            ResourceName = "time",
                            Capabilities = ResourceCapability.Get
                        }
                    }
                };

                // Get the resources which are supported
                foreach (var itm in this.m_resourceHandler.Handlers)
                {
                    var svc = new ServiceResourceOptions()
                    {
                        ResourceName = itm.ResourceName,
                        Capabilities = itm.Capabilities
                    };
                    if (ApplicationServiceContext.Current.GetService<IPatchService>() != null)
                        svc.Capabilities |= ResourceCapability.Patch;
                    retVal.Services.Add(svc);
                }

                return retVal;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
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

            var handler = this.m_resourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
            if (handler == null)
                throw new FileNotFoundException(resourceType);
            else
                return new ServiceResourceOptions(resourceType, handler.Capabilities);
        }
    }
}
