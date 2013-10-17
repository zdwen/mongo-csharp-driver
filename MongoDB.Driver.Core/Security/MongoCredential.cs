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
        private readonly MechanismDefinition _mechanism;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoCredential" /> class.
        /// </summary>
        /// <param name="mechanism">Mechanism to authenticate with.</param>
        /// <param name="identity">The identity.</param>
        /// <param name="evidence">The evidence.</param>
        public MongoCredential(MechanismDefinition mechanism, MongoIdentity identity, MongoIdentityEvidence evidence)
        {
            if(mechanism == null)
            {
                throw new ArgumentNullException("mechanism");
            }
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
        /// Gets the mechanism.
        /// </summary>
        public MechanismDefinition Mechanism
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
                    source = source ?? "admin";
                    if (evidence == null || !(evidence is PasswordEvidence))
                    {
                        throw new ArgumentException("A MONGODB-CR credential must have a password.");
                    }

                    return new MongoCredential(
                        new MechanismDefinition(mechanism, null),
                        new MongoInternalIdentity(source, username),
                        evidence);
                case "GSSAPI":
                    // always $external for GSSAPI.  
                    source = "$external";

                    return new MongoCredential(
                        new MechanismDefinition("GSSAPI", null),
                        new MongoExternalIdentity(source, username),
                        evidence);
                case "PLAIN":
                    source = source ?? "admin";
                    if (evidence == null || !(evidence is PasswordEvidence))
                    {
                        throw new ArgumentException("A PLAIN credential must have a password.");
                    }

                    MongoIdentity identity;
                    if(source == "$external")
                    {
                        identity = new MongoExternalIdentity(source, username);
                    }
                    else
                    {
                        identity = new MongoInternalIdentity(source, username);
                    }

                    return new MongoCredential(
                        new MechanismDefinition(mechanism, null),
                        identity,
                        evidence);
                default:
                    throw new NotSupportedException(string.Format("Unsupported MongoAuthenticationMechanism {0}.", mechanism));
            }
        }
    }
}