using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Extensions;
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
    /// Operation for moving mail
    /// </summary>
    public class MoveMailOperation : IApiChildOperation
    {

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(MoveMailOperation));
        private readonly IMailMessageService m_mailMessageService;

        public const string DESTINATION_PARAMETER_NAME = "destination";
        public const string COPY_PARAMETER_NAME = "copy";
        public const string MESSAGE_ID_PARAMETER_NAME = "message";

        /// <summary>
        /// DI Ctor
        /// </summary>
        public MoveMailOperation(IMailMessageService mailMessageService)
        {
            this.m_mailMessageService = mailMessageService;
        }
        
        /// <inheritdoc/>
        public string Name => "move-mail";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(Mailbox) };

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(!parameters.TryGet(DESTINATION_PARAMETER_NAME, out Guid targetMailbox))
            {
                throw new ArgumentNullException(DESTINATION_PARAMETER_NAME);
            }
            if(!parameters.TryGet(MESSAGE_ID_PARAMETER_NAME, out Guid messageId))
            {
                throw new ArgumentNullException(MESSAGE_ID_PARAMETER_NAME);
            }

            _ = parameters.TryGet(COPY_PARAMETER_NAME, out bool isCopy);

            // Source mailbox
            if(!(scopingKey is Guid sourceMailboxKey) && scopingKey is String str)
            {
                sourceMailboxKey = this.m_mailMessageService.GetMailboxByName(str)?.Key ?? throw new KeyNotFoundException(String.Format(ErrorMessages.OBJECT_NOT_FOUND, $"{typeof(Mailbox).GetSerializationName()}/{sourceMailboxKey}")); 
            }

            return this.m_mailMessageService.MoveMessage(sourceMailboxKey, messageId, targetMailbox, isCopy);
        }
    }
}
