using System;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;

namespace CyberVault.Main
{
    public static class AesEncryption
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory")]
        public static extern bool ZeroMemory(IntPtr destination, int length);

        public static byte[] Encrypt(string plainText, byte[] key, byte[] iv)
        {
            if (string.IsNullOrEmpty(plainText))
                return new byte[0];

            byte[] encrypted;
            byte[] plainBytes = null;

            try
            {
                plainBytes = Encoding.UTF8.GetBytes(plainText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    {
                        encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    }
                }

                return encrypted;
            }
            finally
            {
                if (plainBytes != null)
                {
                    Array.Clear(plainBytes, 0, plainBytes.Length);
                }
            }
        }

        public static string Decrypt(byte[] cipherText, byte[] key, byte[] iv)
        {
            if (cipherText == null || cipherText.Length == 0)
                return string.Empty;

            byte[] decryptedBytes = null;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            finally
            {
                if (decryptedBytes != null)
                {
                    Array.Clear(decryptedBytes, 0, decryptedBytes.Length);
                }
            }
        }

        public static byte[] GenerateIV()
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateIV();
                return aes.IV;
            }
        }

        public static byte[] GenerateKey(int keySize = 32)
        {
            byte[] key = new byte[keySize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        public static bool SecureEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }
            return result == 0;
        }
    }

    public sealed class SecureStringHelper : IDisposable
    {
        private GCHandle _handle;
        private byte[] _data;
        private bool _disposed = false;

        public SecureStringHelper(string value)
        {
            _data = Encoding.UTF8.GetBytes(value);
            _handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
        }

        public byte[] GetBytes()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureStringHelper));
            return _data;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_data != null)
                {
                    Array.Clear(_data, 0, _data.Length);
                    ZeroMemory(_handle.AddrOfPinnedObject(), _data.Length);
                }

                if (_handle.IsAllocated)
                    _handle.Free();

                _disposed = true;
            }
        }

        [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory")]
        private static extern bool ZeroMemory(IntPtr destination, int length);
    }
}