using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Security.Mechanisms
{
    /// <summary>
    /// Implements the SASL-PLAIN rfc: http://tools.ietf.org/html/rfc4616.
    /// </summary>
    internal class PlainMechanism : ISaslMechanism
    {
        // public properties
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        public string Name
        {
            get { return "PLAIN"; }
        }

        // public methods
        /// <summary>
        /// Determines whether this instance can authenticate with the specified credential.
        /// </summary>
        /// <param name="credential">The credential.</param>
        /// <returns>
        ///   <c>true</c> if this instance can authenticate with the specified credential; otherwise, <c>false</c>.
        /// </returns>
        public bool CanUse(MongoCredential credential)
        {
            return credential.Evidence is PasswordEvidence;
        }

        /// <summary>
        /// Initializes the mechanism.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        /// <returns>
        /// The initial step.
        /// </returns>
        public ISaslStep Initialize(IConnection connection, MongoCredential credential)
        {
            // this might throw an error if they provided the password using a SecureString.
            // in this case, we will simply refuse
            var passwordEvidence = (PasswordEvidence)credential.Evidence;
            if(passwordEvidence.UsesSecureString)
            {
                throw new MongoSecurityException("The password was provided as a SecureString, but the PLAIN mechanism requires plain text.");
            }

            var dataString = string.Format("\0{0}\0{1}",
                credential.Identity.Username,
                passwordEvidence.PlainTextPassword);

            var bytes = new UTF8Encoding(false, true).GetBytes(dataString);
            return new SaslCompletionStep(bytes);
        }
    }
}