using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Exceptions;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.AppService.Model;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml.XPath;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior (APP)
    /// </summary>
    [ServiceBehavior(Name = "APP", InstanceMode = ServiceInstanceMode.Singleton)]
    public class AppServiceBehavior : ResourceServiceBehaviorBase<IAppServiceContract>, IAppServiceContract
    {
        /// <summary>
        /// Instantiates a new instance of the behavior.
        /// </summary>
        public AppServiceBehavior()
            : this(
                  ApplicationServiceContext.Current.GetService<IConfigurationManager>(),
                  ApplicationServiceContext.Current.GetService<IServiceManager>(),
                  ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>(),
                  ApplicationServiceContext.Current.GetService<IPatchService>()
                  )
        { }
        

        /// <summary>
        /// DI constructor.
        /// </summary>
        /// <param name="configurationManager"></param>
        /// <param name="serviceManager"></param>
        /// <param name="policyEnforcementService"></param>
        /// <param name="patchService"></param>
        public AppServiceBehavior(IConfigurationManager configurationManager, IServiceManager serviceManager, IPolicyEnforcementService policyEnforcementService, IPatchService patchService = null)
            : base(AppServiceMessageHandler.ResourceHandler, new Tracer(nameof(AppServiceBehavior)), configurationManager, serviceManager, policyEnforcementService, patchService)
        {

        }

        /// <inheritdoc />
        protected override RestCollectionBase CreateResultCollection(IEnumerable<object> result, int offset, int totalCount)
            => new AppServiceCollection(result, offset, totalCount);
    }
}
