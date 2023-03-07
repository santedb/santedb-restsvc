using RestSrvr;
using System.Collections.Specialized;

namespace SanteDB.Rest.OAuth.Model
{
    public class OAuthSessionRequestContext : OAuthRequestContextBase
    {
        public OAuthSessionRequestContext(RestOperationContext operationContext) : base(operationContext)
        {
        }

        public OAuthSessionRequestContext(RestOperationContext operationContext, NameValueCollection formFields) : base(operationContext, formFields)
        {

        }


    }
}
