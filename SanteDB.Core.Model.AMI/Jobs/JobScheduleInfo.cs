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
using SanteDB.Core.Configuration;
using SanteDB.Core.Jobs;
using System;
using System.Xml;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Jobs
{


    /// <summary>
    /// Job scheduling information
    /// </summary>
    [XmlType(nameof(JobScheduleInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(JobScheduleInfo))]
    public class JobScheduleInfo
    {

        /// <summary>
        /// Default ctor for serialization
        /// </summary>
        public JobScheduleInfo()
        {

        }

        /// <summary>
        /// Job scheduling information
        /// </summary>
        public JobScheduleInfo(IJobSchedule schedule)
        {
            this.Type = schedule.Type;
            this.Interval = schedule.Interval.GetValueOrDefault();
            this.IntervalXmlSpecified = schedule.Interval.HasValue;
            this.StartDate = schedule.StartTime;
            this.StopDate = schedule.StopTime.GetValueOrDefault();
            this.StopDateSpecified = schedule.StopTime.HasValue;
            this.RepeatOn = schedule.Days;
        }

        /// <summary>
        /// Gets or sets the schedule type
        /// </summary>
        [XmlElement("type"), JsonProperty("type")]
        public JobScheduleType Type { get; set; }

        /// <summary>
        /// Gets or sets the interval
        /// </summary>
        [XmlElement("interval"), JsonProperty("interval")]
        public String IntervalXml
        {
            get => XmlConvert.ToString(this.Interval);
            set => this.Interval = XmlConvert.ToTimeSpan(value);
        }

        /// <summary>
        /// Interval
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// Gets or sets whether the interval is specified
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public bool IntervalXmlSpecified { get; set; }

        /// <summary>
        /// Gets or sets the start date
        /// </summary>
        [XmlElement("start"), JsonProperty("start")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the stop date
        /// </summary>
        [XmlElement("stop"), JsonProperty("stop")]
        public DateTime StopDate { get; set; }

        /// <summary>
        /// Gets or sets whether the start/stop is specified
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public bool StopDateSpecified { get; set; }

        /// <summary>
        /// Gets or sets the repeat
        /// </summary>
        [XmlElement("repeat"), JsonProperty("repeat")]
        public DayOfWeek[] RepeatOn { get; set; }
    }
}