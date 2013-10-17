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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections.Security
{
    /// <summary>
    /// Creates a <see cref="SslStream"/>.
    /// </summary>
    public class SslStreamFactory : StreamFactoryBase
    {
        // private static fields
        private static readonly TraceSource __trace = MongoTraceSources.Connections;

        // private fields
        private readonly SslStreamSettings _settings;
        private readonly IStreamFactory _wrapped;
        private readonly string _toStringRepresentation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SslStreamFactory" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="wrapped">The wrapped.</param>
        public SslStreamFactory(SslStreamSettings settings, IStreamFactory wrapped)
        {
            Ensure.IsNotNull("settings", settings);
            Ensure.IsNotNull("wrapped", wrapped);

            _settings = settings;
            _wrapped = wrapped;
            _toStringRepresentation = string.Format("sslstreamfactory#{0}({1})", IdGenerator<IStreamFactory>.GetNextId(), wrapped);
        }

        // public methods
        /// <summary>
        /// Creates a stream for the specified dns end point.
        /// </summary>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <returns>A stream.</returns>
        public override Stream Create(DnsEndPoint dnsEndPoint)
        {
            var stream = _wrapped.Create(dnsEndPoint);

            var checkCertificateRevocation = _settings.CheckCertificateRevocation;
            var clientCertificateCollection = _settings.ClientCertificates;
            var clientCertificateSelectionCallback = _settings.ClientCertificateSelectionCallback;
            var enabledSslProtocols = _settings.EnabledSslProtocols;
            var serverCertificateValidationCallback = _settings.ServerCertificateValidationCallback;

            var sslStream = new SslStream(stream, false, serverCertificateValidationCallback, clientCertificateSelectionCallback);
            sslStream.AuthenticateAsClient(dnsEndPoint.Host, clientCertificateCollection, enabledSslProtocols, checkCertificateRevocation);
            return sslStream;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return _toStringRepresentation;
        }
    }
}
