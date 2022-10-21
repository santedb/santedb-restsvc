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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.AMI.Configuration;
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

namespace SanteDB.Rest.AMI
{
    /// <summary>
    /// Administration Management Interface (AMI)
    /// </summary>
    /// <remarks>Represents a generic implementation of the Administrative Management Interface (AMI) contract</remarks>
    [ServiceBehavior(Name = "AMI", InstanceMode = ServiceInstanceMode.Singleton)]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class AmiServiceBehavior : ResourceServiceBehaviorBase<IAmiServiceContract>, IAmiServiceContract
    {
        /// <summary>
        /// Trace source for logging
        /// </summary>
        protected readonly Tracer m_traceSource = Tracer.GetTracer(typeof(AmiServiceBehavior));
        private readonly ILocalizationService m_localizationService;
        private readonly IPatchService m_patchService;
        private readonly IConfigurationManager m_configurationManager;
        private readonly IServiceManager m_serviceManager;

        /// <summary>
        /// The resource handler tool for executing operations
        /// </summary>
        private ResourceHandlerTool m_resourceHandler;

        /// <summary>
        /// Default CTOR for rest creation
        /// </summary>
        public AmiServiceBehavior() :
            this(
                ApplicationServiceContext.Current.GetService<ILocalizationService>(),
                ApplicationServiceContext.Current.GetService<IConfigurationManager>(),
                ApplicationServiceContext.Current.GetService<IServiceManager>(),
                ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>(),
                ApplicationServiceContext.Current.GetService<IPatchService>())
        {
        }

        /// <summary>
        /// AMI Service Behavior constructor
        /// </summary>
        public AmiServiceBehavior(ILocalizationService localizationService, IConfigurationManager configurationManager, IServiceManager serviceManager, IPolicyEnforcementService policyEnforcementService, IPatchService patchService = null)
            : base(AmiMessageHandler.ResourceHandler, new Tracer(nameof(AmiServiceBehavior)), configurationManager, serviceManager, policyEnforcementService, patchService)
        {
            
        }

        /// <inheritdoc />
        protected override RestCollectionBase CreateResultCollection(IEnumerable<object> result, int offset, int totalCount)
            => new AmiCollection(result, offset, totalCount);


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
                {
                    exporter.ExportTypeMapping(importer.ImportTypeMapping(cls, "http://santedb.org/ami"));
                }

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
        /// Perform a ping
        /// </summary>
        public void Ping()
        {
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.NoContent;
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
                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));
                    var retVal = handler.Get(Guid.Parse(key), Guid.Empty) as IVersionedData;
                    List<IVersionedData> histItm = new List<IVersionedData>() { retVal };
                    while (retVal.PreviousVersionKey.HasValue)
                    {
                        retVal = handler.Get(Guid.Parse(key), retVal.PreviousVersionKey.Value) as IVersionedData;
                        if (retVal != null)
                        {
                            histItm.Add(retVal);
                        }
                        // Should we stop fetching?
                        if (retVal?.VersionKey == sinceGuid)
                        {
                            break;
                        }
                    }

                    // Lock the item
                    return new AmiCollection(histItm, 0, histItm.Count);
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }



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
                    {
                        throw new FileNotFoundException(key);
                    }

                    var idata = retVal as IdentifiedData;
                    var adata = retVal as IAmiIdentified;

                    RestOperationContext.Current.OutgoingResponse.SetETag(idata?.Tag ?? adata?.Tag);
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(idata?.ModifiedOn.DateTime ?? adata?.ModifiedOn.DateTime ?? DateTime.Now);

                    // HTTP IF headers?
                    RestOperationContext.Current.OutgoingResponse.StatusCode = retVal == null ? (int)HttpStatusCode.NoContent : (int)HttpStatusCode.Created;
                    return retVal;
                }
                else if (handler == null)
                {
                    throw new FileNotFoundException(resourceType);
                }
                else
                {
                    throw new NotSupportedException();
                }
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
                    {
                        throw new FileNotFoundException(key);
                    }

                    var idata = retVal as IdentifiedData;
                    var adata = retVal as IAmiIdentified;

                    RestOperationContext.Current.OutgoingResponse.SetETag(idata?.Tag ?? adata?.Tag);
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(idata?.ModifiedOn.DateTime ?? adata?.ModifiedOn.DateTime ?? DateTime.Now);

                    // HTTP IF headers?
                    RestOperationContext.Current.OutgoingResponse.StatusCode = retVal == null ? (int)HttpStatusCode.NoContent : (int)HttpStatusCode.Created;
                    return retVal;
                }
                else if (handler == null)
                {
                    throw new FileNotFoundException(resourceType);
                }
                else
                {
                    throw new NotSupportedException();
                }
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