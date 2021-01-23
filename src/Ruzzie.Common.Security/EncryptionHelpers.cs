using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Ruzzie.Common.Security
{
    /// <summary>
    /// Helper methods for symmetrical encryption.
    /// </summary>
    public static class EncryptionHelpers
    {
        private static readonly Encoding Encoding = Encoding.UTF8;

        /// <summary>
        /// Decrypts the string.
        /// </summary>
        /// <param name="encryptedString">The encrypted string (base 64 encoded).</param>
        /// <param name="keyOne">The key one.</param>
        /// <param name="keyTwo">The key two.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Value cannot be null or whitespace.
        /// or
        /// Value cannot be an empty collection.
        /// or
        /// Value cannot be an empty collection.
        /// </exception>
        public static string DecryptString(string encryptedString, byte[] keyOne, byte[] keyTwo)
        {
            if (keyOne == null)
            {
                throw new ArgumentNullException(nameof(keyOne));
            }
            if (keyTwo == null)
            {
                throw new ArgumentNullException(nameof(keyTwo));
            }
            if (string.IsNullOrWhiteSpace(encryptedString))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(encryptedString));
            }
            if (keyOne.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(keyOne));
            }
            if (keyTwo.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(keyTwo));
            }

            return DecryptBytes(Convert.FromBase64String(encryptedString), keyOne, keyTwo);
        }

        /// <summary>
        /// Decrypts the bytes.
        /// </summary>
        /// <param name="encryptedBytes">The encrypted bytes.</param>
        /// <param name="keyOne">The first key (base 64 encoded).</param>
        /// <param name="keyTwo">The second key (base 64 encoded).</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException">
        /// Value cannot be null or whitespace.
        /// or
        /// Value cannot be null or whitespace.
        /// or
        /// Value cannot be an empty collection.
        /// </exception>
        private static string DecryptBytes(byte[] encryptedBytes, byte[] keyOne, byte[] keyTwo)
        {
            if (encryptedBytes == null)
            {
                throw new ArgumentNullException(nameof(encryptedBytes));
            }
            if (encryptedBytes.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(encryptedBytes));
            }

            using (var t = CreateEncryptionAlgorithm(keyOne, keyTwo))
            {
                var decryptor = t.CreateDecryptor();
                MemoryStream? memoryStream = null;
                CryptoStream? cryptoStream = null;
                try
                {
                    memoryStream = new MemoryStream(encryptedBytes);
                    cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                    try
                    {
                        using (var streamReader = new StreamReader(cryptoStream, Encoding))
                        {

                            var result = streamReader.ReadToEnd();
                            return result;
                        }
                    }
                    finally
                    {
                        cryptoStream = null;
                        memoryStream = null;
                    }
                }
                finally
                {
                    if (cryptoStream != null)
                    {
                        cryptoStream.Dispose();
                    }

                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (memoryStream != null)
                    {
                        memoryStream.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Encrypts a string, based on 2 keys.
        /// </summary>
        /// <param name="inputString">The input string to encrypt.</param>
        /// <param name="keyOne">The first key.</param>
        /// <param name="keyTwo">The second key .</param>
        /// <returns>
        /// A base64 encoded encrypted string of the password.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Value cannot be an empty collection.
        /// </exception>
        /// <exception cref="ArgumentException">Value cannot be null or whitespace.</exception>
        public static string EncryptString(ref string? inputString, byte[] keyOne, byte[] keyTwo)
        {
            if (keyOne == null)
            {
                throw new ArgumentNullException(nameof(keyOne));
            }
            if (keyTwo == null)
            {
                throw new ArgumentNullException(nameof(keyTwo));
            }
            if (string.IsNullOrWhiteSpace(inputString))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(inputString));
            }
            if (keyOne.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(keyOne));
            }
            if (keyTwo.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(keyTwo));
            }

            try
            {
                return Convert.ToBase64String(EncryptBytes(Encoding.GetBytes(inputString), keyOne, keyTwo));
            }
            finally
            {
                inputString = null;
            }
        }

        /// <summary>
        /// Encrypts a string, based on 2 keys.
        /// </summary>
        /// <param name="inputString">The input string to encrypt.</param>
        /// <param name="keyOne">The first key.</param>
        /// <param name="keyTwo">The second key .</param>
        /// <returns>
        /// A base64 encoded encrypted string of the password.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Value cannot be an empty collection.
        /// </exception>
        /// <exception cref="ArgumentException">Value cannot be null or whitespace.</exception>
        public static string EncryptString(string inputString, byte[] keyOne, byte[] keyTwo)
        {
            if (keyOne == null)
            {
                throw new ArgumentNullException(nameof(keyOne));
            }

            if (keyTwo == null)
            {
                throw new ArgumentNullException(nameof(keyTwo));
            }

            if (string.IsNullOrWhiteSpace(inputString))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(inputString));
            }

            if (keyOne.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(keyOne));
            }

            if (keyTwo.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(keyTwo));
            }

            return Convert.ToBase64String(EncryptBytes(Encoding.GetBytes(inputString), keyOne, keyTwo));

        }

        /// <summary>
        /// Encrypt given bytes with a composite key
        /// </summary>
        /// <param name="bytesToEncrypt"></param>
        /// <param name="keyOne"></param>
        /// <param name="keyTwo"></param>
        /// <returns></returns>
        public static byte[] EncryptBytes(byte[] bytesToEncrypt, byte[] keyOne, byte[] keyTwo)
        {
            using (var t = CreateEncryptionAlgorithm(keyOne, keyTwo))
            {
                var encryptor = t.CreateEncryptor();
                MemoryStream? memoryStream = null;
                try
                {
                    memoryStream = new MemoryStream();
                    try
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(bytesToEncrypt, 0, bytesToEncrypt.Length);
                            cryptoStream.Clear();

                            var result = memoryStream.ToArray();

                            return result;
                        }
                    }
                    finally
                    {
                        memoryStream = null;
                    }
                }
                finally
                {
                    if (memoryStream != null)
                    {
                        memoryStream.Dispose();
                    }
                }
            }
        }

        private static RijndaelManaged CreateEncryptionAlgorithm(byte[] keyOne, byte[] keyTwo)
        {
            var initSettings = CreateInitSettings(keyOne, keyTwo);
            return new RijndaelManaged
                {
                    IV = initSettings.IV,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7,
                    KeySize = 256,
                    BlockSize = 128,
                    Key = initSettings.Key
                };
        }

        private readonly ref struct EncryptionInitSettings
        {
            public EncryptionInitSettings(byte[] iv, byte[] key)
            {
                if (iv == null)
                {
                    throw new ArgumentNullException(nameof(iv));
                }
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }
                if (iv.Length == 0)
                {
                    throw new ArgumentException("Value cannot be an empty collection.", nameof(iv));
                }
                if (key.Length == 0)
                {
                    throw new ArgumentException("Value cannot be an empty collection.", nameof(key));
                }
                IV = iv;
                Key = key;
            }
            // ReSharper disable once InconsistentNaming
            internal byte[] IV { get;  }
            internal byte[] Key { get; }
        }

        private static EncryptionInitSettings CreateInitSettings(byte[] keyOne, byte[] keyTwo, int keySizeInBytes = 32, int ivSizeInBytes = 16)
        {
            using (var db = new Rfc2898DeriveBytes(keyOne, AddPepper(keyTwo), 2048))
            {
                return new EncryptionInitSettings(db.GetBytes(ivSizeInBytes), db.GetBytes(keySizeInBytes));
            }
        }

        private static byte[] AddPepper(byte[] salt)
        {
            //TODO: FIX PEPPER USAGE
            byte[] pepper = { 1, 3, 253, 2, 8, 134, 65, 87 };
            byte[] result = new byte[salt.Length + pepper.Length];
            Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
            Buffer.BlockCopy(pepper, 0, result, salt.Length, pepper.Length);
            return result;
        }
    }
}