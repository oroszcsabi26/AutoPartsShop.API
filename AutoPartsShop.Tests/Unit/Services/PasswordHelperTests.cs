using AutoPartsShop.Core.Helpers;
using Xunit;

namespace AutoPartsShop.Tests.Unit.Services
{
    public class PasswordHelperTests
    {
        [Fact]
        public void HashPassword_ShouldReturnBase64String()
        {
            // Act
            var hash = PasswordHelper.HashPassword("MyPassword123");

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.Matches("^[A-Za-z0-9+/=]+$", hash); // base64 regex
        }

        [Fact]
        public void HashPassword_ShouldProduceSameHash_ForSameInput()
        {
            var h1 = PasswordHelper.HashPassword("secret");
            var h2 = PasswordHelper.HashPassword("secret");
            Assert.Equal(h1, h2);
        }

        [Fact]
        public void HashPassword_ShouldProduceDifferentHash_ForDifferentInputs()
        {
            var h1 = PasswordHelper.HashPassword("secret1");
            var h2 = PasswordHelper.HashPassword("secret2");
            Assert.NotEqual(h1, h2);
        }
    }
}
