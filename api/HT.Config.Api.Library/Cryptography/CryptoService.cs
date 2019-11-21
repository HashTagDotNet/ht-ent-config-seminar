using HT.Config.ConfigApi.Library.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HT.Config.ConfigApi.Library.Cryptography
{
    public class CryptoService : ICryptoService,IDisposable
    {
        private ApiOptions _options;
        private byte[] _staticSalt = Encoding.UTF8.GetBytes(typeof(CryptoService).FullName);

        public CryptoService(IOptions<ApiOptions> options)
        {
            _options = options.Value;
        }
        public CryptoService()
        {

        }
        public string Decrypt(byte[] encryptedBytes)
        {
            return Decrypt(encryptedBytes,_options.PrimaryCryptoKey);
        }
        public string Decrypt(byte[] encryptedBytes,string password)
        {
            return decryptBytes(encryptedBytes, password);
        }
        public byte[] Encrypt(string plaintext)
        {
            return encryptString(plaintext, _options.PrimaryCryptoKey);
        }

        public byte[] Encrypt(string plaintext,string password)
        {
            return encryptString(plaintext,password);
        }
        private string decryptBytes(byte[] encryptedBytes, string password)
        {
            if (encryptedBytes == null || encryptedBytes.Length == 0) throw new ArgumentNullException(nameof(encryptedBytes));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentNullException(nameof(password));
            byte[] plaintextBytes = null;
            using (var derivedBytes = new Rfc2898DeriveBytes(password, _staticSalt, iterations: 50000))
            {
                using (var algorithm = new AesManaged())
                {
                    algorithm.Key = derivedBytes.GetBytes(algorithm.KeySize / 8);
                    algorithm.IV = derivedBytes.GetBytes(16);

                    using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
                    {                    
                        using (MemoryStream msDecrypt = new MemoryStream())
                        {
                            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                            {
                                csDecrypt.Write(encryptedBytes, 0, encryptedBytes.Length);
                            }
                            plaintextBytes = msDecrypt.ToArray();
                        }
                    }
                }
            }
           return Encoding.UTF8.GetString(plaintextBytes);
        }

        private byte[] encryptString(string plaintext, string password)
        {
            if (plaintext == null) return null;
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentNullException(nameof(password));
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            using (var derivedBytes = new System.Security.Cryptography.Rfc2898DeriveBytes(password, _staticSalt, iterations: 50000))
            {            
                using (var algorithm = new AesManaged()) 
                {
                    algorithm.Key = derivedBytes.GetBytes(algorithm.KeySize / 8); 
                    algorithm.IV = derivedBytes.GetBytes(16); 
                    
                    ICryptoTransform encryptor = algorithm.CreateEncryptor();

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(plaintextBytes, 0, plaintextBytes.Length);
                        }
                        return memoryStream.ToArray();
                    }
                }
            }         
        }

        private bool _isDisposed = false;
        public void Dispose(bool isDisposing)
        {
            if (isDisposing && !_isDisposed)
            {

            }
            _isDisposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
        }
        ~CryptoService()
        {
            Dispose(false);
        }
    }
}
