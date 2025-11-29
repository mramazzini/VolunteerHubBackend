namespace SixSeven.Infrastructure.Tests
{
    [TestFixture]
    public class EncryptionServiceTests
    {
        private EncryptionService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _service = new EncryptionService();
        }

        [Test]
        public void HashPassword_ValidPassword_ReturnsHash()
        {
            var password = "SuperSecurePassword123!";

            var hash = _service.HashPassword(password);

            Assert.That(hash, Is.Not.Null.And.Not.Empty);
            Assert.That(hash, Is.Not.EqualTo(password));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void HashPassword_InvalidPassword_ThrowsArgumentException(string? invalidPassword)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.HashPassword(invalidPassword!));

            Assert.That(ex!.ParamName, Is.EqualTo("password"));
        }

        [Test]
        public void VerifyPassword_CorrectPassword_ReturnsTrue()
        {
            var password = "CorrectHorseBatteryStaple";

            var hash = _service.HashPassword(password);

            var result = _service.VerifyPassword(password, hash);

            Assert.That(result, Is.True);
        }

        [Test]
        public void VerifyPassword_IncorrectPassword_ReturnsFalse()
        {
            var hash = _service.HashPassword("RealPassword");

            var result = _service.VerifyPassword("WrongPassword", hash);

            Assert.That(result, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        public void VerifyPassword_NullOrEmptyHash_ReturnsFalse(string? invalidHash)
        {
            var result = _service.VerifyPassword("password", invalidHash!);

            Assert.That(result, Is.False);
        }

        [Test]
        public void VerifyPassword_NullPassword_ThrowsArgumentNullException()
        {
            var hash = _service.HashPassword("password");

            Assert.Throws<ArgumentNullException>(() =>
                _service.VerifyPassword(null!, hash));
        }
    }
}
