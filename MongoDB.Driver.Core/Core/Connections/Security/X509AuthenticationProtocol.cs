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
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Security;

namespace MongoDB.Driver.Core.Connections.Security
{
    /// <summary>
    /// Authentication protocol using the SSL X509 certificates as the client identity.
    /// </summary>
    internal class X509AuthenticationProtocol : IAuthenticationProtocol
    {
        // public properties
        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return "MONGODB-X509"; }
        }

        // public methods
        /// <summary>
        /// Authenticates the specified connection with the given credential.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Authenticate(IConnection connection, MongoCredential credential)
        {
            var authenticateCommand = new BsonDocument
            {
                { "authenticate", 1 },
                { "mechanism", Name },
                { "user", credential.Identity.Username }
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
        /// <exception cref="System.NotImplementedException"></exception>
        public bool CanUse(MongoCredential credential)
        {
            return credential.Mechanism.Name.Equals(Name, StringComparison.InvariantCultureIgnoreCase) &&
                credential.Identity is MongoExternalIdentity &&
                credential.Evidence is ExternalEvidence;
        }
    }
}
