using SanteDB.Core.Data.Import;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Alien;
using SanteDB.Core.Model.Parameters;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Schedule a foreign data import job
    /// </summary>
    public class ForeignDataScheduleJobOperation : IApiChildOperation
    {
        private readonly IForeignDataManagerService m_foreignDataManagerService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public ForeignDataScheduleJobOperation(IForeignDataManagerService foreignDataManagerService)
        {
            this.m_foreignDataManagerService = foreignDataManagerService;
        }

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(IForeignDataSubmission) };

        /// <inheritdoc/>
        public string Name => "schedule";

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (scopingKey is Guid uuid || Guid.TryParse(scopingKey.ToString(), out uuid))
            {
                return new ForeignDataInfo( this.m_foreignDataManagerService.Schedule(uuid));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(scopingKey), String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), scopingKey.GetType()));
            }
        }
    }
}
