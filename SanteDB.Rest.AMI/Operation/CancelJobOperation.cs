using SanteDB.Core.Interop;
using SanteDB.Core.Jobs;
using SanteDB.Core.Model.AMI.Jobs;
using SanteDB.Core.Model.Parameters;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Execute job rest operation
    /// </summary>
    public class CancelJobOperation : IApiChildOperation
    {

        // Manager
        private readonly IJobManagerService m_manager;

        /// <summary>
        /// DI constructor
        /// </summary>
        public CancelJobOperation(IJobManagerService managerService)
        {
            this.m_manager = managerService;
        }

        /// <summary>
        /// Gets the scope binding for the object
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Gets the types this applies to
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(JobInfo) };

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name => "cancel";

        /// <summary>
        /// Invoke the operation
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (Guid.TryParse(scopingKey.ToString(), out var value))
            {
                var job = this.m_manager.GetJobInstance(value);
                if(job == null)
                {
                    throw new KeyNotFoundException($"Cannot find job {value}");
                }
                if(job.CanCancel)
                {
                    job.Cancel();
                }
                return null;
            }
            else
            {
                throw new InvalidOperationException("Job ID must be provided");
            }
        }
    }
}
