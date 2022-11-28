using Microsoft.IdentityModel.Tokens;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.OAuth.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SanteDB.Rest.OAuth
{
    /// <summary>
    /// Extension methods on <see cref="Dictionary{String, Object}"/>
    /// </summary>
    internal static class Extensions
    {

        public static void AddClaim(this Dictionary<string, object> claims, string type, object value)
        {
            if (null != value)
            {
                if (claims.ContainsKey(type))
                {
                    var val = claims[type];

                    if (val is List<object> lst)
                    {
                        if (!lst.Contains(value))
                        {
                            lst.Add(value);
                        }
                    }
                    else if (!val.Equals(value))
                    {
                            claims[type] = new List<Object> { val, value };
                    }
                    
                }
                else
                {
                    claims.Add(type, value);
                }
            }
        }

        public static void AddClaim(this Dictionary<string, object> claims, IClaim claim)
            => AddClaim(claims, claim?.Type, claim?.Value);

        public static IClaimsIdentity GetUserIdentity(this OAuthRequestContextBase context)
            => context?.UserIdentity ?? (context?.UserPrincipal?.Identity as IClaimsIdentity);

        public static IClaimsIdentity GetApplicationIdentity(this OAuthRequestContextBase context)
            => context?.ApplicationIdentity ?? (context?.ApplicationPrincipal?.Identity as IClaimsIdentity);

        public static IClaimsIdentity GetDeviceIdentity(this OAuthRequestContextBase context)
            => context?.DeviceIdentity ?? (context?.DevicePrincipal?.Identity as IClaimsIdentity);

        public static IClaimsIdentity GetPrimaryIdentity(this OAuthRequestContextBase context)
            => context?.GetUserIdentity() ?? context?.GetApplicationIdentity();

        public static string GetSessionId(this OAuthRequestContextBase context)
        {
            if (null == context?.Session?.Id)
            {
                return null;
            }

            if (context.Session.Id.Length == 16)
            {
                return new Guid(context.Session.Id).ToString();
            }

            return Convert.ToBase64String(context.Session.Id);
        }

        public static string ComputeHash(this HashAlgorithm algorithm, string input, int? numberOfBits = null)
        {
            var hashtext = ComputeHashInternal(algorithm, input);

            if (numberOfBits != null)
            {
                return Base64UrlEncoder.Encode(hashtext, 0, numberOfBits.Value / 8);
            }
            else
            {
                return Base64UrlEncoder.Encode(hashtext);
            }
        }

        public static bool VerifyHash(this HashAlgorithm algorithm, string input, string hash)
        {
            var hashtext1 = Base64UrlEncoder.DecodeBytes(hash);
            var hashtext2 = ComputeHashInternal(algorithm, input);

            return hashtext2.SequenceEqual(hashtext1);
        }

        private static byte[] ComputeHashInternal(HashAlgorithm algorithm, string input)
        {
            if (null == input)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var plaintext = System.Text.Encoding.UTF8.GetBytes(input);

            return algorithm.ComputeHash(plaintext);
        }

        public static void TracePolicyDemand(this Tracer tracer, Guid requestIdentifier, string permission, object securable)
        {
            tracer.TraceVerbose("{0}: Demand {1} from {2}", requestIdentifier, permission, securable.ToString());
        }

        public static string GetPurposeOfUse(this IEnumerable<IClaim> claims)
            => claims?.FirstOrDefault(c => c.Type == SanteDBClaimTypes.PurposeOfUse)?.Value;

        public static bool HasOverrideClaim(this IEnumerable<IClaim> claims)
            => claims?.Any(o => o.Type == SanteDBClaimTypes.SanteDBOverrideClaim) == true;

        public static bool HasOverrideScope(this IEnumerable<string> scopes)
            => scopes?.Any(o => o == PermissionPolicyIdentifiers.OverridePolicyPermission) == true;

        public static string GetLanguage(this IEnumerable<IClaim> claims, string defaultLanguage = null)
            => claims?.FirstOrDefault(c => c.Type == SanteDBClaimTypes.Language)?.Value ?? defaultLanguage;
    }
}
