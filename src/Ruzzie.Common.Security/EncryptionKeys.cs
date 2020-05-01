using System;
using System.IO;

namespace Ruzzie.Common.Security
{
    /// <summary>
    /// A type that hold a Single use encryption keys
    /// </summary>
    public class EncryptionKeys
    {
        private byte[]? _keyOne;
        private byte[]? _keyTwo;

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptionKeys"/> class.
        /// After reading the keys once they are cleared and an invalid data exception is thrown.
        /// </summary>
        /// <param name="keyOne">The first key base64 encoded.</param>
        /// <param name="keyTwo">The second key base64 encoded.</param>
        /// <remarks>Will clear the keys after one read.</remarks>
        public EncryptionKeys(string keyOne, string keyTwo)
        {
            if (string.IsNullOrWhiteSpace(keyOne))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(keyOne));
            }
            if (string.IsNullOrWhiteSpace(keyTwo))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(keyTwo));
            }

            _keyOne = Convert.FromBase64String(keyOne);
            _keyTwo = Convert.FromBase64String(keyTwo);
        }

        /// <summary>
        /// Gets the key one.
        /// </summary>
        /// <returns>
        ///     The key one.
        /// </returns>
        /// <exception cref="InvalidDataException">The key was already read once. Please create a new instance of the EncryptionKeys</exception>
        public byte[] GetKeyOne()
        {
            if (_keyOne != null)
            {
                try
                {
                    var res = _keyOne;
                    return res;
                }
                finally
                {
                    _keyOne = null;
                }
            }
            throw new InvalidDataException("The key was already read once. Please create a new instance of the EncryptionKeys");
        }

        /// <summary>
        /// Gets the key two.
        /// </summary>
        /// <returns>
        ///     The key two.
        /// </returns>
        /// <exception cref="InvalidDataException">The key was already read once. Please create a new instance of the EncryptionKeys</exception>
        public byte[] GetKeyTwo()
        {
            if (_keyTwo != null)
            {
                try
                {
                    var res = _keyTwo;
                    return res;
                }
                finally
                {
                    _keyTwo = null;
                }
            }
            throw new InvalidDataException("The key was already read once. Please create a new instance of the EncryptionKeys");
        }
    }
}