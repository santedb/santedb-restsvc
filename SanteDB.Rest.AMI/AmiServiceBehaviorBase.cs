/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2021-8-5
 */

using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Exceptions;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.AMI.Logging;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.AMI;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SanteDB.Messaging.AMI.Wcf
{
    /// <summary>
    /// Administration Management Interface (AMI)
    /// </summary>
    /// <remarks>Represents a generic implementation of the Administrative Management Interface (AMI) contract</remarks>
    [ServiceBehavior(Name = "AMI", InstanceMode = ServiceInstanceMode.Singleton)]
    public abstract class AmiServiceBehaviorBase : IAmiServiceContract
    {
        /// <summary>
        /// Trace source for logging
        /// </summary>
        protected readonly Tracer m_traceSource = Tracer.GetTracer(typeof(AmiServiceBehaviorBase));

        /// <summary>
        /// The resource handler tool for executing operations
        /// </summary>
        protected abstract ResourceHandlerTool GetResourceHandler();

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
        /// Get TFA mechanisms in this service
        /// </summary>
        /// <returns></returns>
        public abstract AmiCollection GetTfaMechanisms();

        /// <summary>
        /// Get the log stream
        /// </summary>
        public abstract Stream DownloadLog(String logId);

        /// <summary>
        /// Get log files on the server and their sizes.
        /// </summary>
        /// <returns>Returns a collection of log files.</returns>
        public abstract AmiCollection GetLogs();

        /// <summary>
        /// Perform an ACL check
        /// </summary>
        private void AclCheck(Object handler, String action)
        {
            foreach (var dmn in this.GetDemands(handler, action))
                ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>().Demand(dmn);
        }

        /// <summary>
        /// Get demands
        /// </summary>
        private String[] GetDemands(object handler, string action)
        {
            var demands = handler.GetType().GetMethods().Where(o => o.Name == action).SelectMany(method => method.GetCustomAttributes<DemandAttribute>());
            if (demands.Any(o => o.Override))
                return demands.Where(o => o.Override).Select(o => o.PolicyId).ToArray();
            else
                return demands.Select(o => o.PolicyId).ToArray();
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

                foreach (var cls in this.GetResourceHandler().Handlers.Select(o => o.Type))
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
        /// Gets options for the AMI service.
        /// </summary>
        /// <returns>Returns options for the AMI service.</returns>
        public abstract ServiceOptions Options();

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
                // First we load
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType);

                if (handler == null)
                    throw new FileNotFoundException(resourceType);

                // Validate
                var match = RestOperationContext.Current.IncomingRequest.Headers["If-Match"];
                if (match == null && typeof(IVersionedEntity).IsAssignableFrom(handler.Type))
                    throw new InvalidOperationException("Missing If-Match header for versioned objects");

                // Next we get the current version
                this.AclCheck(handler, nameof(IApiResourceHandler.Get));

                var rawExisting = handler.Get(Guid.Parse(id), Guid.Empty);
                IdentifiedData existing = (rawExisting as ISecurityEntityInfo)?.ToIdentifiedData() ?? rawExisting as IdentifiedData;

                // Object cannot be patched
                if (existing == null)
                    throw new NotSupportedException();

                var force = Convert.ToBoolean(RestOperationContext.Current.IncomingRequest.Headers["X-Patch-Force"] ?? "false");

                if (existing == null)
                    throw new FileNotFoundException($"/{resourceType}/{id}");
                else if (!String.IsNullOrEmpty(match) && (existing as IdentifiedData)?.Tag != match && !force)
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
                    this.AclCheck(handler, nameof(IApiResourceHandler.Update));
                    var updateResult = handler.Update(applied);
                    var data = (updateResult as ISecurityEntityInfo)?.ToIdentifiedData() ?? updateResult as IdentifiedData;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 204;
                    RestOperationContext.Current.OutgoingResponse.SetETag(data.Tag);
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(applied.ModifiedOn.DateTime);
                    var versioned = (data as IVersionedEntity)?.VersionKey;
                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/history/{2}",
                                RestOperationContext.Current.IncomingRequest.Url,
                                id,
                                versioned));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}",
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
        /// Perform a ping
        /// </summary>
        public void Ping()
        {
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.NoContent;
        }

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
                IApiResourceHandler handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Create));
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
                throw new Exception($"Error creating resource {resourceType}", e);
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
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    if (data is IdentifiedData)
                        (data as IdentifiedData).Key = Guid.Parse(key);
                    else if (data is IAmiIdentified)
                        (data as IAmiIdentified).Key = key;

                    this.AclCheck(handler, nameof(IApiResourceHandler.Create));
                    var retVal = handler.Create(data, true);
                    var versioned = retVal as IVersionedEntity;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;

                    if (retVal is IdentifiedData)
                        RestOperationContext.Current.OutgoingResponse.SetETag((retVal as IdentifiedData).Tag);

                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/history/{2}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            versioned.Key,
                            versioned.VersionKey));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}",
                            RestOperationContext.Current.IncomingRequest.Url,
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
        /// Delete the specified resource
        /// </summary>
        public virtual Object Delete(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Obsolete));

                    object retVal = null;
                    if (Guid.TryParse(key, out Guid uuid))
                        retVal = handler.Obsolete(uuid);
                    else
                        retVal = handler.Obsolete(key);

                    var versioned = retVal as IVersionedEntity;

                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;
                    if (retVal is IdentifiedData)
                        RestOperationContext.Current.OutgoingResponse.SetETag((retVal as IdentifiedData).Tag);

                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/history/{2}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            versioned.Key,
                            versioned.VersionKey));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}",
                            RestOperationContext.Current.IncomingRequest.Url,
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
        /// Get the specified resource
        /// </summary>
        public virtual Object Get(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    object strongKey = key;
                    Guid guidKey = Guid.Empty;
                    if (Guid.TryParse(key, out guidKey))
                        strongKey = guidKey;

                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));
                    var retVal = handler.Get(strongKey, Guid.Empty);
                    if (retVal == null)
                        throw new FileNotFoundException(key);

                    var idata = retVal as IdentifiedData;
                    var adata = retVal as IAmiIdentified;

                    var tag = idata?.Tag ?? adata?.Tag;
                    if (!String.IsNullOrEmpty(tag))
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
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    object strongKey = key, strongVersionKey = versionKey;
                    Guid guidKey = Guid.Empty;
                    if (Guid.TryParse(key, out guidKey))
                        strongKey = guidKey;
                    if (Guid.TryParse(versionKey, out guidKey))
                        strongVersionKey = guidKey;

                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));
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
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType);

                if (handler != null)
                {
                    String since = RestOperationContext.Current.IncomingRequest.QueryString["_since"];
                    Guid sinceGuid = since != null ? Guid.Parse(since) : Guid.Empty;

                    // Query
                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));
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
        /// Options resource
        /// </summary>
        public virtual ServiceResourceOptions ResourceOptions(string resourceType)
        {
            var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType);
            if (handler == null)
                throw new FileNotFoundException(resourceType);
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
                if (ApplicationServiceContext.Current.GetService<IPatchService>() != null &&
                    handler.Capabilities.HasFlag(ResourceCapabilityType.Update))
                    caps.Add(new ServiceResourceCapability(ResourceCapabilityType.Patch, this.GetDemands(handler, nameof(IApiResourceHandler.Update))));

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
        /// Performs a search of the specified AMI resource
        /// </summary>
        public virtual AmiCollection Search(string resourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Query));

                    // Send the query to the resource handler
                    var query = NameValueCollection.ParseQueryString(RestOperationContext.Current.IncomingRequest.Url.Query);

                    // Modified on?
                    if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null)
                        query.Add("modifiedOn", ">" + RestOperationContext.Current.IncomingRequest.GetIfModifiedSince()?.ToString("o"));

                    // Query for results
                    var results = handler.Query(query);

                    // Now apply controls
                    var retVal = results.ApplyResultInstructions(query, out int offset, out int totalCount).OfType<Object>();

                    RestOperationContext.Current.OutgoingResponse.SetLastModified((retVal.OfType<IdentifiedData>().OrderByDescending(o => o.ModifiedOn).FirstOrDefault()?.ModifiedOn.DateTime ?? DateTime.Now));

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
                        return new AmiCollection(retVal, offset, totalCount);
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error searching resource type {resourceType}", e);
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
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    // Get target of update and ensure
                    if (data is IdentifiedData)
                    {
                        var iddata = data as IdentifiedData;
                        if (iddata.Key.HasValue && iddata.Key != Guid.Parse(key))
                            throw new FaultException(400, "Key mismatch");
                        iddata.Key = Guid.Parse(key);
                    }
                    else if (data is IAmiIdentified)
                    {
                        var iddata = data as IAmiIdentified;
                        if (!String.IsNullOrEmpty(iddata.Key) && iddata.Key != key)
                            throw new FaultException(400, "Key mismatch");
                        iddata.Key = key;
                    }

                    this.AclCheck(handler, nameof(IApiResourceHandler.Update));
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
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString().Replace("{", "{{").Replace("}", "}}")));
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
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType);
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
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType);
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

        /// <summary>
        /// Perform a search on the specified entity
        /// </summary>
        public virtual AmiCollection AssociationSearch(string resourceType, string key, string childResourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IApiResourceHandler.Query));

                    // Send the query to the resource handler
                    var query = NameValueCollection.ParseQueryString(RestOperationContext.Current.IncomingRequest.Url.Query);

                    // Modified on?
                    if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null)
                        query.Add("modifiedOn", ">" + RestOperationContext.Current.IncomingRequest.GetIfModifiedSince()?.ToString("o"));

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
                    var retVal = results.ApplyResultInstructions(query, out int offset, out int totalCount).OfType<Object>();

                    RestOperationContext.Current.OutgoingResponse.SetLastModified((retVal.OfType<IdentifiedData>().OrderByDescending(o => o.ModifiedOn).FirstOrDefault()?.ModifiedOn.DateTime ?? DateTime.Now));

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
                        return new AmiCollection(retVal, offset, totalCount);
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
        /// Create an associated entity
        /// </summary>
        public virtual object AssociationCreate(string resourceType, string key, string childResourceType, object body)
        {
            this.ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.AddChildObject));
                    object retVal = null;
                    if (Guid.TryParse(key, out Guid uuid))
                        retVal = handler.AddChildObject(uuid, childResourceType, body);
                    else
                        retVal = handler.AddChildObject(key, childResourceType, body);

                    var versioned = retVal as IVersionedEntity;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = retVal == null ? (int)HttpStatusCode.NoContent : (int)System.Net.HttpStatusCode.Created;
                    RestOperationContext.Current.OutgoingResponse.SetETag((retVal as IAmiIdentified)?.Tag ?? (retVal as IdentifiedData)?.Tag ?? Guid.NewGuid().ToString());
                    RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/{3}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            key,
                            childResourceType,
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
        /// Removes an associated entity from the scoping property path
        /// </summary>
        public virtual object AssociationRemove(string resourceType, string key, string childResourceType, string scopedEntityKey)
        {
            this.ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.RemoveChildObject));

                    object retVal = null;
                    if (Guid.TryParse(key, out Guid uuid) && Guid.TryParse(scopedEntityKey, out Guid scopedUuid))
                        retVal = handler.RemoveChildObject(uuid, childResourceType, scopedUuid);
                    else
                        retVal = handler.RemoveChildObject(key, childResourceType, scopedEntityKey);

                    var versioned = retVal as IVersionedEntity;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.OK;
                    RestOperationContext.Current.OutgoingResponse.SetETag((retVal as IAmiIdentified)?.Tag ?? (retVal as IdentifiedData)?.Tag ?? Guid.NewGuid().ToString());
                    RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/{3}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            key,
                            childResourceType,
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
        /// Removes an associated entity from the scoping property path
        /// </summary>
        public virtual object AssociationGet(string resourceType, string key, string childResourceType, string scopedEntityKey)
        {
            this.ThrowIfNotReady();

            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.GetChildObject));

                    object retVal = null;
                    if (Guid.TryParse(key, out Guid uuid) && Guid.TryParse(scopedEntityKey, out Guid scopedUuid))
                        retVal = handler.GetChildObject(uuid, childResourceType, scopedUuid);
                    else
                        retVal = handler.GetChildObject(key, childResourceType, scopedEntityKey);

                    var versioned = retVal as IVersionedEntity;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.OK;
                    RestOperationContext.Current.OutgoingResponse.SetETag((retVal as IAmiIdentified)?.Tag ?? (retVal as IdentifiedData)?.Tag ?? Guid.NewGuid().ToString());
                    RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/{3}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            key,
                            childResourceType,
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
        /// Invoke the specified method on the API
        /// </summary>
        public virtual object InvokeMethod(string resourceType, string id, string operationName, ApiOperationParameterCollection body)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType) as IOperationalApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IOperationalApiResourceHandler.InvokeOperation));

                    var retValRaw = handler.InvokeOperation(id, operationName, body);

                    if (retValRaw is IdentifiedData retVal)
                    {
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
                    }
                    return retValRaw;
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
        /// Check-in the specified object
        /// </summary>
        public virtual object CheckIn(string resourceType, string key)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType) as ICheckoutResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(ICheckoutResourceHandler.CheckIn));
                    return handler.CheckIn(Guid.Parse(key));
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
        /// Check-out the specified object
        /// </summary>
        public virtual object CheckOut(string resourceType, string key)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType) as ICheckoutResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(ICheckoutResourceHandler.CheckIn));
                    return handler.CheckOut(Guid.Parse(key));
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
        /// Invoke a method which is not tied to a classifier object
        /// </summary>
        public virtual object InvokeMethod(string resourceType, string operationName, ApiOperationParameterCollection body)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = this.GetResourceHandler().GetResourceHandler<IAmiServiceContract>(resourceType) as IOperationalApiResourceHandler;
                if (handler != null)
                {
                    this.AclCheck(handler, nameof(IOperationalApiResourceHandler.InvokeOperation));

                    var retValRaw = handler.InvokeOperation(null, operationName, body);

                    if (retValRaw is IdentifiedData retVal)
                    {
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
                    }
                    return retValRaw;
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
    }
}