using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler which retrieves and executes subscription definitions
    /// </summary>
    public class SubscriptionResourceHandler : IApiResourceHandler, IAuditEventSource
    {

        // Log tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(SubscriptionResourceHandler));

        /// <summary>
        /// Gets the name of the resource
        /// </summary>
        public string ResourceName => "Subscription";

        /// <summary>
        /// Gets the type of resource
        /// </summary>
        public Type Type => typeof(IdentifiedData);

        /// <summary>
        /// Gets the scope of the resource handler
        /// </summary>
        public Type Scope => typeof(IHdsiServiceContract);

        /// <summary>
        /// Gets the capabilities of this resource handler
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get;

        /// <summary>
        /// Fired when data was created
        /// </summary>
        public event EventHandler<AuditDataEventArgs> DataCreated;
        /// <summary>
        /// Fired after data was updated
        /// </summary>
        public event EventHandler<AuditDataEventArgs> DataUpdated;
        /// <summary>
        /// Fired after data is obsoleted
        /// </summary>
        public event EventHandler<AuditDataEventArgs> DataObsoleted;
        /// <summary>
        /// Fired when data was disclosed
        /// </summary>
        public event EventHandler<AuditDataDisclosureEventArgs> DataDisclosed;

        /// <summary>
        /// Create the specified resource
        /// </summary>
        public object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Retrieves the specified subscription results for the identified object
        /// </summary>
        public object Get(object id, object versionId)
        {
            var nvc = NameValueCollection.ParseQueryString(RestOperationContext.Current.IncomingRequest.Url.Query);
            nvc.Add("_id", id.ToString());

            int tc = 0, ofs = Int32.Parse(RestOperationContext.Current.IncomingRequest.QueryString["_offset"] ?? "0");
            return BundleUtil.CreateBundle(this.Query(nvc, ofs, Int32.Parse(RestOperationContext.Current.IncomingRequest.QueryString["_count"] ?? "100"), out tc).OfType<IdentifiedData>(), tc, ofs, true);
        }

        /// <summary>
        /// Obsoletes the specified key
        /// </summary>
        public object Obsolete(object key)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Perform the specified query 
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            int tr = 0;
            return this.Query(queryParameters, 0, 100, out tr);
        }

        /// <summary>
        /// Executes the specified subscription
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            try
            {
                // Attempt ot get the identifier of the subscription
                List<String> id = null;
                if (queryParameters.TryGetValue("_id", out id))
                {
                    Guid subscriptionKey = Guid.Parse(id.First()), queryId = Guid.Empty;
                    if (queryParameters.TryGetValue("_queryId", out id))
                        queryId = Guid.Parse(id.First());
                    queryParameters.Remove("_id");
                    queryParameters.Remove("_count");
                    queryParameters.Remove("_offset");
                    queryParameters.Remove("_queryId");
                    totalCount = 0;
                    return ApplicationServiceContext.Current.GetService<ISubscriptionExecutor>()?.Execute(subscriptionKey, queryParameters, offset, count, out totalCount, queryId).OfType<Object>();
                }
                else
                    throw new KeyNotFoundException("No subscription identifier provided");
            }
            catch(KeyNotFoundException)
            {
                throw;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error executing subscription: {0}", e);
                throw new Exception($"Error executing subscription logic: {e.Message}", e);
            }
        }

        /// <summary>
        /// Updates the specified key
        /// </summary>
        public object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}
