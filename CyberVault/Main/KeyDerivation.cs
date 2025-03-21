using System;
using System.Security.Cryptography;

namespace CyberVault
{
    public static class KeyDerivation
    {
        public static byte[] DeriveKey(string password, byte[] salt, int iterations = 10000, int keySize = 32)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(keySize); // AES-256 requires a 32-byte key
            }
        }
    }
}