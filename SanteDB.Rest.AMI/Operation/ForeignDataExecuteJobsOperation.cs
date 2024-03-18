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
using SanteDB.Core.Data.Import;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Jobs;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Execute an import immediately
    /// </summary>
    public class ForeignDataExecuteJobsOperation : IApiChildOperation
    {
        private readonly IJobManagerService m_jobManagerService;
        private readonly IJobStateManagerService m_jobStateManager;
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(ForeignDataExecuteJobsOperation));

        /// <summary>
        /// DI constructor
        /// </summary>
        public ForeignDataExecuteJobsOperation(IServiceManager serviceManager, IJobManagerService jobManagerService, IJobStateManagerService jobStateManager)
        {
            this.m_jobManagerService = jobManagerService;
            this.m_jobStateManager = jobStateManager;

            try
            {
                if (jobManagerService.GetJobInstance(ForeignDataImportJob.JOB_ID) == null)
                {
                    var importJob = serviceManager.CreateInjected<ForeignDataImportJob>();
                    jobManagerService.AddJob(importJob, JobStartType.Never);
                    jobManagerService.SetJobSchedule(importJob, new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }, DateTime.Now.Date);
                }
            }
            catch (Exception e)
            {
                this.m_tracer.TraceWarning("Could not register foreign data importer job due to {0}", e);
            }
        }

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(IForeignDataSubmission) };

        /// <inheritdoc/>
        public string Name => "execute";

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            var job = this.m_jobManagerService.GetJobInstance(ForeignDataImportJob.JOB_ID);
            if (job == null)
            {
                throw new InvalidOperationException(String.Format(ErrorMessages.MISSING_JOB, ForeignDataImportJob.JOB_ID));
            }
            else if (this.m_jobStateManager.GetJobState(job).CurrentState != JobStateType.Running)
            {
                job.Run(this, EventArgs.Empty, null);
            }
            return null;

        }
    }
}
