using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ruzzie.Common.Validation
{
    public static class EmailValidation
    {
        private static readonly IdnMapping IdnMapping = new IdnMapping();

        private static readonly Regex DomainReplace =
            new Regex(@"(@)(.+)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

        private static readonly Regex IsEmailRegex = new Regex(
            @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
            @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));

        public static bool IsValidEmailAddress(this string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return false;
            }

            bool isValidEmail = true;
            try
            {
                email = DomainReplace.Replace(
                    email,
                    match =>
                    {
                        var emailPlusDomain = DomainMapper(match, out var isValidDomain);
                        if (isValidDomain == false)
                        {
                            isValidEmail = false;
                        }

                        return emailPlusDomain;
                    });
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }

            if (isValidEmail == false)
            {
                return false;
            }

            try
            {
                return IsEmailRegex.IsMatch(email);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private static string DomainMapper(Match match, out bool isValid)
        {
            // IdnMapping class with default property values.
            isValid = true;
            var domainName = match.Groups[2].Value;
            try
            {
                domainName = IdnMapping.GetAscii(domainName);
            }
            catch (ArgumentException)
            {
                isValid = false;
            }

            return match.Groups[1].Value + domainName;
        }
    }
}