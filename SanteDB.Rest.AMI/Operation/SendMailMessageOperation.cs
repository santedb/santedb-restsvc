using SanteDB.Core.Interop;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Send a mail message
    /// </summary>
    public class SendMailMessageOperation : IApiChildOperation
    {

        public const string TO_PARAMETER_NAME = "to";
        public const string SUBJECT_PARAMETER_NAME = "subject";
        public const string FLAG_PARAMETER_NAME = "flag";
        public const string BODY_PARAMETER_NAME = "body";

        private readonly ILocalizationService m_localizationService;
        private readonly IMailMessageService m_mailMessageService;

        /// <summary>
        /// Send mail
        /// </summary>
        public SendMailMessageOperation(ILocalizationService localizationService, IMailMessageService mailMessageService)
        {
            this.m_localizationService = localizationService;
            this.m_mailMessageService = mailMessageService;
        }

        /// <summary>
        /// Get the binding type
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <summary>
        /// Get the parent type
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(Mailbox) };

        /// <summary>
        /// Get the name 
        /// </summary>
        public string Name => "sendmail";

        /// <summary>
        /// Invoke
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            var mail = new MailMessage();
            if(parameters.TryGet(TO_PARAMETER_NAME, out string to))
            {
                mail.To = to;
            }
            else
            {
                throw new ArgumentNullException(TO_PARAMETER_NAME);
            }

            if(parameters.TryGet(SUBJECT_PARAMETER_NAME, out string subject))
            {
                mail.Subject = subject;
            }
            else
            {
                throw new ArgumentNullException(SUBJECT_PARAMETER_NAME);
            }

            if(parameters.TryGet(BODY_PARAMETER_NAME, out string body))
            {
                mail.Body = body;
            }
            else
            {
                throw new ArgumentNullException(BODY_PARAMETER_NAME);
            }

            if(parameters.TryGet(FLAG_PARAMETER_NAME, out MailMessageFlags flags))
            {
                mail.Flags = flags;
            }

            return this.m_mailMessageService.Send(mail);
        }
    }
}
