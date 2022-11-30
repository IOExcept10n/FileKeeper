using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable IDE0090
namespace MyCryptLib
{
    /// <summary>
    /// A helper class for cryptography. Provide tools to encrypt and decrypt data. 
    /// </summary>
    public static class Cryptography
    {

        /// <summary>
        /// An event which triggers when an encryption operation gets an exception in middle of a command.
        /// </summary>
        public static event EventHandler<Exception> OnEncryptionFailure;
        /// <summary>
        /// An event which triggers when a decryption operation gets an exception in middle of a command.
        /// </summary>
        public static event EventHandler<Exception> OnDecryptionFailure;

        /// <summary>
        /// Creates a random salt that will be used to encrypt your file.
        /// Salt is needed to make our cipher more protected.
        /// </summary>
        /// <returns></returns>
        public static byte[] GenerateRandomSalt()
        {
            byte[] data = RandomNumberGenerator.GetBytes(32);
            return data;
        }

        /// <summary>
        /// Encrypts the file with an AES (Rijndael) algorithm.
        /// </summary>
        /// <param name="sourceStream">Stream of an original data.</param>
        /// <param name="outputStream">Stream to save encrypted data.</param>
        /// <param name="password">Password to encrypt the file.</param>
        public static async Task EncryptStreamAsync(this Stream sourceStream, Stream outputStream, string password)
        {
            if (!outputStream.CanWrite)
            {
                await Task.Run(() => OnEncryptionFailure?.Invoke(null, new ArgumentException("Can't write data to out stream!", nameof(outputStream))));
                return;
            }
            if (!sourceStream.CanRead)
            {
                await Task.Run(() => OnEncryptionFailure?.Invoke(null, new ArgumentException("Can't read data from input stream!", nameof(sourceStream))));
                return;
            }

            // Generate a salt which will be needed later.
            byte[] salt = GenerateRandomSalt();
            // Initialize an AES encryption algorithm class.
            Aes aes = Aes.Create();
            // Set required setting for the algorithm.
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.None;
            // Repeatedly hash our password and salt to make our encryption more effective. 
            // This code makes recomputing the hash 50 thousand times which makes our password almost unreal to hack.
            var key = new Rfc2898DeriveBytes(password, salt, 49999);
            // These code lines set generated values into our cryptographic algorithm and creates initialization data to prepare for encryption.
            aes.Key = key.GetBytes(aes.KeySize / 8);
            aes.IV = key.GetBytes(aes.BlockSize / 8);
            // AES cipher have a lot of realizations. 
            aes.Mode = CipherMode.CFB;
            
            // Start writing data to file.
            // At the start of the file is salt for encryption.
            outputStream.Write(salt, 0, salt.Length);

            // Create a cryptostream to encrypting file needed to write.
            using CryptoStream cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

            // Create a buffer of size 1Mb to limit memory using. 
            byte[] buffer = new byte[0x100000];
            int readCursor;

            try
            {
                // Reads a buffer from file and encrypts it and writes into new file.
                while ((readCursor = await sourceStream.ReadAsync(buffer)) > 0)
                {
                    await cryptoStream.WriteAsync(buffer.AsMemory(0, readCursor));
                }
            }
            catch (Exception ex)
            {
                // Notify that we have an exception.
                await Task.Run(() => OnEncryptionFailure?.Invoke(null, ex));
            }
        }
        /// <summary>
        /// Decrypts the file with an AES algorithm.
        /// </summary>
        /// <param name="inputStream">Stream to encrypt data.</param>
        /// <param name="outputStream">Stream with encrypted data.</param>
        /// <param name="password">Password to encrypt the file.</param>
        public static async Task DecryptStreamAsync(this Stream inputStream, Stream outputStream, string password)
        {
            if (!outputStream.CanWrite || !outputStream.CanSeek)
            {
                await Task.Run(() => OnDecryptionFailure?.Invoke(null, new ArgumentException("Can't write data to out stream!", nameof(outputStream))));
                return;
            }
            if (!inputStream.CanRead)
            {
                await Task.Run(() => OnDecryptionFailure?.Invoke(null, new ArgumentException("Can't read data from input stream!", nameof(inputStream))));
                return;
            }
            // Prepare to read the file. Read the salt from start of the stream.
            byte[] salt = new byte[32];

            inputStream.Read(salt, 0, salt.Length);

            // Creates an Aes algorithm to decrypt the file
            Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            // Creates a key analogic to encryption algorithm.
            var key = new Rfc2898DeriveBytes(password, salt, 49999);
            aes.Key = key.GetBytes(aes.KeySize / 8);
            aes.IV = key.GetBytes(aes.BlockSize / 8);
            aes.Padding = PaddingMode.None;
            aes.Mode = CipherMode.CFB;

            // ..End of setting up the decryptor and start to setting up streams to write into a stream. 
            using CryptoStream cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            // TODO: make all of this parallel.
            // And prepare the buffer to read. 
            int readCursor;
            byte[] buffer = new byte[0x100000];
            // Read from file. All like in encryption function.
            try
            {
                while ((readCursor = await cryptoStream.ReadAsync(buffer)) > 0)
                {
                    await outputStream.WriteAsync(buffer.AsMemory(0, readCursor));
                }
            }
            catch (Exception ex)
            {
                await Task.Run(() => OnDecryptionFailure?.Invoke(null, ex));
            }
        }
        /// <summary>
        /// Encypts file and writes it into a new file.
        /// </summary>
        /// <param name="path">Path of a source file.</param>
        /// <param name="output">Path to create an encrypted file.</param>
        /// <param name="password">Password to encrypt the file.</param>
        /// <returns></returns>
        public static async Task EncryptFileAsync(string path, string output, string password)
        {
            using FileStream inputFile = new FileStream(path, FileMode.Open, FileAccess.Read);
            using FileStream outputFile = new FileStream(output, FileMode.Create, FileAccess.Write);
            string ext = inputFile.Name[inputFile.Name.LastIndexOf('.')..];
            // Add cryptomode to start of the file 
            await outputFile.WriteAsync(new byte[] { 0x41 }.AsMemory(0, 1));
            // Add extension length to start of new file.
            await outputFile.WriteAsync(BitConverter.GetBytes(ext.Length));
            // Add extension to start of new file.
            await outputFile.WriteAsync(Encoding.UTF8.GetBytes(ext));
            await EncryptStreamAsync(inputFile, outputFile, password);
        }
        /// <summary>
        /// Decryts a file and writes it into a new file.
        /// </summary>
        /// <param name="path">Path to a source file.</param>
        /// <param name="output">Path to create a decrypted file.</param>
        /// <param name="password">Passowrd to decrypt the file.</param>
        /// <returns></returns>
        public static async Task DecryptFileAsync(string path, string output, string password)
        {
            using FileStream inputFile = new FileStream(path, FileMode.Open, FileAccess.Read);
            // Check right mode.
            byte[] intBuf = new byte[1];
            await inputFile.ReadAsync(intBuf.AsMemory(0, 1));
            byte mode = intBuf[0];
            if (mode != 0x41) throw new InvalidOperationException("File was encrypted with an another algorithm.");
            // Read an extension
            intBuf = new byte[4];
            await inputFile.ReadAsync(intBuf.AsMemory(0, 4));
            int length = BitConverter.ToInt32(intBuf);
            intBuf = new byte[length];
            await inputFile.ReadAsync(intBuf.AsMemory(0, length));
            string ext = Encoding.UTF8.GetString(intBuf);
            using FileStream outputFile = new FileStream(output + '.' + ext, FileMode.Create, FileAccess.Write);
            await DecryptStreamAsync(inputFile, outputFile, password);
        }
        /// <summary>
        /// Encrypts a value with a RSA algorithm.
        /// </summary>
        /// <param name="value">Value which will be encrypted.</param>
        /// <param name="publicKey">Returns a public key to encrypt a file.</param>
        /// <param name="privateKey">Returns a private key to decrypt a file.</param>
        /// <returns></returns>
        public static byte[] EncryptInitiallyWithRSA(this byte[] value, out string publicKey, out string privateKey)
        {
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            var result = provider.Encrypt(value, false);
            publicKey = provider.ToXmlString(false);
            privateKey = provider.ToXmlString(true);
            return result;
        }
        /// <summary>
        /// Generates a keys pair for the encryption.
        /// </summary>
        /// <param name="includePrivate">Manage if we should include private key into prarmeters.</param>
        /// <param name="publicKey">Public key to encrypt the file.</param>
        /// <param name="privateKey">Private key to decrypt the file.</param>
        /// <returns>Paramters for the RSA.</returns>
        public static RSAParameters CreateKeysForRSA(bool includePrivate, out string publicKey, out string privateKey)
        {
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            publicKey = provider.ToXmlString(false);
            privateKey = provider.ToXmlString(true);
            return provider.ExportParameters(includePrivate);
        }
        /// <summary>
        /// Encrypts the file with a RSA algorithm. 
        /// </summary>
        /// <param name="value">Value which we should encrypt.</param>
        /// <param name="publicKey">Key to encrypt data.</param>
        /// <returns>Encrypted data.</returns>
        public static byte[] EncryptWithRSA(this byte[] value, string publicKey)
        {
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            provider.FromXmlString(publicKey);
            // Add mode identicator
            List<byte> result = new List<byte>(100) { 0x52 };
            result.AddRange(provider.Encrypt(value, false));
            return result.ToArray();
        }

        /// <summary>
        /// Encrypts the file with a RSA algorithm. 
        /// </summary>
        /// <param name="value">Value which we should encrypt.</param>
        /// <param name="publicKey">Key to encrypt data.</param>
        /// <returns>Encrypted data.</returns>
        public static byte[] EncryptWithRSA(this byte[] value, RSAParameters publicKey)
        {
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            provider.ImportParameters(publicKey);
            var result = provider.Encrypt(value, false);
            return result;
        }

        /// <summary>
        /// Decrypts data with a RSA algorithm.
        /// </summary>
        /// <param name="value">Value to decrypt</param>
        /// <param name="privateKey">A key to decrypt data.</param>
        /// <returns>Decrypted data.</returns>
        public static byte[] DecryptWithRSA(this byte[] value, RSAParameters privateKey)
        {
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            provider.ImportParameters(privateKey);
            byte[] result = provider.Decrypt(value, false);
            return result;
        }

        /// <summary>
        /// Decrypts data with a RSA algorithm.
        /// </summary>
        /// <param name="value">Value to decrypt.</param>
        /// <param name="privateKey">A key to decrypt data.</param>
        /// <returns>Decrypted data.</returns>
        public static byte[] DecryptWithRSA(this byte[] value, string privateKey)
        {
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            provider.FromXmlString(privateKey);
            byte[] result = provider.Decrypt(value, false);
            return result;
        }

        // Below are less effective algorithms.
        /// <summary>
        /// Encrypts a string with <see langword="Caesar Cipher"/>: every single character moves right throw the alphabet by 3 characters.
        /// </summary>
        /// <param name="source">String to encrypt</param>
        /// <returns>An encrypted string.</returns>
        public static string EncryptWithCaesar(this string source)
        {
            StringBuilder result = new StringBuilder();
            foreach (char c in source)
            {
                result.Append((char)(c + 3));
            }
            return result.ToString();
        }
        /// <summary>
        /// Decrypts a string with <see langword="Caesar Cipher"/>: every single character moves right throw the alphabet by 3 characters.
        /// </summary>
        /// <param name="enc">String to decrypt</param>
        /// <returns>An decrypted string.</returns>
        public static string DecryptWithCaesar(this string enc)
        {
            StringBuilder result = new StringBuilder();
            foreach (char c in enc)
            {
                result.Append((char)(c - 3));
            }
            return result.ToString();
        }
        /// <summary>
        /// Encrypts or decrypts a string with given key with <see langword="XOR"/> algorithm.
        /// </summary>
        /// <param name="source">String to do an algorithm.</param>
        /// <param name="key">Key to encrypt or decrypt a string.</param>
        /// <returns>Resulting string. First time it encrypts, second - decrypts.</returns>
        public static string CryptWithXor(this string source, string key)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < source.Length; i++)
            {
                result.Append((char)(source[i] ^ key[i % key.Length]));
            }
            return result.ToString();
        }
    }
}