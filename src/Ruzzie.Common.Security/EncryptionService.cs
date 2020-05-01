using System;
using Microsoft.AspNetCore.DataProtection;

namespace Ruzzie.Common.Security
{
    /// <summary>
    /// Encryption service for the application. The service stores the encryption keys as protected data.
    /// This way the keys only have to be read once and the service can be reused after initialization.
    /// </summary>
    /// <seealso cref="IEncryptionService" />
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _protectedKeyOne;
        private readonly byte[] _protectedKeyTwo;
        private readonly IDataProtector _protector;

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptionService"/> class.
        /// </summary>
        /// <param name="keys">The keys. These are cleared after calling the constructor.</param>
        /// <param name="protectionProvider">The protectionProvider to use for the DPAPI</param>
        public EncryptionService(EncryptionKeys? keys, IDataProtectionProvider protectionProvider)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            _protector = protectionProvider.CreateProtector("EncryptionService");

            try
            {
                _protectedKeyOne = _protector.Protect(keys.GetKeyOne());
                _protectedKeyTwo = _protector.Protect(keys.GetKeyTwo());
            }
            finally
            {
                // ReSharper disable once RedundantAssignment
                keys = null;
            }
        }

        /// <summary>
        /// Encrypts a given string.
        /// </summary>
        /// <param name="stringToEncrypt">The string to encrypt.</param>
        /// <returns>
        /// A base64 encoded encrypted string.
        /// </returns>
        /// <exception cref="ArgumentException">Value cannot be null or whitespace.</exception>
        public string EncryptString(ref string? stringToEncrypt)
        {
            if (string.IsNullOrWhiteSpace(stringToEncrypt))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(stringToEncrypt));
            }

            return EncryptionHelpers.EncryptString(ref stringToEncrypt,
                _protector.Unprotect(_protectedKeyOne),
                _protector.Unprotect(_protectedKeyTwo));
        }

        /// <summary>
        /// Encrypts a given string.
        /// </summary>
        /// <param name="stringToEncrypt">The string to encrypt.</param>
        /// <returns>
        /// A base64 encoded encrypted string.
        /// </returns>
        /// <exception cref="ArgumentException">Value cannot be null or whitespace.</exception>
        public string EncryptString(string stringToEncrypt)
        {
            if (string.IsNullOrWhiteSpace(stringToEncrypt))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(stringToEncrypt));
            }

            return EncryptionHelpers.EncryptString(stringToEncrypt,
                _protector.Unprotect(_protectedKeyOne),
                _protector.Unprotect(_protectedKeyTwo));
        }

        /// <summary>
        /// Decrypts the string.
        /// </summary>
        /// <param name="encryptedString">The base 64 encoded encrypted string to decrypt.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Value cannot be null or whitespace.</exception>
        public string DecryptString(string encryptedString)
        {
            if (string.IsNullOrWhiteSpace(encryptedString))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(encryptedString));
            }
            return EncryptionHelpers.DecryptString(encryptedString,
                _protector.Unprotect(_protectedKeyOne),
                _protector.Unprotect(_protectedKeyTwo));
        }
    }
}