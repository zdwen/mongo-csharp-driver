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
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;

namespace MongoDB.Driver.Core.Security
{
    /// <summary>
    /// Credential to access a MongoDB database.
    /// </summary>
    public class MongoCredential
    {
        // private fields
        private readonly MongoIdentityEvidence _evidence;
        private readonly MongoIdentity _identity;
        private readonly string _mechanism;
        
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoCredential" /> class.
        /// </summary>
        /// <param name="mechanism">Mechanism to authenticate with.</param>
        /// <param name="identity">The identity.</param>
        /// <param name="evidence">The evidence.</param>
        public MongoCredential(string mechanism, MongoIdentity identity, MongoIdentityEvidence evidence)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }
            if (evidence == null)
            {
                throw new ArgumentNullException("evidence");
            }

            _mechanism = mechanism;
            _identity = identity;
            _evidence = evidence;
        }

        // public properties
        /// <summary>
        /// Gets the evidence.
        /// </summary>
        public MongoIdentityEvidence Evidence
        {
            get { return _evidence; }
        }

        /// <summary>
        /// Gets the identity.
        /// </summary>
        public MongoIdentity Identity
        {
            get { return _identity; }
        }

        /// <summary>
        /// Gets the mechanism to authenticate with.
        /// </summary>
        public string Mechanism
        {
            get { return _mechanism; }
        }

        /// <summary>
        /// Gets the source.
        /// </summary>
        public string Source
        {
            get { return _identity.Source; }
        }

        /// <summary>
        /// Gets the username.
        /// </summary>
        public string Username
        {
            get { return _identity.Username; }
        }

        // public static methods
        /// <summary>
        /// Creates a GSSAPI credential.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>A credential for GSSAPI.</returns>
        /// <remarks>This overload is used primarily on linux.</remarks>
        public static MongoCredential CreateGssapiCredential(string username)
        {
            return FromComponents("GSSAPI",
                "$external",
                username,
                new ExternalEvidence());
        }

        /// <summary>
        /// Creates a GSSAPI credential.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A credential for GSSAPI.</returns>
        public static MongoCredential CreateGssapiCredential(string username, string password)
        {
            return FromComponents("GSSAPI",
                "$external",
                username,
                new PasswordEvidence(password));
        }

        /// <summary>
        /// Creates a GSSAPI credential.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A credential for GSSAPI.</returns>
        public static MongoCredential CreateGssapiCredential(string username, SecureString password)
        {
            return FromComponents("GSSAPI",
                "$external",
                username,
                new PasswordEvidence(password));
        }

        /// <summary>
        /// Creates a credential used with MONGODB-CR.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public static MongoCredential CreateMongoCRCredential(string databaseName, string username, string password)
        {
            return FromComponents("MONGODB-CR",
                databaseName,
                username,
                new PasswordEvidence(password));
        }

        /// <summary>
        /// Creates a credential used with MONGODB-CR.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public static MongoCredential CreateMongoCRCredential(string databaseName, string username, SecureString password)
        {
            return FromComponents("MONGODB-CR",
                databaseName,
                username,
                new PasswordEvidence(password));
        }

        // internal static methods
        internal static MongoCredential FromComponents(string mechanism, string source, string username, string password)
        {
            var evidence = password == null ? (MongoIdentityEvidence)new ExternalEvidence() : new PasswordEvidence(password);
            return FromComponents(mechanism ?? "MONGODB-CR", source ?? "admin", username, evidence);
        }

        // private methods
        private void ValidatePassword(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }
            if (password.Any(c => (int)c >= 128))
            {
                throw new ArgumentException("Password must contain only ASCII characters.");
            }
        }

        // private static methods
        private static MongoCredential FromComponents(string mechanism, string source, string username, MongoIdentityEvidence evidence)
        {
            if (string.IsNullOrEmpty(mechanism))
            {
                throw new ArgumentException("Cannot be null or empty.", "mechanism");
            }
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            switch (mechanism.ToUpperInvariant())
            {
                case "MONGODB-CR":
                    // it is allowed for a password to be an empty string, but not a username
                    source = source ?? "admin";
                    if (evidence == null || !(evidence is PasswordEvidence))
                    {
                        throw new ArgumentException("A MONGODB-CR credential must have a password.");
                    }

                    return new MongoCredential(
                        mechanism,
                        new MongoInternalIdentity(source, username),
                        evidence);
                case "GSSAPI":
                    // always $external for GSSAPI.  
                    // this will likely need to change in 2.6.
                    source = "$external";

                    return new MongoCredential(
                        "GSSAPI",
                        new MongoExternalIdentity(source, username),
                        evidence);
                default:
                    throw new NotSupportedException(string.Format("Unsupported MongoAuthenticationMechanism {0}.", mechanism));
            }
        }
    }
}