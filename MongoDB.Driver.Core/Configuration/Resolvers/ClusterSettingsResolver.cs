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
using System.Net;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    internal class ClusterSettingsResolver : TypedDbDependencyResolver<ClusterSettings>
    {
        protected override ClusterSettings Resolve(IDbConfigurationContainer container)
        {
            var props = container.Resolve<IDbConfigurationPropertyProvider>();

            return ClusterSettings.Create(x =>
            {
                IEnumerable<DnsEndPoint> hosts;
                if (props.TryGetValue(DbConfigurationProperties.Cluster.Hosts, out hosts))
                {
                    x.AddHosts(hosts);
                }

                string replicaSetName;
                if (props.TryGetValue(DbConfigurationProperties.Cluster.ReplicaSetName, out replicaSetName))
                {
                    x.SetReplicaSetName(replicaSetName);
                }

                ClusterType clusterType;
                if (props.TryGetValue(DbConfigurationProperties.Cluster.ClusterType, out clusterType))
                {
                    x.SetType(clusterType);
                }
            });
        }
    }
}