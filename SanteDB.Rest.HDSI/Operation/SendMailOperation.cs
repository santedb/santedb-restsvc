using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Send mail 
    /// </summary>
    public class SendMailOperation : IApiChildOperation
    {

        /// <summary>
        /// The parameter for subject
        /// </summary>
        public const string SUBJECT_PARAMETER_NAME = "subject";
        /// <summary>
        /// The parameter for body
        /// </summary>
        public const string BODY_PARAMETER_NAME = "body";
        /// <summary>
        /// The parameter for the TO address
        /// </summary>
        public const string TO_PARAMETER_NAME = "to";
        /// <summary>
        /// The parameter for the TO address
        /// </summary>
        public const string RCPTTO_PARAMETER_NAME = "rcptTo";
        /// <summary>
        /// The parameter for the flag
        /// </summary>
        public const string FLAG_PARAMETER_NAME = "flag";

        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(SendMailOperation));
        private readonly IMailMessageService m_mailMessageService;

        /// <summary>
        /// Mail message service DI ctor
        /// </summary>
        public SendMailOperation(IMailMessageService mailMessageService)
        {
            this.m_mailMessageService = mailMessageService;
        }

        /// <inheritdoc/>
        public string Name => "send-mail";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(Mailbox) };

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            
            if(!parameters.TryGet(BODY_PARAMETER_NAME, out String body))
            {
                throw new ArgumentNullException(BODY_PARAMETER_NAME);
            }
            if(!parameters.TryGet(SUBJECT_PARAMETER_NAME, out String subject))
            {
                throw new ArgumentNullException(SUBJECT_PARAMETER_NAME);
            }
            if(!parameters.TryGet(FLAG_PARAMETER_NAME, out int flags))
            {
                throw new ArgumentNullException(FLAG_PARAMETER_NAME);
            }
            if(!parameters.TryGet(RCPTTO_PARAMETER_NAME, out String[] toList))
            {
                throw new ArgumentNullException(RCPTTO_PARAMETER_NAME);
            }

            _ = parameters.TryGet(TO_PARAMETER_NAME, out string toLine);

            this.m_tracer.TraceInfo("Sending mail message on REST API to: {0}", String.Join(",", toList));

            // Construct the message
            return this.m_mailMessageService.Send(subject, body, (MailMessageFlags)flags, toLine, toList.Select(o => Guid.Parse(o)).ToArray());

        }
    }
}
