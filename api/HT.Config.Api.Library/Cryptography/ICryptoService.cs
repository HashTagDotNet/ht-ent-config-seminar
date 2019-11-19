using System;
using System.Collections.Generic;
using System.Text;

namespace HT.Config.ConfigApi.Library.Cryptography
{
    public interface ICryptoService
    {
        byte[] Encrypt(string plaintext);
        string Decrypt(byte[] encryptedBytes);

    }
}
