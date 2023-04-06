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
using NUnit.Framework;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.TestFramework;
using SanteDB.Rest.OAuth.Token;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SanteDB.Rest.OAuth.Test
{
    public class JwtClaimMapperTests
    {
        [OneTimeSetUp]
        public void Initialize()
        {
            _ = FirebirdSql.Data.FirebirdClient.FbCharset.Ascii.ToString();
            TestApplicationContext.TestAssembly = typeof(JwtClaimMapperTests).Assembly;
            TestApplicationContext.Initialize(TestContext.CurrentContext.TestDirectory);

            AuthenticationContext.EnterSystemContext();
        }

        [Test]
        public void ExternalTokenFormatTest()
        {
            var dut = new JwtClaimMapper();

            Assert.AreEqual(ClaimMapper.ExternalTokenTypeJwt, dut.ExternalTokenFormat);
        }

        [Test]
        public void TestMapToExternal()
        {
            var dut = new JwtClaimMapper();

            Assert.AreEqual(OAuthConstants.ClaimType_Actor, dut.MapToExternalClaimType(SanteDBClaimTypes.Actor));
            Assert.AreEqual(OAuthConstants.ClaimType_Name, dut.MapToExternalClaimType(SanteDBClaimTypes.DefaultNameClaimType));

            Assert.AreEqual("external-magic", dut.MapToExternalClaimType("external-magic"));

            Assert.Throws<ArgumentNullException>(() => dut.MapToExternalClaimType(null));
            Assert.Throws<ArgumentNullException>(() => dut.MapToExternalClaimType(string.Empty));
            Assert.DoesNotThrow(() => dut.MapToExternalClaimType("\0"));
            Assert.DoesNotThrow(() => dut.MapToExternalClaimType("name"));
        }

        [Test]
        public void TestMapToInternal()
        {
            var dut = new JwtClaimMapper();

            Assert.AreEqual(SanteDBClaimTypes.DefaultNameClaimType, dut.MapToInternalClaimType(OAuthConstants.ClaimType_Name));
            Assert.AreEqual(SanteDBClaimTypes.Actor, dut.MapToInternalClaimType(OAuthConstants.ClaimType_Actor));

            Assert.AreEqual("test-decode", dut.MapToInternalClaimType("test-decode"));

            Assert.DoesNotThrow(() => dut.MapToInternalClaimType(null));
            Assert.DoesNotThrow(() => dut.MapToInternalClaimType(string.Empty));
            Assert.DoesNotThrow(() => dut.MapToInternalClaimType("\0"));
            Assert.DoesNotThrow(() => dut.MapToInternalClaimType("name"));
        }

        [Test]
        public void TestMapToExternal_ListGrouping()
        {
            var dut = new JwtClaimMapper();

            var claims = new List<IClaim>();

            claims.Add(new SanteDBClaim(SanteDBClaimTypes.DefaultNameClaimType, "name"));
            claims.Add(new SanteDBClaim(SanteDBClaimTypes.DefaultNameClaimType, "name2"));

            var result = dut.MapToExternalIdentityClaims(claims);

            Assert.NotNull(result);
            Assert.AreEqual(1, result.Count);

            var val = result[OAuthConstants.ClaimType_Name];

            Assert.IsTrue(val is IList);

            var lst = val as IList;

            Assert.AreEqual(2, lst.Count);
        }

        [Test]
        public void TestMapToInternal_Degrouping()
        {
            var group = new List<string>();
            group.Add("name");
            group.Add("name2");

            var dut = new JwtClaimMapper();

            var input = new Dictionary<string, object>();
            input.Add(OAuthConstants.ClaimType_Name, group);

            var result = dut.MapToInternalIdentityClaims(input);

            Assert.NotNull(result);

            Assert.AreEqual(2, result.Count());

            foreach(var claim in result)
            {
                Assert.NotNull(claim);
                Assert.AreEqual(SanteDBClaimTypes.Name, claim.Type, "The claim type does not match the expected claim type. Claim {0}", claim);
            }
        }
    }
}
