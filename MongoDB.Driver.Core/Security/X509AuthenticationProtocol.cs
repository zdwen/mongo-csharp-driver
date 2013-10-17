using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Security
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
