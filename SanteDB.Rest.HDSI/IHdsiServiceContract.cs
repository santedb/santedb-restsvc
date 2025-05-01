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
using RestSrvr.Attributes;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Xml.Schema;

namespace SanteDB.Rest.HDSI
{
    /// <summary>
    /// Health Data Services Interface (HDSI)
    /// </summary>
    /// <remarks>This contract represents necessary REST functions to interact with the SanteDB CDR</remarks>
    [ServiceContractAttribute(Name = "HDSI")]
    [ServiceKnownResource(typeof(Concept))]
    [ServiceKnownResource(typeof(ConceptClass))]
    [ServiceKnownResource(typeof(ConceptRelationship))]
    [ServiceKnownResource(typeof(ReferenceTerm))]
    [ServiceKnownResource(typeof(Act))]
    [ServiceKnownResource(typeof(TextObservation))]
    [ServiceKnownResource(typeof(CodedObservation))]
    [ServiceKnownResource(typeof(QuantityObservation))]
    [ServiceKnownResource(typeof(PatientEncounter))]
    [ServiceKnownResource(typeof(SubstanceAdministration))]
    [ServiceKnownResource(typeof(Entity))]
    [ServiceKnownResource(typeof(Patient))]
    [ServiceKnownResource(typeof(Person))]
    [ServiceKnownResource(typeof(EntityRelationship))]
    [ServiceKnownResource(typeof(Provider))]
    [ServiceKnownResource(typeof(Organization))]
    [ServiceKnownResource(typeof(Place))]
    [ServiceKnownResource(typeof(ServiceOptions))]
    [ServiceKnownResource(typeof(Material))]
    [ServiceKnownResource(typeof(ExtensionType))]
    [ServiceKnownResource(typeof(ManufacturedMaterial))]
    [ServiceKnownResource(typeof(DeviceEntity))]
    [ServiceKnownResource(typeof(UserEntity))]
    [ServiceKnownResource(typeof(SecurityUser))]
    [ServiceKnownResource(typeof(SecurityRole))]
    [ServiceKnownResource(typeof(ApplicationEntity))]
    [ServiceKnownResource(typeof(CarePlan))]
    [ServiceKnownResource(typeof(Bundle))]
    [ServiceKnownResource(typeof(ConceptSet))]
    [ServiceKnownResource(typeof(ConceptReferenceTerm))]
    [ServiceKnownResource(typeof(Parameter))]
    [ServiceKnownResource(typeof(PatchCollection))]
    [ServiceProduces("application/json")]
    [ServiceProduces(SanteDBExtendedMimeTypes.JsonViewModel)]
    [ServiceConsumes(SanteDBExtendedMimeTypes.JsonViewModel)]

    [ServiceProduces("application/xml")]
    [ServiceConsumes("application/json")]
    [ServiceConsumes("application/xml")]
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
    public interface IHdsiServiceContract : IRestApiContractImplementation
    {
        /// <summary>
        /// Gets the current server time from the API allowing for time synchronization
        /// </summary>
        [Get("/time"), Obsolete]
        DateTime Time();

        /// <summary>
        /// Get the dataset for the specified object
        /// </summary>
        [Get("/{resourceType}/_export")]
        [RestInvoke("GET", "/{resourceType}/{id}/_export")]
        Stream GetDataset(string resourceType, string id);

        /// <summary>
        /// Get the schema of the HDSI API for use in code generation frameworks
        /// </summary>
        /// <remarks>
        /// This method can be used to retrieve individual numbered schemas from the service
        /// </remarks>
        [Get("/xsd")]
        [ServiceProduces("text/xml")]
        XmlSchema GetSchema();

        /// <summary>
        /// Returns a service capability statement of the HDSI
        /// </summary>
        /// <remarks>
        /// This service options statement contains a complete list of all operations supported, their
        /// capabilities (get, set, etc.) and the authentication mechanism required.
        /// </remarks>
        [RestInvoke("OPTIONS", "/")]
        ServiceOptions Options();

        /// <summary>
        /// Returns specific options for a single resource
        /// </summary>
        /// <param name="resourceType">The type of resource for which the service options should be retrieved</param>
        [RestInvoke("OPTIONS", "/{resourceType}")]
        ServiceResourceOptions ResourceOptions(string resourceType);

        /// <summary>
        /// Performs a minimal PING request to test service availability
        /// </summary>
        /// <remarks>The PING operation is used by the mobile (or other applications)
        /// to ensure that not only is the network available (like a network ping) but that the
        /// API is available on this endpoint. PING returns the current service time and should
        /// be used in lieu of the /time operation</remarks>
        [RestInvoke("PING", "/")]
        void Ping();

        /// <summary>
        /// Perform a search (query) for <paramref name="resourceType"/> matching the HTTP query parameters provided
        /// </summary>
        /// <param name="resourceType">The type of resource against which the search should be performed</param>
        /// <remarks>The search operation calls the database and loads results either from cache or data store. Care should be taken in the number of results retrieved by this operation.</remarks>
        [Get("/{resourceType}")]
        IdentifiedData Search(string resourceType);

        /// <summary>
        /// Perform a search (query) and return only the headers
        /// </summary>
        /// <param name="resourceType">The type of resource against which the HEAD operation should be performed</param>
        /// <remarks>The HEAD operation is useful if you wish to determine if the header information for a specific result set has resulted in a change</remarks>
        [RestInvoke("HEAD", "/{resourceType}")]
        void HeadSearch(string resourceType);

        /// <summary>
        /// Downloads a copy of the specified resource and all dependent objects from another cdr to this CDR
        /// </summary>
        /// <param name="resourceType">The type of resource which should be downloaded/copied</param>
        /// <param name="id">The identifier of the object to be copied</param>
        /// <returns>The copied data</returns>
        [RestInvoke("COPY", "/{resourceType}/{id}")]
        IdentifiedData Copy(string resourceType, string id);

        /// <summary>
        /// Retrieves the current version of the specified resource
        /// </summary>
        /// <param name="id">The identifier of the object which should be retrieved</param>
        /// <param name="resourceType">The type of resource which should be retrieved</param>
        /// <returns>The current version of <paramref name="resourceType"/>/<paramref name="id"/></returns>
        /// <remarks>This method will first use cache to locate the most recent copy before searching the database. It is recommended to use this method rather than a query to Resource?id={id}</remarks>
        [Get("/{resourceType}/{id}")]
        IdentifiedData Get(string resourceType, string id);

        /// <summary>
        /// Retrieves only the metadata of the specified resource
        /// </summary>
        /// <param name="id">The identifier of the resource to be retrieved</param>
        /// <param name="resourceType">The type of resource to be retrieved</param>
        /// <remarks>The metadata for the most recent version of this resource includes the last modified time, the e-tag, etc.</remarks>
        [RestInvoke("HEAD", "/{resourceType}/{id}")]
        void Head(string resourceType, string id);

        /// <summary>
        /// Gets a complete history of all changes made to the specified resource
        /// </summary>
        /// <param name="resourceType">The type of resource for which history should be retrieved</param>
        /// <param name="id">The identifier of the resource to retrieve</param>
        /// <remarks>The result of the history operation is a <see cref="Bundle"/> which contains a complete list of all previous versions associated with the specified object</remarks>
        [Get("/{resourceType}/{id}/_history")]
        IdentifiedData History(string resourceType, string id);

        /// <summary>
        /// Invokes the specified operation
        /// </summary>
        /// <remarks>In SanteDB an operation or method is invoked using <c>POST /resource/123/$do-somthing</c> and is used to invoke remote procedures on the server</remarks>
        /// <param name="resourceType">The type of operation being invoked</param>
        /// <param name="id">The ID of the object which scopes the operation</param>
        /// <param name="parameters">The parameters to invoke the operation with</param>
        /// <param name="operationName">The name of the operation</param>
        /// <returns>The result of the operation invokation</returns>
        [RestInvoke("POST", "/{resourceType}/{id}/${operationName}")]
        object InvokeMethod(String resourceType, String id, String operationName, ParameterCollection parameters);

        /// <summary>
        /// Releases an edit lock on the specified object
        /// </summary>
        /// <remarks>This is the opposite of the <see cref="CheckOut(string, string)"/> command</remarks>
        /// <param name="resourceType">The type of resource to release the lock on</param>
        /// <param name="id">The identifier of the resource to release the lock on</param>
        [RestInvoke("CHECKIN", "/{resourceType}/{id}")]
        object CheckIn(String resourceType, String id);

        /// <summary>
        /// Acquires an edit lock on the specified object
        /// </summary>
        /// <param name="resourceType">The type of resource which should be checked out</param>
        /// <param name="id">The identifier of the resource to be checked out</param>
        /// <remarks>The checkout operation allows for multi-user control of objects. This operation allows the caller to attempt to 
        /// lock the object for edit. If the lock is success a lock reference object is returned</remarks>
        [RestInvoke("CHECKOUT", "/{resourceType}/{id}")]
        object CheckOut(String resourceType, String id);

        /// <summary>
        /// Updates the specified resource according to the instructions in the PATCH file
        /// </summary>
        /// <remarks>The PATCH operation allows for partial updating of a resource.
        ///
        /// This method must be used in conjunction with an If-Match header indicating the version that you would like apply the patch against.
        ///
        /// The server will load the most recent version and compre the version code with If-Match, if the match is successful the instructions in the PATCH are applied to the loaded version.
        /// </remarks>
        /// <param name="id">The identifier of the resource to be patched</param>
        /// <param name="resourceType">The type of resource to be patched</param>
        /// <param name="body">The patch body which contains update instructions on the object referenced by <paramref name="id"/></param>
        [RestInvoke("PATCH", "/{resourceType}/{id}")]
        [RestServiceFault(409, "The patch submitted does not match the current version of the object being patched")]
        void Patch(string resourceType, string id, Patch body);

        /// <summary>
        /// Performs a patch on all resources in the <paramref name="resourceType"/> collection
        /// </summary>
        /// <param name="resourceType">The type of resource being patched</param>
        /// <param name="patchCollection">The collection of patches which are to be applied</param>
        [RestInvoke("PATCH", "/")]
        [RestServiceFault(409, "One or more of the patch instructions provided does not match the current version of the object being patched")]
        void PatchAll(PatchCollection patchCollection);

        /// <summary>
        /// Retrieves a specific version of the specified resource
        /// </summary>
        /// <remarks>This method allows the caller to retrieve a specific version of the identified object, which is useful for loading a previous copy of a resource for reference.</remarks>
        /// <param name="resourceType">The type of resource for which history should be retrieved</param>
        /// <param name="id">The identifier of the resource for which history sould be retrieved</param>
        /// <param name="versionId">The version of the resource to retrieve</param>
        [Get("/{resourceType}/{id}/_history/{versionId}")]
        IdentifiedData GetVersion(string resourceType, string id, string versionId);

        /// <summary>
        /// Creates the resource. If the resource already exists, then a 409 is thrown
        /// </summary>
        /// <param name="body">The body of the request</param>
        /// <remarks>This operation is a CREATE ONLY operation, and will throw an error if the operation results in a duplicate. If you are looking for a CREATE OR UPDATE method use the POST with identifier operation</remarks>
        /// <param name="resourceType">The type of resource to create</param>
        [RestServiceFault(409, "There is a conflict in the update request (version mismatch)")]
        [Post("/{resourceType}")]
        IdentifiedData Create(string resourceType, IdentifiedData body);

        /// <summary>
        /// Updates the specified resource. If the resource does not exist than a 404 is thrown, if there is a conflict (such a mismatch of data) a 409 is thrown
        /// </summary>
        /// <remarks>This operation will update an existing resource on the server such that the data in the database exactly matches the data passed via the API.
        ///
        /// Note: If you post an incomplete object (such as one missing identifiers, or addresses) then the current resource will have those attributes removed. For partial updates, use the PATCH operation instead.</remarks>
        /// <param name="resourceType">The type of resource which should be updated</param>
        /// <param name="id">The identifier of the resource to be updated</param>
        /// <param name="body">The new contents of the resource to be updated</param>
        [Put("/{resourceType}/{id}")]
        [RestServiceFault(409, "There is a conflict in the update request (version mismatch)")]
        IdentifiedData Update(string resourceType, string id, IdentifiedData body);


        /// <summary>
        /// Gets the specified barcode for the resource
        /// </summary>
        /// <remarks>This method returns a barcode which explicitly points at the specified resource in the specified authority.
        ///
        /// The barcode is generated using the server's configuration, and is signed by the server's signing key.
        ///
        /// For complete specification see: https://help.santesuite.org/santedb/extending-santedb/service-apis/health-data-service-interface-hdsi/digitally-signed-visual-code-api</remarks>
        /// <param name="id">The identifier of the resource for which a VRP should be generated</param>
        /// <param name="resourceType">The type of resource for which the VRP should be generated</param>
        /// <returns>The generated VRP code in PNG format</returns>
        [Get("/{resourceType}/{id}/_code")]
        [ServiceProduces("image/png")]
        Stream GetBarcode(String resourceType, String id);

        /// <summary>
        /// Gets the digitally signed pointer (in JWS format) for the resource
        /// </summary>
        /// <remarks>
        /// This operation (like the _code operation) generates a digitally signed pointer for an object. Rather than rendering that pointer as a visual code, the API will return
        /// the structured contents of that code so the client can best determine how to represent the data.
        ///
        /// For complete specification see: https://help.santesuite.org/santedb/extending-santedb/service-apis/health-data-service-interface-hdsi/digitally-signed-visual-code-api
        /// </remarks>
        /// <param name="id">The identifier of the object for which the pointer should be created</param>
        /// <param name="resourceType">The type of resource which should be generated</param>
        /// <returns>A JSON structure in JWS form</returns>
        [Get("/{resourceType}/{id}/_ptr")]
        Stream GetVrpPointerData(String resourceType, String id);

        /// <summary>
        /// Resolve a code to a resource by posting a form-encoded search to the API
        /// </summary>
        /// <remarks>
        /// This operation will decode and validate the pointer passed in the <code>code</code> parameter of the form submission and will return a 303 (method redirect) to the object that matches the code.
        /// </remarks>
        /// <param name="body">The search parameters in in HTTP form query</param>
        [RestInvoke("SEARCH", "/_ptr")]
        [ServiceConsumes("application/x-www-form-urlencoded")]
        void ResolvePointer(NameValueCollection body);

        /// <summary>
        /// Creates or updates a resource. That is, creates the resource if it does not exist, or updates it if it does
        /// </summary>
        /// <remarks>This method will attempt to update the resource if it exists (a-la PUT style) however, if a PUT fails the operation will create (a-la POST)</remarks>
        /// <param name="resource">The resource which is to be created or updated</param>
        /// <param name="resourceType">The type of resource to be created or updated</param>
        /// <param name="id">The identifier of the resource to be created or updated</param>
        /// <returns>The created or updated resource</returns>
        [Post("/{resourceType}/{id}")]
        IdentifiedData CreateUpdate(string resourceType, string id, IdentifiedData resource);

        /// <summary>
        /// Touch the resource (update its timestamp) without modifying the resource itself
        /// </summary>
        /// <remarks>The touch operation is useful for updating the modified time (forcing a re-download) without creating a new version of the resource.</remarks>
        /// <param name="id">The identity of the resource to be touched</param>
        /// <param name="resourceType">The type of resource to be touched</param>
        /// <returns>The object which was updated as part of the touch operation</returns>
        [RestInvoke("TOUCH", "/{resourceType}/{id}")]
        IdentifiedData Touch(string resourceType, string id);

        /// <summary>
        /// Deletes the specified resource from the server
        /// </summary>
        /// <remarks>This operation either logically or permanently deletes the identified resource so that it no longer appears in general searches. The method of deletion is controlled by the 
        /// <c>X-SanteDB-DeleteMode</c> header:
        /// <list type="table">
        ///     <item><term>LogicalDelete</term><description>Creates a null head version of the object which no longer appears in searches, however can be restored from the database</description></item>
        ///     <item><term>PermanentDelete</term><description>Purges the record from the CDR</description></item>
        /// </list></remarks>
        /// <returns>The deleted object</returns>
        /// <param name="resourceType">The type of resource to be deleted</param>
        /// <param name="id">The identifier of the resource to be deleted</param>
        [Delete("/{resourceType}/{id}")]
        [RestServiceFault(409, "There is a conflict in the update request (version mismatch)")]
        IdentifiedData Delete(string resourceType, string id);

        /// <summary>
        /// Performs a linked or chained search on a sub-property
        /// </summary>
        /// <param name="resourceType">The type of resource which should be searched</param>
        /// <param name="id">The key of the hosting (container object)</param>
        /// <param name="childResourceKey">The key of the sub-item to fetch</param>
        /// <param name="childResourceType">The property to search</param>
        /// <returns>The search for the specified resource type limited to the specified object</returns>
        [Get("/{resourceType}/{id}/{childResourceType}/{childResourceKey}")]
        Object AssociationGet(String resourceType, String id, String childResourceType, String childResourceKey);

        /// <summary>
        /// Performs a linked or chained search on a sub-property
        /// </summary>
        /// <param name="resourceType">The type of resource which should be searched</param>
        /// <param name="id">The key of the hosting (container object)</param>
        /// <param name="childResourceType">The property to search</param>
        /// <returns>The search for the specified resource type limited to the specified object</returns>
        /// <remarks>This method performs a general search on the child resource type, however scoped to the container of the parent (so users within role container)</remarks>
        [Get("/{resourceType}/{id}/{childResourceType}")]
        Object AssociationSearch(String resourceType, String id, String childResourceType);

        /// <summary>
        /// Assigns the child object as a child (or link) of the parent
        /// </summary>
        /// <param name="resourceType">The type of container resource</param>
        /// <param name="id">The identiifer of the container</param>
        /// <param name="childResourceType">The property which is the association to be added</param>
        /// <param name="body">The object to be added to the collection</param>
        /// <remarks>This method adds a new child resource instance to the container and performs the necessary linking</remarks>
        [Post("/{resourceType}/{id}/{childResourceType}")]
        object AssociationCreate(String resourceType, String id, String childResourceType, Object body);

        /// <summary>
        /// Removes a child resource instance from the parent container
        /// </summary>
        /// <param name="resourceType">The type of resource which is the container</param>
        /// <param name="id">The key of the container</param>
        /// <param name="childResourceType">The property on which the sub-key resides</param>
        /// <param name="childResourceKey">The actual value of the sub-key</param>
        /// <returns>The removed object</returns>
        /// <remarks>This method will unlink, or delete (depending on behavior of the provider) the child resource from the container.</remarks>
        [Delete("/{resourceType}/{id}/{childResourceType}/{childResourceKey}")]
        object AssociationRemove(String resourceType, String id, String childResourceType, String childResourceKey);

        /// <summary>
        /// Invokes the specified operation
        /// </summary>
        /// <param name="resourceType">The type of operation being invoked</param>
        /// <param name="operationName">The name of the operation</param>
        /// <param name="parameters">The parameters to invoke the operation with</param>
        /// <returns>The result of the operation invokation</returns>
        [RestInvoke("POST", "/{resourceType}/${operationName}")]
        object InvokeMethod(String resourceType, String operationName, ParameterCollection parameters);

        /// <summary>
        /// Performs a linked or chained search on a sub-property
        /// </summary>
        /// <param name="resourceType">The type of resource which should be searched</param>
        /// <param name="childResourceType">The property to search</param>
        /// <returns>The search for the specified resource type limited to the specified object</returns>
        [Get("/{resourceType}/{childResourceType}")]
        Object AssociationSearch(String resourceType, String childResourceType);

        /// <summary>
        /// Performs a linked or chained search on a sub-property
        /// </summary>
        /// <param name="resourceType">The type of resource which should be searched</param>
        /// <param name="childResourceKey">The key of the sub-item to fetch</param>
        /// <param name="childResourceType">The property to search</param>
        /// <returns>The search for the specified resource type limited to the specified object</returns>
        [Get("/{resourceType}/{childResourceType}/{childResourceKey}")]
        Object AssociationGet(String resourceType, String childResourceType, String childResourceKey);

        /// <summary>
        /// Removes a child resource instance from the parent container
        /// </summary>
        /// <param name="resourceType">The type of resource which is the container</param>
        /// <param name="childResourceType">The property on which the sub-key resides</param>
        /// <param name="childResourceKey">The actual value of the sub-key</param>
        /// <returns>The removed object</returns>
        /// <remarks>This method will unlink, or delete (depending on behavior of the provider) the child resource from the container.</remarks>
        [Delete("/{resourceType}/{childResourceType}/{childResourceKey}")]
        object AssociationRemove(String resourceType, String childResourceType, String childResourceKey);
    }
}