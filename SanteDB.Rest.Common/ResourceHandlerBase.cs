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
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Resource handler base
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public abstract class ResourceHandlerBase<TResource> : IServiceImplementation, IApiResourceHandler where TResource : IdentifiedData, new()
    {
        // Tracer
        protected readonly Tracer m_tracer = Tracer.GetTracer(typeof(ResourceHandlerBase<TResource>));

        /// <summary>
        /// IRepository service
        /// </summary>
        protected IRepositoryService<TResource> m_repository = null;

        /// <summary>
        /// Localization service
        /// </summary>
        protected readonly ILocalizationService m_localizationService;

        /// <summary>
        /// Freetext search
        /// </summary>
        protected readonly IFreetextSearchService m_freetextSearch;

        readonly IAuditService _AuditService;

        /// <summary>
        /// Constructs the resource handler base
        /// </summary>
        public ResourceHandlerBase(ILocalizationService localizationService, IFreetextSearchService freetextSearchService, IRepositoryService<TResource> repositoryService, IAuditService auditService)
        {
            this.m_localizationService = localizationService;
            this.m_repository = repositoryService;
            this.m_freetextSearch = freetextSearchService;
            _AuditService = auditService;
        }

        /// <summary>
        /// Gets the scope of the resource handler
        /// </summary>
        public abstract Type Scope { get; }

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public virtual string ResourceName
        {
            get
            {
                return typeof(TResource).GetCustomAttribute<XmlRootAttribute>().ElementName;
            }
        }

        /// <summary>
        /// Gets the type
        /// </summary>
        public Type Type
        {
            get
            {
                return typeof(TResource);
            }
        }

        /// <summary>
        /// Gets the capabilities of this resource handler
        /// </summary>
        public virtual ResourceCapabilityType Capabilities
        {
            get
            {
                return ResourceCapabilityType.Create |
                    ResourceCapabilityType.CreateOrUpdate |
                    ResourceCapabilityType.Delete |
                    ResourceCapabilityType.Get |
                    ResourceCapabilityType.GetVersion |
                    ResourceCapabilityType.History |
                    ResourceCapabilityType.Search |
                    ResourceCapabilityType.Update;
            }
        }

        /// <summary>
        /// Get service name
        /// </summary>
        public string ServiceName => typeof(ResourceHandlerBase<TResource>).Name;

        /// <summary>
        /// Create a resource
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual Object Create(Object data, bool updateIfExists)
        {
            if (data == null)
            {
                this.m_tracer.TraceError($"{nameof(data)} cannot be null");
                throw new ArgumentNullException(this.m_localizationService.GetString("error.type.ArgumentNullException.param", new { param = nameof(data) }));
            }
            else if ((this.Capabilities & ResourceCapabilityType.Create) == 0 &&
                (this.Capabilities & ResourceCapabilityType.CreateOrUpdate) == 0)
                throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));

            var bundle = data as Bundle;

            bundle?.Reconstitute();

            var processData = bundle?.GetFocalObject() ?? data;

            try
            {
                if (!(processData is TResource))
                {
                    this.m_tracer.TraceError($"Invalid data submission. Expected {typeof(TResource).FullName} but received {processData.GetType().FullName}. If you are submitting a bundle, ensure it has an entry point.");
                    throw new ArgumentException(this.m_localizationService.GetString("error.rest.common.invalidDataSubmission", new
                    {
                        param = typeof(TResource).FullName,
                        param1 = processData.GetType().FullName
                    }));
                }
                else if (processData is TResource)
                {
                    var resourceData = processData as TResource;
                    resourceData = updateIfExists ? this.m_repository.Save(resourceData) : this.m_repository.Insert(resourceData);

                    _AuditService.Audit().ForCreate(OutcomeIndicator.Success, null, resourceData).Send();

                    return resourceData;
                }
            }
            catch (Exception e)
            {
                _AuditService.Audit().ForCreate(OutcomeIndicator.MinorFail, null, data).Send();
                this.m_tracer.TraceError($"Error creating {data}");
                throw new Exception(this.m_localizationService.GetString("error.rest.common.errorCreatingParam", new { param = nameof(data) }), e);
            }
            this.m_tracer.TraceError($"Invalid data type: {nameof(data)}");
            throw new ArgumentException(nameof(data), this.m_localizationService.GetString("error.rest.common.invalidDataType"));
        }

        /// <summary>
        /// Read clinical data
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual Object Get(object id, object versionId)
        {
            if ((this.Capabilities & ResourceCapabilityType.Get) == 0 &&
                (this.Capabilities & ResourceCapabilityType.GetVersion) == 0)
                throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));

            try
            {
                var retVal = this.m_repository.Get((Guid)id, (Guid)versionId);
                if (retVal is Entity || retVal is Act)
                    _AuditService.Audit().ForRead(OutcomeIndicator.Success, id.ToString(), retVal).Send();
                return retVal;
            }
            catch (Exception e)
            {
                _AuditService.Audit().ForRead<TResource>(OutcomeIndicator.MinorFail, id.ToString()).Send();
                this.m_tracer.TraceError($"Error getting resource {id}");
                throw new Exception(this.m_localizationService.GetString("error.rest.common.gettingResource", new { param = nameof(id) }), e);
            }
        }

        /// <summary>
        /// Obsolete data
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual Object Delete(object key)
        {
            if ((this.Capabilities & ResourceCapabilityType.Delete) == 0)
                throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));

            try
            {
                var retVal = this.m_repository.Delete((Guid)key);
                _AuditService.Audit().ForDelete(OutcomeIndicator.Success, key.ToString(), retVal).Send();
                return retVal;
            }
            catch (Exception e)
            {
                _AuditService.Audit().ForDelete<TResource>(OutcomeIndicator.MinorFail, key.ToString()).Send();
                this.m_tracer.TraceError($"Error obsoleting resource {key}");
                throw new Exception(this.m_localizationService.GetString("error.rest.common.obsoletingResource", new { param = nameof(key) }), e);
            }
        }

        /// <summary>
        /// Perform a query
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual IQueryResultSet Query(NameValueCollection queryParameters)
        {
            if ((this.Capabilities & ResourceCapabilityType.Search) == 0)
                throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));

            try
            {
                if ((this.Capabilities & ResourceCapabilityType.Search) == 0)
                {
                    throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));
                }

                if (queryParameters.TryGetValue("_any", out var terms))
                {
                    return this.HandleFreeTextSearch(terms);
                }
                else
                {
                    var queryExpression = QueryExpressionParser.BuildLinqExpression<TResource>(queryParameters, null, false);
                    return this.m_repository.Find(queryExpression);
                }
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error querying data source");
                throw new Exception(this.m_localizationService.GetString("error.rest.common.queryDataSource"), e);
            }
        }

        /// <summary>
        /// Handle freetext search
        /// </summary>
        protected virtual IQueryResultSet HandleFreeTextSearch(IEnumerable<string> terms)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Perform an update
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual Object Update(Object data)
        {
            if ((this.Capabilities & ResourceCapabilityType.Update) == 0)
                throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));

            Bundle bundleData = data as Bundle;
            bundleData?.Reconstitute();
            var processData = bundleData?.GetFocalObject() ?? data;

            try
            {
                if (!(processData is TResource))
                {
                    this.m_tracer.TraceError($"Invalid data submission. Expected {typeof(TResource).FullName} but received {processData.GetType().FullName}. If you are submitting a bundle, ensure it has an entry point");
                    throw new ArgumentException(this.m_localizationService.GetString("error.rest.common.invalidDataSubmission", new
                    {
                        param = typeof(TResource).FullName,
                        param1 = processData.GetType().FullName
                    }));
                }
                else if (processData is TResource)
                {
                    var entityData = processData as TResource;

                    var retVal = this.m_repository.Save(entityData);
                    _AuditService.Audit().ForUpdate(OutcomeIndicator.Success, null, retVal).Send();
                    return retVal;
                }
                else
                {
                    this.m_tracer.TraceError("Invalid persistence type");
                    throw new ArgumentException(this.m_localizationService.GetString("error.rest.common.invalidPersistentType"));
                }
            }
            catch (Exception e)
            {
                _AuditService.Audit().ForUpdate(OutcomeIndicator.MinorFail, null, data).Send();
                this.m_tracer.TraceError("Error updating resource");
                throw new Exception(this.m_localizationService.GetString("error.rest.common.updatingResource"), e);
            }
        }
    }
}