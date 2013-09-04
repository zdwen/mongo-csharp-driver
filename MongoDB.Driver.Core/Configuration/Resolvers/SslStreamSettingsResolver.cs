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
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Security;

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    internal class SslStreamSettingsResolver : TypedDbDependencyResolver<SslStreamSettings>
    {
        protected override SslStreamSettings Resolve(IDbConfigurationContainer container)
        {
            var props = container.Resolve<IDbConfigurationPropertyProvider>();

            return SslStreamSettings.Create(x =>
            {
                bool checkCertificateRevocation;
                if (props.TryGetValue(DbConfigurationProperties.Ssl.CheckCertificateRevocationProperty, out checkCertificateRevocation))
                {
                    x.CheckCertificateRevocation(checkCertificateRevocation);
                }

                IEnumerable<X509Certificate> clientCertificates;
                if (props.TryGetValue(DbConfigurationProperties.Ssl.ClientCertificatesProperty, out clientCertificates))
                {
                    x.AddClientCertificates(clientCertificates);
                }

                LocalCertificateSelectionCallback selector;
                if (props.TryGetValue(DbConfigurationProperties.Ssl.LocalCertificateSelectionCallbackProperty, out selector))
                {
                    x.SelectClientCertificateWith(selector);
                }

                SslProtocols protocols;
                if (props.TryGetValue(DbConfigurationProperties.Ssl.ProtocolsProperty, out protocols))
                {
                    x.UseSslProtocols(protocols);
                }

                RemoteCertificateValidationCallback validator;
                if (props.TryGetValue(DbConfigurationProperties.Ssl.RemoteCertificateValidationCallbackProperty, out validator))
                {
                    x.ValidateServerCertificateWith(validator);
                }
            });
        }
    }
}