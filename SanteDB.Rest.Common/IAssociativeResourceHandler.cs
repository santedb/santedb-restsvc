/*
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
using SanteDB.Core.Model.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Represnets a resource handler which can link sub-objects (or certain sub-objects) with a parent object
    /// </summary>
    public interface IAssociativeResourceHandler : IApiResourceHandler
    {

        /// <summary>
        /// Removes the specified associated entity form the specified property name collection
        /// </summary>
        /// <param name="scopingEntityKey">The instance of the parent entity from which the object should be removed</param>
        /// <param name="propertyName">The name of the relationship which the entity should be removed from</param>
        /// <param name="subItemKey">The sub-item key that should be removed</param>
        Object RemoveAssociatedEntity(object scopingEntityKey, string propertyName, object subItemKey);

        /// <summary>
        /// Queries the associated entities which are contained within the specified scoping entity
        /// </summary>
        /// <param name="scopingEntityKey">The container (scope) entity to which the sub entity belongs</param>
        /// <param name="propertyName">The name of the property/relationship to scope to</param>
        /// <param name="filter">The filter to apply</param>
        /// <param name="offset">The offset of the first row to be retrieved </param>
        /// <param name="count">The number of objects which should be returned from the query</param>
        /// <param name="totalCount">The total matching results</param>
        /// <returns>The matching results</returns>
        IEnumerable<Object> QueryAssociatedEntities(object scopingEntityKey, string propertyName, NameValueCollection filter, int offset, int count, out Int32 totalCount);

        /// <summary>
        /// Adds the specified object with sub item key 
        /// </summary>
        /// <param name="scopingEntityKey">The scoping entity key</param>
        /// <param name="propertyName">The property name</param>
        /// <param name="scopedItem">The sub-item to be added</param>
        /// <returns>The newly created associative entity</returns>
        Object AddAssociatedEntity(object scopingEntityKey, string propertyName, object scopedItem);

        /// <summary>
        /// Fetchs the scoped entity
        /// </summary>
        Object GetAssociatedEntity(object scopingEntity, string propertyName, object subItemKey);
    }
}
