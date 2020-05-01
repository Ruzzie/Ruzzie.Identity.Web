using System.IO;
using NUnit.Framework;

namespace Ruzzie.Common.Security.UnitTests
{
    [TestFixture]
    public class EncryptionKeysTest
    {
        [Test]
        public void ThrowsExceptionWhenTryToReadKeysASecondTime_KeyOne()
        {
            var keys = new EncryptionKeys("Zmlyc3RrZXk=", "Zmlyc3RrZXk=");

            // ReSharper disable once UnusedVariable
            var keyOne = keys.GetKeyOne();
            Assert.That(keyOne, Is.Not.Null);
            Assert.That(()=> keys.GetKeyOne().Length, Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void ThrowsExceptionWhenTryToReadKeysASecondTime_KeyTwo()
        {
            var keys = new EncryptionKeys("Zmlyc3RrZXk=", "Zmlyc3RrZXk=");

            // ReSharper disable once UnusedVariable
            var keyTwo = keys.GetKeyTwo();
            Assert.That(keyTwo, Is.Not.Null);
            Assert.That(() => keys.GetKeyTwo().Length, Throws.TypeOf<InvalidDataException>());
        }
    }
}