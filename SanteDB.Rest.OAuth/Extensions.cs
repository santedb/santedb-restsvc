﻿using Microsoft.IdentityModel.Tokens;
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
        public static void AddClaim(this Dictionary<string, object> claims, string type, string value)
        {
            if (null != value)
            {
                if (claims.ContainsKey(type))
                {
                    var val = claims[type];

                    if (val is string originalstr)
                    {
                        if (value != originalstr)
                        {
                            claims[type] = new List<string> { originalstr, value };
                        }
                    }
                    else if (val is List<string> lst)
                    {
                        if (!lst.Contains(value))
                        {
                            lst.Add(value);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Claim harmonization error: existing claims type is {val.GetType().Name} which is unrecognized.");
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

        public static string ComputeHash(this HashAlgorithm algorithm, string input)
        {
            var hashtext = ComputeHashInternal(algorithm, input);

            return Base64UrlEncoder.Encode(hashtext);
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

    }
}