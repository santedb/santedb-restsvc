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
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Service diagnostics resource handler
    /// </summary>
    public class DiagnosticReportResourceHandler : IApiResourceHandler
    {
        private readonly IAppletManagerService m_appletManager;
        private readonly IAppletSolutionManagerService m_solutionManager;
        private readonly IServiceManager m_serviceManager;
        private readonly INetworkInformationService m_networkInformationService;
        private readonly IOperatingSystemInfoService m_operatingSystemInfoService;
        private readonly IDataPersistenceService<DiagnosticReport> m_persistenceService;

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName => "Sherlock";

        /// <inheritdoc/>
        public Type Type => typeof(DiagnosticReport);

        /// <summary>
        /// Gets the scope of the service
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the capabilities of this resource
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Get;

        /// <summary>
        /// DI ctor
        /// </summary>
        public DiagnosticReportResourceHandler(IAppletManagerService appletManager, IServiceManager serviceManager,
            INetworkInformationService networkInformationService, IOperatingSystemInfoService operatingSystemInfoService, IDataPersistenceService<DiagnosticReport> persistenceService = null, IAppletSolutionManagerService solutionManager = null)
        {
            this.m_appletManager = appletManager;
            this.m_solutionManager = solutionManager;
            this.m_serviceManager = serviceManager;
            this.m_networkInformationService = networkInformationService;
            this.m_operatingSystemInfoService = operatingSystemInfoService;
            this.m_persistenceService = persistenceService;
        }

        /// <summary>
        /// Create a diagnostic report with the configured
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public object Create(object data, bool updateIfExists)
        {
            if (data is DiagnosticReport dr)
            {
                return this.m_persistenceService.Insert(dr, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            }
            else
            {
                return this.m_persistenceService.Insert(this.Get(null, null) as DiagnosticReport, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            }
        }

        /// <inheritdoc/>
        public object Delete(object key)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get a diagnostic report from this service
        /// </summary>
        public object Get(object id, object versionId)
        {
            var retVal = new DiagnosticReport()
            {
                ApplicationInfo = new DiagnosticApplicationInfo(Assembly.GetEntryAssembly()),
                CreatedByKey = Guid.Parse(AuthenticationContext.SystemUserSid),
                Threads = Process.GetCurrentProcess()?.Threads.OfType<ProcessThread>().Select(o => new DiagnosticThreadInfo()
                {
                    Name = o.Id.ToString(),
                    CpuTime = o.TotalProcessorTime,
                    State = o.ThreadState.ToString(),
                    TaskInfo = o.ThreadState == ThreadState.Wait ? o.WaitReason.ToString() : "N/A"
                }).ToList()
            };
            retVal.ApplicationInfo.Solutions = this.m_solutionManager?.Solutions.Select(o => o.Meta).ToList();
            retVal.ApplicationInfo.Applets = this.m_appletManager.Applets.Select(o => o.Info).ToList();
            retVal.ApplicationInfo.EnvironmentInfo = new DiagnosticEnvironmentInfo()
            {
                Is64Bit = Environment.Is64BitProcess,
                OSVersion = String.Format("{0} v{1}", System.Environment.OSVersion.Platform, System.Environment.OSVersion.Version),
                OSType = this.m_operatingSystemInfoService.OperatingSystem,
                ProcessorCount = Environment.ProcessorCount,
                UsedMemory = GC.GetTotalMemory(false),
                Version = this.m_operatingSystemInfoService.VersionString,
                ManufacturerName = this.m_operatingSystemInfoService.ManufacturerName,
                MachineName = this.m_operatingSystemInfoService.MachineName
            };
            retVal.ApplicationInfo.ServiceInfo = this.m_serviceManager.GetServices().Select(o => new DiagnosticServiceInfo(o)).ToList();
            return retVal;
        }

        /// <inheritdoc/>
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}
