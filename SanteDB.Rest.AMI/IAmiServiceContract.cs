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
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Auditing;
using SanteDB.Core.Interop;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.AMI.Applet;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.AMI.Logging;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Rest.Common.Attributes;
using System;
using System.IO;
using System.Xml.Schema;

namespace SanteDB.Rest.AMI
{
    /// <summary>
    /// Represents a service contract for the AMI 
    /// </summary>
    [ServiceContractAttribute(Name = "AMI")]
    [ServiceKnownResource(typeof(SecurityUserInfo))]
    [ServiceKnownResource(typeof(Entity))]
    [ServiceKnownResource(typeof(ExtensionType))]
    [ServiceKnownResource(typeof(MailMessage))]
    [ServiceKnownResource(typeof(SecurityApplication))]
    [ServiceKnownResource(typeof(TfaRequestInfo))]
    [ServiceKnownResource(typeof(SecurityDeviceInfo))]
    [ServiceKnownResource(typeof(SecurityApplicationInfo))]
    [ServiceKnownResource(typeof(SecurityPolicyInfo))]
    [ServiceKnownResource(typeof(SecurityProvenance))]
    [ServiceKnownResource(typeof(SecurityRoleInfo))]
    [ServiceKnownResource(typeof(AuditSubmission))]
    [ServiceKnownResource(typeof(AuditData))]
    [ServiceKnownResource(typeof(AppletManifest))]
    [ServiceKnownResource(typeof(AppletManifestInfo))]
    [ServiceKnownResource(typeof(DeviceEntity))]
    [ServiceKnownResource(typeof(DiagnosticApplicationInfo))]
    [ServiceKnownResource(typeof(DiagnosticAttachmentInfo))]
    [ServiceKnownResource(typeof(DiagnosticBinaryAttachment))]
    [ServiceKnownResource(typeof(DiagnosticTextAttachment))]
    [ServiceKnownResource(typeof(DiagnosticEnvironmentInfo))]
    [ServiceKnownResource(typeof(DiagnosticReport))]
    [ServiceKnownResource(typeof(DiagnosticSyncInfo))]
    [ServiceKnownResource(typeof(DiagnosticVersionInfo))]
    [ServiceKnownResource(typeof(SubmissionInfo))]
    [ServiceKnownResource(typeof(SubmissionResult))]
    [ServiceKnownResource(typeof(ApplicationEntity))]
    [ServiceKnownResource(typeof(SubmissionRequest))]
    [ServiceKnownResource(typeof(ServiceOptions))]
    [ServiceKnownResource(typeof(X509Certificate2Info))]
    [ServiceKnownResource(typeof(CodeSystem))]
    [ServiceKnownResource(typeof(LogFileInfo))]
    [ServiceKnownResource(typeof(AmiCollection))]
    [ServiceProduces("application/json")]
    [ServiceProduces("application/xml")]
    [ServiceProduces("application/json+sdb-viewModel")]
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
    public interface IAmiServiceContract
    {
        /// <summary>
        /// Get the schema for this service
        /// </summary>
        [Get("/?xsd={schemaId}")]
        XmlSchema GetSchema(int schemaId);
        #region Diagnostic / Ad-Hoc interfaces

        /// <summary>
        /// Creates a diagnostic report.
        /// </summary>
        /// <param name="report">The diagnostic report to be created.</param>
        /// <returns>Returns the created diagnostic report.</returns>
        [Post("/Sherlock")]
        DiagnosticReport CreateDiagnosticReport(DiagnosticReport report);

        /// <summary>
		/// Gets a specific log file.
		/// </summary>
		/// <param name="logId">The log identifier.</param>
		/// <returns>Returns the log file information.</returns>
		[Get("/Log/{logId}")]
        LogFileInfo GetLog(string logId);

        /// <summary>
        /// Get log files on the server and their sizes.
        /// </summary>
        /// <returns>Returns a collection of log files.</returns>
        [Get("/Log")]
        AmiCollection GetLogs();

        /// <summary>
		/// Gets a server diagnostic report.
		/// </summary>
		/// <returns>Returns the created diagnostic report.</returns>
		[Get("/Sherlock")]
        DiagnosticReport GetServerDiagnosticReport();

        /// <summary>
		/// Ping the service to determine up/down
		/// </summary>
		[RestInvoke("PING", "/")]
        void Ping();

        #endregion

        #region Two-Factor Authentication

        /// <summary>
		/// Creates a request that the server issue a reset code
		/// </summary>
		[Post("/Tfa")]
        void SendTfaSecret(TfaRequestInfo resetInfo);

        /// <summary>
        /// Gets the list of TFA mechanisms.
        /// </summary>
        /// <returns>Returns a list of TFA mechanisms.</returns>
        [Get("/Tfa")]
        AmiCollection GetTfaMechanisms();

        #endregion

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
        /// Locks the specified resource from the service
        /// </summary>
        /// <param name="resourceType">The type of resource to be locked</param>
        /// <param name="key">The key of the resource</param>
        /// <returns>The locked resource</returns>
        [RestInvoke("LOCK","/{resourceType}/{key}")]
        Object Lock(String resourceType, String key);

        /// <summary>
        /// Unlocks the specified resource from the service
        /// </summary>
        /// <param name="resourceType">The type of resource to be unlocked</param>
        /// <param name="key">The key of the resource</param>
        /// <returns>The unlocked resource</returns>
        [RestInvoke("UNLOCK","/{resourceType}/{key}")]
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
        /// Gets the specified versioned copy of the data
        /// </summary>
        /// <param name="resourceType">The type of resource</param>
        /// <param name="key">The key of the resource</param>
        /// <param name="versionKey">The version key to retrieve</param>
        /// <returns>The object as it existed at that version</returns>
        [Get("/{resourceType}/{key}/history/{versionKey}")]
        Object GetVersion(String resourceType, String key, String versionKey);

        /// <summary>
        /// Gets a complete history of changes made to the object (if supported)
        /// </summary>
        /// <param name="resourceType">The type of resource</param>
        /// <param name="key">The key of the object to retrieve the history for</param>
        /// <returns>The history</returns>
        [Get("/{resourceType}/{key}/history")]
        AmiCollection History(String resourceType, String key);

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
        /// Get the specific options supported for the 
        /// </summary>
        /// <param name="resourceType">The type of resource to get service options</param>
        [RestInvoke("OPTIONS", "/{resourceType}")]
        ServiceResourceOptions ResourceOptions(String resourceType);

    
    }
}
