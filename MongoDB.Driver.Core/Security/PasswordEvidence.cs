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

namespace MongoDB.Driver.Core.Security
{
    /// <summary>
    /// Evidence of a MongoIdentity via a shared secret.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public sealed class PasswordEvidence : MongoIdentityEvidence
    {
        // private fields
        private readonly string _plainTextPassword;
        private readonly SecureString _securePassword;
        private readonly bool _usesSecureString;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordEvidence" /> class.
        /// </summary>
        /// <param name="password">The password.</param>
        public PasswordEvidence(SecureString password)
        {
            _securePassword = password.Copy();
            _securePassword.MakeReadOnly();
            _usesSecureString = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordEvidence" /> class.
        /// </summary>
        /// <param name="password">The password.</param>
        public PasswordEvidence(string password)
        {
            _usesSecureString = false;
            _plainTextPassword = password;
            _securePassword = new SecureString();
            foreach (char c in password)
            {
                _securePassword.AppendChar(c);
            }
            _securePassword.MakeReadOnly();
        }

        // public properties
        /// <summary>
        /// Gets the plain text password.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public string PlainTextPassword
        {
            get
            {
                if (_usesSecureString)
                {
                    throw new MongoDriverException("The password is stored as a secure string. Check UsesSecureString before calling this property.");
                }
                return _plainTextPassword;
            }
        }
        /// <summary>
        /// Gets the secure password.
        /// </summary>
        public SecureString SecurePassword
        {
            get { return _securePassword; }
        }

        /// <summary>
        /// Gets a value indicating whether the password is stored as a secure string.
        /// </summary>
        public bool UsesSecureString
        {
            get { return _usesSecureString; }
        }
    }
}
