﻿/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Authentication
{
    public class MongoDBX509Authenticator : IAuthenticator
    {
        // fields
        private readonly string _username;

        // constructors
        public MongoDBX509Authenticator(string username)
        {
            _username = Ensure.IsNotNullOrEmpty(username, "username");
        }

        // properties
        public string Name
        {
            get { return "MONGODB-X509"; }
        }

        // methods
        public async Task AuthenticateAsync(IConnection connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, "connection");

            try
            {
                var command = new BsonDocument
                {
                    { "authenticate", 1 },
                    { "mechanism", Name },
                    { "user", _username }
                };
                var protocol = new CommandWireProtocol("$external", command, true, null);
                await protocol.ExecuteAsync(connection, timeout, cancellationToken);
            }
            catch (MongoCommandException ex)
            {
                var message = string.Format("Unable to authenticate username '{0}' using protocol '{1}'.", _username, Name);
                throw new MongoAuthenticationException(message, ex);
            }
        }
    }
}
