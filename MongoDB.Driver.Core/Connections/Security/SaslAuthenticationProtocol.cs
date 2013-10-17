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

using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Security;

namespace MongoDB.Driver.Core.Connections.Security
{
    /// <summary>
    /// Authenticates a credential using the SASL protocol.
    /// </summary>
    internal class SaslAuthenticationProtocol : IAuthenticationProtocol
    {
        // private fields
        private readonly ISaslMechanism _mechanism;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaslAuthenticationProtocol" /> class.
        /// </summary>
        /// <param name="mechanism">The mechanism.</param>
        public SaslAuthenticationProtocol(ISaslMechanism mechanism)
        {
            _mechanism = mechanism;
        }

        // public properties
        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return _mechanism.Name; }
        }

        // public methods
        /// <summary>
        /// Authenticates the connection against the given database.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        public void Authenticate(IConnection connection, MongoCredential credential)
        {
            using (var conversation = new SaslConversation())
            {
                var currentStep = _mechanism.Initialize(connection, credential);

                var command = new BsonDocument
                {
                    { "saslStart", 1 },
                    { "mechanism", _mechanism.Name },
                    { "payload", currentStep.BytesToSendToServer }
                };

                while (true)
                {
                    var result = CommandHelper.RunCommand<CommandResult>(new DatabaseNamespace(credential.Identity.Source), command, connection);
                    if (!result.Ok)
                    {
                        var message = "Unknown error occured during authentication.";
                        var code = result.Code;
                        var errorMessage = result.ErrorMessage;
                        if (code.HasValue && errorMessage != null)
                        {
                            message = string.Format("Error: {0} - {1}", code, errorMessage);
                        }

                        throw new MongoAuthenticationException(message, result.Response);
                    }

                    if (result.Response["done"].AsBoolean)
                    {
                        break;
                    }

                    currentStep = currentStep.Transition(conversation, result.Response["payload"].AsByteArray);

                    command = new BsonDocument
                    {
                        { "saslContinue", 1 },
                        { "conversationId", result.Response["conversationId"].AsInt32 },
                        { "payload", currentStep.BytesToSendToServer }
                    };
                }
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
            return _mechanism.CanUse(credential);
        }
    }
}