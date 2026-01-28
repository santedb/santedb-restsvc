/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using Newtonsoft.Json;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Data.Import.Definition;
using SanteDB.Core.Interop;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.AMI.Alien;
using SanteDB.Core.Model.AMI.Applet;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.AMI.Jobs;
using SanteDB.Core.Model.AMI.Logging;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.AMI.Types;
using SanteDB.Core.Model.Attributes;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Model.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Collections
{
    /// <summary>
    /// Represents an administrative collection item.
    /// </summary>
    [AddDependentSerializersAttribute]
    [XmlType(nameof(AmiCollection), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(AmiCollection))]
    [XmlInclude(typeof(Entity))]
    [XmlInclude(typeof(ExtensionType))]
    [XmlInclude(typeof(MailMessage))]
    [XmlInclude(typeof(SecurityApplicationInfo))]
    [XmlInclude(typeof(SecurityDeviceInfo))]
    [XmlInclude(typeof(SecurityPolicyInfo))]
    [XmlInclude(typeof(ForeignDataMap))]
    [XmlInclude(typeof(ForeignDataInfo))]
    [XmlInclude(typeof(SecurityRoleInfo))]
    [XmlInclude(typeof(SecurityUser))]
    [XmlInclude(typeof(SecurityRole))]
    [XmlInclude(typeof(JobInfo))]
    [XmlInclude(typeof(SecurityDevice))]
    [XmlInclude(typeof(SecurityApplication))]
    [XmlInclude(typeof(SecurityUserInfo))]
    [XmlInclude(typeof(AuditSubmission))]
    [XmlInclude(typeof(AppletManifest))]
    [XmlInclude(typeof(AppletSolutionInfo))]
    [XmlInclude(typeof(AppletManifestInfo))]
    [XmlInclude(typeof(AmiTypeDescriptor))]
    [XmlInclude(typeof(DeviceEntity))]
    [XmlInclude(typeof(DiagnosticApplicationInfo))]
    [XmlInclude(typeof(DiagnosticAttachmentInfo))]
    [XmlInclude(typeof(DiagnosticBinaryAttachment))]
    [XmlInclude(typeof(DiagnosticTextAttachment))]
    [XmlInclude(typeof(DiagnosticEnvironmentInfo))]
    [XmlInclude(typeof(DiagnosticReport))]
    [XmlInclude(typeof(DiagnosticSyncInfo))]
    [XmlInclude(typeof(DiagnosticVersionInfo))]
    [XmlInclude(typeof(SubmissionInfo))]
    [XmlInclude(typeof(TfaMechanismInfo))]
    [XmlInclude(typeof(SubmissionResult))]
    [XmlInclude(typeof(MailMessage))]
    [XmlInclude(typeof(MailboxMailMessage))]
    [XmlInclude(typeof(Mailbox))]
    [XmlInclude(typeof(ApplicationEntity))]
    [XmlInclude(typeof(SubmissionRequest))]
    [XmlInclude(typeof(ServiceOptions))]
    [XmlInclude(typeof(X509Certificate2Info))]
    [XmlInclude(typeof(CodeSystem))]
    [XmlInclude(typeof(SecurityProvenance))]
    [XmlInclude(typeof(SubscriptionDefinition))]
    [XmlInclude(typeof(TfaMechanismInfo))]
    [XmlInclude(typeof(DiagnosticsProbe))]
    [XmlInclude(typeof(DiagnosticsProbeReading))]
    [XmlInclude(typeof(LogFileInfo))]
    [XmlInclude(typeof(RelationshipValidationRule))]
    public class AmiCollection : IResourceCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AmiCollection"/> class.
        /// </summary>
        public AmiCollection()
        {
            this.CollectionItem = new List<Object>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmiCollection"/> class
        /// with a specific list of collection items.
        /// </summary>
        public AmiCollection(IEnumerable<Object> collectionItems)
        {
            this.CollectionItem = new List<object>(collectionItems);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmiCollection"/> class
        /// with a specific list of collection items.
        /// </summary>
        public AmiCollection(IEnumerable<Object> collectionItems, int offset, int totalCount)
        {
            this.CollectionItem = new List<Object>(collectionItems);
            this.Offset = offset;
            this.Size = totalCount;
        }

        /// <summary>
        /// Gets or sets a list of collection items.
        /// </summary>
        [XmlElement("resource"), JsonProperty("resource")]
        public List<Object> CollectionItem { get; set; }

        /// <summary>
        /// Gets or sets the total offset.
        /// </summary>
        [XmlElement("offset"), JsonProperty("offset")]
        public int Offset { get; set; }

        /// <summary>
        /// Gets or sets the total collection size.
        /// </summary>
        [XmlElement("size"), JsonProperty("size")]
        public int Size { get; set; }

        /// <inheritdoc/>
        int? IResourceCollection.TotalResults => this.Size;

        /// <summary>
        /// Get the items 
        /// </summary>
        [JsonIgnore, XmlIgnore]
        IEnumerable<IIdentifiedResource> IResourceCollection.Item => this.CollectionItem.OfType<IIdentifiedResource>();

        /// <summary>
        /// Add annotations to al
        /// </summary>
        void IResourceCollection.AddAnnotationToAll(object annotation) => this.CollectionItem.OfType<IdentifiedData>().ToList().ForEach(o => o.AddAnnotation(annotation));
    }
}