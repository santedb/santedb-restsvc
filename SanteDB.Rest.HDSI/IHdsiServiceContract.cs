/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Copyright 2019-2019 SanteSuite Contributors (See NOTICE)
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
 * User: justi
 * Date: 2019-1-12
 */
using RestSrvr.Attributes;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using SanteDB.Rest.Common.Attributes;
using System;
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
    [ServiceProduces("application/json")]
    [ServiceProduces("application/json+sdb-viewModel")]
    [ServiceProduces("application/xml")]
    [ServiceConsumes("application/json")]
    [ServiceConsumes("application/xml")]
    [ServiceConsumes("application/json+sdb-viewModel")]
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
    public interface IHdsiServiceContract 
    {

        /// <summary>
        /// Get the current time
        /// </summary>
        /// <returns></returns>
        [Get("/time")]
        DateTime Time();

        /// <summary>
        /// Get the schema
        /// </summary>
        [Get("/?xsd={schemaId}")]
        [ServiceProduces("text/xml")]
        XmlSchema GetSchema(int schemaId);

        /// <summary>
        /// Gets the operations that each resource in this IMS instance supports.
        /// </summary>
        [RestInvoke("OPTIONS", "/")]
        ServiceOptions Options();

        /// <summary>
        /// Options for resource
        /// </summary>
        [RestInvoke("OPTIONS", "/{resourceType}")]
        ServiceResourceOptions ResourceOptions(string resourceType);

        /// <summary>
        /// Performs a minimal PING request to test service uptime
        /// </summary>
        [RestInvoke("PING", "/")]
        void Ping();

        /// <summary>
        /// Performs a search for the specified resource, returning only current version items.
        /// </summary>
        [Get("/{resourceType}")]
        IdentifiedData Search(string resourceType);

        /// <summary>
        /// Searches for the specified resource and returns only the HEADer metadata
        /// </summary>
        [RestInvoke("HEAD", "/{resourceType}")]
        void HeadSearch(string resourceType);

        /// <summary>
        /// Retrieves the current version of the specified resource from the IMS.
        /// </summary>
        [Get("/{resourceType}/{id}")]
        IdentifiedData Get(string resourceType, string id);

        /// <summary>
        /// Retrieves only the metadata of the specified resource
        /// </summary>
        [RestInvoke("HEAD", "/{resourceType}/{id}")]
        void Head(string resourceType, string id);

        /// <summary>
        /// Gets a complete history of all changes made to the specified resource
        /// </summary>
        [Get("/{resourceType}/{id}/history")]
        IdentifiedData History(string resourceType, string id);

        /// <summary>
        /// Updates the specified resource according to the instructions in the PATCH file
        /// </summary>
        /// <returns></returns>
        [RestInvoke("PATCH", "/{resourceType}/{id}")]
        [RestServiceFault(409, "The patch submitted does not match the current version of the object being patched")]
        void Patch(string resourceType, string id , Patch body);

        /// <summary>
        /// Returns a list of patches for the specified resource 
        /// </summary>
        [Get("/{resourceType}/{id}/patch")]
        Patch GetPatch(string resourceType, string id);

        /// <summary>
        /// Retrieves a specific version of the specified resource
        /// </summary>
        [Get("/{resourceType}/{id}/history/{versionId}")]
        IdentifiedData GetVersion(string resourceType, string id, string versionId);

        /// <summary>
        /// Creates the resource. If the resource already exists, then a 409 is thrown
        /// </summary>
        [Post("/{resourceType}")]
        IdentifiedData Create(string resourceType, IdentifiedData body);

        /// <summary>
        /// Updates the specified resource. If the resource does not exist than a 404 is thrown
        /// </summary>
        [Put("/{resourceType}/{id}")]
        [RestServiceFault(409, "There is a conflict in the update request (version mismatch)")]
        IdentifiedData Update(string resourceType, string id, IdentifiedData body);

        /// <summary>
        /// Creates or updates a resource. That is, creates the resource if it does not exist, or updates it if it does
        /// </summary>
        [Post("/{resourceType}/{id}")]
        IdentifiedData CreateUpdate(string resourceType, string id, IdentifiedData body);

        /// <summary>
        /// Deletes the specified resource from the IMS instance
        /// </summary>
        [Delete("/{resourceType}/{id}")]
        [RestServiceFault(409, "There is a conflict in the update request (version mismatch)")]
        IdentifiedData Delete(string resourceType, string id);


    }
}
