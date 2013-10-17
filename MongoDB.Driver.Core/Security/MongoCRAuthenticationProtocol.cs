/* Copyright 2010-2013 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Security
{
    /// <summary>
    /// Authenticates a credential using the MONGODB-CR protocol.
    /// </summary>
    internal class MongoCRAuthenticationProtocol : IAuthenticationProtocol
    {
        // public properties
        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return "MONGODB-CR"; }
        }

        // public methods
        /// <summary>
        /// Authenticates the connection against the given database.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        public void Authenticate(IConnection connection, MongoCredential credential)
        {
            var nonceCommand = new BsonDocument("getnonce", 1);
            var nonceResult = CommandHelper.RunCommand<CommandResult>(new DatabaseNamespace(credential.Identity.Source), nonceCommand, connection);
            if (!nonceResult.Ok)
            {
                throw new MongoAuthenticationException("Error getting nonce for authentication.", nonceResult.Response);
            }
            var nonce = nonceResult.Response["nonce"].AsString;

            var passwordDigest = ComputePasswordDigest(credential.Identity.Username, (PasswordEvidence)credential.Evidence);
            var digest = MongoUtils.Hash(nonce + credential.Identity.Username + passwordDigest);
            var authenticateCommand = new BsonDocument
            {
                { "authenticate", 1 },
                { "user", credential.Identity.Username },
                { "nonce", nonce },
                { "key", digest }
            };

            var authenticateResult = CommandHelper.RunCommand<CommandResult>(new DatabaseNamespace(credential.Identity.Source), authenticateCommand, connection);
            if (!authenticateResult.Ok)
            {
                var message = string.Format("Invalid credential for username '{0}' on database '{1}'.", credential.Identity.Username, credential.Identity.Source);
                throw new MongoAuthenticationException(message, authenticateResult.Response);
            }
        }

        /// <summary>
        /// Determines whether this instance can use the specified credential.
        /// </summary>
        /// <param name="credential">The credential.</param>
        /// <returns>
        ///   <c>true</c> if this instance can use the specified credential; otherwise, <c>false</c>.
        /// </returns>
        public bool CanUse(MongoCredential credential)
        {
            return credential.Mechanism.Name.Equals(Name, StringComparison.InvariantCultureIgnoreCase) &&
                credential.Evidence is PasswordEvidence;
        }

        // private methods
        private string ComputePasswordDigest(string username, PasswordEvidence passwordEvidence)
        {
            using (var md5 = MD5.Create())
            {
                var encoding = new UTF8Encoding(false, true);
                if (passwordEvidence.UsesSecureString)
                {
                    var prefixBytes = encoding.GetBytes(username + ":mongo:");
                    md5.TransformBlock(prefixBytes, 0, prefixBytes.Length, null, 0);
                    TransformFinalBlock(md5, passwordEvidence.SecurePassword);
                    return BsonUtils.ToHexString(md5.Hash);
                }
                else
                {
                    var bytes = encoding.GetBytes(username + ":mongo:" + passwordEvidence.PlainTextPassword);
                    return BsonUtils.ToHexString(md5.ComputeHash(bytes));
                }
            }
        }

        [SecuritySafeCritical]
        private static void TransformFinalBlock(HashAlgorithm hash, SecureString secureString)
        {
            var bstr = Marshal.SecureStringToBSTR(secureString);
            try
            {
                var passwordChars = new char[secureString.Length];
                var passwordCharsHandle = GCHandle.Alloc(passwordChars, GCHandleType.Pinned);
                try
                {
                    Marshal.Copy(bstr, passwordChars, 0, passwordChars.Length);

                    var passwordBytes = new byte[secureString.Length * 3]; // worst case for UTF16 to UTF8 encoding
                    var passwordBytesHandle = GCHandle.Alloc(passwordBytes, GCHandleType.Pinned);
                    try
                    {
                        var encoding = new UTF8Encoding(false, true);
                        var length = encoding.GetBytes(passwordChars, 0, passwordChars.Length, passwordBytes, 0);
                        hash.TransformFinalBlock(passwordBytes, 0, length);
                    }
                    finally
                    {
                        Array.Clear(passwordBytes, 0, passwordBytes.Length);
                        passwordBytesHandle.Free();
                    }
                }
                finally
                {
                    Array.Clear(passwordChars, 0, passwordChars.Length);
                    passwordCharsHandle.Free();
                }
            }
            finally
            {
                Marshal.ZeroFreeBSTR(bstr);
            }
        }
    }
}