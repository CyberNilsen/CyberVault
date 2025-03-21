using System;

using System.IO;

using System.Security.Cryptography;

using System.Text;

namespace CyberVault

{

    public static class AesEncryption

    {

        public static byte[] Encrypt(string plainText, byte[] key, byte[] iv)

        {

            if (string.IsNullOrEmpty(plainText))

                return new byte[0];

            byte[] encrypted;

            using (Aes aes = Aes.Create())

            {

                aes.Key = key;

                aes.IV = iv;

                aes.Mode = CipherMode.CBC;

                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream msEncrypt = new MemoryStream())

                {

                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))

                    {

                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))

                        {

                            swEncrypt.Write(plainText);

                        }

                        encrypted = msEncrypt.ToArray();

                    }

                }

            }

            return encrypted;

        }

        public static string Decrypt(byte[] cipherText, byte[] key, byte[] iv)

        {

            if (cipherText == null || cipherText.Length == 0)

                return string.Empty;

            string plaintext = null;

            using (Aes aes = Aes.Create())

            {

                aes.Key = key;

                aes.IV = iv;

                aes.Mode = CipherMode.CBC;

                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))

                {

                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))

                    {

                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))

                        {

                            plaintext = srDecrypt.ReadToEnd();

                        }

                    }

                }

            }

            return plaintext;

        }

        public static byte[] GenerateIV()

        {

            using (Aes aes = Aes.Create())

            {

                aes.GenerateIV();

                return aes.IV;

            }

        }

    }

}
