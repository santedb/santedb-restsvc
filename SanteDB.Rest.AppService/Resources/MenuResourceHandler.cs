using RestSrvr;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.AppService.Model;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AppService.Resources
{
    public class MenuResourceHandler : IApiResourceHandler
    {
        readonly IAppletManagerService _AppletManagerService;
        readonly IPolicyDecisionService _PolicyDecisionService;

        private class MenuEqualityComparer : IEqualityComparer<Menu>
        {
            public bool Equals(Menu x, Menu y)
            {
                return x?.Text == y?.Text && x?.Icon == y?.Icon;
            }

            public int GetHashCode(Menu obj)
            {
                return obj?.Text?.GetHashCode() ?? 1 ^ obj?.Icon?.GetHashCode() ?? 1;
            }
        }

        private static MenuEqualityComparer s_MenuEqualityComparer = new MenuEqualityComparer();

        public MenuResourceHandler(IAppletManagerService appletManagerService, IPolicyDecisionService policyDecisionService)
        {
            _AppletManagerService = appletManagerService;
            _PolicyDecisionService = policyDecisionService;
        }

        public string ResourceName => nameof(Menu);

        public Type Type => typeof(Menu);

        public Type Scope => typeof(IAppServiceContract);

        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get;

        public object Create(object data, bool updateIfExists)
        {
            throw new NotImplementedException();
        }

        public object Delete(object key)
        {
            throw new NotImplementedException();
        }

        [Demand(PermissionPolicyIdentifiers.Login)]
        public object Get(object id, object versionId)
        {
            throw new NotImplementedException();
        }

        [Demand(PermissionPolicyIdentifiers.Login)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            var rootmenus = _AppletManagerService?.Applets.SelectMany(a => a.Menus).OrderBy(m => m.Order).ToArray();
            var context = RestOperationContext.Current.IncomingRequest.QueryString.Get("context");

            var results = ProcessMenus(rootmenus, context);

            return new MemoryQueryResultSet<Menu>(results);
        }

        private List<Menu> ProcessMenus(IEnumerable<AppletMenu> appletMenus, string context)
        {
            if (null == appletMenus)
            {
                return null;
            }

            var results = new List<Model.Menu>();

            foreach(var appletmenu in appletMenus)
            {
                var menu = ProcessMenu(appletmenu, context);

                if (null == menu || (null == menu.Action && menu.MenuItems?.Count < 1))
                {
                    continue;
                }

                if (!results.Contains(menu, s_MenuEqualityComparer))
                {
                    results.Add(menu);
                }
            }

            return results;
        }

        private Model.Menu ProcessMenu(AppletMenu appletMenu, string context)
        {
            if (null == appletMenu)
            {
                return null;
            }

            var asset = _AppletManagerService.Applets.ResolveAsset(appletMenu.Asset, appletMenu.Manifest);
            var principal = AuthenticationContext.Current.Principal;

            //TODO: Why are we doing this?
            //if (appletMenu.Context != context || null != appletMenu.Asset && 
            if (asset?.Policies?.Any(p=> _PolicyDecisionService.GetPolicyOutcome(principal, p) == Core.Model.Security.PolicyGrantType.Deny) == true)
            {
                return null;
            }

            var menutext = appletMenu.GetText(principal.GetClaimValue(SanteDBClaimTypes.Language) ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

            var menu = new Menu
            {
                Action = appletMenu.Launch,
                Icon = appletMenu.Icon,
                Text = menutext
            };

            menu.MenuItems = ProcessMenus(appletMenu.Menus, context);

            return menu;
        }

        public object Update(object data)
        {
            throw new NotImplementedException();
        }
    }
}
