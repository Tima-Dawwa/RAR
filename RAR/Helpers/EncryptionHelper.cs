using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RAR.Helper
{
    public static class EncryptionHelper
    {
        private const int SaltSize = 16; 
        private const int IvSize = 16;   
        private const int KeySize = 32;  
        private const int Iterations = 10000;

     
        public static byte[] Encrypt(byte[] data, string password)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty");

            byte[] salt = GenerateRandomBytes(SaltSize);
            byte[] iv = GenerateRandomBytes(IvSize);

            byte[] key = DeriveKey(password, salt);

            byte[] encryptedData;
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(data, 0, data.Length);
                    csEncrypt.FlushFinalBlock();
                    encryptedData = msEncrypt.ToArray();
                }
            }

            byte[] result = new byte[SaltSize + IvSize + encryptedData.Length];
            Array.Copy(salt, 0, result, 0, SaltSize);
            Array.Copy(iv, 0, result, SaltSize, IvSize);
            Array.Copy(encryptedData, 0, result, SaltSize + IvSize, encryptedData.Length);

            return result;
        }

       
        public static byte[] Decrypt(byte[] encryptedData, string password)
        {
            Console.Write(password);
            if (encryptedData == null || encryptedData.Length < SaltSize + IvSize + 1)
                throw new ArgumentException("Invalid encrypted data");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty");

            byte[] salt = new byte[SaltSize];
            byte[] iv = new byte[IvSize];
            byte[] cipherData = new byte[encryptedData.Length - SaltSize - IvSize];

            Array.Copy(encryptedData, 0, salt, 0, SaltSize);
            Array.Copy(encryptedData, SaltSize, iv, 0, IvSize);
            Array.Copy(encryptedData, SaltSize + IvSize, cipherData, 0, cipherData.Length);

            byte[] key = DeriveKey(password, salt);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var msDecrypt = new MemoryStream(cipherData))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var msResult = new MemoryStream())
                {
                    csDecrypt.CopyTo(msResult);
                    return msResult.ToArray();
                }
            }
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(KeySize);
            }
        }

      
        private static byte[] GenerateRandomBytes(int size)
        {
            byte[] bytes = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return bytes;
        }

        public static bool ValidatePassword(byte[] encryptedData, string password)
        {
            try
            {
                Decrypt(encryptedData, password);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsFileEncrypted(string filePath)
        {
            try
            {
                byte[] header = new byte[5];
                using (var fs = new FileStream(filePath, FileMode.Open))
                {
                    fs.Read(header, 0, 5);
                }
                return (header[4] & 1) == 1;
            }
            catch
            {
                return false;
            }
        }
    }
}