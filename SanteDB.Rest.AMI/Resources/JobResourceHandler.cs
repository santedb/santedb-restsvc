/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2021-8-5
 */

using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Jobs;
using SanteDB.Core.Model.AMI.Jobs;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler which handles the execution and enumeration of jobs
    /// </summary>
    public class JobResourceHandler : IServiceImplementation, IApiResourceHandler, IOperationalApiResourceHandler
    {


        // Property providers
        private ConcurrentDictionary<String, IApiChildOperation> m_operationHandlers = new ConcurrentDictionary<string, IApiChildOperation>();

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
            ResourceCapabilityType.Search | // find
            ResourceCapabilityType.Get;

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Job Resource Handler";

        /// <summary>
        /// Gets the operations for this job
        /// </summary>
        public IEnumerable<IApiChildOperation> Operations => this.m_operationHandlers.Values;

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(JobResourceHandler));

        // Localization service
        private readonly ILocalizationService m_localizationService;

        /// <summary>
        /// Initializes the job resource handler
        /// </summary>
        /// <param name="localizationService"></param>
        public JobResourceHandler(ILocalizationService localizationService)
        {
            this.m_localizationService = localizationService;
        }

        /// <summary>
        /// Create a new job instance
        /// </summary>
        public object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));
        }

        /// <summary>
        /// Get the specified job
        /// </summary>
        public object Get(object id, object versionId)
        {
            ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>().Demand(ApplicationServiceContext.Current.HostType == SanteDBHostType.Server ? PermissionPolicyIdentifiers.UnrestrictedAdministration : PermissionPolicyIdentifiers.AccessClientAdministrativeFunction);
            var manager = ApplicationServiceContext.Current.GetService<IJobManagerService>();
            var job = manager.GetJobInstance(Guid.Parse(id.ToString()));
            if (job == null)
            {
                this.m_tracer.TraceError($"No IJob of type {id} found");
                throw new KeyNotFoundException(this.m_localizationService.FormatString("error.rest.ami.noIJobType", new { param = id.ToString() }));
            }
            return new JobInfo(job, manager.GetJobSchedules(job));
        }

        /// <summary>
        /// Cancels a job
        /// </summary>
        public object Obsolete(object key)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));
        }

        /// <summary>
        /// Query for all jobs
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));
        }

        /// <summary>
        /// Query for jobs
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>().Demand(ApplicationServiceContext.Current.HostType == SanteDBHostType.Server ? PermissionPolicyIdentifiers.UnrestrictedAdministration : PermissionPolicyIdentifiers.AccessClientAdministrativeFunction);

            var manager = ApplicationServiceContext.Current.GetService<IJobManagerService>();
            var jobs = manager.Jobs;
            if (queryParameters.TryGetValue("name", out List<string> data))
                jobs = jobs.Where(o => o.Name.Contains(data.First()));
            totalCount = jobs.Count();
            return jobs.Skip(offset).Take(count).Select(o => new JobInfo(o, manager.GetJobSchedules(o)));
        }

        /// <summary>
        /// Update a job
        /// </summary>
        public object Update(object data)
        {
            // First try to cast data as JobInfo
            if (data is JobInfo)
            {
                ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>().Demand(ApplicationServiceContext.Current.HostType == SanteDBHostType.Server ? PermissionPolicyIdentifiers.UnrestrictedAdministration : PermissionPolicyIdentifiers.AccessClientAdministrativeFunction);

                var jobInfo = data as JobInfo;
                var jobManager = ApplicationServiceContext.Current.GetService<IJobManagerService>();
                if (jobManager == null)
                {
                    this.m_tracer.TraceError("No IJobManager configured");
                    throw new InvalidOperationException(this.m_localizationService.GetString("error.rest.ami.noIJobManager"));
                }    
                
                var job = jobManager.GetJobInstance(Guid.Parse(jobInfo.Key));
                if (job == null)
                {
                    this.m_tracer.TraceError($"Could not find job with ID {jobInfo.Key}");
                    throw new KeyNotFoundException(this.m_localizationService.FormatString("error.rest.ami.couldNotFindJob", new { param = jobInfo.Key }));
                }
                
                if(jobInfo.Schedule != null)
                {
                    this.m_tracer.TraceInfo("User setting job schedule for {0}", job.Name);
                    foreach(var itm in jobInfo.Schedule)
                    {
                        if(itm.IntervalSpecified)
                        {
                            jobManager.SetJobSchedule(job, itm.Interval);
                        }
                        else
                        {
                            jobManager.SetJobSchedule(job, itm.RepeatOn, itm.StartDate);
                        }
                    }
                }
                return jobInfo;
            }
            else
            {
                this.m_tracer.TraceError("Need to pass JobInfo to update a Job");
                throw new InvalidOperationException(this.m_localizationService.GetString("error.rest.ami.missingJobInfo"));
            }
        }

        /// <inheritdoc/>
        public void AddOperation(IApiChildOperation property)
        {
            this.m_operationHandlers.TryAdd(property.Name, property);
        }

        /// <inheritdoc/>
        public object InvokeOperation(object scopingEntityKey, string operationName, ParameterCollection parameters)
        {
            if(this.TryGetOperation(operationName, scopingEntityKey != null ? ChildObjectScopeBinding.Instance : ChildObjectScopeBinding.Class, out var handler))
            {
                return handler.Invoke(typeof(JobInfo), scopingEntityKey, parameters);
            }
            else
            {
                throw new KeyNotFoundException($"Operation {operationName} on JobInfo not found");
            }
        }

        /// <inheritdoc/>
        public bool TryGetOperation(string propertyName, ChildObjectScopeBinding bindingType, out IApiChildOperation operationHandler)
        {
            if(this.m_operationHandlers.TryGetValue(propertyName, out operationHandler))
            {
                return operationHandler.ScopeBinding.HasFlag(bindingType);
            }
            else
            {
                return false;
            }
        }
    }
}