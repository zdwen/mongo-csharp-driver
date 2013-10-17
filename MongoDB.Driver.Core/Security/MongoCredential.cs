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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Security
{
    /// <summary>
    /// Credential to access a MongoDB database.
    /// </summary>
    public sealed class MongoCredential
    {
        // private fields
        private readonly Mechanism _mechanism;
        private readonly MongoIdentityEvidence _evidence;
        private readonly MongoIdentity _identity;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoCredential" /> class.
        /// </summary>
        /// <param name="mechanism">The mechanism to authenticate with.</param>
        /// <param name="identity">The identity.</param>
        /// <param name="evidence">The evidence.</param>
        public MongoCredential(Mechanism mechanism, MongoIdentity identity, MongoIdentityEvidence evidence)
        {
            Ensure.IsNotNull("mechanism", mechanism);
            Ensure.IsNotNull("identity", identity);
            Ensure.IsNotNull("evidence", evidence);

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
        /// Gets the mechanism.
        /// </summary>
        public Mechanism Mechanism
        {
            get { return _mechanism; }
        }

        // internal static methods
        internal static MongoCredential FromComponents(string mechanism, string source, string username, string password)
        {
            var evidence = password == null ? (MongoIdentityEvidence)new ExternalEvidence() : new PasswordEvidence(password);
            return FromComponents(mechanism ?? "MONGODB-CR", source ?? "admin", username, evidence);
        }

        // private static methods
        private static MongoCredential FromComponents(string mechanism, string source, string username, MongoIdentityEvidence evidence)
        {
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            switch (mechanism.ToUpperInvariant())
            {
                case "MONGODB-CR":
                    source = source ?? "admin";
                    if (!(evidence is PasswordEvidence))
                    {
                        throw new ArgumentException("A MONGODB-CR credential must have a password.");
                    }

                    return new MongoCredential(
                        new Mechanism(mechanism, null),
                        new MongoInternalIdentity(source, username),
                        evidence);
                case "MONGODB-X509":
                    // always $external for GSSAPI.  
                    source = "$external";
                    
                    if (!(evidence is ExternalEvidence))
                    {
                        throw new ArgumentException("A MONGODB-X509 does not support a password.");
                    }

                    return new MongoCredential(
                        new Mechanism(mechanism, null),
                        new MongoExternalIdentity(username),
                        evidence);
                case "GSSAPI":
                    // always $external for GSSAPI.  
                    source = "$external";

                    return new MongoCredential(
                        new Mechanism("GSSAPI", null),
                        new MongoExternalIdentity(source, username),
                        evidence);
                case "PLAIN":
                    source = source ?? "admin";
                    if (!(evidence is PasswordEvidence))
                    {
                        throw new ArgumentException("A PLAIN credential must have a password.");
                    }

                    MongoIdentity identity;
                    if (source == "$external")
                    {
                        identity = new MongoExternalIdentity(source, username);
                    }
                    else
                    {
                        identity = new MongoInternalIdentity(source, username);
                    }

                    return new MongoCredential(
                        new Mechanism(mechanism, null),
                        identity,
                        evidence);
                default:
                    throw new NotSupportedException(string.Format("Unsupported MongoAuthenticationMechanism {0}.", mechanism));
            }
        }
    }
}