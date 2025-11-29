using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using SixSeven.Domain.Config;

namespace SixSeven.Infrastructure.Tests
{
    [TestFixture]
    public class JwtServiceTests
    {
        private JwtOptions _options = null!;
        private JwtService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _options = new JwtOptions
            {
                Secret = "this_is_a_test_secret_key_123456789",
                Issuer = "SixSeven.Test",
                Audience = "SixSeven.Client",
                ExpiryMinutes = 60
            };

            _service = new JwtService(Options.Create(_options));
        }

        [Test]
        public void GenerateToken_ValidInputs_ReturnsJwt()
        {
            var token = _service.GenerateToken("user-123", "test@example.com", "Admin");

            Assert.That(token, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void GenerateToken_CreatesValidJwtWithExpectedClaims()
        {
            var tokenString = _service.GenerateToken("user-123", "test@example.com", "Admin");

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            Assert.Multiple(() =>
            {
                Assert.That(token.Issuer, Is.EqualTo(_options.Issuer));
                Assert.That(token.Audiences.Single(), Is.EqualTo(_options.Audience));
                Assert.That(token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.NameIdentifier).Value,
                    Is.EqualTo("user-123"));
                Assert.That(token.Claims.Single(c => c.Type == ClaimTypes.Email).Value,
                    Is.EqualTo("test@example.com"));
                Assert.That(token.Claims.Single(c => c.Type == ClaimTypes.Role).Value,
                    Is.EqualTo("Admin"));
            });
        }

        [Test]
        public void GenerateToken_SetsExpirationBasedOnOptions()
        {
            var before = DateTime.UtcNow;

            var tokenString = _service.GenerateToken("user-123", "test@example.com", "Admin");

            var after = DateTime.UtcNow;

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            Assert.That(token.ValidTo, Is.GreaterThan(before));
            Assert.That(token.ValidTo, Is.LessThan(after.AddMinutes(_options.ExpiryMinutes + 1)));
        }

        [Test]
        public void GenerateToken_Twice_ProducesValidTokens()
        {
            var token1 = _service.GenerateToken("user-123", "test@example.com", "Admin");
            var token2 = _service.GenerateToken("user-123", "test@example.com", "Admin");

            Assert.That(token1, Is.Not.Empty);
            Assert.That(token2, Is.Not.Empty);
        }

    }
}
