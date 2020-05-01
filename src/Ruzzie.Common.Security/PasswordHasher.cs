using System;
using System.Security.Cryptography;

namespace Ruzzie.Common.Security
{
    /// <summary>
    /// Provides an interface for hashing passwords.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Returns a hashed representation of the supplied <paramref name="providedPassword"/>
        /// </summary>
        /// <param name="providedPassword">The password to hash.</param>
        /// <returns>A hashed representation of the supplied <paramref name="providedPassword"/></returns>
        string HashPassword(string providedPassword);
        /// <summary>
        /// Returns a <see cref="bool"/> indicating the result of a password hash comparison.
        /// </summary>
        /// <param name="hashedPassword">The hash value for a stored password.</param>
        /// <param name="providedPassword">The password supplied for comparison.</param>
        /// <returns>True when the they are equal otherwise false.</returns>
        bool VerifyHashedPassword(string hashedPassword, string providedPassword);
    }

    /// <summary>
    /// Default password hasher based on best practices.
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        private readonly byte[] _pepper;
        private const byte Version = 1;
        private const int SaltSize = 16;//128 bits
        private const int HashKeySize = 64;//512 bits
        private const int Iterations = 10000;

        /// <summary>
        /// Creates a new <see cref="PasswordHasher"/> with the provided pepper.
        /// </summary>
        /// <param name="pepper">The pepper bytes to use. These should always be the same in the same application.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public PasswordHasher(byte[] pepper)
        {
            if (ReferenceEquals(pepper, null))
            {
                throw new ArgumentNullException(nameof(pepper));
            }

            if (pepper.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(pepper));
            }

            _pepper = pepper;
        }

        /// <inheritdoc />
        public string HashPassword(string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(providedPassword))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(providedPassword));
            }

            Span<byte> saltNPepper = stackalloc byte[SaltSize+_pepper.Length];

            //Create salt
            using var crypto = new RNGCryptoServiceProvider();
            crypto.GetBytes(saltNPepper.Slice(0, SaltSize));

            //Add pepper
            _pepper.CopyTo(saltNPepper.Slice(SaltSize));

            using var hasher = new Rfc2898DeriveBytes(providedPassword, saltNPepper.ToArray(), Iterations, HashAlgorithmName.SHA512);

            var hashKey = hasher.GetBytes(HashKeySize);

            Span<byte> hashToStore = stackalloc byte[1 + SaltSize + HashKeySize];

            hashToStore[0] = Version;
            //Copy the Salt (without pepper)
            saltNPepper.Slice(0, SaltSize).CopyTo(hashToStore.Slice(1));
            //Copy the Hashed string
            hashKey.CopyTo(hashToStore.Slice(1 + SaltSize));

            return Convert.ToBase64String(hashToStore);
        }

        /// <inheritdoc />
        public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(hashedPassword));
            }

            if (string.IsNullOrEmpty(providedPassword))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(providedPassword));
            }

            Span<byte> hashedPasswordBytes = Convert.FromBase64String(hashedPassword);

            Span<byte> saltNPepper = stackalloc byte[SaltSize+_pepper.Length];

            //Get the salt from the stored hash
            hashedPasswordBytes.Slice(1, SaltSize).CopyTo(saltNPepper.Slice(0, SaltSize));

            //Add pepper
            _pepper.CopyTo(saltNPepper.Slice(SaltSize));

            //Now Hash the provided password with the stored Salt (& local Pepper)
            using var hasher = new Rfc2898DeriveBytes(providedPassword, saltNPepper.ToArray(), Iterations, HashAlgorithmName.SHA512);

            var providedHashKey = hasher.GetBytes(HashKeySize);

            var storedHashKey = hashedPasswordBytes.Slice(1 + SaltSize);

            return CryptographicOperations.FixedTimeEquals(providedHashKey, storedHashKey);
        }
    }
}