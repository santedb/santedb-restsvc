/*
 * Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Copyright (C) 2019 - 2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2019-11-27
 */
using SanteDB.Core.Model;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler base type that is always bound to HDSI
    /// </summary>
    /// <typeparam name="TData">The data which the resource handler is bound to</typeparam>
    public abstract class ResourceHandlerBase<TData> : SanteDB.Rest.Common.ResourceHandlerBase<TData>, INullifyResourceHandler, ICancelResourceHandler
        where TData : IdentifiedData
    {

        /// <summary>
        /// Gets the scope
        /// </summary>
        override public Type Scope => typeof(IHdsiServiceContract);

        /// <summary>
        /// Cancel the specified object
        /// </summary>
        public object Cancel(object key)
        {
            if (this.GetRepository() is ICancelRepositoryService<TData>)
                return (this.GetRepository() as ICancelRepositoryService<TData>).Cancel((Guid)key);
            else
                throw new NotSupportedException($"Repository for {this.ResourceName} does not support Cancel");
        }

        /// <summary>
        /// Nullify the specified object
        /// </summary>
        public object Nullify(object key)
        {
            if (this.GetRepository() is INullifyRepositoryService<TData>)
                return (this.GetRepository() as INullifyRepositoryService<TData>).Nullify((Guid)key);
            else
                throw new NotSupportedException($"Repository for {this.ResourceName} does not support Nullify");
        }
    }
}