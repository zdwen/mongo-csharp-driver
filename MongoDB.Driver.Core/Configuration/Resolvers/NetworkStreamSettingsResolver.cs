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
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    internal class NetworkStreamSettingsResolver : TypedDbDependencyResolver<NetworkStreamSettings>
    {
        protected override NetworkStreamSettings Resolve(IDbConfigurationContainer container)
        {
            var props = container.Resolve<IDbConfigurationPropertyProvider>();

            return NetworkStreamSettings.Create(x =>
            {
                TimeSpan connectTimeout;
                if(props.TryGetValue<TimeSpan>(DbConfigurationProperties.Network.ConnectTimeout, out connectTimeout))
                {
                    x.SetConnectTimeout(connectTimeout);
                }

                TimeSpan readTimeout;
                if (props.TryGetValue<TimeSpan>(DbConfigurationProperties.Network.ReadTimeout, out readTimeout))
                {
                    x.SetReadTimeout(readTimeout);
                }

                int tcpReceiveBufferSize;
                if (props.TryGetValue<int>(DbConfigurationProperties.Network.TcpReceiveBufferSize, out tcpReceiveBufferSize))
                {
                    x.SetTcpReceiveBufferSize(tcpReceiveBufferSize);
                }

                int tcpSendBufferSize;
                if (props.TryGetValue<int>(DbConfigurationProperties.Network.TcpSendBufferSize, out tcpSendBufferSize))
                {
                    x.SetTcpReceiveBufferSize(tcpSendBufferSize);
                }

                TimeSpan writeTimeout;
                if (props.TryGetValue<TimeSpan>(DbConfigurationProperties.Network.WriteTimeout, out writeTimeout))
                {
                    x.SetReadTimeout(writeTimeout);
                }
            });
        }
    }
}