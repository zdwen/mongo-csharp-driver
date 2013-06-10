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
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace MongoDB.Driver.Core
{
    /// <summary>
    /// Represents the settings for using SSL.
    /// </summary>
    public class SslSettings
    {
        // public static fields
        public static readonly SslSettings Defaults = new Builder().Build();

        // private fields
        private readonly bool _checkCertificateRevocation;
        private readonly X509CertificateCollection _clientCertificates;
        private readonly LocalCertificateSelectionCallback _clientCertificateSelector;
        private readonly SslProtocols _enabledSslProtocols;
        private readonly RemoteCertificateValidationCallback _serverCertificateValidator;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SslSettings" /> class.
        /// </summary>
        /// <param name="checkCertificateRevocation">whether to check certificate revocation.</param>
        /// <param name="clientCertificates">The client certificates.</param>
        /// <param name="clientCertificateSelector">The client certificate selector.</param>
        /// <param name="enabledProtocols">The enabled protocols.</param>
        /// <param name="serverCertificateValidator">The server certificate validator.</param>
        public SslSettings(bool checkCertificateRevocation, X509CertificateCollection clientCertificates, LocalCertificateSelectionCallback clientCertificateSelector, SslProtocols enabledProtocols, RemoteCertificateValidationCallback serverCertificateValidator)
        {
            _checkCertificateRevocation = checkCertificateRevocation;
            _clientCertificates = clientCertificates;
            _clientCertificateSelector = clientCertificateSelector;
            _enabledSslProtocols = enabledProtocols;
            _serverCertificateValidator = serverCertificateValidator;
        }

        // public properties
        /// <summary>
        /// Gets whether to check for certificate revocation.
        /// </summary>
        public bool CheckCertificateRevocation
        {
            get { return _checkCertificateRevocation; }
        }

        /// <summary>
        /// Gets the client certificates.
        /// </summary>
        public X509CertificateCollection ClientCertificates
        {
            get { return _clientCertificates; }
        }

        /// <summary>
        /// Gets the client certificate selection callback.
        /// </summary>
        public LocalCertificateSelectionCallback ClientCertificateSelectionCallback
        {
            get { return _clientCertificateSelector; }
        }

        /// <summary>
        /// Gets the enabled SSL protocols.
        /// </summary>
        public SslProtocols EnabledSslProtocols
        {
            get { return _enabledSslProtocols; }
        }

        /// <summary>
        /// Gets the server certificate validation callback.
        /// </summary>
        public RemoteCertificateValidationCallback ServerCertificateValidationCallback
        {
            get { return _serverCertificateValidator; }
        }

        // public static methods
        /// <summary>
        /// A method used to build up settings.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <returns>The built settings.</returns>
        public static SslSettings Create(Action<Builder> callback)
        {
            var builder = new Builder();
            callback(builder);
            return builder.Build();
        }

        /// <summary>
        /// Used to build up <see cref="SslSettings"/>.
        /// </summary>
        public class Builder
        {
            private bool _checkCertificateRevocation;
            private X509CertificateCollection _clientCertificates;
            private LocalCertificateSelectionCallback _clientCertificateSelector;
            private SslProtocols _enabledSslProtocols;
            private RemoteCertificateValidationCallback _serverCertificateValidator;

            internal Builder()
            {
                _checkCertificateRevocation = true;
                _enabledSslProtocols = SslProtocols.Default;
            }

            internal SslSettings Build()
            {
                return new SslSettings(
                    checkCertificateRevocation: _checkCertificateRevocation,
                    clientCertificates: _clientCertificates,
                    clientCertificateSelector: _clientCertificateSelector,
                    enabledProtocols: _enabledSslProtocols,
                    serverCertificateValidator: _serverCertificateValidator);
            }

            /// <summary>
            /// Sets whether to check certificate revocation.
            /// </summary>
            public void CheckCertificateRevocation(bool value)
            {
                _checkCertificateRevocation = value;
            }

            /// <summary>
            /// Adds the client certificate.
            /// </summary>
            /// <param name="certificate">The certificate.</param>
            public void AddClientCertificate(X509Certificate certificate)
            {
                if (_clientCertificates == null)
                {
                    _clientCertificates = new X509CertificateCollection();
                }
                _clientCertificates.Add(certificate);
            }

            /// <summary>
            /// Adds the client certificates.
            /// </summary>
            /// <param name="certificates">The certificates.</param>
            public void AddClientCertificates(IEnumerable<X509Certificate> certificates)
            {
                if (_clientCertificates == null)
                {
                    _clientCertificates = new X509CertificateCollection();
                }
                _clientCertificates.AddRange(certificates.ToArray());
            }

            /// <summary>
            /// Set the local certificate selection callback.
            /// </summary>
            /// <param name="selector">The selector.</param>
            public void SelectClientCertificateWith(LocalCertificateSelectionCallback selector)
            {
                _clientCertificateSelector = selector;
            }

            /// <summary>
            /// Set the ssl protocols to use.
            /// </summary>
            /// <param name="protocols">The protocols.</param>
            public void UseSslProtocols(SslProtocols protocols)
            {
                _enabledSslProtocols = protocols;
            }

            /// <summary>
            /// Sets the validator for remote certificates.
            /// </summary>
            /// <param name="validator">The validator.</param>
            public void ValidateServerCertificateWith(RemoteCertificateValidationCallback validator)
            {
                _serverCertificateValidator = validator;
            }
        }
    }
}