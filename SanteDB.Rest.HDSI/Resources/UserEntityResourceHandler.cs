﻿/*
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2021-2-9
 */
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using SanteDB.Core;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.Core.Model;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a user entity resource handler.
    /// </summary>
    public class UserEntityResourceHandler : ResourceHandlerBase<UserEntity>
	{

        /// <summary>
        /// Create the specified user entity
        /// </summary>
        public override Object Create(Object data, bool updateIfExists)
        {
            // Check the claimed user exists
            if (data is UserEntity userEntity)
            {
                var securityService = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>();
                if (securityService.Get(userEntity.SecurityUserKey.GetValueOrDefault()) != null)
                    return base.Create(data, updateIfExists);
                else
                {
                    this.m_tracer.TraceWarning("Security user {0} doesn't exist here, ignoring update", userEntity.SecurityUserKey);
                    userEntity.SecurityUserKey = null;
                    return base.Create(userEntity, updateIfExists);
                }
            }
            else 
                throw new ArgumentOutOfRangeException("Can only handle UserEntity");
        }

        /// <summary>
        /// Gets the specified user 
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override Object Get(object id, object versionId)
        {
            return base.Get((Guid)id, (Guid)versionId);
        }

        /// <summary>
        /// Obsolete
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override Object Obsolete(object key)
        {
            return base.Obsolete((Guid)key);
        }

        /// <summary>
        /// Query the specified data
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }

        /// <summary>
        /// Query specified user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            return base.Query(queryParameters, offset, count, out totalCount);
        }

        /// <summary>
        /// Update specified data
        /// </summary>
        public override Object Update(Object data)
        {
            // Check the claimed user exists
            if (data is UserEntity userEntity)
            {
                var securityService = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>();
                if (securityService.Get(userEntity.SecurityUserKey.GetValueOrDefault()) != null)
                    return base.Update(data);
                else
                {
                    this.m_tracer.TraceWarning("Security user {0} doesn't exist here, ignoring update", userEntity.SecurityUserKey);
                    userEntity.SecurityUserKey = null;
                    return base.Update(userEntity);
                }
                    
            }
            else
                throw new ArgumentOutOfRangeException("Can only handle UserEntity");
        }
    }
}