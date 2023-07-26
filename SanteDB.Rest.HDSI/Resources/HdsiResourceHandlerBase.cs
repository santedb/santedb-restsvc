/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-5-19
 */
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler base type that is always bound to HDSI
    /// </summary>
    /// <typeparam name="TData">The data which the resource handler is bound to</typeparam>
    [ServiceProvider("HDSI Resource Handler")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public abstract class HdsiResourceHandlerBase<TData> : SanteDB.Rest.Common.ResourceHandlerBase<TData>,
        ICancelResourceHandler,
        ICheckoutResourceHandler,
        IApiResourceHandlerEx,
        IOperationalApiResourceHandler

        where TData : IdentifiedData, new()
    {

        private readonly IResourceCheckoutService m_resourceCheckoutService;

        /// <summary>
        /// DI Constructor
        /// </summary>
        protected HdsiResourceHandlerBase(ILocalizationService localizationService, IRepositoryService<TData> repositoryService, IResourceCheckoutService resourceCheckoutService, ISubscriptionExecutor subscriptionExecutor, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, subscriptionExecutor, freetextSearchService)
        {
            this.m_resourceCheckoutService = resourceCheckoutService;
        }

        /// <summary>
        /// Gets the scope
        /// </summary>
        public override Type Scope => typeof(IHdsiServiceContract);

        /// <inheritdoc/>
        public override object Get(object id, object versionId)
        {
            var retVal = base.Get(id, versionId);
            if (retVal is TData td)
            {
                if (this.m_resourceCheckoutService.IsCheckedout<TData>(td.Key.Value, out var owner) &&
                    retVal is ITaggable taggable)
                {
                    taggable.AddTag(SystemTagNames.CheckoutStatusTag, owner.Name);
                    RestOperationContext.Current.OutgoingResponse.AddHeader(ExtendedHttpHeaderNames.CheckoutStatusHeader, owner.Name);
                }
            }
            return retVal;
        }

        /// <summary>
        /// OBsoletion wrapper with locking
        /// </summary>
        public override object Delete(object key)
        {
            try
            {
                this.CheckOut((Guid)key);
                return base.Delete(key);
            }
            finally
            {
                this.CheckIn((Guid)key);
            }
        }

        /// <summary>
        /// Update with lock
        /// </summary>
        public override object Update(object data)
        {
            try
            {
                if (data is IdentifiedData id)
                {
                    this.CheckOut((Guid)id.Key);
                }

                return base.Update(data);
            }
            finally
            {
                if (data is IdentifiedData id)
                {
                    this.CheckIn((Guid)id.Key);
                }
            }
        }


        /// <summary>
        /// Cancel the specified object
        /// </summary>
        public object Cancel(object key)
        {
            try
            {
                this.CheckOut(key);
                if (this.m_repository is ICancelRepositoryService<TData> cr)
                {
                    return cr.Cancel((Guid)key);
                }
                else
                {
                    this.m_tracer.TraceError($"Repository for {this.ResourceName} does not support Cancel");
                    throw new NotSupportedException(this.LocalizationService.GetString("error.rest.hdsi.notSupportCancel", new
                    {
                        param = this.ResourceName
                    }));
                }
            }
            finally
            {
                this.CheckIn(key);
            }
        }

        /// <summary>
        /// Attempt to get a lock on the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public object CheckOut(object key)
        {
            if (this.m_resourceCheckoutService?.Checkout<TData>((Guid)key) == false)
            {
                throw new ObjectLockedException(this.LocalizationService.GetString("error.type.ObjectLockedException"));
            }
            return null;
        }

        /// <summary>
        /// Release the specified lock
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public object CheckIn(object key)
        {
            if (this.m_resourceCheckoutService?.Checkin<TData>((Guid)key) == false)
            {
                throw new ObjectLockedException(this.LocalizationService.GetString("error.type.ObjectLockedException"));
            }
            return null;
        }

        /// <summary>
        /// Touch the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public object Touch(object key)
        {
            if (this.m_repository is IRepositoryServiceEx<TData> exRepo)
            {
                var objectKey = (Guid)key;
                exRepo.Touch(objectKey);
                ApplicationServiceContext.Current.GetService<IDataCachingService>().Remove(objectKey);
                return this.Get(key, Guid.Empty);
            }
            else
            {
                this.m_tracer.TraceError("Repository service does not support TOUCH");
                throw new InvalidOperationException(this.LocalizationService.GetString("error.rest.hdsi.supportTouch"));
            }
        }

    }
}