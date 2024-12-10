using RestSrvr;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Http;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Core.Templates;
using SanteDB.Core.Templates.Definition;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Data template resource handler
    /// </summary>
    public class DataTemplateResourceHandler : ChainedResourceHandlerBase, ICheckoutResourceHandler
    {
        private readonly IDataTemplateManagementService m_dataTemplateManagementService;
        private readonly IResourceCheckoutService m_checkoutService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public DataTemplateResourceHandler(IDataTemplateManagementService dataTemplateManagementService, ILocalizationService localizationService, IResourceCheckoutService checkoutService) : base(localizationService)
        {
            this.m_dataTemplateManagementService = dataTemplateManagementService;
            this.m_checkoutService = checkoutService;
        }

        /// <inheritdoc/>
        public override string ResourceName => typeof(DataTemplateDefinition).GetSerializationName();

        /// <inheritdoc/>
        public override Type Type => typeof(DataTemplateDefinition);

        /// <inheritdoc/>
        public override Type Scope => typeof(IAmiServiceContract);

        /// <inheritdoc/>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.CreateOrUpdate | ResourceCapabilityType.Update | ResourceCapabilityType.Delete | ResourceCapabilityType.Get | ResourceCapabilityType.GetVersion | ResourceCapabilityType.Search;
        
        /// <inheritdoc/>
        public object CheckIn(object key)
        {
            if (key is Guid kuuid || Guid.TryParse(key.ToString(), out kuuid))
            {
                if(!this.m_checkoutService.Checkin<DataTemplateDefinition>(kuuid))
                {
                    throw new ObjectLockedException();
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public object CheckOut(object key)
        {
            if (key is Guid kuuid || Guid.TryParse(key.ToString(), out kuuid))
            {
                if (!this.m_checkoutService.Checkout<DataTemplateDefinition>(kuuid))
                {
                    throw new ObjectLockedException();
                }
            }
            return null;
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AlterDataTemplates)]
        public override object Create(object data, bool updateIfExists)
        {
            if(data is DataTemplateDefinition dte)
            {
                return this.m_dataTemplateManagementService.AddOrUpdate(dte);
            }
            else if (data is IEnumerable<MultiPartFormData> multiForm)
            {
                var sourceFile = multiForm.FirstOrDefault(o => o.IsFile);
                using(var ms = new MemoryStream(sourceFile.Data))
                {
                    var definition = DataTemplateDefinition.Load(ms);
                    return this.m_dataTemplateManagementService.AddOrUpdate(definition);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(DataTemplateDefinition), data.GetType()));
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AlterDataTemplates)]
        public override object Delete(object key)
        {
            if(key is Guid keyUuid || Guid.TryParse(key.ToString(), out keyUuid))
            {
                return this.m_dataTemplateManagementService.Remove(keyUuid);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override object Get(object id, object versionId)
        {
            if(id is Guid keyUuid || Guid.TryParse(id.ToString(), out keyUuid))
            {
                var retVal = this.m_dataTemplateManagementService.Get(keyUuid);
                if(retVal == null)
                {
                    throw new KeyNotFoundException($"{this.ResourceName}/{id}");
                }
                if (Boolean.TryParse(RestOperationContext.Current.IncomingRequest.QueryString["_download"], out var download) && download)
                {
                    RestOperationContext.Current.OutgoingResponse.AddHeader("Content-Disposition", $"attachment; filename={retVal.Mnemonic}.xml");
                    var ms = new MemoryStream();
                    retVal.Save(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms;
                }
                else
                {
                    return retVal;
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc/>
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            // Parse 
            var query = QueryExpressionParser.BuildLinqExpression<DataTemplateDefinition>(queryParameters);
            return this.m_dataTemplateManagementService.Find(query);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AlterDataTemplates)]
        public override object Update(object data) => this.Create(data, true);
    }
}
