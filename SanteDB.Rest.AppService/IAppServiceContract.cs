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
using RestSrvr.Attributes;
using SanteDB.Core.Interop;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.IO;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service contract
    /// </summary>
    [ServiceContract(Name = "APP")]
    [ServiceProduces("application/json")]
    [ServiceProduces("application/json+sdb-viewmodel")]
    [ServiceConsumes("application/json")]
    [ServiceConsumes("application/json+sdb-viewmodel")]
    [ServiceKnownResource(typeof(MenuInformation))]
    [RestServiceFault(400, "The provided resource was in an incorrect format")]
    [RestServiceFault(401, "The principal is unauthorized and needs to either elevate or authenticate themselves")]
    [RestServiceFault(403, "The principal is not permitted (cannot elevate) to perform the operation")]
    [RestServiceFault(404, "The requested object does not exist")]
    [RestServiceFault(410, "The specified object did exist however is no-longer present")]
    [RestServiceFault(415, "The client is submitting an invalid object")]
    [RestServiceFault(422, "There was a business rule violation executing the operation")]
    [RestServiceFault(429, "The server rejected the request due to a throttling constraint")]
    [RestServiceFault(500, "The server encountered an error processing the result")]
    [RestServiceFault(503, "The service is not available (starting up or shutting down)")]
    public interface IAppServiceContract
    {

        /// <summary>
        /// Creates the specified resource
        /// </summary>
        /// <param name="resourceType">The type of resource to be created</param>
        /// <param name="data">The resource data to be created</param>
        /// <returns>The stored resource</returns>
        [Post("/{resourceType}")]
        Object Create(String resourceType, Object data);

        /// <summary>
        /// Creates the specified resource if it does not exist, otherwise updates it
        /// </summary>
        /// <param name="resourceType">The type of resource to be created</param>
        /// <param name="key">The key of the resource </param>
        /// <param name="data">The resource itself</param>
        /// <returns>The updated or created resource</returns>
        [Post("/{resourceType}/{key}")]
        Object CreateUpdate(String resourceType, String key, Object data);

        /// <summary>
        /// Updates the specified resource
        /// </summary>
        /// <param name="resourceType">The type of resource to be updated</param>
        /// <param name="key">The key of the resource</param>
        /// <param name="data">The resource data to be updated</param>
        /// <returns>The updated resource</returns>
        [Put("/{resourceType}/{key}")]
        [RestServiceFault(409, "The provided update has a conflict with the current state of the object in the server")]
        Object Update(String resourceType, String key, Object data);

        /// <summary>
        /// Deletes the specified resource
        /// </summary>
        /// <param name="resourceType">The type of resource being deleted</param>
        /// <param name="key">The key of the resource being deleted</param>
        /// <returns>The last version of the deleted resource</returns>
        [Delete("/{resourceType}/{key}")]
        [RestServiceFault(409, "The provided delete cannot occur due to a conflicted If- header")]
        Object Delete(String resourceType, String key);

        /// <summary>
        /// Gets the specified resource from the service
        /// </summary>
        /// <param name="resourceType">The type of resource to be fetched</param>
        /// <param name="key">The key of the resource</param>
        /// <returns>The retrieved resource</returns>
        [Get("/{resourceType}/{key}")]
        Object Get(String resourceType, String key);

        /// <summary>
        /// Performs a linked or chained search on a sub-property
        /// </summary>
        /// <param name="resourceType">The type of resource which should be searched</param>
        /// <param name="id">The key of the hosting (container object)</param>
        /// <param name="childKey">The key of the sub-item to fetch</param>
        /// <param name="childResourceType">The property to search</param>
        /// <returns>The search for the specified resource type limited to the specified object</returns>
        [Get("/{resourceType}/{id}/{childResourceType}/{childKey}")]
        Object AssociationGet(String resourceType, String id, String childResourceType, String childKey);

        /// <summary>
        /// Performs a linked or chained search on a sub-property
        /// </summary>
        /// <param name="resourceType">The type of resource which should be searched</param>
        /// <param name="id">The key of the hosting (container object)</param>
        /// <param name="childResourceType">The property to search</param>
        /// <returns>The search for the specified resource type limited to the specified object</returns>
        [Get("/{resourceType}/{id}/{childResourceType}")]
        Object AssociationSearch(String resourceType, String id, String childResourceType);

        /// <summary>
        /// Assigns the <paramref name="body"/> object with the resource at <paramref name="resourceType"/>/<paramref name="id"/>
        /// </summary>
        /// <param name="resourceType">The type of container resource</param>
        /// <param name="id">The identiifer of the container</param>
        /// <param name="childResourceType">The property which is the association to be added</param>
        /// <param name="body">The object to be added to the collection</param>
        [Post("/{resourceType}/{id}/{childResourceType}")]
        object AssociationCreate(String resourceType, String id, String childResourceType, Object body);

        /// <summary>
        /// Removes an association
        /// </summary>
        /// <param name="resourceType">The type of resource which is the container</param>
        /// <param name="id">The key of the container</param>
        /// <param name="childResourceType">The property on which the sub-key resides</param>
        /// <param name="childKey">The actual value of the sub-key</param>
        /// <returns>The removed object</returns>
        [Delete("/{resourceType}/{id}/{childResourceType}/{childKey}")]
        Object AssociationRemove(String resourceType, String id, String childResourceType, String childKey);

        /// <summary>
        /// Searches the specified resource type for matches
        /// </summary>
        /// <param name="resourceType">The resource type to be searched</param>
        /// <returns>The results of the search</returns>
        [Get("/{resourceType}")]
        Object Search(String resourceType);

        /// <summary>
        /// Releases an edit lock on the specified object
        /// </summary>
        [RestInvoke("CHECKIN", "/{resourceType}")]
        [RestInvoke("CHECKIN", "/{resourceType}/{id}")]
        object CheckIn(String resourceType, String id);

        /// <summary>
        /// Acquires an edit lock on the specified object
        /// </summary>
        [RestInvoke("CHECKOUT", "/{resourceType}/{id}")]
        [RestInvoke("CHECKOUT", "/{resourceType}")]
        object CheckOut(String resourceType, String id);

        /// <summary>
        /// Invokes the specified operation
        /// </summary>
        /// <param name="resourceType">The type of operation being invoked</param>
        /// <param name="body">The parameters which should be used to execute the operation</param>
        /// <param name="operationName">The name of the operation</param>
        /// <returns>The result of the operation invokation</returns>
        [RestInvoke("POST", "/{resourceType}/${operationName}")]
        object InvokeMethod(String resourceType, String operationName, ParameterCollection body);

        /// <summary>
        /// Invokes the specified operation
        /// </summary>
        /// <param name="resourceType">The type of operation being invoked</param>
        /// <param name="id">The ID of the operation</param>
        /// <param name="operationName">The name of the operation</param>
        /// <param name="body">The parameters to send to the operation</param>
        /// <returns>The result of the operation invokation</returns>
        [RestInvoke("POST", "/{resourceType}/{id}/${operationName}")]
        object InvokeMethod(String resourceType, String id, String operationName, ParameterCollection body);


    }
}
