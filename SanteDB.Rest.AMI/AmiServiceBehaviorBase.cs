using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Patch;
using System.Diagnostics;
using SanteDB.Core.Diagnostics;
using System.IO;
using SanteDB.Core.Model.Interfaces;
using System.Net;
using System.ServiceModel.Channels;
using SanteDB.Rest.Common;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Security;
using System.Security.Permissions;
using SanteDB.Core.Model.AMI.Logging;
using System.Reflection;
using SanteDB.Core.Model.AMI.Security;
using System.Xml.Serialization;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Services;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.AMI;
using RestSrvr.Attributes;
using RestSrvr;
using SanteDB.Rest.AMI;
using SanteDB.Core;
using SanteDB.Rest.Common.Attributes;

namespace SanteDB.Messaging.AMI.Wcf
{
    /// <summary>
    /// Implementation of the AMI service behavior
    /// </summary>
    [ServiceBehavior(Name = "AMI", InstanceMode = ServiceInstanceMode.PerCall)]
    public abstract class AmiServiceBehaviorBase : IAmiServiceContract
    {

        // The trace source for logging
        protected Tracer m_traceSource = Tracer.GetTracer(typeof(AmiServiceBehaviorBase));

        private ResourceHandlerTool m_resourceHandler;

        /// <summary>
        /// Create the AMI service handler with the specified resource handler resolver
        /// </summary>
        public AmiServiceBehaviorBase(ResourceHandlerTool resourceHandler)
        {
            this.m_resourceHandler = resourceHandler;
        }

        /// <summary>
        /// Create a diagnostic report
        /// </summary>
        public abstract DiagnosticReport CreateDiagnosticReport(DiagnosticReport report);

        /// <summary>
        /// Gets a specific log file.
        /// </summary>
        /// <param name="logId">The log identifier.</param>
        /// <returns>Returns the log file information.</returns>
        public abstract LogFileInfo GetLog(string logId);

        /// <summary>
        /// Get log files on the server and their sizes.
        /// </summary>
        /// <returns>Returns a collection of log files.</returns>
        public abstract AmiCollection GetLogs();

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
        /// Gets the schema for the administrative interface.
        /// </summary>
        /// <param name="schemaId">The id of the schema to be retrieved.</param>
        /// <returns>Returns the administrative interface schema.</returns>
        public XmlSchema GetSchema(int schemaId)
        {
            try
            {
                XmlSchemas schemaCollection = new XmlSchemas();

                XmlReflectionImporter importer = new XmlReflectionImporter("http://santedb.org/ami");
                XmlSchemaExporter exporter = new XmlSchemaExporter(schemaCollection);

                foreach (var cls in this.m_resourceHandler.Handlers.Select(o => o.Type))
                    exporter.ExportTypeMapping(importer.ImportTypeMapping(cls, "http://santedb.org/ami"));

                if (schemaId > schemaCollection.Count)
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    return null;
                }
                else
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.OK;
                    RestOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
                    return schemaCollection[schemaId];
                }
            }
            catch (Exception e)
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                this.m_traceSource.TraceError(e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Gets a server diagnostic report.
        /// </summary>
        /// <returns>Returns the created diagnostic report.</returns>
        public abstract DiagnosticReport GetServerDiagnosticReport();

        /// <summary>
        /// Get a list of TFA mechanisms
        /// </summary>
        /// <returns>Returns a list of TFA mechanisms.</returns>
        public abstract AmiCollection GetTfaMechanisms();

        /// <summary>
        /// Gets options for the AMI service.
        /// </summary>
        /// <returns>Returns options for the AMI service.</returns>
        public abstract ServiceOptions Options();
        
        /// <summary>
        /// Perform a ping
        /// </summary>
        public void Ping()
        {
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Creates security reset information
        /// </summary>
        public abstract void SendTfaSecret(TfaRequestInfo resetInfo);
        
        /// <summary>
        /// Creates the specified resource for the AMI service 
        /// </summary>
        /// <param name="resourceType">The type of resource being created</param>
        /// <param name="data">The resource data being created</param>
        /// <returns>The created the data</returns>
        public virtual Object Create(string resourceType, Object data)
        {
            this.ThrowIfNotReady();

            try
            {

                IResourceHandler handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IResourceHandler.Create));
                    var retVal = handler.Create(data, false);

                    var versioned = retVal as IVersionedEntity;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = retVal == null ? (int)HttpStatusCode.NoContent : (int)System.Net.HttpStatusCode.Created;
                    RestOperationContext.Current.OutgoingResponse.SetETag((retVal as IAmiIdentified)?.Tag ?? (retVal as IdentifiedData)?.Tag ?? Guid.NewGuid().ToString());
                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/history/{2}",
                           RestOperationContext.Current.IncomingRequest.Url,
                           (retVal as IdentifiedData).Key,
                           versioned.Key));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            (retVal as IAmiIdentified)?.Key ?? (retVal as IdentifiedData)?.Key.ToString()));
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
        /// Create or update the specific resource
        /// </summary>
        public virtual Object CreateUpdate(string resourceType, string key, Object data)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    if (data is IdentifiedData)
                        (data as IdentifiedData).Key = Guid.Parse(key);
                    else if (data is IAmiIdentified)
                        (data as IAmiIdentified).Key = key;

                    this.AclCheck(handler, nameof(IResourceHandler.Create));
                    var retVal = handler.Create(data, true) as IdentifiedData;
                    var versioned = retVal as IVersionedEntity;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;
                    RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);

                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/history/{2}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            retVal.Key,
                            versioned.Key));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}",
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
        /// Delete the specified resource
        /// </summary>
        public virtual Object Delete(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {

                    this.AclCheck(handler, nameof(IResourceHandler.Obsolete));
                    var retVal = handler.Obsolete(Guid.Parse(key)) as IdentifiedData;

                    var versioned = retVal as IVersionedEntity;

                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;
                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/history/{2}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            retVal.Key,
                            versioned.Key));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}",
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
        /// Get the specified resource
        /// </summary>
        public virtual Object Get(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {


                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    object strongKey = key;
                    Guid guidKey = Guid.Empty;
                    if (Guid.TryParse(key, out guidKey))
                        strongKey = guidKey;

                    this.AclCheck(handler, nameof(IResourceHandler.Get));
                    var retVal = handler.Get(strongKey, Guid.Empty);
                    if (retVal == null)
                        throw new FileNotFoundException(key);

                    var idata = retVal as IdentifiedData;
                    var adata = retVal as IAmiIdentified;

                    var tag = idata?.Tag ?? adata?.Tag;
                    if(!String.IsNullOrEmpty(tag))
                        RestOperationContext.Current.OutgoingResponse.SetETag(tag);
                    RestOperationContext.Current.OutgoingResponse.SetLastModified((idata?.ModifiedOn.DateTime ?? adata?.ModifiedOn.DateTime ?? DateTime.Now));

                    // HTTP IF headers?
                    if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince().HasValue &&
                        (adata?.ModifiedOn ?? idata?.ModifiedOn) <= RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() ||
                        RestOperationContext.Current.IncomingRequest.GetIfNoneMatch()?.Any(o => idata?.Tag == o || adata?.Tag == o) == true)
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
        /// Get a specific version of the resource 
        /// </summary>
        public virtual Object GetVersion(string resourceType, string key, string versionKey)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    object strongKey = key, strongVersionKey = versionKey;
                    Guid guidKey = Guid.Empty;
                    if (Guid.TryParse(key, out guidKey))
                        strongKey = guidKey;
                    if (Guid.TryParse(versionKey, out guidKey))
                        strongVersionKey = guidKey;

                    this.AclCheck(handler, nameof(IResourceHandler.Get));
                    var retVal = handler.Get(strongKey, strongVersionKey) as IdentifiedData;
                    if (retVal == null)
                        throw new FileNotFoundException(key);


                    RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
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
        /// Get the complete history of a resource 
        /// </summary>
        public virtual AmiCollection History(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);

                if (handler != null)
                {
                    String since = RestOperationContext.Current.IncomingRequest.QueryString["_since"];
                    Guid sinceGuid = since != null ? Guid.Parse(since) : Guid.Empty;

                    // Query 
                    this.AclCheck(handler, nameof(IResourceHandler.Get));
                    var retVal = handler.Get(Guid.Parse(key), Guid.Empty) as IVersionedEntity;
                    List<IVersionedEntity> histItm = new List<IVersionedEntity>() { retVal };
                    while (retVal.PreviousVersionKey.HasValue)
                    {
                        retVal = handler.Get(Guid.Parse(key), retVal.PreviousVersionKey.Value) as IVersionedEntity;
                        if (retVal != null)
                            histItm.Add(retVal);
                        // Should we stop fetching?
                        if (retVal?.VersionKey == sinceGuid)
                            break;

                    }

                    // Lock the item
                    return new AmiCollection(histItm, 0, histItm.Count);
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
        /// Get options / capabilities of a specific object
        /// </summary>
        public virtual ServiceResourceOptions ResourceOptions(string resourceType)
        {
            var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
            if (handler == null)
                throw new FileNotFoundException(resourceType);
            else
                return new ServiceResourceOptions(resourceType, handler.Capabilities);
        }

        /// <summary>
        /// Performs a search of the specified AMI resource
        /// </summary>
        public virtual AmiCollection Search(string resourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    String offset = RestOperationContext.Current.IncomingRequest.QueryString["_offset"],
                        count = RestOperationContext.Current.IncomingRequest.QueryString["_count"];

                    var query = RestOperationContext.Current.IncomingRequest.QueryString.ToQuery();

                    // Modified on?
                    if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince().HasValue)
                        query.Add("modifiedOn", ">" + RestOperationContext.Current.IncomingRequest.GetIfModifiedSince().Value.ToString("o"));

                    // No obsoletion time?
                    if (typeof(BaseEntityData).IsAssignableFrom(handler.Type) && !query.ContainsKey("obsoletionTime"))
                        query.Add("obsoletionTime", "null");

                    int totalResults = 0;

                    // Lean mode
                    var lean = RestOperationContext.Current.IncomingRequest.QueryString["_lean"];
                    bool parsedLean = false;
                    bool.TryParse(lean, out parsedLean);

                    this.AclCheck(handler, nameof(IResourceHandler.Query));
                    var retVal = handler.Query(query, Int32.Parse(offset ?? "0"), Int32.Parse(count ?? "100"), out totalResults).ToList();
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(retVal.OfType<IdentifiedData>().OrderByDescending(o => o.ModifiedOn).FirstOrDefault()?.ModifiedOn.DateTime ?? DateTime.Now);

                    // Last modification time and not modified conditions
                    if ((RestOperationContext.Current.IncomingRequest.GetIfModifiedSince().HasValue ||
                        RestOperationContext.Current.IncomingRequest.GetIfNoneMatch() != null) &&
                        totalResults == 0)
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.NotModified;
                        return null;
                    }
                    else
                    {
                        return new AmiCollection(retVal, Int32.Parse(offset ?? "0"), totalResults);
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
        /// Updates the specified object on the server
        /// </summary>
        public virtual Object Update(string resourceType, string key, Object data)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    if (data is IdentifiedData)
                        (data as IdentifiedData).Key = Guid.Parse(key);
                    else if (data is IAmiIdentified)
                        (data as IAmiIdentified).Key = key;

                    this.AclCheck(handler, nameof(IResourceHandler.Update));
                    var retVal = handler.Update(data);
                    if (retVal == null)
                        RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.NoContent;
                    else
                        RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;

                    var versioned = retVal as IVersionedEntity;
                    RestOperationContext.Current.OutgoingResponse.SetETag((retVal as IdentifiedData)?.Tag);

                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/history/{2}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            (retVal as IIdentifiedEntity)?.Key?.ToString() ?? (retVal as IAmiIdentified)?.Key,
                            versioned.Key));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            (retVal as IIdentifiedEntity)?.Key?.ToString() ?? (retVal as IAmiIdentified)?.Key));

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
        /// Perform a head operation
        /// </summary>
        /// <param name="resourceType">The resource type</param>
        /// <param name="id">The id of the resource</param>
        public virtual void Head(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            this.Get(resourceType, id);
        }

        /// <summary>
        /// Service is not ready
        /// </summary>
        protected abstract void ThrowIfNotReady();

        /// <summary>
        /// Lock resource
        /// </summary>
        public virtual object Lock(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {

                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null && handler is ILockableResourceHandler)
                {
                    this.AclCheck(handler, nameof(ILockableResourceHandler.Lock));
                    var retVal = (handler as ILockableResourceHandler).Lock(Guid.Parse(key));
                    if (retVal == null)
                        throw new FileNotFoundException(key);

                    var idata = retVal as IdentifiedData;
                    var adata = retVal as IAmiIdentified;

                    RestOperationContext.Current.OutgoingResponse.SetETag(idata?.Tag ?? adata?.Tag);
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(idata?.ModifiedOn.DateTime ?? adata?.ModifiedOn.DateTime ?? DateTime.Now);

                    // HTTP IF headers?
                    RestOperationContext.Current.OutgoingResponse.StatusCode = retVal == null ? (int)HttpStatusCode.NoContent : (int)HttpStatusCode.Created;
                    return retVal;
                }
                else if (handler == null)
                    throw new FileNotFoundException(resourceType);
                else
                    throw new NotSupportedException();
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Unlock resource
        /// </summary>
        public virtual object UnLock(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {

                var handler = this.m_resourceHandler.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null && handler is ILockableResourceHandler)
                {
                    this.AclCheck(handler, nameof(ILockableResourceHandler.Unlock));
                    var retVal = (handler as ILockableResourceHandler).Unlock(Guid.Parse(key));
                    if (retVal == null)
                        throw new FileNotFoundException(key);

                    var idata = retVal as IdentifiedData;
                    var adata = retVal as IAmiIdentified;

                    RestOperationContext.Current.OutgoingResponse.SetETag(idata?.Tag ?? adata?.Tag);
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(idata?.ModifiedOn.DateTime ?? adata?.ModifiedOn.DateTime ?? DateTime.Now);

                    // HTTP IF headers?
                    RestOperationContext.Current.OutgoingResponse.StatusCode = retVal == null ? (int)HttpStatusCode.NoContent : (int)HttpStatusCode.Created;
                    return retVal;
                }
                else if (handler == null)
                    throw new FileNotFoundException(resourceType);
                else
                    throw new NotSupportedException();
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
