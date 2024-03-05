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
using SanteDB.Core.Diagnostics;
using System;
using System.Collections.Generic;

namespace SanteDB.Rest.Common.Performance
{
    /// <summary>
    /// Represents a thread pool performance counter
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class RestPoolPerformanceProbe : ICompositeDiagnosticsProbe
    {
        // Performance counters
        private readonly IDiagnosticsProbe[] m_performanceCounters;

        /// <summary>
        /// Generic performance counter
        /// </summary>
        private class QueueDepthProbe : DiagnosticsProbeBase<int>
        {
            /// <summary>
            /// Non pooled workers counter
            /// </summary>
            public QueueDepthProbe() : base("Waiting Requests", "Shows the number of HTTP requests waiting to be executed")
            {
            }

            /// <summary>
            /// Gets the identifier for the pool
            /// </summary>
            public override Guid Uuid => PerformanceConstants.RestPoolDepthCounter;

            /// <summary>
            /// Gets the value
            /// </summary>
            public override int Value
            {
                get
                {
                    RestSrvr.RestServerThreadPool.Current.GetWorkerStatus(out _, out _, out int waitingInQueue);
                    return waitingInQueue;
                }
            }

            /// <summary>
            /// Gets the unit
            /// </summary>
            public override string Unit => null;

        }

        /// <summary>
        /// Generic performance counter
        /// </summary>
        private class PooledWorkersProbe : DiagnosticsProbeBase<int>
        {
            /// <summary>
            /// Non pooled workers counter
            /// </summary>
            public PooledWorkersProbe() : base("Executing", "Shows the number of busy worker threads processing HTTP requests")
            {
            }

            /// <summary>
            /// Gets the identifier for the pool
            /// </summary>
            public override Guid Uuid => PerformanceConstants.RestPoolWorkerCounter;

            /// <summary>
            /// Gets the value
            /// </summary>
            public override int Value
            {
                get
                {
                    RestSrvr.RestServerThreadPool.Current.GetWorkerStatus(out int totalWorkers, out int availableWorkers, out _);
                    return totalWorkers - availableWorkers;
                }
            }

            /// <summary>
            /// Gets the unit
            /// </summary>
            public override string Unit => null;

        }

        /// <summary>
        /// Generic performance counter
        /// </summary>
        private class PoolConcurrencyProbe : DiagnosticsProbeBase<int>
        {

            /// <summary>
            /// Non pooled workers counter
            /// </summary>
            public PoolConcurrencyProbe() : base("Allocated Workers", "Shows the total number of threads which are allocated to processing HTTP requests")
            {

            }

            /// <summary>
            /// Gets the identifier for the pool
            /// </summary>
            public override Guid Uuid => PerformanceConstants.RestPoolConcurrencyCounter;

            /// <summary>
            /// Gets the value
            /// </summary>
            public override int Value
            {
                get
                {
                    //ThreadPool.GetMaxThreads(out int workerCount, out int completionPort);
                    RestSrvr.RestServerThreadPool.Current.GetWorkerStatus(out int workerCount, out _, out _);
                    return workerCount;
                }
            }

            /// <summary>
            /// Gets the unit
            /// </summary>
            public override string Unit => null;
        }

        /// <summary>
        /// Thread pool performance probe
        /// </summary>
        public RestPoolPerformanceProbe()
        {
            this.m_performanceCounters = new IDiagnosticsProbe[]
            {
                new PoolConcurrencyProbe(),
                new PooledWorkersProbe(),
                new QueueDepthProbe()
            };
        }

        /// <summary>
        /// Get the UUID of the thread pool
        /// </summary>
        public Guid Uuid => PerformanceConstants.RestPoolPerformanceCounter;

        /// <summary>
        /// Gets the value of the
        /// </summary>
        public IEnumerable<IDiagnosticsProbe> Value => this.m_performanceCounters;

        /// <summary>
        /// Gets thename of hte composite
        /// </summary>
        public string Name => "HTTP Workers";

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => "Monitors the server's ability to process HTTP requests";

        /// <summary>
        /// Gets the type of the performance counter
        /// </summary>
        public Type Type => typeof(Array);

        /// <summary>
        /// Gets the value
        /// </summary>
        object IDiagnosticsProbe.Value => this.Value;

        /// <summary>
        /// Unit of the probe
        /// </summary>
        public string Unit => null;
    }
}