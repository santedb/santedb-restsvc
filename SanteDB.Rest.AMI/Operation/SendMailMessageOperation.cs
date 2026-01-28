/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Send a mail message
    /// </summary>
    public class SendMailMessageOperation : IApiChildOperation
    {

        /// <summary>
        /// The name of the TO parameter
        /// </summary>
        public const string TO_PARAMETER_NAME = "to";
        /// <summary>
        /// The name of the subject
        /// </summary>
        public const string SUBJECT_PARAMETER_NAME = "subject";
        /// <summary>
        /// The name of the flags parameter
        /// </summary>
        public const string FLAG_PARAMETER_NAME = "flag";
        /// <summary>
        /// The name of the body parameter
        /// </summary>
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
            if (parameters.TryGet(TO_PARAMETER_NAME, out string to))
            {
                mail.To = to;
            }
            else
            {
                throw new ArgumentNullException(TO_PARAMETER_NAME);
            }

            if (parameters.TryGet(SUBJECT_PARAMETER_NAME, out string subject))
            {
                mail.Subject = subject;
            }
            else
            {
                throw new ArgumentNullException(SUBJECT_PARAMETER_NAME);
            }

            if (parameters.TryGet(BODY_PARAMETER_NAME, out string body))
            {
                mail.Body = body;
            }
            else
            {
                throw new ArgumentNullException(BODY_PARAMETER_NAME);
            }

            if (parameters.TryGet(FLAG_PARAMETER_NAME, out MailMessageFlags flags))
            {
                mail.Flags = flags;
            }

            return this.m_mailMessageService.Send(mail);
        }
    }
}
