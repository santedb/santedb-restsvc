using SanteDB.Core.Model;
using SanteDB.Core.Services;
using SanteDB.Messaging.HDSI.Wcf;
using SanteDB.Rest.Common;
using System;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler base type that is always bound to HDSI
    /// </summary>
    /// <typeparam name="TData">The data which the resource handler is bound to</typeparam>
    public abstract class ResourceHandlerBase<TData> : SanteDB.Rest.Common.ResourceHandlerBase<TData>, INullifyResourceHandler, ICancelResourceHandler
        where TData : IdentifiedData
    {

        /// <summary>
        /// Gets the scope
        /// </summary>
        override public Type Scope => typeof(IHdsiServiceContract);

        /// <summary>
        /// Cancel the specified object
        /// </summary>
        public object Cancel(object key)
        {
            if (this.m_repository is ICancelRepositoryService)
                return (this.m_repository as ICancelRepositoryService).Cancel<TData>((Guid)key);
            else
                throw new NotSupportedException($"Repository for {this.ResourceName} does not support Cancel");
        }

        /// <summary>
        /// Nullify the specified object
        /// </summary>
        public object Nullify(object key)
        {
            if (this.m_repository is INullifyRepositoryService)
                return (this.m_repository as INullifyRepositoryService).Nullify<TData>((Guid)key);
            else
                throw new NotSupportedException($"Repository for {this.ResourceName} does not support Nullify");
        }
    }
}