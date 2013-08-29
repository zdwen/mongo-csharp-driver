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
using MongoDB.Driver.Core.Security;
using MongoDB.Driver.Core.Security.Mechanisms;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Authentication settings.
    /// </summary>
    public class StreamConnectionSettings
    {
        // public static readonly fields
        /// <summary>
        /// The default settings.
        /// </summary>
        public static readonly StreamConnectionSettings Defaults = new Builder().Build();

        // private fields
        private readonly IEnumerable<MongoCredential> _credentials;
        private readonly IEnumerable<IAuthenticationProtocol> _protocols;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamConnectionSettings" /> class.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="protocols">The protocols.</param>
        public StreamConnectionSettings(IEnumerable<MongoCredential> credentials, IEnumerable<IAuthenticationProtocol> protocols)
        {
            Ensure.IsNotNull("credentials", credentials);
            Ensure.IsNotNull("protocols", protocols);

            _credentials = credentials.ToList().AsReadOnly();
            _protocols = protocols.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the credentials.
        /// </summary>
        public IEnumerable<MongoCredential> Credentials
        {
            get { return _credentials; }
        }

        /// <summary>
        /// Gets the protocols.
        /// </summary>
        public IEnumerable<IAuthenticationProtocol> Protocols
        {
            get { return _protocols; }
        }

        // public static methods
        /// <summary>
        /// A method used to build up settings.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <returns>The built settings.</returns>
        public static StreamConnectionSettings Create(Action<Builder> callback)
        {
            var builder = new Builder();
            callback(builder);
            return builder.Build();
        }

        // public methods
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{{ Protocols: [{0}], Credentials: [{1}] }}",
                string.Join(", ", _protocols.Select(x => string.Format("'{0}'", x.Name))),
                string.Join(", ", _credentials.Select(x => string.Format("'{0}'", x))));
        }

        // nested classes
        /// <summary>
        /// Builds up <see cref="StreamConnectionSettings"/>.
        /// </summary>
        public class Builder
        {
            private List<MongoCredential> _credentials;
            private List<IAuthenticationProtocol> _protocols;

            internal Builder()
            {
                _credentials = new List<MongoCredential>();
                _protocols = new List<IAuthenticationProtocol>
                {
                    new MongoCRAuthenticationProtocol(),
                    new SaslAuthenticationProtocol(new GssapiMechanism()),
                };
            }

            internal StreamConnectionSettings Build()
            {
                return new StreamConnectionSettings(_credentials, _protocols);
            }

            /// <summary>
            /// Adds the credential.
            /// </summary>
            /// <param name="credential">The credential.</param>
            public void AddCredential(MongoCredential credential)
            {
                _credentials.Add(credential);
            }

            /// <summary>
            /// Adds the credentials.
            /// </summary>
            /// <param name="credentials">The credentials.</param>
            public void AddCredentials(IEnumerable<MongoCredential> credentials)
            {
                _credentials.AddRange(credentials);
            }

            /// <summary>
            /// Adds the protocol.
            /// </summary>
            /// <param name="protocol">The protocol.</param>
            public void AddProtocol(IAuthenticationProtocol protocol)
            {
                _protocols.Add(protocol);
            }

            /// <summary>
            /// Adds the sasl mechanism.
            /// </summary>
            /// <param name="mechanism">The mechanism.</param>
            public void AddSaslMechanism(ISaslMechanism mechanism)
            {
                _protocols.Add(new SaslAuthenticationProtocol(mechanism));
            }

            /// <summary>
            /// Adds the sasl mechanisms.
            /// </summary>
            /// <param name="mechanisms">The mechanisms.</param>
            public void AddSaslMechanisms(IEnumerable<ISaslMechanism> mechanisms)
            {
                _protocols.AddRange(mechanisms.Select(x => new SaslAuthenticationProtocol(x)));
            }

            /// <summary>
            /// Adds the sasl mechanism.
            /// </summary>
            /// <typeparam name="TMechanism">The type of the mechanism.</typeparam>
            public void AddSaslMechanism<TMechanism>() where TMechanism : ISaslMechanism, new()
            {
                _protocols.Add(new SaslAuthenticationProtocol(new TMechanism()));
            }

            /// <summary>
            /// Adds the protocols.
            /// </summary>
            /// <param name="protocols">The protocols.</param>
            public void AddProtocols(IEnumerable<IAuthenticationProtocol> protocols)
            {
                _protocols.AddRange(protocols);
            }

            /// <summary>
            /// Adds the protocol.
            /// </summary>
            /// <typeparam name="TProtocol">The type of the protocol.</typeparam>
            public void AddProtocol<TProtocol>() where TProtocol : IAuthenticationProtocol, new()
            {
                _protocols.Add(new TProtocol());
            }

            /// <summary>
            /// Clears the credentials.
            /// </summary>
            public void ClearCredentials()
            {
                _credentials.Clear();
            }

            /// <summary>
            /// Clears the protocols.
            /// </summary>
            public void ClearProtocols()
            {
                _protocols.Clear();
            }
        }
    }
}