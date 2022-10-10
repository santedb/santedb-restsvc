using SanteDB.Core.Model.Parameters;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior
    /// </summary>
    public class AppServiceBehavior : IAppServiceContract
    {
        public object AssociationCreate(string resourceType, string id, string childResourceType, object body)
        {
            throw new NotImplementedException();
        }

        public object AssociationGet(string resourceType, string id, string childResourceType, string childKey)
        {
            throw new NotImplementedException();
        }

        public object AssociationRemove(string resourceType, string id, string childResourceType, string childKey)
        {
            throw new NotImplementedException();
        }

        public object AssociationSearch(string resourceType, string id, string childResourceType)
        {
            throw new NotImplementedException();
        }

        public object CheckIn(string resourceType, string id)
        {
            throw new NotImplementedException();
        }

        public object CheckOut(string resourceType, string id)
        {
            throw new NotImplementedException();
        }

        public object Create(string resourceType, object data)
        {
            throw new NotImplementedException();
        }

        public object CreateUpdate(string resourceType, string key, object data)
        {
            throw new NotImplementedException();
        }

        public object Delete(string resourceType, string key)
        {
            throw new NotImplementedException();
        }

        public object Get(string resourceType, string key)
        {
            throw new NotImplementedException();
        }

        public object InvokeMethod(string resourceType, string operationName, ParameterCollection body)
        {
            throw new NotImplementedException();
        }

        public object InvokeMethod(string resourceType, string id, string operationName, ParameterCollection body)
        {
            throw new NotImplementedException();
        }

        public object Search(string resourceType)
        {
            throw new NotImplementedException();
        }

        public object Update(string resourceType, string key, object data)
        {
            throw new NotImplementedException();
        }
    }
}
