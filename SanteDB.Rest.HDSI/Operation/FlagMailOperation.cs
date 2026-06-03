using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Flag mail
    /// </summary>
    public class FlagMailOperation : IApiChildOperation
    {
        /// <summary>
        /// The parameter for the flag
        /// </summary>
        public const string FLAG_PARAMETER_NAME = "flag";

        /// <summary>
        /// The message to be flagged
        /// </summary>
        public const string MESSAGE_ID_PARAMETER_NAME = "message";
        /// <summary>
        /// Tracer
        /// </summary>
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(FlagMailOperation));
        /// <summary>
        /// Mail message service
        /// </summary>
        private readonly IMailMessageService m_mailMessageService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public FlagMailOperation(IMailMessageService mailMessageService)
        {
            this.m_mailMessageService = mailMessageService;
        }

        /// <inheritdoc/>
        public string Name => "flag-mail";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(Mailbox) };

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(!(scopingKey is Guid mailboxId))
            {
                throw new ArgumentOutOfRangeException(nameof(scopingKey));
            }
            if(!parameters.TryGet(MESSAGE_ID_PARAMETER_NAME, out Guid messageId))
            {
                throw new ArgumentNullException(MESSAGE_ID_PARAMETER_NAME);
            }
            if(!parameters.TryGet(FLAG_PARAMETER_NAME, out int flagSet))
            {
                throw new ArgumentNullException(FLAG_PARAMETER_NAME);
            }

            return this.m_mailMessageService.UpdateStatusFlag(mailboxId, messageId, (MailStatusFlags)flagSet);
        }
    }
}
