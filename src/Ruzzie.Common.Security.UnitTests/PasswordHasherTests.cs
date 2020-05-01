using System;
using FluentAssertions;
using FsCheck;
using NUnit.Framework;

namespace Ruzzie.Common.Security.UnitTests
{
    [TestFixture]
    public class PasswordHasherTests
    {
        private readonly IPasswordHasher _hasher = new PasswordHasher(new byte[] {5, 21, 69, 246, 37, 10, 145, 58});

        [Test]
        public void HashPasswordSmokeTest()
        {
            //Arrange
            var providedPassword = "neverevereverusethispasswordreallynever";

            //Act
            var result = _hasher.HashPassword(providedPassword);

            //Assert
            var decodedBase64 = Convert.FromBase64String(result);
            decodedBase64.Length.Should().Be(81);
        }

        [Test]
        public void VerifyHashedPasswordTrueTest()
        {
            //Arrange
            var storedHashOfPassword = "AfG8z8D+sB4LOQBpI0aUBLwYxBpVdyxq81qBFnA5m1wACWPi26Fr0iph/c8QCL1xUfYX7qriSDEwhtz/ddqLPLeXQa/CYywoIMuZ9isof0bo";
            var providedUserInputPassword = "neverevereverusethispasswordreallynever";

            //Act & Assert
            _hasher.VerifyHashedPassword(storedHashOfPassword, providedUserInputPassword).Should().BeTrue();
        }

        [Test]
        public void VerifyHashedPasswordFalseTest()
        {
            //Arrange
            var storedHashOfPassword = "AZvJILr8RE+4Dfz+E38mI5PdRO2dfFMlndM9QKrgzeI8mXeAvb5lW8WOj9Zj0lOkzx88Om0sFrOX/oCicTZAnMMfMl0KCye7mVtG5IaYndR1";
            var providedUserInputPassword = "neverevereverusethispasswordreallynever";

            //Act & Assert
            _hasher.VerifyHashedPassword(storedHashOfPassword, providedUserInputPassword).Should().BeFalse();
        }

        [FsCheck.NUnit.Property]
        public void HashPasswordNoExceptionsPropertyTest(NonEmptyString providedPassword)
        {
            _hasher.HashPassword(providedPassword.Get + "z");
        }

        [FsCheck.NUnit.Property]
        public void VerifyHashedPasswordNoExceptionsPropertyTest(NonEmptyString providedPassword)
        {
            //Arrange
            var hashedPassword = "AfG8z8D+sB4LOQBpI0aUBLwYxBpVdyxq81qBFnA5m1wACWPi26Fr0iph/c8QCL1xUfYX7qriSDEwhtz/ddqLPLeXQa/CYywoIMuZ9isof0bo";

            //Act
            _hasher.VerifyHashedPassword(hashedPassword, providedPassword.Get).Should().BeFalse();
        }
    }
}