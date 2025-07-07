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
using Newtonsoft.Json;
using SanteDB.Core.Model.Attributes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Diagnostics
{
    /// <summary>
    /// Diagnostics report
    /// </summary>
    [JsonObject(nameof(DiagnosticReport)), XmlType(nameof(DiagnosticReport), Namespace = "http://santedb.org/ami/diagnostics"), ResourceName("Sherlock")]
    [XmlRoot(nameof(DiagnosticReport), Namespace = "http://santedb.org/ami/diagnostics")]
    public class DiagnosticReport : BaseEntityData, ITaggable
    {
        /// <summary>
        /// Default ctor for serialization
        /// </summary>
        public DiagnosticReport()
        {
            this.Tags = new List<DiagnosticReportTag>();
            this.Attachments = new List<DiagnosticAttachmentInfo>();
            this.Threads = new List<DiagnosticThreadInfo>();
        }

        /// <summary>
        /// Application configuration information
        /// </summary>
        [XmlElement("appInfo"), JsonProperty("appInfo")]
        public DiagnosticApplicationInfo ApplicationInfo { get; set; }

        /// <summary>
        /// Represents the most recent logs for the bug report
        /// </summary>
        [XmlElement("attachText", typeof(DiagnosticTextAttachment)), JsonProperty("attach")]
        [XmlElement("attachBin", typeof(DiagnosticBinaryAttachment))]
        public List<DiagnosticAttachmentInfo> Attachments { get; set; }

        /// <summary>
        /// Gets or sets any ticket related information
        /// </summary>
        [XmlElement("ticketId"), JsonProperty("ticketId")]
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the note
        /// </summary>
        [XmlElement("note"), JsonProperty("note")]
        public String Note { get; set; }

        /// <summary>
        /// Represents the submitter
        /// </summary>
        [XmlElement("submitter"), JsonProperty("submitter")]
        public UserEntity Submitter { get; set; }

        /// <summary>
        /// Thread information
        /// </summary>
        [XmlElement("thread"), JsonProperty("thread")]
        public List<DiagnosticThreadInfo> Threads { get; set; }

        /// <summary>
        /// Gets the tags
        /// </summary>
        [JsonProperty("tag"), XmlElement("tag")]
        public List<DiagnosticReportTag> Tags { get; set; }

        /// <summary>
        /// Gets the tags
        /// </summary>
        IEnumerable<ITag> ITaggable.Tags => this.Tags;

        /// <summary>
        /// Add a tag
        /// </summary>
        ITag ITaggable.AddTag(string tagKey, string tagValue)
        {
            var tag = new DiagnosticReportTag(tagKey, tagValue);
            this.Tags.Add(tag);
            return tag;
        }

        /// <summary>
        /// Get the specified tag
        /// </summary>
        public string GetTag(string tagKey) => this.Tags.Find(o => o.TagKey == tagKey)?.Value;

        /// <summary>
        /// Remove the specified tag
        /// </summary>
        public void RemoveTag(string tagKey) => this.Tags.RemoveAll(o => o.TagKey == tagKey);

        /// <summary>
        /// Remove tags matching <paramref name="predicate"/> from the tag collection
        /// </summary>
        public void RemoveAllTags(Predicate<ITag> predicate) => this.Tags.RemoveAll(predicate);

        /// <summary>
        /// Try to fetch the tag
        /// </summary>
        public bool TryGetTag(string tagKey, out ITag tag)
        {
            tag = this.Tags.FirstOrDefault(o => o.TagKey == tagKey);
            return tag != null;
        }
    }
}