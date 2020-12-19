using System;
using System.Collections.Generic;
using Ruzzie.FuzzyStrings;

namespace Ruzzie.Identity.Storage.Azure
{//todo: move to ruzzie.common.azure.storage or something
    public static class KeyGenerators
    {
        public static readonly IReadOnlyList<string> AllAlphaNumericPartitions = new[]
        {
            "-","0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J",
            "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
        };

        [Flags]
        public enum AlphaNumericKeyGenOptions
        {
            None,
            TrimInput,
            PreserveSpacesAsDashes
        }

        public static char CreateAlphaNumericPartitionKey(this string value, AlphaNumericKeyGenOptions options = AlphaNumericKeyGenOptions.None)
        {
            var strippedString = CreateAlphaNumericKey(value, options);

            return strippedString[0];
        }

        public static string CalculatePartitionKeyForAlphaNumericRowKey(this string rowKey)
        {
            return rowKey[0].ToString();
        }

        public static string CreateAlphaNumericKey(this string value, AlphaNumericKeyGenOptions options)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            }

            if ((options & AlphaNumericKeyGenOptions.TrimInput) != 0)
            {
                value = value.Trim();
            }

            if ((options & AlphaNumericKeyGenOptions.PreserveSpacesAsDashes) != 0)
            {
                value = value.Replace(' ', '-');
            }

            var strippedString = StringExtensions.StripAlternative(value).ToUpperInvariant().Trim();
            if (strippedString.Length == 0)
            {
                throw new ArgumentException(
                    "Stripped value has a length of 0. Provide a input string with at least 1 ASCII character", nameof(value));
            }

            return strippedString;
        }
    }
}