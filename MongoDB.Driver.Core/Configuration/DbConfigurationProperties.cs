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
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Security;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// Well-known DbConfigurationProperties.
    /// </summary>
    public static class DbConfigurationProperties
    {
        /// <summary>
        /// Authentication related properties.
        /// </summary>
        public static class Authentication
        {
            /// <summary>
            /// Set authentication protocols.
            /// </summary>
            public static readonly DbConfigurationProperty AuthenticationProtocols = new DbConfigurationProperty("auth.protocols", typeof(IEnumerable<IAuthenticationProtocol>));
            /// <summary>
            /// Sets credentials.
            /// </summary>
            public static readonly DbConfigurationProperty Credentials = new DbConfigurationProperty("auth.credentials", typeof(IEnumerable<MongoCredential>));
        }

        /// <summary>
        /// Cluster related properties.
        /// </summary>
        public static class Cluster
        {
            /// <summary>
            /// Sets the cluster type to use.
            /// </summary>
            public static readonly DbConfigurationProperty ClusterType = new DbConfigurationProperty("cluster.type", typeof(ClusterType));
            /// <summary>
            /// Sets the hosts to use.
            /// </summary>
            public static readonly DbConfigurationProperty Hosts = new DbConfigurationProperty("cluster.hosts", typeof(IEnumerable<DnsEndPoint>));
            /// <summary>
            /// Sets the replica set name.
            /// </summary>
            public static readonly DbConfigurationProperty ReplicaSetName = new DbConfigurationProperty("cluster.replicaSetName", typeof(string));
        }

        /// <summary>
        /// Connection related properties.
        /// </summary>
        public static class Connection
        {
            /// <summary>
            /// Sets the maximum amount of time a connection can be idle.
            /// </summary>
            public static readonly DbConfigurationProperty MaxIdleTime = new DbConfigurationProperty("conn.maxIdleTime", typeof(TimeSpan));
            /// <summary>
            /// Sets the maximum amount of time a connection should be allowed to live.
            /// </summary>
            public static readonly DbConfigurationProperty MaxLifeTime = new DbConfigurationProperty("conn.maxLifeTime", typeof(TimeSpan));
        }

        /// <summary>
        /// Network related properties.
        /// </summary>
        public static class Network
        {
            /// <summary>
            /// Sets the timeout for connecting to the server.
            /// </summary>
            public static readonly DbConfigurationProperty ConnectTimeout = new DbConfigurationProperty("net.connectTimeout", typeof(TimeSpan));
            /// <summary>
            /// Sets the timeout for reading from the server.
            /// </summary>
            public static readonly DbConfigurationProperty ReadTimeout = new DbConfigurationProperty("net.readTimeout", typeof(TimeSpan));
            /// <summary>
            /// Sets the TCP receive buffer size.
            /// </summary>
            public static readonly DbConfigurationProperty TcpReceiveBufferSize = new DbConfigurationProperty("net.tcpReceiveBufferSize", typeof(int));
            /// <summary>
            /// Sets the TCP send buffer size.
            /// </summary>
            public static readonly DbConfigurationProperty TcpSendBufferSize = new DbConfigurationProperty("net.tcpSendBufferSize", typeof(int));
            /// <summary>
            /// Sets the timeout for writing to the server.
            /// </summary>
            public static readonly DbConfigurationProperty WriteTimeout = new DbConfigurationProperty("net.writeTimeout", typeof(TimeSpan));
        }

        /// <summary>
        /// Pooling related properties.
        /// </summary>
        public static class Pool
        {
            /// <summary>
            /// Sets the maximum size of the pool.
            /// </summary>
            public static readonly DbConfigurationProperty MaxSize = new DbConfigurationProperty("pool.maxSize", typeof(int));
            /// <summary>
            /// Sets the multiple identifying the maximum size of the wait queue.
            /// </summary>
            public static readonly DbConfigurationProperty MaxWaitQueueSizeMultiple = new DbConfigurationProperty("pool.waitQueueSizeMultiple", typeof(int));
            /// <summary>
            /// Sets the minumum size of the pool.
            /// </summary>
            public static readonly DbConfigurationProperty MinSize = new DbConfigurationProperty("pool.minSize", typeof(int));
            /// <summary>
            /// Sets the frequency with which the pool should reap/create connections.
            /// </summary>
            public static readonly DbConfigurationProperty SizeMaintenanceFrequency = new DbConfigurationProperty("pool.sizeMaintenanceFrequency", typeof(TimeSpan));
        }

        /// <summary>
        /// Ssl related properties.
        /// </summary>
        public static class Ssl
        {
            /// <summary>
            /// Sets whether to check revocation of the server certificate.
            /// </summary>
            public static readonly DbConfigurationProperty CheckCertificateRevocationProperty = new DbConfigurationProperty("ssl.checkCertificateRevocation", typeof(bool));
            /// <summary>
            /// Sets the client certificates to use.
            /// </summary>
            public static readonly DbConfigurationProperty ClientCertificatesProperty = new DbConfigurationProperty("ssl.clientCertificates", typeof(IEnumerable<X509Certificate>));
            /// <summary>
            /// Sets a callback to select a local certificate.
            /// </summary>
            public static readonly DbConfigurationProperty LocalCertificateSelectionCallbackProperty = new DbConfigurationProperty("ssl.localCertificateSelector", typeof(LocalCertificateSelectionCallback));
            /// <summary>
            /// Sets the Ssl protocols to use.
            /// </summary>
            public static readonly DbConfigurationProperty ProtocolsProperty = new DbConfigurationProperty("ssl.protocols", typeof(SslProtocols));
            /// <summary>
            /// Sets a callback to validate the server certificate.
            /// </summary>
            public static readonly DbConfigurationProperty RemoteCertificateValidationCallbackProperty = new DbConfigurationProperty("ssl.removeCertificateValidator", typeof(RemoteCertificateValidationCallback));
        }
    }
}
