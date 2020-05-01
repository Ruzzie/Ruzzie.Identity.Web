using System;
using FluentAssertions;
using FsCheck;
using NUnit.Framework;
using Ruzzie.Identity.Storage.Azure;

namespace Ruzzie.Identity.Storage.UnitTests.Azure
{
    [TestFixture]
    public class KeyGeneratorsTests
    {
        [TestFixture]
        public class CreateAlphaNumericKeyTests
        {
            [FsCheck.NUnit.Property]
            public void PropertySmokeTest(NonEmptyString input)
            {
                try
                {
                    var result = input.Item.CreateAlphaNumericPartitionKey();
                    (char.IsLetterOrDigit(result) || result == '-').Should().BeTrue();
                }
                catch (ArgumentException e)
                    when( e.Message.StartsWith("Stripped value has a length of 0. Provide a input string with at least 1 ASCII character"))
                {
                    //This is a valid case: for example: "%^%^#$#$    "
                }
                catch (ArgumentException e)
                    when( e.Message.StartsWith("Value cannot be null or whitespace."))
                {
                    //This is a valid case: only control characters or lots of whitespaces
                    // for example: "\n\r \esc \t    "
                }
            }

            [Test]
            public void ShouldTrimInput()
            {
                var input = " a";
                var result = input.CreateAlphaNumericPartitionKey();

                result.Should().Be('A');
            }

            [Test]
            public void ShouldTrimAfterStripInput()
            {
                var input = "_ a      ";
                var result = input.CreateAlphaNumericPartitionKey();

                result.Should().Be('A');
            }

            [TestCase("Acme Inc.  ", "ACME-INC")]
            [TestCase("  8.125 Bri&wst Ncnrp", "8125-BRIWST-NCNRP")]
            [TestCase("Pablo's Ijscobar", "PABLOS-IJSCOBAR")]
            [TestCase("Chiensûr", "CHIENSR")]
            [TestCase("So-de-Jus", "SO-DE-JUS")]
            public void WithPreserveSpacesAndTrimInput(string companyName, string expected)
            {
                companyName.CreateAlphaNumericKey(KeyGenerators.AlphaNumericKeyGenOptions.PreserveSpacesAsDashes |
                                                  KeyGenerators.AlphaNumericKeyGenOptions.TrimInput).Should().Be(expected);
            }

            [FsCheck.NUnit.Property]
            public void WithPreserveSpacesAndTrimInputPropertyTest(NonEmptyString input)
            {
                try
                {
                    var result = input.Item.CreateAlphaNumericPartitionKey(KeyGenerators.AlphaNumericKeyGenOptions.PreserveSpacesAsDashes |
                                                                        KeyGenerators.AlphaNumericKeyGenOptions.TrimInput);
                    (char.IsLetterOrDigit(result) || result == '-').Should().BeTrue();
                }
                catch (ArgumentException e)
                    when( e.Message.StartsWith("Stripped value has a length of 0. Provide a input string with at least 1 ASCII character"))
                {
                    //This is a valid case: for example: "%^%^#$#$    "
                }
                catch (ArgumentException e)
                    when( e.Message.StartsWith("Value cannot be null or whitespace."))
                {
                    //This is a valid case: only control characters or lots of whitespaces
                    // for example: "\n\r \esc \t    "
                }
            }
        }
    }
}