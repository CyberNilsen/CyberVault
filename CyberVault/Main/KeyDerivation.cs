using System;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace CyberVault
{
    public static class KeyDerivation
    {

        private const int DEFAULT_ITERATIONS = 1000000;

        public static byte[] DeriveKey(string password, byte[] salt, int iterations = DEFAULT_ITERATIONS, int keySize = 32)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(keySize);
            }
        }

        public static byte[] GenerateSalt(int size = 16)
        {
            byte[] salt = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
    }
}