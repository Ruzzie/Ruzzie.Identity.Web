namespace Ruzzie.Common.Security
{
    /// <summary>
    /// Provides an interface for encrypting and decrypting strings.
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts a provided string.
        /// </summary>
        /// <param name="stringToEncrypt"></param>
        /// <returns></returns>
        string EncryptString(ref string? stringToEncrypt);
        /// <summary>
        /// Encrypts a provided string.
        /// </summary>
        /// <param name="stringToEncrypt"></param>
        /// <returns></returns>
        string EncryptString(string stringToEncrypt);
        /// <summary>
        /// Decrypts a string.
        /// </summary>
        /// <param name="encryptedString"></param>
        /// <returns></returns>
        string DecryptString(string encryptedString);
    }
}