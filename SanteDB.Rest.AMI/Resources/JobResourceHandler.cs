﻿/*
 * Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Copyright (C) 2019 - 2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2020-1-1
 */
using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Jobs;
using SanteDB.Core.Model.AMI.Jobs;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler which handles the execution and enumeration of jobs
    /// </summary>
    public class JobResourceHandler : IApiResourceHandler
    {
        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName => "JobInfo";

        /// <summary>
        /// Gets the type of resource handler
        /// </summary>
        public Type Type => typeof(JobInfo);

        /// <summary>
        /// Gets the scoped service
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the capabilities
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Update | // start
            ResourceCapabilityType.Delete | // cancel
            ResourceCapabilityType.Search | // find
            ResourceCapabilityType.Get; 

        /// <summary>
        /// Create a new job instance
        /// </summary>
        
        public object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get the specified job
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public object Get(object id, object versionId)
        {
            var manager = ApplicationServiceContext.Current.GetService<IJobManagerService>();
            var job = manager.GetJobInstance(id.ToString());
            if (job == null)
                throw new KeyNotFoundException($"No IJob of type {id.ToString()} found");
            return new JobInfo(job);
        }

        /// <summary>
        /// Cancels a job
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public object Obsolete(object key)
        {
            var manager = ApplicationServiceContext.Current.GetService<IJobManagerService>();
            var job = manager.GetJobInstance(key.ToString());
            if (job == null)
                throw new KeyNotFoundException($"No IJob of type {key.ToString()} found");

            if (job.CanCancel)
                job.Cancel();
            else
                throw new InvalidOperationException("Job cannot be cancelled");

            // Last execution
            return new JobInfo(job);
        }

        /// <summary>
        /// Query for all jobs
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Query for jobs
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            var manager = ApplicationServiceContext.Current.GetService<IJobManagerService>();
            var jobs = manager.Jobs;
            if (queryParameters.TryGetValue("name", out List<string> data))
                jobs = jobs.Where(o => o.Name.Contains(data.First()));
            totalCount = jobs.Count();
            return jobs.Skip(offset).Take(count).Select(o => new JobInfo(o));
        }

        /// <summary>
        /// Update a job
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public object Update(object data)
        {
            // First try to cast dat as JobInfo
            if (data is JobInfo)
            {
                var jobInfo = data as JobInfo;
                var jobManager = ApplicationServiceContext.Current.GetService<IJobManagerService>();
                if (jobManager == null)
                    throw new InvalidOperationException("No IJobManager configured");

                var job = jobManager.GetJobInstance(jobInfo.Key);
                if (job == null)
                    throw new KeyNotFoundException($"Could not find job with ID {jobInfo.Key}");
                jobManager.StartJob(job, jobInfo.Parameters?.Select(o => o.Value).ToArray());
                jobInfo.State = job.CurrentState;
                return jobInfo;
            }
            else
                throw new InvalidOperationException("Need to pass JobInfo to start a Job");
        }
    }
}
