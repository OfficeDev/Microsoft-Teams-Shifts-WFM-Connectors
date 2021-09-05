// ---------------------------------------------------------------------------
// <copyright file="Aes256CbcHmacSha256Encryptor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace Microsoft.Teams.Shifts.Encryption.Encryptors
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using Bond;
    using Bond.IO.Safe;
    using Bond.Protocols;

    /// <summary>
    /// Aes256CbcHmacSha256Encryptor, based off of
    /// Microsoft.Internal.M365.Security.Encryption.EncryptionManager by Filip Sebesta
    /// [filipse@microsoft.com], Andrey Belenko [anbelen@microsoft.com]
    /// </summary>
    /// <remarks>
    /// Stripped down version of Aes256CbcHmacSha256Encryptor that we can give to developers to
    /// decrypt based on a shared secret
    /// </remarks>
    public class Aes256CbcHmacSha256Encryptor
    {
        /// <summary>
        /// The algorithm
        /// </summary>
        private const string Algorithm = "AES-256-CBC-HMAC-SHA256";

        [ThreadStatic]
        private static Aes _cipher;

        private readonly byte[] _authenticationKey;

        private readonly ICryptoTransform _keyDecryptor;

        private readonly ICryptoTransform _keyEncryptor;

        /// <summary>
        /// Constructor for the class.
        /// </summary>
        /// <param name="masterKey">
        /// Master key associated with the class. This will be used to encrypt/decrypt data.
        /// </param>
        public Aes256CbcHmacSha256Encryptor(byte[] masterKey)
        {
            const int expLength = 64;
            if (masterKey == null)
            {
                throw new ArgumentException($"Aes256CbcHmacSha256Encryptor: masterKey key {nameof(masterKey)} is missing.");
            }

            int length = masterKey.Length;
            if (length != expLength)
            {
                throw new ArgumentException($"Aes256CbcHmacSha256Encryptor: Encryption instance was not created. The key provided has wrong length of {length}B. The expected length for AES-256-CBC-HMAC-SHA256 is {expLength}B");
            }

            // Split supplied master key into authentication key (32 bytes, first half) and key
            // encryption key (32 bytes, second half)
            _authenticationKey = new ArraySegment<byte>(masterKey, 0, 32).ToArray();
            var keyEncryptionKey = new ArraySegment<byte>(masterKey, 32, 32).ToArray();

            // Create encryption instance for record key encryption (key length = 32B, cipher mode =
            // ECB, no padding) There is no need for padding as the key is exactly two blocks
            using (var cipher = Aes.Create())
            {
                cipher.KeySize = 256;
                cipher.Mode = CipherMode.ECB;
                cipher.Padding = PaddingMode.None;
                cipher.Key = keyEncryptionKey;

                _keyEncryptor = cipher.CreateEncryptor();
                _keyDecryptor = cipher.CreateDecryptor();
            }
        }

        private static Aes Cipher
        {
            get
            {
                if (_cipher == null)
                {
                    _cipher = new AesCryptoServiceProvider
                    {
                        KeySize = 256,
                        Mode = CipherMode.CBC,
                        Padding = PaddingMode.PKCS7
                    };
                }
                return _cipher;
            }
        }

        /// <summary> The encryption key identifier, currently we only keep a single key. </value>
        private static int KeyId { get; set; } = 1;

        public byte[] Decrypt(byte[] encrypted)
        {
            if (encrypted == null)
            {
                throw new ArgumentNullException(nameof(encrypted));
            }

            // 1. De-serialize data into EncryptionManagerPayload
            var input = new InputBuffer(encrypted);
            var reader = new CompactBinaryReader<InputBuffer>(input);
            var encryptionManagerPayload = Deserialize<EncryptionManagerPayload>.From(reader);
            int keyId = encryptionManagerPayload.KeyID;

            // 2. Confirm KeyId matches
            if (keyId != KeyId)
            {
                throw new Exception("There was a problem with the decryption of the data, expected KeyId " + KeyId + " but found " + keyId);
            }

            // 3. De-serialize cipher text from EncryptionManagerPayload into AesCbcHmacEncryptorPayload
            input = new InputBuffer(encryptionManagerPayload.Ciphertext);
            reader = new CompactBinaryReader<InputBuffer>(input);
            var payload = Deserialize<AesCbcHmacEncryptorPayload>.From(reader);

            byte[] encryptedDek = payload.EK.ToArray();
            byte[] iv = payload.IV.ToArray();
            byte[] ciphertext = payload.CT.ToArray();

            // 2. Compute and verify authentication tag
            byte[] authenticationTag = ComputeTag(_authenticationKey, CreateAad(KeyId, Algorithm), encryptedDek, iv, ciphertext);
            if (!authenticationTag.CryptographicEqual(payload.AT.ToArray()))
            {
                throw new Exception("There was a problem with the decryption of the data");
            }

            // 3. Decrypt data encryption key
            byte[] dek = _keyDecryptor.TransformFinalBlock(encryptedDek, 0, encryptedDek.Length);

            // 4. Decrypt data
            using (var decryptor = Cipher.CreateDecryptor(dek, iv))
            {
                return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
            }
        }

        public byte[] Encrypt(byte[] plaintext)
        {
            if (plaintext == null)
            {
                throw new ArgumentNullException(nameof(plaintext));
            }

            // 1. Generate data encryption key: DEK = Random(), IV = Random()
            Cipher.GenerateKey();
            Cipher.GenerateIV();

            // 2. Encrypt data: CT = AES-CBC(PT, DEK)
            byte[] ciphertext;
            using (var dataEncryptor = Cipher.CreateEncryptor())
            {
                ciphertext = dataEncryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
            }

            // 3. Encrypt data encryption key with key encryption key: EDEK = AES-ECB(DEK, KEK)
            byte[] encryptedDek = _keyEncryptor.TransformFinalBlock(Cipher.Key, 0, Cipher.Key.Length);

            // 4. Compute authentication tag: AT = HMAC-SHA256(AKEY, AAD | EDEK | IV | CT)
            byte[] authenticationTag = ComputeTag(_authenticationKey, CreateAad(KeyId, Algorithm), encryptedDek, Cipher.IV, ciphertext);

            // 5. Serialize data
            var encryptorOutput = new OutputBuffer();
            var encryptWriter = new CompactBinaryWriter<OutputBuffer>(encryptorOutput);

            Serialize.To(encryptWriter, new AesCbcHmacEncryptorPayload
            {
                EK = new ArraySegment<byte>(encryptedDek),
                IV = new ArraySegment<byte>(Cipher.IV),
                CT = new ArraySegment<byte>(ciphertext),
                AT = new ArraySegment<byte>(authenticationTag)
            });

            // 6. Wrap it up as Encryption Manager Payload
            var output = new OutputBuffer();
            var writer = new CompactBinaryWriter<OutputBuffer>(output);
            Serialize.To(writer, new EncryptionManagerPayload
            {
                KeyID = KeyId,
                Ciphertext = new ArraySegment<byte>(encryptorOutput.Data.ToArray())
            });

            return output.Data.ToArray();
        }

        internal static byte[] ComputeTag(byte[] key, params byte[][] components)
        {
            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            var totalLength = components.Sum(arr => arr?.Length ?? 0);

            using (var hmac = new HMACSHA256(key))
            using (var stream = new System.IO.MemoryStream(totalLength))
            {
                foreach (var component in components.Where(component => component != null))
                {
                    stream.Write(component, 0, component.Length);
                }

                stream.Seek(0, System.IO.SeekOrigin.Begin);

                return hmac.ComputeHash(stream);
            }
        }

        /// <summary>
        /// Creates Additional Authenticated Data, keyed by KeyId. If/when KeyId changes, adds an
        /// additional level of security.
        /// </summary>
        /// <param name="keyId">The key identifier.</param>
        /// <param name="algorithm">The algorithm.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">algorithm</exception>
        private static byte[] CreateAad(int keyId, string algorithm)
        {
            if (string.IsNullOrEmpty(algorithm))
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            byte[] kidBytes = BitConverter.GetBytes(keyId);
            byte[] algBytes = System.Text.Encoding.UTF8.GetBytes(algorithm);

            byte[] result = new byte[algBytes.Length + kidBytes.Length];

            Buffer.BlockCopy(algBytes, 0, result, 0, algBytes.Length);
            Buffer.BlockCopy(kidBytes, 0, result, algBytes.Length, kidBytes.Length);

            return result;
        }
    }
}
