using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.Common.Performance
{
    /// <summary>
    /// Performance constants
    /// </summary>
    public static class PerformanceConstants
    {

        /// <summary>
        /// Gets the thread pooling performance counter
        /// </summary>
        public static readonly Guid RestPoolPerformanceCounter = new Guid("8877D692-1F71-4442-BDA1-056D3DB1A480");

        /// <summary>
        /// Gets the thread pooling performance counter
        /// </summary>
        public static readonly Guid RestPoolConcurrencyCounter = new Guid("8877D692-1F71-4442-BDA1-056D3DB1A481");

        /// <summary>
        /// Gets the thread pooling performance counter
        /// </summary>
        public static readonly Guid RestPoolWorkerCounter = new Guid("8877D692-1F71-4442-BDA1-056D3DB1A482");

        /// <summary>
        /// Gets the thread pooling performance counter for queue depth
        /// </summary>
        public static readonly Guid RestPoolDepthCounter = new Guid("8877D692-1F71-4442-BDA1-056D3DB1A485");
    }
}
