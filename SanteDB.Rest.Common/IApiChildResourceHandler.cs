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
 * Date: 2021-8-27
 */

using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using System;
using System.Collections.Generic;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Allows a programmatic way of providing associated properties on other objects
    /// </summary>
    public interface IApiChildResourceHandler : IApiChildObject
    {
        /// <summary>
        /// Gets the type of data this associative property is expecting
        /// </summary>
        Type PropertyType { get; }

        /// <summary>
        /// The capabilities of the sub-resource
        /// </summary>
        ResourceCapabilityType Capabilities { get; }

        /// <summary>
        /// Get the value of the associated property with no context (exmaple: GET /hdsi/resource/property)
        /// </summary>
        IQueryResultSet Query(Type scopingType, Object scopingKey, NameValueCollection filter);

        /// <summary>
        /// Get the value of the associated property with context (exmaple: GET /hdsi/resource/property/key)
        /// </summary>
        object Get(Type scopingType, Object scopingKey, object key);

        /// <summary>
        /// Remove an object from the associated property
        /// </summary>
        object Remove(Type scopingType, Object scopingKey, object key);

        /// <summary>
        /// Add a value to the associated property
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        object Add(Type scopingType, Object scopingKey, object item);
    }
}