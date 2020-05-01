using System;
using NUnit.Framework;

namespace Ruzzie.Common.Security.UnitTests
{
    [TestFixture]
    public class EncryptionHelpersTests
    {
        [TestCase("1")]
        [TestCase("p@assW0Rd!")]
        [TestCase("097898787870973209384lsjallkajsd9q08lasdh;)()90skdj98q7sjc")]
        public void EncryptAndDecryptSmokeTest(string inputStringToTest)
        {
            var copyOfInput = inputStringToTest;
            //Never store the keys this way, this is for testing only
            var keyOne = "ZjcxZGI2M2RkYTg3YmQ1YzM4ZWQ4MWEwN2FmZjU4OGNmMmFhM2I0YjUyNzNhYTEzN2I5N2E0OWEyMzVlNDhhMg==";
            var keyTwo = "OTA5MWI3NzRlMDkxYTY4ZjJmNTdlMDc0YTY0MzY1MGY1ZWMwZWE2ODc1NTU3Njk1NDU5NzUwMzFkNTUxYTNlYQ==";

            var encryptString = EncryptionHelpers.EncryptString(ref inputStringToTest, Convert.FromBase64String(keyOne), Convert.FromBase64String(keyTwo));

            Assert.That(encryptString.Length, Is.GreaterThan(0));
            string evilString = EncryptionHelpers.DecryptString(encryptString, Convert.FromBase64String(keyOne), Convert.FromBase64String(keyTwo));

            Assert.That(evilString, Is.EqualTo(copyOfInput));
        }

        [Test]
        public void EncryptHasDifferentOutputForDifferentKeys()
        {
            //Never store the keys this way, this is for testing only
            string inputStringToTest = "TEst123242";
            string inputString2ToTest = "TEst123242";

            var encryptStringOne = EncryptionHelpers.EncryptString(ref inputStringToTest, Convert.FromBase64String("YXNkYXNkYXNk"), Convert.FromBase64String("YXNkYXNkYXNk"));
            var encryptStringTwo = EncryptionHelpers.EncryptString(ref inputString2ToTest, Convert.FromBase64String("YXNkMzI0"), Convert.FromBase64String("YXNkMzI0"));

            Assert.That(encryptStringOne, Is.Not.EqualTo(encryptStringTwo).And.Length.AtLeast("TEst123242".Length));
        }
    }
}