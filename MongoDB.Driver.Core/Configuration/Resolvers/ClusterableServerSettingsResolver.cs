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
    internal class ClusterableServerSettingsResolver : TypedDbDependencyResolver<ClusterableServerSettings>
    {
        public static readonly DbConfigurationProperty ConnectRetryFrequencyProperty = new DbConfigurationProperty("server.connectRetryFrequency", typeof(TimeSpan));
        public static readonly DbConfigurationProperty HeartbeatFrequency = new DbConfigurationProperty("server.heartbeatFrequency", typeof(TimeSpan));
        public static readonly DbConfigurationProperty MaxDocumentSizeDefault = new DbConfigurationProperty("server.maxDocumentSizeDefault", typeof(int));
        public static readonly DbConfigurationProperty MaxMessageSizeDefault = new DbConfigurationProperty("server.maxMessageSizeDefault", typeof(int));

        protected override ClusterableServerSettings Resolve(IDbConfigurationContainer container)
        {
            var props = container.Resolve<IDbConfigurationPropertyProvider>();

            return ClusterableServerSettings.Create(x =>
            {
                TimeSpan connectRetryFrequency;
                if (props.TryGetValue<TimeSpan>(ConnectRetryFrequencyProperty, out connectRetryFrequency))
                {
                    x.SetConnectRetryFrequency(connectRetryFrequency);
                }

                TimeSpan heartbeatFrequency;
                if (props.TryGetValue<TimeSpan>(HeartbeatFrequency, out heartbeatFrequency))
                {
                    x.SetHeartbeatFrequency(heartbeatFrequency);
                }

                int maxDocumentSizeDefault;
                if (props.TryGetValue<int>(MaxDocumentSizeDefault, out maxDocumentSizeDefault))
                {
                    x.SetMaxDocumentSizeDefault(maxDocumentSizeDefault);
                }

                int maxMessageSizeDefault;
                if (props.TryGetValue<int>(MaxMessageSizeDefault, out maxMessageSizeDefault))
                {
                    x.SetMaxMessageSizeDefault(maxMessageSizeDefault);
                }
            });
        }
    }
}