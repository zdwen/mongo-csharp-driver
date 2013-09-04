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
using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver.Core.Configuration.Resolvers;
using MongoDB.Driver.Core.Security;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Configuration
{
    internal class DbConnectionStringConfigurationPropertiesAdapter : IDbConfigurationPropertyProvider
    {
        // private fields
        private readonly DbConnectionString _connectionString;

        // constructors
        public DbConnectionStringConfigurationPropertiesAdapter(DbConnectionString connectionString)
        {
            Ensure.IsNotNull("connectionString", connectionString);

            _connectionString = connectionString;
        }

        // public methods
        public bool TryGetValue(DbConfigurationProperty property, out object value)
        {
            if (property.Name == DbConfigurationProperties.Network.ConnectTimeout.Name)
            {
                if (_connectionString.ConnectTimeout.HasValue)
                {
                    value = _connectionString.ConnectTimeout.Value;
                    return true;
                }
            }
            else if (property.Name == DbConfigurationProperties.Network.ReadTimeout.Name ||
                property.Name == DbConfigurationProperties.Network.WriteTimeout.Name)
            {
                if (_connectionString.SocketTimeout.HasValue)
                {
                    value = _connectionString.SocketTimeout.Value;
                    return true;
                }
            }
            else if (property.Name == DbConfigurationProperties.Ssl.RemoteCertificateValidationCallbackProperty.Name)
            {
                if (_connectionString.SslVerifyCertificate.HasValue && _connectionString.SslVerifyCertificate.Value)
                {
                    value = new RemoteCertificateValidationCallback((o, c, ch, s) => true);
                    return true;
                }
            }
            else if (property.Name == DbConfigurationProperties.Authentication.Credentials.Name)
            {
                if (!string.IsNullOrEmpty(_connectionString.Username))
                {
                    value = MongoCredential.FromComponents(
                        _connectionString.AuthMechanism,
                        _connectionString.AuthSource ?? _connectionString.DatabaseName,
                        _connectionString.Username,
                        _connectionString.Password);
                    return true;
                }
            }
            else if (property.Name == DbConfigurationProperties.Connection.MaxIdleTime.Name)
            {
                if (_connectionString.MaxIdleTime.HasValue)
                {
                    value = _connectionString.MaxIdleTime.Value;
                    return true;
                }
            }
            else if (property.Name == DbConfigurationProperties.Connection.MaxLifeTime.Name)
            {
                if (_connectionString.MaxLifeTime.HasValue)
                {
                    value = _connectionString.MaxLifeTime.Value;
                    return true;
                }
            }
            else if (property.Name == DbConfigurationProperties.Pool.MaxSize.Name)
            {
                if (_connectionString.MaxPoolSize.HasValue)
                {
                    value = _connectionString.MaxPoolSize.Value;
                    return true;
                }
            }
            else if (property.Name == DbConfigurationProperties.Pool.MaxWaitQueueSizeMultiple.Name)
            {
                if (_connectionString.WaitQueueMultiple.HasValue)
                {
                    value = _connectionString.WaitQueueMultiple.Value;
                    return true;
                }
            }
            else if (property.Name == DbConfigurationProperties.Pool.MinSize.Name)
            {
                if (_connectionString.MinPoolSize.HasValue)
                {
                    value = _connectionString.MinPoolSize.Value;
                    return true;
                }
            }
            else if (property.Name == DbConfigurationProperties.Cluster.Hosts.Name)
            {
                value = _connectionString.Hosts;
                return true;
            }
            else if (property.Name == DbConfigurationProperties.Cluster.ReplicaSetName.Name)
            {
                if (!string.IsNullOrEmpty(_connectionString.ReplicaSet))
                {
                    value = _connectionString.ReplicaSet;
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}