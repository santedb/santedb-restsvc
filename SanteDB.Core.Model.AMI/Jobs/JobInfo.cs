using Newtonsoft.Json;
using SanteDB.Core.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Jobs
{
    /// <summary>
    /// Represents job information
    /// </summary>
    [XmlType(nameof(JobInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(JobInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(JobInfo))]
    public class JobInfo : IAmiIdentified
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
        public JobInfo(IJob job)
        {
            if(job is IAmiIdentified ident)
            {
                this.Key = ident.Key;
                this.Tag = ident.Tag;
                this.ModifiedOn = ident.ModifiedOn;
            }
            else
            {
                this.Key = job.GetType().FullName;
                this.Tag = job.GetType().GetTypeInfo().Assembly.GetName().Version.ToString();
                this.ModifiedOn = ApplicationServiceContext.Current.StartTime;
            }
            this.Name = job.Name;
            this.CanCancel = job.CanCancel;
            this.State = job.CurrentState;
            this.Parameters = job.Parameters?.Select(o=>new JobParameter() { Key = o.Key, Type = o.Value.Name }).ToList();
            this.LastStart = job.LastStarted;
            this.LastFinish = job.LastFinished;
        }
        /// <summary>
        /// Gets or sets the key for the object
        /// </summary>
        [XmlElement("id"), JsonProperty("id")]
        public string Key { get; set; }

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
        public string Name { get; set;  }

        /// <summary>
        /// Gets whether the job can be cancelled
        /// </summary>
        [XmlElement("canCancel"), JsonProperty("canCancel")]
        public bool CanCancel { get; set; }
        /// <summary>
        /// Gets the current state of the job
        /// </summary>
        [XmlElement("state"), JsonProperty("state")]
        public JobStateType State { get; set;  }
        /// <summary>
        /// Gets the parameters for this job execution
        /// </summary>
        [XmlArray("parameters"), XmlArrayItem("set"), JsonProperty("parameters")]
        public List<JobParameter> Parameters { get; }

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
    }
}
