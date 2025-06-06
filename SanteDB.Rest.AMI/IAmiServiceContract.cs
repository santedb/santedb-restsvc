﻿/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using RestSrvr.Attributes;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Interop;
using SanteDB.Core.Mail;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI.Alien;
using SanteDB.Core.Model.AMI.Applet;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.AMI.Jobs;
using SanteDB.Core.Model.AMI.Logging;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Model.Subscription;
using SanteDB.Core.PubSub;
using SanteDB.Core.Queue;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Xml.Schema;

namespace SanteDB.Rest.AMI
{
    /// <summary>
    /// Administrative Management Interface (AMI)
    /// </summary>
    /// <remarks>
    /// This service contract represents the functionality required to administer the SanteDB server.</remarks>
    [ServiceContractAttribute(Name = "AMI")]
    [ServiceKnownResource(typeof(SecurityUserInfo))]
    [ServiceKnownResource(typeof(Entity))]
    [ServiceKnownResource(typeof(ExtensionType))]
    [ServiceKnownResource(typeof(MailMessage))]
    [ServiceKnownResource(typeof(SecurityApplication))]
    [ServiceKnownResource(typeof(IdentityDomain))]
    [ServiceKnownResource(typeof(SecurityDeviceInfo))]
    [ServiceKnownResource(typeof(SecurityApplicationInfo))]
    [ServiceKnownResource(typeof(SecurityPolicyInfo))]
    [ServiceKnownResource(typeof(SecurityProvenance))]
    [ServiceKnownResource(typeof(SecurityUserChallengeInfo))]
    [ServiceKnownResource(typeof(SecurityRoleInfo))]
    [ServiceKnownResource(typeof(AuditSubmission))]
    [ServiceKnownResource(typeof(SecurityUser))]
    [ServiceKnownResource(typeof(SecurityPolicy))]
    [ServiceKnownResource(typeof(SecurityRole))]
    [ServiceKnownResource(typeof(SecurityApplication))]
    [ServiceKnownResource(typeof(SecurityDevice))]
    [ServiceKnownResource(typeof(SubscriptionDefinition))]
    [ServiceKnownResource(typeof(TfaMechanismInfo))]
    [ServiceKnownResource(typeof(AuditEventData))]
    [ServiceKnownResource(typeof(AppletManifest))]
    [ServiceKnownResource(typeof(JobInfo))]
    [ServiceKnownResource(typeof(AppletManifestInfo))]
    [ServiceKnownResource(typeof(DeviceEntity))]
    [ServiceKnownResource(typeof(DiagnosticApplicationInfo))]
    [ServiceKnownResource(typeof(DiagnosticAttachmentInfo))]
    [ServiceKnownResource(typeof(DiagnosticBinaryAttachment))]
    [ServiceKnownResource(typeof(DiagnosticTextAttachment))]
    [ServiceKnownResource(typeof(DiagnosticEnvironmentInfo))]
    [ServiceKnownResource(typeof(DiagnosticReport))]
    [ServiceKnownResource(typeof(DiagnosticSyncInfo))]
    [ServiceKnownResource(typeof(SecurityChallenge))]
    [ServiceKnownResource(typeof(DiagnosticVersionInfo))]
    [ServiceKnownResource(typeof(SubmissionInfo))]
    [ServiceKnownResource(typeof(SubmissionResult))]
    [ServiceKnownResource(typeof(ApplicationEntity))]
    [ServiceKnownResource(typeof(SubmissionRequest))]
    [ServiceKnownResource(typeof(X509Certificate2Info))]
    [ServiceKnownResource(typeof(CodeSystem))]
    [ServiceKnownResource(typeof(LogFileInfo))]
    [ServiceKnownResource(typeof(DispatcherQueueInfo))]
    [ServiceKnownResource(typeof(DispatcherQueueEntry))]
    [ServiceKnownResource(typeof(ForeignDataInfo))]
    [ServiceKnownResource(typeof(PubSubSubscriptionDefinition))]
    [ServiceKnownResource(typeof(PubSubChannelDefinition))]
    [ServiceProduces("application/json")]
    [ServiceProduces("application/xml")]
    [ServiceProduces(SanteDBExtendedMimeTypes.JsonViewModel)]
    [ServiceConsumes("application/json")]
    [ServiceConsumes("application/xml")]
    [ServiceConsumes(SanteDBExtendedMimeTypes.JsonViewModel)]
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
    public interface IAmiServiceContract : IRestApiContractImplementation
    {
        /// <summary>
        /// Get the schema for this service
        /// </summary>
        [Get("/xsd")]
        XmlSchema GetSchema();

        #region Diagnostic / Ad-Hoc interfaces

        /// <summary>
		/// Ping the service to determine up/down
		/// </summary>
		[RestInvoke("PING", "/")]
        void Ping();

        #endregion Diagnostic / Ad-Hoc interfaces

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
        /// Gets the specified versioned copy of the data
        /// </summary>
        /// <param name="resourceType">The type of resource</param>
        /// <param name="key">The key of the resource</param>
        /// <param name="versionKey">The version key to retrieve</param>
        /// <returns>The object as it existed at that version</returns>
        [Get("/{resourceType}/{key}/_history/{versionKey}")]
        Object GetVersion(String resourceType, String key, String versionKey);

        /// <summary>
        /// Gets a complete history of changes made to the object (if supported)
        /// </summary>
        /// <param name="resourceType">The type of resource</param>
        /// <param name="key">The key of the object to retrieve the history for</param>
        /// <returns>The history</returns>
        [Get("/{resourceType}/{key}/_history")]
        AmiCollection History(String resourceType, String key);

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
        AmiCollection AssociationSearch(String resourceType, String id, String childResourceType);

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
        object AssociationRemove(String resourceType, String id, String childResourceType, String childKey);

        /// <summary>
        /// Locks the specified resource from the service
        /// </summary>
        /// <param name="resourceType">The type of resource to be locked</param>
        /// <param name="key">The key of the resource</param>
        /// <returns>The locked resource</returns>
        [RestInvoke("LOCK", "/{resourceType}/{key}")]
        Object Lock(String resourceType, String key);

        /// <summary>
        /// Unlocks the specified resource from the service
        /// </summary>
        /// <param name="resourceType">The type of resource to be unlocked</param>
        /// <param name="key">The key of the resource</param>
        /// <returns>The unlocked resource</returns>
        [RestInvoke("UNLOCK", "/{resourceType}/{key}")]
        Object UnLock(String resourceType, String key);

        /// <summary>
        /// Heads the specified resource from the service
        /// </summary>
        /// <param name="resourceType">The type of resource to be fetched</param>
        /// <param name="key">The key of the resource</param>
        /// <returns>Headers for the specified resource</returns>
        [RestInvoke("HEAD", "/{resourceType}/{key}")]
        void Head(String resourceType, String key);

        /// <summary>
        /// Searches the specified resource type for matches
        /// </summary>
        /// <param name="resourceType">The resource type to be searched</param>
        /// <returns>The results of the search</returns>
        [Get("/{resourceType}")]
        AmiCollection Search(String resourceType);

        /// <summary>
        /// Get the service options
        /// </summary>
        /// <returns>The options of the server</returns>
        [RestInvoke("OPTIONS", "/")]
        ServiceOptions Options();

        /// <summary>
        /// Updates the specified resource according to the instructions in the PATCH file
        /// </summary>
        /// <returns></returns>
        [RestInvoke("PATCH", "/{resourceType}/{id}")]
        [RestServiceFault(409, "The patch submitted does not match the current version of the object being patched")]
        void Patch(string resourceType, string id, Patch body);

        /// <summary>
        /// Get the specific options supported for the
        /// </summary>
        /// <param name="resourceType">The type of resource to get service options</param>
        [RestInvoke("OPTIONS", "/{resourceType}")]
        ServiceResourceOptions ResourceOptions(String resourceType);

        /// <summary>
        /// Releases an edit lock on the specified object
        /// </summary>
        [RestInvoke("CHECKIN", "/{resourceType}/{id}")]
        object CheckIn(String resourceType, String id);

        /// <summary>
        /// Acquires an edit lock on the specified object
        /// </summary>
        [RestInvoke("CHECKOUT", "/{resourceType}/{id}")]
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
        /// <param name="body">The body of the request.</param>
        /// <returns>The result of the operation invokation</returns>
        [RestInvoke("POST", "/{resourceType}/{id}/${operationName}")]
        object InvokeMethod(String resourceType, String id, String operationName, ParameterCollection body);
    }
}