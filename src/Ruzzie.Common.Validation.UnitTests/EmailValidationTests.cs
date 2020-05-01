using FluentAssertions;
using NUnit.Framework;
using PropertyAttribute = FsCheck.NUnit.PropertyAttribute;

namespace Ruzzie.Common.Validation.UnitTests
{
    [TestFixture]
    public class EmailValidationTests
    {
        [TestCase("j..s@@test.com", false)]
        [TestCase("js@test..com", false)]
        [TestCase("js@acme.中国",true)]
        [TestCase("ruzzie+jace@acme.com",true)]
        public void SmokeTest(string email, bool expected)
        {
            email.IsValidEmailAddress().Should().Be(expected);
        }

        [Property]
        public void NoExceptionShouldBeThrownPropertyTest(string email)
        {
            email.IsValidEmailAddress();
        }
    }
}
