using NUnit.Framework;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.TestFramework;
using SanteDB.Rest.OAuth.Token;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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

            Assert.DoesNotThrow(() => dut.MapToExternalClaimType(null));
            Assert.DoesNotThrow(() => dut.MapToExternalClaimType(string.Empty));
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
    }
}
