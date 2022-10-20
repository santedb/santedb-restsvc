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
 * Date: 2022-5-30
 */
using Newtonsoft.Json;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Interop;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.AMI.Applet;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.AMI.Jobs;
using SanteDB.Core.Model.AMI.Logging;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.Attributes;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Model.Subscription;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;


namespace SanteDB.Core.Model.AMI.Collections
{
    /// <summary>
    /// Represents an administrative collection item.
    /// </summary>
    [AddDependentSerializers]
    [XmlType(nameof(AmiCollection), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(AmiCollection))]
    [XmlInclude(typeof(Entity))]
    [XmlInclude(typeof(ExtensionType))]
    [XmlInclude(typeof(MailMessage))]
    [XmlInclude(typeof(SecurityApplicationInfo))]
    [XmlInclude(typeof(SecurityDeviceInfo))]
    [XmlInclude(typeof(SecurityPolicyInfo))]
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
    public class AmiCollection : RestCollectionBase
    {
        public AmiCollection()
        {
        }

        public AmiCollection(IEnumerable<object> collectionItems) : base(collectionItems)
        {
        }

        public AmiCollection(IEnumerable<object> collectionItems, int offset, int totalCount) : base(collectionItems, offset, totalCount)
        {
        }
    }
}