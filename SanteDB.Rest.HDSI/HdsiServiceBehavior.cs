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
using SanteDB.Core.Data;
using SanteDB.Core.Data.Initialization;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Attributes;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Security.Privacy;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using SanteDB.Rest.HDSI.Configuration;
using SanteDB.Rest.HDSI.Vrp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SanteDB.Rest.HDSI
{
    /// <summary>
    /// Health Data Service Interface (HDSI)
    /// </summary>
    /// <remarks>Represents generic implementation of the the Health Data Service Interface (HDSI) contract</remarks>
    [ServiceBehavior(Name = HdsiMessageHandler.ConfigurationName, InstanceMode = ServiceInstanceMode.Singleton)]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class HdsiServiceBehavior : IHdsiServiceContract
    {

        private readonly String[] m_nonDisclosureAuditableResourceTypes =
        {
            nameof(ReferenceTerm),
            nameof(ConceptSet),
            nameof(Concept),
            nameof(DeviceEntity),
            nameof(ApplicationEntity),
            nameof(ConceptName),
            nameof(ConceptRelationship),
            nameof(Place),
            nameof(Material),
            nameof(ManufacturedMaterial),
            nameof(Organization)
        };

        /// <summary>
        /// The trace source for HDSI based implementations
        /// </summary>
        protected readonly Tracer m_traceSource = Tracer.GetTracer(typeof(HdsiServiceBehavior));

        // Resource handler tool
        private readonly ResourceHandlerTool m_resourceHandlerTool;

        /// <summary>
        /// Ad-hoc cache method
        /// </summary>
        protected readonly IDataCachingService m_dataCachingService;

        /// <summary>
        /// The configuration loaded/injected into the HDSI
        /// </summary>
        protected readonly HdsiConfigurationSection m_configuration;

        private readonly ModelSerializationBinder m_modelSerlizationBinder = new ModelSerializationBinder();
        /// <summary>
        /// Locale service
        /// </summary>
        protected readonly ILocalizationService m_localeService;
        private readonly IPolicyEnforcementService m_pepService;
        private readonly IPatchService m_patchService;
        private readonly IBarcodeProviderService m_barcodeService;
        private readonly IResourcePointerService m_resourcePointerService;
        private readonly IAuditService m_auditService;

        /// <summary>
        /// Get the resource handler for the named resource
        /// </summary>
        protected IApiResourceHandler GetResourceHandler(String resourceTypeName) => this.m_resourceHandlerTool.GetResourceHandler<IHdsiServiceContract>(resourceTypeName);

        /// <summary>
        /// For REST service initialization
        /// </summary>
        public HdsiServiceBehavior() :
            this(ApplicationServiceContext.Current.GetService<IDataCachingService>(),
                ApplicationServiceContext.Current.GetService<ILocalizationService>(),
                ApplicationServiceContext.Current.GetService<IPatchService>(),
                ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>(),
                ApplicationServiceContext.Current.GetService<IBarcodeProviderService>(),
                ApplicationServiceContext.Current.GetService<IResourcePointerService>(),
                ApplicationServiceContext.Current.GetService<IServiceManager>(),
                ApplicationServiceContext.Current.GetService<IConfigurationManager>(),
                ApplicationServiceContext.Current.GetService<IAuditService>()

                )
        {

        }
        /// <summary>
        /// HDSI Service Behavior
        /// </summary>
        public HdsiServiceBehavior(IDataCachingService dataCache, ILocalizationService localeService, IPatchService patchService, IPolicyEnforcementService pepService, IBarcodeProviderService barcodeService, IResourcePointerService resourcePointerService, IServiceManager serviceManager, IConfigurationManager configurationManager, IAuditService auditBuilder)
        {
            this.m_auditService = auditBuilder;
            this.m_configuration = configurationManager.GetSection<HdsiConfigurationSection>();
            this.m_dataCachingService = dataCache;
            this.m_localeService = localeService;
            this.m_pepService = pepService;
            this.m_patchService = patchService;
            this.m_barcodeService = barcodeService;
            this.m_resourcePointerService = resourcePointerService;
            this.m_resourceHandlerTool = new ResourceHandlerTool(
                        serviceManager.GetAllTypes()
                        .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IApiResourceHandler).IsAssignableFrom(t))
                        .ToList(), typeof(IHdsiServiceContract)
                    );
        }

        /// <summary>
        /// Create content location
        /// </summary>
        protected String CreateContentLocation(params Object[] parts)
        {
            var requestUri = RestOperationContext.Current.IncomingRequest.Url;
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = requestUri.Scheme;
            uriBuilder.Host = requestUri.Host;
            uriBuilder.Port = requestUri.Port;
            uriBuilder.Path = RestOperationContext.Current.ServiceEndpoint.Description.ListenUri.AbsolutePath;
            if (!uriBuilder.Path.EndsWith("/"))
            {
                uriBuilder.Path += "/";
            }

            uriBuilder.Path += String.Join("/", parts);
            return uriBuilder.ToString();
        }

        /// <summary>
        /// Perform an ACL check
        /// </summary>
        private void AclCheck(Object handler, String action)
        {
            foreach (var dmn in this.GetDemands(handler, action))
            {
                this.m_pepService.Demand(dmn);
            }
        }

        /// <summary>
        /// Get demands
        /// </summary>
        private String[] GetDemands(object handler, string action)
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

        /// <inheritdoc/>
        public void Ping()
        {
            this.ThrowIfNotReady();
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.NoContent;
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData Create(string resourceType, IdentifiedData body)
        {
            this.ThrowIfNotReady();

            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            var audit = this.m_auditService.Audit()
                .WithAction(Core.Model.Audit.ActionType.Create)
                .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Import)
               .WithEventType("CREATE", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Create")
                .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithLocalDestination()
                .WithPrincipal()
                .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IApiResourceHandler.Create));
                    var retVal = handler.Create(body, false) as IdentifiedData;
                    var versioned = retVal as IVersionedData;

                    if (retVal == null)
                    {
                        return null;
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                        if (versioned != null)
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, retVal.Key, "_history", versioned.VersionKey));
                        }
                        else
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, retVal.Key));
                        }

                        audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                            .WithObjects(retVal.BatchOperation == Core.Model.DataTypes.BatchOperationType.Update ? Core.Model.Audit.AuditableObjectLifecycle.Amendment : Core.Model.Audit.AuditableObjectLifecycle.Creation, retVal);

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
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw new Exception($"Error creating {body}", e);
            }
            finally
            {
                audit.WithTimestamp().Send();

            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData CreateUpdate(string resourceType, string id, IdentifiedData body)
        {
            this.ThrowIfNotReady();

            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            var audit = this.m_auditService.Audit()
                .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Import)
               .WithEventType("CREATE_UPDATE", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Create or Update")
                .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
                .WithLocalDestination()
                .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            this.ThrowIfNotReady();
            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    IdentifiedData retVal = null;
                    IVersionedData versioned = null;
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());

                    if (!string.IsNullOrEmpty(id) && handler is IChainedApiResourceHandler chainedHandler &&
                        chainedHandler.TryGetChainedResource(id, ChildObjectScopeBinding.Class, out var childHandler))
                    {
                        this.AclCheck(childHandler, nameof(IApiResourceHandler.Create));
                        retVal = childHandler.Add(handler.Type, null, body) as IdentifiedData;
                    }
                    else
                    {
                        this.AclCheck(handler, nameof(IApiResourceHandler.Create));
                        retVal = handler.Create(body, true) as IdentifiedData;
                        versioned = retVal as IVersionedData;
                    }

                    if (retVal == null)
                    {
                        return null;
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);

                        if (versioned != null)
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned.VersionKey));
                        }
                        else
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));
                        }

                        audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                            .WithAction(retVal.BatchOperation == Core.Model.DataTypes.BatchOperationType.Update ? Core.Model.Audit.ActionType.Update : Core.Model.Audit.ActionType.Create)
                            .WithObjects(retVal.BatchOperation == Core.Model.DataTypes.BatchOperationType.Update ? Core.Model.Audit.AuditableObjectLifecycle.Amendment : Core.Model.Audit.AuditableObjectLifecycle.Creation, retVal)
                            .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retVal);


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
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw new Exception($"Error creating/updating {body}", e);
            }
            finally
            {
                audit.WithTimestamp().Send();

            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpBundleRelatedParameterName, typeof(bool), "True if the server should send related objects to the caller in a bundle")]
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData Get(string resourceType, string id)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
               .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Export)
               .WithAction(Core.Model.Audit.ActionType.Read)
               .WithEventType("READ", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Read (Current Version)")
               .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
               .WithLocalDestination()
               .WithQueryPerformed($"{resourceType}/{id}")
               .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    if (handler is IChainedApiResourceHandler chainedHandler && chainedHandler.TryGetChainedResource(id, ChildObjectScopeBinding.Class, out IApiChildResourceHandler childHandler))
                    {
                        return this.AssociationSearch(resourceType, id) as IdentifiedData;
                    }

                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));

                    Guid objectId = Guid.Parse(id);
                    this.ThrowIfPreConditionFails(handler, objectId);

                    IdentifiedData retVal = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = handler.Get(objectId, Guid.Empty) as IdentifiedData;
                    }

                    if (retVal == null)
                    {
                        throw new FileNotFoundException(id);
                    }

                    RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(retVal.ModifiedOn.DateTime);

                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success);
                    if (!RestOperationContext.Current.IncomingRequest.HttpMethod.Equals("head", StringComparison.OrdinalIgnoreCase))
                    {
                        audit = audit.WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retVal);
                    }

                    // Did the client ask us to throw on a privacy violation
                    if(retVal.GetAnnotations<PrivacyMaskingAnnotation>().Any(r => r.ActionTaken != Core.Security.Configuration.ResourceDataPolicyActionType.None) &&
                        Enum.TryParse<ResourceDataPolicyActionType>(RestOperationContext.Current.IncomingRequest.Headers[ExtendedHttpHeaderNames.ThrowOnPrivacyViolation], out var actionsForThrow))
                    {
                        foreach (var itm in retVal.GetAnnotations<PrivacyMaskingAnnotation>()) {
                            if (actionsForThrow.HasFlag(itm.ActionTaken))
                            {
                                throw new PolicyViolationException(AuthenticationContext.Current.Principal, itm.MaskingReason);
                            }
                        }
                    }

                    if ((Boolean.TryParse(RestOperationContext.Current.IncomingRequest.QueryString[QueryControlParameterNames.HttpBundleRelatedParameterName], out var bundle)
                        || Boolean.TryParse(RestOperationContext.Current.IncomingRequest.Headers[ExtendedHttpHeaderNames.IncludeRelatedObjectsHeader], out bundle) && bundle))
                    {
                        return Bundle.CreateBundle(retVal);
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
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error getting {resourceType}/{id}", e);
            }
            finally
            {
                if (!this.m_nonDisclosureAuditableResourceTypes.Contains(resourceType))
                {
                    audit.WithTimestamp().Send();
                }
            }
        }

        /// <inheritdoc/>
        private string ExtractValidateMatchHeader(Type resourceType, String headerString)
        {
            if (!headerString.Contains("."))
            {
                return headerString;
            }
            else
            {
                if (headerString.StartsWith("W/")) // weak references - but we'll still use it
                {
                    headerString = headerString.Substring(2);
                }
                var headerParts = headerString.Split('.');
                if (resourceType.IsAssignableFrom(this.m_modelSerlizationBinder.BindToType(null, headerParts[0])))
                {
                    return headerParts[1];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <inheritdoc/>
        /// <exception cref="PreconditionFailedException">When the HTTP header pre-conditions fail</exception>
        private void ThrowIfPreConditionFails(IApiResourceHandler handler, Guid objectId)
        {

            var ifModifiedHeader = RestOperationContext.Current.IncomingRequest.GetIfModifiedSince();
            var ifUnmodifiedHeader = RestOperationContext.Current.IncomingRequest.GetIfUnmodifiedSince();
            var ifNoneMatchHeader = RestOperationContext.Current.IncomingRequest.GetIfNoneMatch()?.Select(o => this.ExtractValidateMatchHeader(handler.Type, o)).Where(o => !String.IsNullOrEmpty(o));
            var ifMatchHeader = RestOperationContext.Current.IncomingRequest.GetIfMatch()?.Select(o => this.ExtractValidateMatchHeader(handler.Type, o)).Where(o => !String.IsNullOrEmpty(o));

            // HTTP IF headers? - before we go to the DB lets check the cache for them
            if (ifNoneMatchHeader?.Any() == true || ifMatchHeader?.Any() == true || ifModifiedHeader.HasValue || ifUnmodifiedHeader.HasValue)
            {
                var cacheResult = this.m_dataCachingService.GetCacheItem(objectId);

                if (cacheResult != null)
                {
                    var cacheHeader = this.ExtractValidateMatchHeader(cacheResult.GetType(), cacheResult.Tag);

                    if (cacheResult is ITaggable tagged && tagged.GetTag(SystemTagNames.DcdrRefetchTag) != null)
                    {
                        tagged.RemoveTag(SystemTagNames.DcdrRefetchTag);
                    }
                    else if (ifNoneMatchHeader?.Contains(cacheHeader) == true ||
                        ifMatchHeader?.Contains(cacheHeader) != true ||
                        cacheResult.ModifiedOn <= ifModifiedHeader.GetValueOrDefault() ||
                        ifUnmodifiedHeader.GetValueOrDefault() >= cacheResult.ModifiedOn)
                    {
                        throw new PreconditionFailedException();
                    }
                }
                else
                {
                    // Load from cache so that future calls will work with the cache
                    using (DataPersistenceControlContext.Create(LoadMode.QuickLoad))
                    {
                        IdentifiedData rawObject = null;
                        if (handler is IApiResourceHandlerRepository iarhr)
                        {
                            rawObject = iarhr.Repository.Get(objectId) as IdentifiedData;
                        }
                        else
                        {
                            rawObject = handler.Get(objectId, null) as IdentifiedData;
                        }

                        if (rawObject == null)
                        {
                            throw new KeyNotFoundException();
                        }
                        else if (ifNoneMatchHeader?.Contains(rawObject.Tag) == true)
                        {
                            throw new PreconditionFailedException(); // if-none-match contains the tag
                        }
                        else if (ifMatchHeader?.Contains(rawObject.Tag) != true)
                        {
                            throw new PreconditionFailedException(); // if-match does not contain the tag
                        }
                        else if (rawObject.ModifiedOn <= ifModifiedHeader.GetValueOrDefault() ||
                            ifUnmodifiedHeader.GetValueOrDefault() >= rawObject.ModifiedOn)
                        {
                            throw new PreconditionFailedException(); // modification
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData GetVersion(string resourceType, string id, string versionId)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
               .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Export)
               .WithAction(Core.Model.Audit.ActionType.Read)
               .WithQueryPerformed($"{resourceType}/{id}/_history/{versionId}")
                .WithEventType("VREAD", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Version Read")
               .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
               .WithLocalDestination()
               .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));

                    var retVal = handler.Get(Guid.Parse(id), Guid.Parse(versionId)) as IdentifiedData;
                    if (retVal == null)
                    {
                        throw new FileNotFoundException(id);
                    }

                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                        .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retVal);

                    if (Boolean.TryParse(RestOperationContext.Current.IncomingRequest.QueryString[QueryControlParameterNames.HttpBundleRelatedParameterName], out var bundle) && bundle)
                    {
                        return Bundle.CreateBundle(retVal);
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
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
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw new Exception($"Error getting version {resourceType}/{id}/history/{versionId}", e);
            }
            finally
            {
                audit.WithTimestamp().Send();

            }
        }

        /// <inheritdoc/>
        public XmlSchema GetSchema()
        {
            this.ThrowIfNotReady();
            try
            {
                XmlSchemas schemaCollection = new XmlSchemas();

                XmlReflectionImporter importer = new XmlReflectionImporter("http://santedb.org/model");
                XmlSchemaExporter exporter = new XmlSchemaExporter(schemaCollection);

                foreach (var cls in this.m_resourceHandlerTool.Handlers.Where(o => o.Scope == typeof(IHdsiServiceContract)).Select(o => o.Type))
                {
                    exporter.ExportTypeMapping(importer.ImportTypeMapping(cls, "http://santedb.org/model"));
                }

                _ = Int32.TryParse(RestOperationContext.Current.IncomingRequest.QueryString["id"], out var schemaId);

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

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpSinceParameterName, typeof(Guid), "The last version of the object that should be returned")]
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData History(string resourceType, string id)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
               .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Query)
               .WithAction(Core.Model.Audit.ActionType.Execute)
               .WithQueryPerformed($"{resourceType}/{id}/_history")
               .WithEventType("HISTORY", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "History Read")
               .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
               .WithLocalDestination()
               .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var handler = this.GetResourceHandler(resourceType);

                if (handler != null)
                {

                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());

                    String since = RestOperationContext.Current.IncomingRequest.QueryString[QueryControlParameterNames.HttpSinceParameterName];
                    Guid sinceGuid = since != null ? Guid.Parse(since) : Guid.Empty;
                    var objectId = Guid.Parse(id);
                    this.ThrowIfPreConditionFails(handler, objectId);

                    // Query
                    this.AclCheck(handler, nameof(IApiResourceHandler.Get));
                    var retVal = handler.Get(objectId, Guid.Empty) as IVersionedData;
                    List<IVersionedData> histItm = new List<IVersionedData>() { retVal };
                    while (retVal.PreviousVersionKey.HasValue)
                    {
                        retVal = handler.Get(Guid.Parse(id), retVal.PreviousVersionKey.Value) as IVersionedData;
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
                    var retBundle = new Bundle(histItm.OfType<IdentifiedData>(), 0, histItm.Count);
                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                        .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retBundle);
                    return retBundle;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw new Exception($"Error getting history for {resourceType}/{id}");
            }
            finally
            {
                audit.WithTimestamp().Send();

            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpOffsetParameterName, typeof(int), "The offet of the first result to return")]
        [UrlParameter(QueryControlParameterNames.HttpCountParameterName, typeof(int), "The count of items to return in this result set")]
        [UrlParameter(QueryControlParameterNames.HttpIncludeTotalParameterName, typeof(bool), "True if the server should count the matching results. May reduce performance")]
        [UrlParameter(QueryControlParameterNames.HttpOrderByParameterName, typeof(string), "Instructs the result set to be ordered (in format: property:(asc|desc))")]
        [UrlParameter(QueryControlParameterNames.HttpQueryStateParameterName, typeof(Guid), "The query state identifier. This allows the client to get a stable result set.")]
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData Search(string resourceType)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
               .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Query)
               .WithAction(Core.Model.Audit.ActionType.Execute)
               .WithQueryPerformed($"{resourceType}?{RestOperationContext.Current.IncomingRequest.QueryString.ToHttpString()}")
               .WithEventType("SEARCH", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Search")
               .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
               .WithLocalDestination()
               .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());


            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IApiResourceHandler.Query));

                    // Send the query to the resource handler
                    var query = RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString();

                    // Modified on?
                    if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null)
                    {
                        query.Add("$self", $":(lastModified)>{RestOperationContext.Current.IncomingRequest.GetIfModifiedSince()?.ToString("o")}");
                    }

                    // Query for results
                    var results = handler.Query(query) as IOrderableQueryResultSet;

                    // Now apply controls
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        var retVal = results.ApplyResultInstructions(query, out int offset, out int totalCount).OfType<IdentifiedData>().ToArray();

                        // Last modified object
                        if (this.m_configuration?.IncludeMetadataHeadersOnSearch == true)
                        {
                            var modifiedOnSelector = QueryExpressionParser.BuildPropertySelector(handler.Type, "modifiedOn", convertReturn: typeof(object));
                            var lastModified = (DateTime)results.OrderByDescending(modifiedOnSelector).Select<DateTimeOffset>(modifiedOnSelector).AsResultSet().FirstOrDefault().DateTime;

                            if (lastModified != default(DateTime))
                            {
                                RestOperationContext.Current.OutgoingResponse.SetLastModified(lastModified);
                            }
                        }

                        audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success);
                        // Last modification time and not modified conditions
                        if ((RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null ||
                            RestOperationContext.Current.IncomingRequest.GetIfNoneMatch() != null) &&
                            !retVal.Any())
                        {
                            RestOperationContext.Current.OutgoingResponse.StatusCode = 304;
                            return null;
                        }
                        else
                        {

                            if (RestOperationContext.Current.IncomingRequest.HttpMethod.Equals("head", StringComparison.OrdinalIgnoreCase))
                            {
                                return null;
                            }
                            else
                            {
                                Bundle retBundle = null;
                                if (XmlConvert.ToBoolean(RestOperationContext.Current.IncomingRequest.Headers[ExtendedHttpHeaderNames.IncludeRelatedObjectsHeader] ?? "false"))
                                {
                                    // TODO: Find a more efficient way of doing this
                                    retBundle = Bundle.CreateBundle(retVal, totalCount, offset);
                                }
                                else if (RestOperationContext.Current.IncomingRequest.QueryString[QueryControlParameterNames.HttpIncludePathParameterName] != null ||
                                    RestOperationContext.Current.IncomingRequest.QueryString[QueryControlParameterNames.HttpExcludePathParameterName] != null)
                                {
                                    var includeProperties = RestOperationContext.Current.IncomingRequest.QueryString.GetValues(QueryControlParameterNames.HttpIncludePathParameterName)?.Select(o => this.ResolvePropertyInfo(handler.Type, o)).ToArray();
                                    var excludeProperties = RestOperationContext.Current.IncomingRequest.QueryString.GetValues(QueryControlParameterNames.HttpExcludePathParameterName)?.Select(o => this.ResolvePropertyInfo(handler.Type, o)).ToArray();
                                    retBundle = Bundle.CreateBundle(retVal, totalCount, offset, includeProperties, excludeProperties);
                                }
                                else
                                {
                                    retBundle = new Bundle(retVal, offset, totalCount);
                                }

                                audit = audit.WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retBundle);
                                return retBundle;
                            }
                        }
                    }
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw new Exception($"Error searching {resourceType}", e);
            }
            finally
            {
                if (!this.m_nonDisclosureAuditableResourceTypes.Contains(resourceType))
                {
                    audit.WithTimestamp().Send();
                }
            }
        }

        /// <summary>
        /// Resolve property info
        /// </summary>
        private PropertyInfo ResolvePropertyInfo(Type rootType, string includePath)
        {
            PropertyInfo retVal = null;
            foreach (var propertyPart in includePath.Split('.'))
            {
                retVal = rootType.GetQueryProperty(propertyPart, dropXmlSuffix: false);
                if (retVal == null)
                {
                    throw new MissingMemberException($"{rootType.Name}.{propertyPart}");
                }
                rootType = retVal.PropertyType.StripGeneric();
            }
            return retVal;
        }

        /// <inheritdoc/>
        public DateTime Time()
        {
            this.ThrowIfNotReady();
            return DateTime.Now;
        }

        /// <summary>
        /// Update the specified resource
        /// </summary>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData Update(string resourceType, string id, IdentifiedData body)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
              .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Import)
              .WithAction(Core.Model.Audit.ActionType.Update)
              .WithEventType("UPDATE", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Update")
              .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
              .WithLocalDestination()
              .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IApiResourceHandler.Update));

                    var objectId = Guid.Parse(id);

                    this.ThrowIfPreConditionFails(handler, objectId);


                    IdentifiedData retVal = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = handler.Update(body) as IdentifiedData;
                    }


                    if (retVal == null)
                    {
                        return null;
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);

                        if (retVal is IVersionedData versioned)
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned.VersionKey));
                        }
                        else
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));
                        }

                        audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                            .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Amendment, retVal)
                            .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retVal);

                        return retVal;
                    }
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw new Exception($"Error updating {resourceType}/{id}", e);
            }
            finally
            {
                audit.WithTimestamp().Send();

            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData Delete(string resourceType, string id)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
              .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Import)
              .WithAction(Core.Model.Audit.ActionType.Delete)
              .WithEventType("DELETE", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Delete")
                .WithPrincipal()
              .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
              .WithLocalDestination()
              .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IApiResourceHandler.Delete));

                    var objectId = Guid.Parse(id);
                    this.ThrowIfPreConditionFails(handler, objectId);

                    IdentifiedData retVal = null;
                    // Adhere to delete mode
                    if (RestOperationContext.Current.IncomingRequest.Headers.TryGetValue(ExtendedHttpHeaderNames.DeleteModeHeaderName, out var deleteModeHeader) &&
                        Enum.TryParse<DeleteMode>(deleteModeHeader[0], out var deleteMode))
                    {
                        using (DataPersistenceControlContext.Create(LoadMode.SyncLoad, deleteMode, false))
                        {
                            retVal = handler.Delete(objectId) as IdentifiedData;
                        }
                    }
                    else
                    {
                        retVal = handler.Delete(objectId) as IdentifiedData;
                    }

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    if (retVal is IVersionedData versioned)
                    {
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned.VersionKey));
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));
                    }

                    if (retVal is BaseEntityData be && be.ObsoletionTime.HasValue)
                    {
                        audit = audit.WithObjects(Core.Model.Audit.AuditableObjectLifecycle.LogicalDeletion, retVal);
                    }
                    else
                    {
                        audit = audit.WithObjects(Core.Model.Audit.AuditableObjectLifecycle.PermanentErasure, retVal);
                    }
                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success);

                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw new Exception($"Error deleting {resourceType}/{id}", e);
            }
            finally
            {
                audit.WithTimestamp().Send();

            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public virtual void PatchAll(PatchCollection body)
        {
            this.ThrowIfNotReady();

            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            var audit = this.m_auditService.Audit()
             .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Import)
             .WithAction(Core.Model.Audit.ActionType.Update)
             .WithEventType("PATCH", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "PatchAll")
                .WithPrincipal()
             .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
             .WithLocalDestination()
             .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var force = Convert.ToBoolean(RestOperationContext.Current.IncomingRequest.Headers[ExtendedHttpHeaderNames.ForceApplyPatchHeaderName] ?? "false");

                using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                {
                    var perssitenceBundle = new Bundle(body.Patches.Select(o =>
                    {
                        var handler = this.GetResourceHandler(o.AppliesTo.TypeXml);
                        if (handler == null)
                        {
                            throw new KeyNotFoundException(o.AppliesTo.TypeXml);
                        }
                        audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                        var existing = handler.Get(o.AppliesTo.Key, null) as IdentifiedData;
                        if (existing == null)
                        {
                            throw new FileNotFoundException($"/{o.AppliesTo.TypeXml}/{o.AppliesTo.Key}");
                        }
                        else
                        {
                            var applied = this.m_patchService.Patch(o, existing, force);
                            applied.BatchOperation = BatchOperationType.Update;
                            return applied;
                        }
                    }));

                    var bundleHandler = this.GetResourceHandler(typeof(Bundle).GetSerializationName());
                    audit.WithSensitivity(bundleHandler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(bundleHandler, nameof(IApiResourceHandler.Update));
                    bundleHandler.Update(perssitenceBundle);
                }
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success);
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (PatchAssertionException e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceWarning(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw new Exception($"Assertion failed while patching", e);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw new Exception($"Error patching", e);
            }
            finally
            {
                audit.WithTimestamp().Send();
            }
        }

        /// <inheritdoc/>
        public virtual void Patch(string resourceType, string id, Patch body)
        {
            this.ThrowIfNotReady();

            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            var audit = this.m_auditService.Audit()
             .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Import)
             .WithAction(Core.Model.Audit.ActionType.Update)
             .WithEventType("PATCH", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Patch")
                .WithPrincipal()
             .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
             .WithLocalDestination()
             .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {

                // First we load
                var handler = this.GetResourceHandler(resourceType);

                if (handler == null)
                {
                    throw new FileNotFoundException(resourceType);
                }

                // Next we get the current version
                audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                this.AclCheck(handler, nameof(IApiResourceHandler.Get));

                var objectId = Guid.Parse(id);
                var force = Convert.ToBoolean(RestOperationContext.Current.IncomingRequest.Headers[ExtendedHttpHeaderNames.ForceApplyPatchHeaderName] ?? "false");

                if (!force)
                {
                    this.ThrowIfPreConditionFails(handler, objectId);
                }

                using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                {
                    var existing = handler.Get(objectId, Guid.Empty) as IdentifiedData;

                    audit = audit.WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Amendment, existing);
                    if (existing == null)
                    {
                        throw new FileNotFoundException($"/{resourceType}/{id}");
                    }
                    else
                    {
                        // Force load all properties for existing
                        var applied = this.m_patchService.Patch(body, existing, force);
                        this.AclCheck(handler, nameof(IApiResourceHandler.Update));
                        var data = handler.Update(applied) as IdentifiedData;
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 204;
                        RestOperationContext.Current.OutgoingResponse.SetETag(data.Tag);
                        RestOperationContext.Current.OutgoingResponse.SetLastModified(applied.ModifiedOn.DateTime);
                        var versioned = (data as IVersionedData)?.VersionKey;
                        if (versioned != null)
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned));
                        }
                        else
                        {
                            RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));
                        }
                    }
                }
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success);
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (PatchAssertionException e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceWarning(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw new Exception($"Assertion failed while patching {resourceType}/{id}", e);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw new Exception($"Error patching {resourceType}/{id}", e);
            }
            finally
            {
                audit.WithTimestamp().Send();

            }
        }

        /// <inheritdoc/>
        public virtual ServiceOptions Options()
        {
            this.ThrowIfNotReady();
            try
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = 200;
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Allow", $"GET, PUT, POST, OPTIONS, HEAD, DELETE{(this.m_patchService != null ? ", PATCH" : null)}");
                if (this.m_patchService != null)
                {
                    RestOperationContext.Current.OutgoingResponse.Headers.Add("Accept-Patch", $"application/xml+sdb-patch, {SanteDBExtendedMimeTypes.XmlPatch}, {SanteDBExtendedMimeTypes.JsonPatch}");
                }

                // Service options
                var retVal = new ServiceOptions()
                {
                    InterfaceVersion = typeof(HdsiServiceBehavior).Assembly.GetName().Version.ToString(),
                    ServerVersion = $"{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product} v{Assembly.GetEntryAssembly().GetName().Version} ({Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion})",
                    Resources = new List<ServiceResourceOptions>()
                };

                // Get the resources which are supported
                foreach (var itm in this.m_resourceHandlerTool.Handlers)
                {
                    var svc = this.ResourceOptions(itm.ResourceName);
                    retVal.Resources.Add(svc);
                }

                return retVal;
            }
            catch (PreconditionFailedException) { throw; }
            catch (FaultException) { throw; }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception("Error gathering service options", e);
            }
        }

        /// <summary>
        /// Throw if the service is not ready
        /// </summary>
        /// <exception cref="DomainStateException">Thrown if the application context is in a half-started state</exception>
        protected void ThrowIfNotReady()
        {
            if (!ApplicationServiceContext.Current.IsRunning)
            {
                throw new DomainStateException();
            }
        }

        /// <inheritdoc/>
        public virtual ServiceResourceOptions ResourceOptions(string resourceType)
        {
            var handler = this.GetResourceHandler(resourceType);
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
                if (this.m_patchService != null &&
                    handler.Capabilities.HasFlag(ResourceCapabilityType.Update))
                {
                    caps.Add(new ServiceResourceCapability(ResourceCapabilityType.Patch, this.GetDemands(handler, nameof(IApiResourceHandler.Update))));
                }

                // To expose associated objects
                var childResources = new List<ChildServiceResourceOptions>();
                if (handler is IChainedApiResourceHandler associative)
                {
                    childResources.AddRange(associative.ChildResources.Select(r => new ChildServiceResourceOptions(r.Name, r.PropertyType, r.Capabilities.ToResourceCapabilityStatement(getCaps).ToList(), r.ScopeBinding, ChildObjectClassification.Resource)));
                }
                if (handler is IOperationalApiResourceHandler operational)
                {
                    childResources.AddRange(operational.Operations.Select(o => new ChildServiceResourceOptions(o.Name, typeof(Object), ResourceCapabilityType.Create.ToResourceCapabilityStatement(getCaps).ToList(), o.ScopeBinding, ChildObjectClassification.RpcOperation)));
                }
                // Associateive
                return new ServiceResourceOptions(resourceType, handler.Type, caps, childResources);
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpOffsetParameterName, typeof(int), "The offet of the first result to return")]
        [UrlParameter(QueryControlParameterNames.HttpCountParameterName, typeof(int), "The count of items to return in this result set")]
        [UrlParameter(QueryControlParameterNames.HttpIncludeTotalParameterName, typeof(bool), "True if the server should count the matching results. May reduce performance")]
        [UrlParameter(QueryControlParameterNames.HttpOrderByParameterName, typeof(string), "Instructs the result set to be ordered (in format: property:(asc|desc))")]
        [UrlParameter(QueryControlParameterNames.HttpQueryStateParameterName, typeof(Guid), "The query state identifier. This allows the client to get a stable result set.")]
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual Object AssociationSearch(string resourceType, string key, string childResourceType)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
             .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Query)
             .WithAction(Core.Model.Audit.ActionType.Execute)
             .WithEventType("ASSOC_SEARCH", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Association Search")
             .WithQueryPerformed($"{resourceType}/{key}/{childResourceType}?{RestOperationContext.Current.IncomingRequest.QueryString.ToHttpString()}")
                .WithPrincipal()
             .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
             .WithLocalDestination()
             .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                if (!Guid.TryParse(key, out Guid keyGuid))
                {
                    throw new ArgumentException(nameof(key));
                }

                var handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IApiResourceHandler.Query));

                    // Send the query to the resource handler
                    var query = RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString();

                    // Modified on?
                    if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null)
                    {
                        query.Add("$self", $":(lastModified)>{RestOperationContext.Current.IncomingRequest.GetIfModifiedSince()?.ToString("o")}");
                    }

                    // Query for results
                    var results = handler.QueryChildObjects(keyGuid, childResourceType, query);

                    // Now apply controls
                    var retVal = results.ApplyResultInstructions(query, out int offset, out int totalCount).OfType<IdentifiedData>();

                    if (this.m_configuration?.IncludeMetadataHeadersOnSearch == true)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetLastModified((retVal.OrderByDescending(o => o.ModifiedOn).FirstOrDefault()?.ModifiedOn.DateTime ?? DateTime.Now));
                    }

                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success);
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
                        using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                        {
                            var retBundle = new Bundle(retVal, offset, totalCount);
                            audit = audit.WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retBundle);
                            return retBundle;
                        }
                    }
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            finally
            {
                if (!this.m_nonDisclosureAuditableResourceTypes.Contains(resourceType))
                {
                    audit.WithTimestamp().Send();
                }
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual object AssociationCreate(string resourceType, string key, string childResourceType, object body)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
                 .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Import)
                 .WithAction(Core.Model.Audit.ActionType.Create)
                 .WithEventType("ASSOC_CREATE", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Association Create")
                 .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
                 .WithLocalDestination()
                 .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.AddChildObject));
                    var objectId = Guid.Parse(key);
                    this.ThrowIfPreConditionFails(handler, objectId);

                    IdentifiedData retVal = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = handler.AddChildObject(objectId, childResourceType, body) as IdentifiedData;
                    }

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    if (retVal != null)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, key, childResourceType, retVal.Key));
                    }

                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                        .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Amendment, retVal)
                        .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retVal);

                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            finally
            {
                audit.WithTimestamp().Send();
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual object AssociationRemove(string resourceType, string key, string childResourceType, string scopedEntityKey)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
             .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Import)
             .WithAction(Core.Model.Audit.ActionType.Delete)
             .WithEventType("ASSOC_DELETE", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Association Delete")
             .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
             .WithLocalDestination()
             .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());


            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.RemoveChildObject));

                    var objectId = Guid.Parse(key);
                    this.ThrowIfPreConditionFails(handler, objectId);
                    IdentifiedData retVal = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = handler.RemoveChildObject(objectId, childResourceType, Guid.Parse(scopedEntityKey)) as IdentifiedData;
                    }

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, key, childResourceType, retVal.Key));
                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success);
                    if (retVal is BaseEntityData be && be.ObsoletionTime.HasValue)
                    {
                        audit = audit.WithObjects(Core.Model.Audit.AuditableObjectLifecycle.LogicalDeletion, retVal);
                    }
                    else
                    {
                        audit = audit.WithObjects(Core.Model.Audit.AuditableObjectLifecycle.PermanentErasure, retVal);
                    }

                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            finally
            {
                audit.WithTimestamp().Send();
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpBundleRelatedParameterName, typeof(bool), "True if the server should include related objects in a bundle to the caller")]
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual object AssociationGet(string resourceType, string key, string childResourceType, string scopedEntityKey)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
             .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Query)
             .WithAction(Core.Model.Audit.ActionType.Read)
             .WithEventType("ASSOC_READ", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Association Read")
             .WithQueryPerformed($"{resourceType}/{key}/{childResourceType}/{scopedEntityKey}?{RestOperationContext.Current.IncomingRequest.QueryString.ToHttpString()}")
             .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
             .WithLocalDestination()
             .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.GetChildObject));

                    var objectId = Guid.Parse(scopedEntityKey);
                    this.ThrowIfPreConditionFails(handler, objectId);

                    object retVal = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = handler.GetChildObject(Guid.Parse(key), childResourceType, objectId);
                    }

                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                        .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retVal);
                    if (retVal is IdentifiedData idData)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(idData.Tag);
                        RestOperationContext.Current.OutgoingResponse.SetLastModified(idData.ModifiedOn.DateTime);

                        if (Boolean.TryParse(RestOperationContext.Current.IncomingRequest.Headers[QueryControlParameterNames.HttpBundleRelatedParameterName], out var bundle) && bundle)
                        {
                            return Bundle.CreateBundle(idData);
                        }
                        else
                        {
                            return retVal;
                        }
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
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            finally
            {
                audit.WithTimestamp().Send();
            }
        }

        /// <inheritdoc/>
        [UrlParameter("_format", typeof(String), "The format of the barcode (santedb-vrp, code39, etc.)")]
        public virtual Stream GetBarcode(string resourceType, string id)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
             .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Query)
             .WithAction(Core.Model.Audit.ActionType.Execute)
             .WithEventType("BARCODE_GEN", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Generate Barcode")
             .WithQueryPerformed($"{resourceType}/{id}?{RestOperationContext.Current.IncomingRequest.QueryString.ToHttpString()}")
             .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
             .WithLocalDestination()
             .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    if (this.m_barcodeService == null)
                    {
                        throw new InvalidOperationException("Cannot find barcode generator service");
                    }

                    Guid objectId = Guid.Parse(id);
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());

                    this.ThrowIfPreConditionFails(handler, objectId);

                    var data = handler.Get(objectId, Guid.Empty) as IHasIdentifiers;
                    if (data == null)
                    {
                        throw new KeyNotFoundException($"{resourceType} {id}");
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.ContentType = "image/png";

                        // Get the generator
                        IBarcodeGenerator barcodeGenerator = this.m_barcodeService.GetBarcodeGenerator(
                            RestOperationContext.Current.IncomingRequest.QueryString["_format"] ?? VrpBarcodeProvider.AlgorithmName);

                        if (barcodeGenerator == null)
                        {
                            throw new ArgumentException($"Barcode format unknown");
                        }
                        else
                        {
                            audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                                .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Access, data);
                            return barcodeGenerator.Generate(data, RestOperationContext.Current.IncomingRequest.QueryString.GetValues("_domain"));
                        }
                    }
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Error generating barcode for {0} - {1}", resourceType, e);
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw new Exception($"Could not generate visual code for {resourceType}/{id}", e);
            }
            finally
            {
                if (!this.m_nonDisclosureAuditableResourceTypes.Contains(resourceType))
                {
                    audit.WithTimestamp().Send();
                }
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData Touch(string resourceType, string id)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
             .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.ApplicationActivity)
             .WithAction(Core.Model.Audit.ActionType.Update)
             .WithEventType("TOUCH", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Touch Resource")
             .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
             .WithLocalDestination()
             .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler is IApiResourceHandlerEx exResourceHandler)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IApiResourceHandler.Update));

                    var objectId = Guid.Parse(id);
                    this.ThrowIfPreConditionFails(handler, objectId);
                    IdentifiedData retVal = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = exResourceHandler.Touch(objectId) as IdentifiedData;
                    }

                    var versioned = retVal as IVersionedData;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);

                    if (versioned != null)
                    {
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id, "_history", versioned.VersionKey));
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, id));
                    }

                    audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                        .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Amendment, retVal)
                        .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retVal);

                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException) { throw; }
            catch (FaultException) { throw; }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error updating {resourceType}/{id}", e);
            }
        }

        /// <inheritdoc/>
        public virtual void ResolvePointer(System.Collections.Specialized.NameValueCollection parms)
        {
            this.ThrowIfNotReady();

            try
            {
                if (this.m_resourcePointerService == null)
                {
                    throw new InvalidOperationException("Cannot find pointer service");
                }

                bool validate = true;
                var code = parms["code"];
                if (String.IsNullOrEmpty(code))
                {
                    throw new ArgumentException("SEARCH have url-form encoded payload with parameter code");
                }
                else if (!String.IsNullOrEmpty(parms["validate"]))
                {
                    Boolean.TryParse(parms["validate"], out validate);
                }

                if (code.Contains("://"))
                {
                    code = code.Substring(code.IndexOf("://") + 3);
                }
                var result = this.m_resourcePointerService.ResolveResource(code, validate);

                // Create a 303 see other
                if (result != null)
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.SeeOther;
                    RestOperationContext.Current.OutgoingResponse.AddHeader("Location", this.CreateContentLocation(result.GetType().GetSerializationName(), result.Key.Value));
                }
                else
                {
                    throw new KeyNotFoundException($"Object not found");
                }
            }
            catch (PreconditionFailedException) { throw; }
            catch (FaultException) { throw; }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw new Exception($"Error searching by pointer", e);
            }
        }

        /// <inheritdoc/>
        public virtual Stream GetVrpPointerData(string resourceType, string id)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
                .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Query)
                .WithAction(Core.Model.Audit.ActionType.Execute)
                .WithEventType("VRP_GEN", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Generate VRP Data")
                .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
                .WithLocalDestination()
                .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var handler = this.GetResourceHandler(resourceType);
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    if (this.m_resourcePointerService == null)
                    {
                        throw new InvalidOperationException("Cannot find resource pointer service");
                    }

                    Guid objectId = Guid.Parse(id);
                    this.ThrowIfPreConditionFails(handler, objectId);

                    var data = handler.Get(objectId, Guid.Empty) as IHasIdentifiers;
                    if (data == null)
                    {
                        throw new KeyNotFoundException($"{resourceType} {id}");
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.ContentType = "application/jose";

                        audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                            .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, data);

                        return new MemoryStream(Encoding.UTF8.GetBytes(this.m_resourcePointerService.GeneratePointer(data)));
                    }
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Error fetching pointer for {0} - {1}", resourceType, e);
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw new Exception($"Could fetching pointer code for {resourceType}/{id}", e);
            }
            finally
            {
                audit.WithTimestamp().Send();
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual IdentifiedData Copy(String reosurceType, String id)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual object InvokeMethod(string resourceType, string id, string operationName, ParameterCollection body)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
                .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.ApplicationActivity)
                .WithAction(Core.Model.Audit.ActionType.Execute)
                .WithQueryPerformed($"{resourceType}/{id}/${operationName}")
                .WithEventType("INSTANCE_INVOKE", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Invoke REST procedure on instance of resource")
                .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
                .WithLocalDestination()
                .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var handler = this.GetResourceHandler(resourceType) as IOperationalApiResourceHandler;
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IOperationalApiResourceHandler.InvokeOperation));

                    var objectId = Guid.Parse(id);
                    this.ThrowIfPreConditionFails(handler, objectId);
                    object retValRaw = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retValRaw = handler.InvokeOperation(objectId, operationName, body);
                    }

                    if (retValRaw is IdentifiedData retVal)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                        RestOperationContext.Current.OutgoingResponse.SetLastModified(retVal.ModifiedOn.DateTime);

                    }

                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                        .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retValRaw);
                    return retValRaw;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            finally
            {
                audit.WithTimestamp().Send();
            }
        }

        /// <inheritdoc/>
        public virtual object CheckIn(string resourceType, string key)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
                .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.ApplicationActivity)
                .WithAction(Core.Model.Audit.ActionType.Execute)
                .WithEventType("CHECKIN", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Release Object Edit Lock")
                .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
                .WithLocalDestination()
                .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var handler = this.GetResourceHandler(resourceType) as ICheckoutResourceHandler;
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(ICheckoutResourceHandler.CheckIn));
                    var objectId = Guid.Parse(key);
                    this.ThrowIfPreConditionFails(handler, objectId);
                    var retVal = handler.CheckIn(objectId);
                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                        .WithObjects(AuditableObjectLifecycle.Access, objectId)
                        .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.PermanentErasure, retVal);
                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            finally
            {
                audit.WithTimestamp().Send();
            }
        }

        /// <inheritdoc/>
        public virtual object CheckOut(string resourceType, string key)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
               .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.ApplicationActivity)
               .WithAction(Core.Model.Audit.ActionType.Execute)
               .WithEventType("CHECKOUT", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Obtain Object Edit Lock")
               .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithPrincipal()
               .WithLocalDestination()
               .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var handler = this.GetResourceHandler(resourceType) as ICheckoutResourceHandler;
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(ICheckoutResourceHandler.CheckIn));
                    var objectId = Guid.Parse(key);
                    this.ThrowIfPreConditionFails(handler, objectId);
                    var retVal = handler.CheckOut(objectId);
                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                        .WithObjects(AuditableObjectLifecycle.Access, objectId)
                        .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Creation, retVal);
                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            finally
            {
                audit.WithTimestamp().Send();
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual object InvokeMethod(string resourceType, string operationName, ParameterCollection body)
        {
            this.ThrowIfNotReady();


            var audit = this.m_auditService.Audit()
                .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.ApplicationActivity)
                .WithAction(Core.Model.Audit.ActionType.Execute)
                .WithQueryPerformed($"{resourceType}/${operationName}")
                .WithPrincipal()
                .WithEventType("CLASS_INVOKE", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Invoke REST operation on class of resource")
                .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithLocalDestination()
                .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());


            try
            {
                var handler = this.GetResourceHandler(resourceType) as IOperationalApiResourceHandler;
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IOperationalApiResourceHandler.InvokeOperation));

                    object retValRaw = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retValRaw = handler.InvokeOperation(null, operationName, body);
                    }

                    if (retValRaw is IdentifiedData retVal)
                    {
                        RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                        RestOperationContext.Current.OutgoingResponse.SetLastModified(retVal.ModifiedOn.DateTime);

                    }

                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                        .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retValRaw);
                    return retValRaw;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            finally
            {
                audit.WithTimestamp().Send();
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpBundleRelatedParameterName, typeof(bool), "True if the server should include related objects")]
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual object AssociationGet(string resourceType, string childResourceType, string childResourceKey)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
                .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Query)
                .WithAction(Core.Model.Audit.ActionType.Read)
                .WithQueryPerformed($"{resourceType}/{childResourceType}/{childResourceKey}")
                .WithPrincipal()
                .WithEventType("ASSOC_READ", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Association Read")
                .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithLocalDestination()
                .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.GetChildObject));
                    var objectId = Guid.Parse(childResourceKey);
                    this.ThrowIfPreConditionFails(handler, objectId);

                    IdentifiedData retVal = null;
                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = handler.GetChildObject(null, childResourceType, objectId) as IdentifiedData;
                    }

                    RestOperationContext.Current.OutgoingResponse.SetETag(retVal.Tag);
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(retVal.ModifiedOn.DateTime);

                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                        .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retVal);
                    if (Boolean.TryParse(RestOperationContext.Current.IncomingRequest.Headers[QueryControlParameterNames.HttpBundleRelatedParameterName], out var bundle) && bundle)
                    {
                        return Bundle.CreateBundle(retVal);
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
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            finally
            {
                audit.WithTimestamp().Send();
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual object AssociationRemove(string resourceType, string childResourceType, string childResourceKey)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
                .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Import)
                .WithAction(Core.Model.Audit.ActionType.Delete)
                .WithEventType("ASSOC_DELETE", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Association Delete")
                .WithPrincipal()
                .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithLocalDestination()
                .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                IChainedApiResourceHandler handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IChainedApiResourceHandler.RemoveChildObject));
                    var objectId = Guid.Parse(childResourceKey);
                    this.ThrowIfPreConditionFails(handler, objectId);
                    IdentifiedData retVal = null;

                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {
                        retVal = handler.RemoveChildObject(null, childResourceType, objectId) as IdentifiedData;
                    }

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, this.CreateContentLocation(resourceType, childResourceType, retVal.Key));

                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success)
                        .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.LogicalDeletion, retVal)
                        .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retVal);

                    return retVal;
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            finally
            {
                audit.WithTimestamp().Send();
            }
        }

        /// <inheritdoc/>
        [UrlParameter(QueryControlParameterNames.HttpOffsetParameterName, typeof(int), "The offet of the first result to return")]
        [UrlParameter(QueryControlParameterNames.HttpCountParameterName, typeof(int), "The count of items to return in this result set")]
        [UrlParameter(QueryControlParameterNames.HttpIncludeTotalParameterName, typeof(bool), "True if the server should count the matching results. May reduce performance")]
        [UrlParameter(QueryControlParameterNames.HttpOrderByParameterName, typeof(string), "Instructs the result set to be ordered (in format: property:(asc|desc))")]
        [UrlParameter(QueryControlParameterNames.HttpQueryStateParameterName, typeof(Guid), "The query state identifier. This allows the client to get a stable result set.")]
        [UrlParameter(QueryControlParameterNames.HttpViewModelParameterName, typeof(String), "When using the view model serializer - specifies the view model definition to use which will load properties and return them inline in the response")]
        public virtual Object AssociationSearch(string resourceType, string childResourceType)
        {
            this.ThrowIfNotReady();

            var audit = this.m_auditService.Audit()
               .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Import)
               .WithAction(Core.Model.Audit.ActionType.Execute)
               .WithEventType("ASSOC_SEARCH", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Association Search")
               .WithQueryPerformed($"{resourceType}/{childResourceType}?{RestOperationContext.Current.IncomingRequest.QueryString.ToHttpString()}")
                .WithPrincipal()
               .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
               .WithLocalDestination()
               .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IApiResourceHandler.Query));

                    // Send the query to the resource handler
                    var query = RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString();

                    // Modified on?
                    if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null)
                    {
                        query.Add("$self", $":(lastModified)>{RestOperationContext.Current.IncomingRequest.GetIfModifiedSince()?.ToString("o")}");
                    }

                    // Query for results
                    var results = handler.QueryChildObjects(null, childResourceType, query) as IOrderableQueryResultSet;

                    // Now apply controls
                    var retVal = results.ApplyResultInstructions(query, out int offset, out int totalCount).OfType<IdentifiedData>();

                    try
                    {
                        var modifiedOnSelector = QueryExpressionParser.BuildPropertySelector(results.ElementType, "modifiedOn", convertReturn: typeof(object));
                        var lastModified = (DateTime)results.OrderByDescending(modifiedOnSelector).Select<DateTimeOffset>(modifiedOnSelector).FirstOrDefault().DateTime;
                        RestOperationContext.Current.OutgoingResponse.SetLastModified(lastModified);
                    }
                    catch { }

                    audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success);
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
                        var retBundle = new Bundle(retVal, offset, totalCount);
                        audit = audit.WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Disclosure, retBundle);
                        return retBundle;
                    }
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            finally
            {
                // Only audit queries for things that are sensitive (codes and whatnot don't need to be audited)
                if (!this.m_nonDisclosureAuditableResourceTypes.Contains(resourceType))
                {
                    audit.WithTimestamp().Send();
                }
            }
        }

        /// <inheritdoc/>
        public virtual Stream GetDataset(string resourceType, string id)
        {


            var audit = this.m_auditService.Audit()
                    .WithAction(Core.Model.Audit.ActionType.Execute)
                    .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Export)
                    .WithEventType("EXPORT", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Export resources to XML")
                    .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                    .WithLocalDestination()
                    .WithPrincipal()
                    .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

            try
            {
                var handler = this.GetResourceHandler(resourceType) as IChainedApiResourceHandler;
                if (handler != null)
                {
                    audit.WithSensitivity(handler.Type.GetResourceSensitivityClassification());
                    this.AclCheck(handler, nameof(IApiResourceHandler.Query));

                    // We want to also check the export permission
                    var exportPolicyAttribute = handler.Type.GetCustomAttribute<ResourceSensitivityAttribute>(true);
                    if (exportPolicyAttribute == null)
                    {
                        throw new SecurityException(ErrorMessages.UNABLE_TO_DETERMINE_EXPORT_POLICY);
                    }
                    else
                    {
                        switch (exportPolicyAttribute.Classification)
                        {
                            case ResourceSensitivityClassification.PersonalHealthInformation:
                                this.m_pepService.Demand(PermissionPolicyIdentifiers.ExportClinicalData);
                                break;
                            case ResourceSensitivityClassification.Metadata:
                            case ResourceSensitivityClassification.Administrative:
                                this.m_pepService.Demand(PermissionPolicyIdentifiers.ExportData);
                                break;
                            default:
                                throw new SecurityException(ErrorMessages.UNABLE_TO_DETERMINE_EXPORT_POLICY);
                        }
                    }

                    using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                    {

                        var query = RestOperationContext.Current.IncomingRequest.QueryString;
                        IQueryResultSet results = null;
                        if (Guid.TryParse(id, out var uuid)) // explicit record get
                        {
                            results = new Object[] { handler.Get(uuid, Guid.Empty) }.AsResultSet();
                        }
                        else if (exportPolicyAttribute.Classification == ResourceSensitivityClassification.PersonalHealthInformation && !RestOperationContext.Current.IncomingRequest.QueryString.ToArray().Any(o => !o.Key.StartsWith("_"))) // no global exports
                        {
                            throw new ArgumentException(ErrorMessages.OPERATION_REQUIRES_QUERY_PARAMETER);
                        }
                        else
                        {
                            // Modified on?
                            if (RestOperationContext.Current.IncomingRequest.GetIfModifiedSince() != null)
                            {
                                query.Add("$self", $":(lastModified)>{RestOperationContext.Current.IncomingRequest.GetIfModifiedSince()?.ToString("o")}");
                            }

                            // Query for results
                            results = handler.Query(query);
                        }

                        // HACK: Use the bundle to include and excludes
                        if (RestOperationContext.Current.IncomingRequest.QueryString[QueryControlParameterNames.HttpExcludePathParameterName] != null)
                        {
                            var excludeProperties = RestOperationContext.Current.IncomingRequest.QueryString.GetValues(QueryControlParameterNames.HttpExcludePathParameterName)?.Select(o => this.ResolvePropertyInfo(handler.Type, o)).ToArray();
                            results = Bundle.CreateBundle(results.OfType<IdentifiedData>(), 0, 0, propertiesToExclude: excludeProperties).Item.AsResultSet();
                        }

                        var retVal = new Dataset()
                        {
                            Id = $"Export {resourceType} - {DateTime.Now:yyyyMMddHHmmSS}",
                            Action = results.OfType<IdentifiedData>().ToArray().Select(o => new DataUpdate()
                            {
                                IgnoreErrors = false,
                                InsertIfNotExists = true,
                                Element = o
                            }).OfType<DataInstallAction>().ToList()
                        };

                        if (query[QueryControlParameterNames.HttpIncludePathParameterName] != null)
                        {
                            var includes = query.GetValues(QueryControlParameterNames.HttpIncludePathParameterName).SelectMany(inc =>
                            {
                                // Is this a direct property reference or another query?
                                if (!inc.Contains(':'))
                                {
                                    throw new ArgumentException(String.Format(ErrorMessages.INVALID_FORMAT, inc, "Type:Query"));
                                }

                                var incParts = inc.Split(':');
                                var repositoryType = new ModelSerializationBinder().BindToType(null, incParts[0]);
                                if (repositoryType == null)
                                {
                                    throw new ArgumentException(String.Format(ErrorMessages.TYPE_NOT_FOUND, incParts[0]));
                                }
                                var incQuery = incParts[1].ParseQueryString();
                                var subQuery = QueryExpressionParser.BuildLinqExpression(repositoryType, incQuery);
                                var incHandlerType = typeof(IRepositoryService<>).MakeGenericType(repositoryType);
                                var incHandler = ApplicationServiceContext.Current.GetService(incHandlerType) as IRepositoryService;
                                var incResults = incHandler.Find(subQuery).OfType<IdentifiedData>();
                                var excludeProperties = incQuery.GetValues(QueryControlParameterNames.HttpExcludePathParameterName)?.Select(o => this.ResolvePropertyInfo(repositoryType, o)).ToArray();

                                return incResults.Select(o => new DataUpdate() { Element = o.NullifyProperties(excludeProperties), InsertIfNotExists = true });
                            });
                            if (Boolean.TryParse(RestOperationContext.Current.IncomingRequest.QueryString["_includesFirst"], out var incFirst) && incFirst)
                            {
                                retVal.Action.InsertRange(0, includes);
                            }
                            else
                            {
                                retVal.Action.AddRange(includes);
                            }
                        }

                        var filename = $"{resourceType}-{DateTime.Now:yyyyMMddHHmmSS}.dataset";
                        RestOperationContext.Current.OutgoingResponse.AddHeader("Content-Disposition", $"attachment; filename={filename}");

                        retVal.Action.ForEach(o =>
                        {
                            if (o.Element is IVersionedData ive)
                            {
                                ive.VersionSequence = null;
                                ive.PreviousVersionKey = null;
                            }
                        });
                        audit.WithOutcome(OutcomeIndicator.Success)
                            .WithObjects(Core.Model.Audit.AuditableObjectLifecycle.Export, retVal.Action.Select(o => o.Element).ToArray())
                            .WithAuditableObjects(new AuditableObject()
                            {
                                IDTypeCode = AuditableObjectIdType.ReportNumber,
                                LifecycleType = AuditableObjectLifecycle.Creation,
                                NameData = filename,
                                ObjectId = retVal.Id,
                                Type = AuditableObjectType.SystemObject,
                                Role = AuditableObjectRole.DataDestination,
                                QueryData = query.ToString()
                            });


                        var tfs = new TemporaryFileStream();
                        retVal.Save(tfs);
                        tfs.Seek(0, SeekOrigin.Begin);
                        return tfs;
                    }
                }
                else
                {
                    throw new FileNotFoundException(resourceType);
                }
            }
            catch (PreconditionFailedException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.MinorFail);
                throw;
            }
            catch (FaultException)
            {
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceError(String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                audit = audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                throw;
            }
            finally
            {
                // Only audit queries for things that are sensitive (codes and whatnot don't need to be audited)
                audit.WithTimestamp().Send();
            }


        }

    }
}