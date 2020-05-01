using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Ruzzie.Common.Security.UnitTests
{
    [TestFixture]
    public class EncryptionServiceTests
    {
        [Test]
        [TestCase("1")]
        [TestCase("p@assW0Rd!")]
        [TestCase("097898787870973209384lsjallkajsd9q08lasdh;)()90skdj98q7sjc")]
        public void SmokeTest(string inputStringToTest)
        {
            // add data protection services: meh!
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDataProtection();
            var services = serviceCollection.BuildServiceProvider();
            var dataProtectionProvider = services.GetService<IDataProtectionProvider>();

            var copyOfInput = inputStringToTest;
            //Never store the keys this way, this is for testing only
            var keyOne = "ZjcxZGI2M2RkYTg3YmQ1YzM4ZWQ4MWEwN2FmZjU4OGNmMmFhM2I0YjUyNzNhYTEzN2I5N2E0OWEyMzVlNDhhMg==";
            var keyTwo = "OTA5MWI3NzRlMDkxYTY4ZjJmNTdlMDc0YTY0MzY1MGY1ZWMwZWE2ODc1NTU3Njk1NDU5NzUwMzFkNTUxYTNlYQ==";
            EncryptionKeys keys = new EncryptionKeys(keyOne,keyTwo);
            IEncryptionService service = new EncryptionService(keys, dataProtectionProvider);

            var encryptString = service.EncryptString(ref inputStringToTest);

            Assert.That(encryptString.Length, Is.GreaterThan(0));
            string evilString = service.DecryptString(encryptString);

            Assert.That(evilString, Is.EqualTo(copyOfInput));
        }
    }
}