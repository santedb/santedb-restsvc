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
using SanteDB.Core.Jobs;
using SanteDB.Core.Model.Attributes;
using SanteDB.Core.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Jobs
{
    /// <summary>
    /// Represents job information
    /// </summary>
    [XmlType(nameof(JobInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(JobInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(JobInfo))]
    [ResourceSensitivity(ResourceSensitivityClassification.Administrative)]
    public class JobInfo : IAmiIdentified, IIdentifiedResource
    {
        /// <summary>
        /// Serialization info
        /// </summary>
        public JobInfo()
        {
        }

        /// <summary>
        /// Create job information from the job
        /// </summary>
        public JobInfo(IJobState job, IEnumerable<IJobSchedule> schedule)
        {
            if (job is IIdentifiedResource iir)
            {
                this.Key = iir.Key;
                this.Tag = iir.Tag;
                this.ModifiedOn = iir.ModifiedOn;
            }
            else
            {
                this.Key = job.Job.Id;
                this.Tag = job.GetType().Assembly.GetName().Version.ToString();
                this.ModifiedOn = ApplicationServiceContext.Current.StartTime;
            }
            this.Name = job.Job.Name;
            this.CanCancel = job.Job.CanCancel;
            this.State = job.CurrentState;
            this.Description = job.Job.Description;
            this.Parameters = job.Job.Parameters?.Select(o => new JobParameter() { Key = o.Key, Type = o.Value.Name }).ToList();
            this.LastStart = job.LastStartTime;
            this.LastFinish = job.LastStopTime;
            this.JobType = job.GetType().AssemblyQualifiedName;
            this.Schedule = schedule?.Select(o => new JobScheduleInfo(o)).ToList();
            this.Progress = job.Progress;
            this.StatusText = job.StatusText;
        }


        /// <summary>
        /// Get the key for the object
        /// </summary>
        [JsonProperty("id"), XmlElement("id")]
        public Guid? Key
        {
            get;
            set;
        }

        /// <inheritdoc/>
        [XmlIgnore, JsonIgnore]
        Object IAmiIdentified.Key
        {
            get => this.Key;
            set => this.Key = Guid.Parse(value.ToString());
        }

        /// <summary>
        /// Gets or sets the key for the object
        /// </summary>
        [XmlElement("schedule"), JsonProperty("schedule")]
        public List<JobScheduleInfo> Schedule { get; set; }

        /// <summary>
        /// Progress of the job
        /// </summary>
        [XmlElement("progress"), JsonProperty("progress")]
        public float Progress { get; set; }

        /// <summary>
        /// Gets or set the status text
        /// </summary>
        [XmlElement("status"), JsonProperty("status")]
        public string StatusText { get; set; }

        /// <summary>
        /// Gets the tag for the job
        /// </summary>
        [XmlAttribute("tag"), JsonProperty("tag")]
        public string Tag { get; }

        /// <summary>
        /// Gets the time that this was modified on
        /// </summary>
        [XmlAttribute("modified"), JsonProperty("modified")]
        public DateTimeOffset ModifiedOn { get; }

        /// <summary>
        /// Gets the name of the job
        /// </summary>
        [XmlElement("name"), JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets whether the job can be cancelled
        /// </summary>
        [XmlElement("canCancel"), JsonProperty("canCancel")]
        public bool CanCancel { get; set; }

        /// <summary>
        /// Gets the current state of the job
        /// </summary>
        [XmlElement("state"), JsonProperty("state")]
        public JobStateType State { get; set; }

        /// <summary>
        /// Gets the parameters for this job execution
        /// </summary>
        [XmlArray("parameters"), XmlArrayItem("set"), JsonProperty("parameters")]
        public List<JobParameter> Parameters { get; set; }

        /// <summary>
        /// Last execution
        /// </summary>
        [XmlElement("lastStart"), JsonProperty("lastStart")]
        public DateTime? LastStart { get; set; }

        /// <summary>
        /// Last execution
        /// </summary>
        [XmlElement("lastFinish"), JsonProperty("lastFinish")]
        public DateTime? LastFinish { get; set; }

        /// <summary>
        /// Get or sets the job type
        /// </summary>
        [XmlElement("jobType"), JsonProperty("jobType")]
        public string JobType { get; set; }

        /// <summary>
        /// Gets or sets the description of the job
        /// </summary>
        [XmlElement("description"), JsonProperty("description")]
        public string Description { get; set; }
    }
}