﻿/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 */
using System;

namespace SanteDB.Rest.OAuth
{
    /// <summary>
    /// Parses an Authorization style header value, that is a Header of the form {Scheme} {Value}.
    /// </summary>
    internal class AuthorizationHeader : IEquatable<AuthorizationHeader>, IEquatable<string>
    {
        public AuthorizationHeader()
        {

        }

        public AuthorizationHeader(string scheme, string value)
        {
            Scheme = scheme;
            Value = value;
        }

        /// <summary>
        /// Gets or sets the scheme of the Authorization Header.
        /// </summary>
        public string Scheme { get; set; }
        /// <summary>
        /// Gets or sets the value of the Authorization Header.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Checks if this instance matches the provided scheme.
        /// </summary>
        /// <param name="scheme">The scheme to check against.</param>
        /// <returns>True of the scheme matches (case insensitive). False otherwise.</returns>
        public bool IsScheme(string scheme) => Scheme.Equals(scheme, StringComparison.InvariantCultureIgnoreCase);

        public override string ToString()
        {
            return $"{Scheme} {Value}";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (null == this && null == obj)
            {
                return true;
            }
            else if (null == this || null == obj)
            {
                return false;
            }
            else
            {
                if (obj is string s)
                {
                    return Equals(s);
                }
                else if (obj is AuthorizationHeader ah)
                {
                    return Equals(ah);
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Attempts to parse a string into an <see cref="AuthorizationHeader"/> object.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="header">Out; the <see cref="AuthorizationHeader"/> object as the result of the parse operation if successful.</param>
        /// <returns>True if the string was parsed successfully.</returns>
        public static bool TryParse(string s, out AuthorizationHeader header)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                header = null;
                return false;
            }

            var parts = s.Split(new[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                header = new AuthorizationHeader(parts[0], parts[1]);
                return true;
            }

            header = null;
            return false;
        }

        public static AuthorizationHeader Parse(string s)
        {
            if (!TryParse(s, out var val))
            {
                throw new FormatException("Header format is malformed.");
            }

            return val;
        }

        public bool Equals(AuthorizationHeader other)
        {
            if (null == this && null == other)
            {
                return true;
            }
            else if (null == this || null == other)
            {
                return false;
            }
            else
            {
                return Scheme.Equals(other.Scheme) && Value.Equals(other.Value);
            }
        }

        public bool Equals(string other)
        {
            if (null == this && null == other)
            {
                return true;
            }
            else if (null == this || null == other)
            {
                return false;
            }
            else
            {
                return other.Equals(ToString());
            }
        }

        public static readonly string Scheme_Basic = "basic";
        public static readonly string Scheme_Bearer = "bearer";
        public static readonly string Scheme_Digest = "digest";

    }
}
