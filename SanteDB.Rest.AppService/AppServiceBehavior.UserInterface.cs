/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 * User: fyfej
 * Date: 2023-3-10
 */
using RestSrvr;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Rest.AppService.Model;
using SanteDB.Rest.Common.Attributes;
using SanteDB.Rest.Common.Behavior;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior for UI
    /// </summary>
    public partial class AppServiceBehavior
    {

        private readonly MenuEqualityComparer m_menuEqualityComparer = new MenuEqualityComparer();
        private byte[] m_routes;

        /// <summary>
        /// Comparitor for <see cref="Menu"/>
        /// </summary>
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

        /// <inheritdoc/>
        public Dictionary<string, string[]> GetLocaleAssets()
        {
            return this.m_appletManagerService.Applets
                        .SelectMany(o => o.Locales)
                        .GroupBy(o => o.Code)
                        .ToDictionary(o => o.Key, o => o.SelectMany(a => a.Assets)
                        .ToArray());
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public List<Menu> GetMenus()
        {
            var rootmenus = this.m_appletManagerService?.Applets.SelectMany(a => a.Menus).OrderBy(m => m.Order).ToArray();
            var context = RestOperationContext.Current.IncomingRequest.QueryString.Get("context");
            return this.ProcessMenus(rootmenus, context).ToList();
        }

        /// <inheritdoc/>
        public Dictionary<string, object> GetState()
        {
            return new Dictionary<String, object>()
                    {
                        { "online", this.m_onlineState },
                        { "hdsi", this.m_hdsiState },
                        { "ami", this.m_amiState },
                        { "client_id", this.m_upstreamSettings?.LocalClientName },
                        { "device_id", this.m_upstreamSettings?.LocalDeviceName },
                        { "realm", this.m_upstreamSettings?.Realm.Host }
                    };
        }

        /// <inheritdoc/>
        public Stream GetRoutes()
        {
            if (this.m_routes == null)
            {
                // Calculate routes
                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter sw = new StreamWriter(ms))
                    {
                        IEnumerable<AppletAsset> viewStates = this.m_appletManagerService.Applets.ViewStateAssets
                            .Select(o => new { Asset = o, Html = (o.Content ?? this.m_appletManagerService.Applets.Resolver?.Invoke(o)) as AppletAssetHtml })
                            .GroupBy(o => o.Html.ViewState.Name)
                            .Select(g => g.OrderByDescending(o => o.Html.ViewState.Priority)
                            .First()
                            .Asset
                        ).ToList();

                        sw.WriteLine("// Generated Routes ");
                        sw.WriteLine("// Include States: ");
                        foreach (var vs in viewStates)
                        {
                            sw.WriteLine("// \t{0}", vs.Name);
                        }

                        sw.WriteLine("SanteDB = SanteDB || {}");
                        sw.WriteLine("SanteDB.UserInterface = SanteDB.UserInterface || {}");
                        sw.WriteLine("SanteDB.UserInterface.states = [");

                        // Collect routes
                        foreach (var itm in viewStates)
                        {
                            var htmlContent = (itm.Content ?? this.m_appletManagerService.Applets.Resolver?.Invoke(itm)) as AppletAssetHtml;
                            var viewState = htmlContent.ViewState;
                            sw.WriteLine($"{{ name: '{viewState.Name}', url: '{viewState.Route}', abstract: {viewState.IsAbstract.ToString().ToLower()}");
                            var displayName = htmlContent.GetTitle(AuthenticationContext.Current.Principal.GetClaimValue(SanteDBClaimTypes.Language) ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
                            if (!String.IsNullOrEmpty(displayName))
                                sw.Write($", displayName: '{displayName}'");
                            if (itm.Policies.Count > 0)
                                sw.Write($", demand: [{String.Join(",", itm.Policies.Select(o => $"'{o}'"))}] ");
                            if (viewState.View.Count > 0)
                            {
                                sw.Write(", views: {");
                                foreach (var view in viewState.View)
                                {
                                    sw.Write($"'{view.Name}' : {{ controller: '{view.Controller}', templateUrl: '{view.Route ?? itm.ToString()}'");
                                    var dynScripts = this.m_appletManagerService.Applets.GetLazyScripts(itm);
                                    if (dynScripts.Any())
                                    {
                                        sw.Write($", lazy: [ {String.Join(",", dynScripts.Select(o => $"'{this.m_appletManagerService.Applets.ResolveAsset(o.Reference, relativeAsset: itm)}'"))}  ]");
                                    }
                                    sw.WriteLine(" }, ");
                                }
                                sw.WriteLine("}");
                            }
                            sw.WriteLine("} ,");
                        }
                        sw.Write("];");
                    }
                    this.m_routes = ms.ToArray();
                }
            }
            RestOperationContext.Current.OutgoingResponse.ContentType = "application/javascript";
            return new MemoryStream(this.m_routes);
        }

        /// <inheritdoc/>
        public Guid GetUuid() => Guid.NewGuid();

        /// <inheritdoc/>
        public Stream GetWidget(string widgetId)
        {
            var widget = this.m_appletManagerService.Applets.WidgetAssets.Select(o => new { W = (o.Content ?? this.m_appletManagerService.Applets.Resolver(o)) as AppletWidget, A = o }).Where(o => o.W.Name == widgetId).ToArray();

            if (widget.Length == 0)
                throw new KeyNotFoundException(widgetId.ToString());
            else
                return new MemoryStream(this.m_appletManagerService.Applets.RenderAssetContent(widget.OrderByDescending(o => o.W.Priority).First().A, AuthenticationContext.Current.Principal.GetClaimValue(SanteDBClaimTypes.Language) ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));

        }

        /// <inheritdoc/>
        public List<AppletWidget> GetWidgets()
        {

            var httpq = RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString();
            var queryExpression = QueryExpressionParser.BuildLinqExpression<AppletWidget>(httpq).Compile();
            return this.m_appletManagerService.Applets.WidgetAssets
                .ToList()
                .Where(o => o.Policies?.All(p => this.m_policyEnforcementService.SoftDemand(p, AuthenticationContext.Current.Principal)) == true)
                .Select(o => (o.Content ?? this.m_appletManagerService.Applets.Resolver(o)) as AppletWidget)
                .Where(queryExpression)
                .GroupBy(o => o.Name)
                .Select(o => o.OrderByDescending(w => w.Priority).First())
                .OrderBy(w => w.Order)
                .ToList();
        }

        /// <summary>
        /// Process menus from <paramref name="appletMenus"/>
        /// </summary>
        private IEnumerable<Menu> ProcessMenus(IEnumerable<AppletMenu> appletMenus, string context)
        {
            if (null == appletMenus)
            {
                yield break;
            }

            foreach (var appletmenu in appletMenus)
            {
                var menu = ProcessMenu(appletmenu, context);

                if (null == menu || (null == menu.Action && menu.MenuItems?.Count < 1))
                {
                    continue;
                }

                yield return menu;
            }
        }

        /// <summary>
        /// Process a single menu
        /// </summary>
        private Model.Menu ProcessMenu(AppletMenu appletMenu, string context)
        {
            if (null == appletMenu || appletMenu.Context != context)
            {
                return null;
            }

            var asset = this.m_appletManagerService.Applets.ResolveAsset(appletMenu.Asset, appletMenu.Manifest);
            var principal = AuthenticationContext.Current.Principal;

            // Restricts the menu context based on policy and whether the menu points to an asset that does not exist
            //if (appletMenu.Context != context || null != appletMenu.Asset && 
            if (asset?.Policies?.Any() == true && asset?.Policies?.All(p => this.m_policyEnforcementService.SoftDemand(p, principal)) != true)
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

            menu.MenuItems = ProcessMenus(appletMenu.Menus, context).Distinct(m_menuEqualityComparer).ToList();

            return menu;
        }

        /// <inheritdoc/>
        [Obsolete("Consider using the id_token extensions instead of calling this method")]
        public Dictionary<String, Object> GetCurrentSessionInfo()
        {
            var retVal = new Dictionary<string, object>();
            var identity = AuthenticationContext.Current.Principal.Identity;
            if (RestOperationContext.Current.Data.TryGetValue(CookieAuthenticationBehavior.RestPropertyNameSession, out var sessionObject) && sessionObject is ISession ses)
            {
                retVal.Add("lang", ses.Claims.FirstOrDefault(o => o.Type == SanteDBClaimTypes.Language)?.Value);
                retVal.Add("scope", ses.Claims.Where(o => o.Type == SanteDBClaimTypes.SanteDBGrantedPolicyClaim).Select(o => o.Value).ToArray());
                retVal.Add("method", ses.Claims.FirstOrDefault(o => o.Type == SanteDBClaimTypes.AuthenticationMethod)?.Value);
                retVal.Add("claims", ses.Claims.GroupBy(o => o.Type).ToDictionary(o => o.Key, o => o.Select(a => a.Value).ToArray()));
                retVal.Add("exp", ses.NotAfter);
                retVal.Add("nbf", ses.NotBefore);

                var userEntity = this.m_securityRepositoryService?.GetUserEntity(identity);

                retVal.Add("entity", userEntity);
                retVal.Add("user", this.m_securityRepositoryService?.GetUser(identity));
                retVal.Add("displayName", userEntity?.Names?.FirstOrDefault()?.ToString() ?? identity.Name);
                retVal.Add("username", identity.Name);
                retVal.Add("authType", identity.AuthenticationType);
                return retVal;
            }
            return null;
        }
    }
}
