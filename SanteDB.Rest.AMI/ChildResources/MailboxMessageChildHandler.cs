using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// Mailbox message child handler
    /// </summary>
    public class MailboxMessageChildHandler : IApiChildResourceHandler
    {
        private readonly IMailMessageService m_mailMessageService;
        private readonly ILocalizationService m_localizationService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public MailboxMessageChildHandler(IMailMessageService mailMessageService, ILocalizationService localizationService)
        {
            this.m_mailMessageService = mailMessageService;
            this.m_localizationService = localizationService;
        }

        /// <summary>
        /// Get the property type
        /// </summary>
        public Type PropertyType => typeof(MailMessage);

        /// <summary>
        /// Get the capabilities of the mail message
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Delete | ResourceCapabilityType.Get | ResourceCapabilityType.Search;

        /// <summary>
        /// Scope binding
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Get the parent types
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(Mailbox) };

        /// <summary>
        /// Get the name
        /// </summary>
        public string Name => "Message";

        /// <summary>
        /// Add a mail message to the mailbox (moves it from another mailbox)
        /// </summary>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            if(!(item is MailMessage message))
            {
                throw new ArgumentOutOfRangeException(nameof(item));
            }
            if (!message.Key.HasValue)
            {
                throw new InvalidOperationException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND));
            }
            return this.m_mailMessageService.MoveMessage(message.Key.Value, scopingKey.ToString(), false);
        }

        /// <summary>
        /// Get the specified mail message
        /// </summary>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            if(!(key is Guid messageKey))
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }
            return this.m_mailMessageService.GetMessages(scopingKey.ToString()).Where(o => o.TargetKey == messageKey).FirstOrDefault()?.LoadProperty(o=>o.Target);
        }

        /// <summary>
        /// Query for all mail messages 
        /// </summary>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
           
            var linqExpression = QueryExpressionParser.BuildLinqExpression<MailboxMailMessage>(filter);
            return this.m_mailMessageService.GetMessages(scopingKey.ToString()).Where(linqExpression);
        }

        /// <summary>
        /// Delete a mail message
        /// </summary>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            
            if(!(key is Guid keyGuid))
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }

            return this.m_mailMessageService.DeleteMessage(scopingKey.ToString(), keyGuid);
        }
    }
}
