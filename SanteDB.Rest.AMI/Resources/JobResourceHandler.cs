/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Jobs;
using SanteDB.Core.Model.AMI.Jobs;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler which handles the execution and enumeration of jobs
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class JobResourceHandler : IServiceImplementation, IApiResourceHandler, IOperationalApiResourceHandler
    {

        // Job manager
        private readonly IJobManagerService m_jobManager;


        // State service
        private readonly IJobStateManagerService m_jobStateService;

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(JobResourceHandler));

        // Localization service
        private readonly ILocalizationService m_localizationService;
        private readonly IJobScheduleManager m_jobScheduleManager;

        // Property providers
        private ConcurrentDictionary<String, IApiChildOperation> m_operationHandlers = new ConcurrentDictionary<string, IApiChildOperation>();

        /// <summary>
        /// DI constructor
        /// </summary>
        public JobResourceHandler(ILocalizationService localizationService,
            IJobManagerService jobManagerService,
            IJobStateManagerService jobStateManagerService,
            IJobScheduleManager jobScheduleManager)
        {
            this.m_jobManager = jobManagerService;
            this.m_jobStateService = jobStateManagerService;
            this.m_localizationService = localizationService;
            this.m_jobScheduleManager = jobScheduleManager;
        }

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
            ResourceCapabilityType.Get |
            ResourceCapabilityType.Create |
            ResourceCapabilityType.Delete;

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Job Resource Handler";

        /// <summary>
        /// Gets the operations for this job
        /// </summary>
        public IEnumerable<IApiChildOperation> Operations => this.m_operationHandlers.Values;


        /// <summary>
        /// Create a new job instance - registers it with the job manager
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.RegisterSystemJob)]
        public object Create(object data, bool updateIfExists)
        {
            if (data is TypeReferenceConfiguration trc)
            {
                var job = this.m_jobManager.RegisterJob(trc.Type);
                return new JobInfo(this.m_jobStateService.GetJobState(job), null);
            }
            else if (data is JobInfo ji)
            {
                var job = this.m_jobManager.RegisterJob(Type.GetType(ji.JobType));
                return new JobInfo(this.m_jobStateService.GetJobState(job), null);
            }
            else
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(TypeReferenceConfiguration), data.GetType()));
            }
        }

        /// <summary>
        /// Get the specified job
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadSystemJobs)]
        public object Get(object id, object versionId)
        {
            ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>().Demand(PermissionPolicyIdentifiers.ReadSystemJobs);
            var job = this.m_jobManager.GetJobInstance(Guid.Parse(id.ToString()));
            if (job == null)
            {
                this.m_tracer.TraceError($"No IJob of type {id} found");
                throw new KeyNotFoundException(this.m_localizationService.GetString("error.rest.ami.noIJobType", new { param = id.ToString() }));
            }

            return new JobInfo(this.m_jobStateService.GetJobState(job), this.m_jobScheduleManager.Get(job));
        }

        /// <summary>
        /// Cancels a job
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedJobManagement)]
        public object Delete(object key)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));
        }

        /// <summary>
        /// Query for all jobs
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadSystemJobs)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>().Demand(PermissionPolicyIdentifiers.ReadSystemJobs);

            // Is the user looking for unconfigured jobs?
            if (Boolean.TryParse(queryParameters["_unconfigured"], out var b) && b)
            {
                return new MemoryQueryResultSet(this.m_jobManager.GetAvailableJobs().Select(o =>
                {
                    if (!this.m_jobManager.IsJobRegistered(o))
                    {
                        return new TypeReferenceConfiguration(o);
                    }
                    return null;
                }).OfType<TypeReferenceConfiguration>());
            }
            else
            {
                var jobs = this.m_jobManager.Jobs;
                if (queryParameters.TryGetValue("name", out var data))
                {
                    var query = data.First();
                    if (query.StartsWith("~"))
                    {
                        jobs = jobs.Where(o => o.Name.ToLowerInvariant().Contains(query.Substring(1).ToLowerInvariant()));
                    }
                    else
                    {
                        jobs = jobs.Where(o => o.Name.Equals(query, StringComparison.OrdinalIgnoreCase));
                    }
                }

                return new MemoryQueryResultSet(jobs.Select(o => new JobInfo(this.m_jobStateService.GetJobState(o), this.m_jobScheduleManager.Get(o))).ToArray());
            }
        }

        /// <summary>
        /// Update a job
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterSystemJobSchedule)]
        public object Update(object data)
        {
            // First try to cast data as JobInfo
            if (data is JobInfo)
            {
                ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>().Demand(PermissionPolicyIdentifiers.AlterSystemJobSchedule);

                var jobInfo = data as JobInfo;
                var job = this.m_jobManager.GetJobInstance(jobInfo.Key.GetValueOrDefault());
                if (job == null)
                {
                    this.m_tracer.TraceError($"Could not find job with ID {jobInfo.Key}");
                    throw new KeyNotFoundException(this.m_localizationService.GetString("error.rest.ami.couldNotFindJob", new { param = jobInfo.Key }));
                }

                if (jobInfo.Schedule != null)
                {
                    this.m_jobScheduleManager.Clear(job);
                    this.m_tracer.TraceInfo("User setting job schedule for {0}", job.Name);
                    foreach (var itm in jobInfo.Schedule)
                    {
                        if (itm.Type == Core.Configuration.JobScheduleType.Interval)
                        {
                            this.m_jobScheduleManager.Add(job, itm.Interval, itm.StopDateSpecified ? (DateTime?)itm.StopDate : null);
                        }
                        else
                        {
                            this.m_jobScheduleManager.Add(job, itm.RepeatOn, itm.StartDate, itm.StopDateSpecified ? (DateTime?)itm.StopDate : null);
                        }
                    }
                }
                else
                {
                    this.m_tracer.TraceInfo("User clearing job schedule for {0}", job.Name);
                    this.m_jobScheduleManager.Clear(job);
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
            if (this.TryGetOperation(operationName, scopingEntityKey != null ? ChildObjectScopeBinding.Instance : ChildObjectScopeBinding.Class, out var handler))
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
            if (this.m_operationHandlers.TryGetValue(propertyName, out operationHandler))
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