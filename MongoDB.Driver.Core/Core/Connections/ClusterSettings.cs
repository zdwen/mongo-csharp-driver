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
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Settings for an <see cref="ICluster" />.
    /// </summary>
    public sealed class ClusterSettings
    {
        // public static fields
        /// <summary>
        /// The default settings.
        /// </summary>
        public static readonly ClusterSettings Defaults = new Builder().Build();

        // private fields
        private readonly ReadOnlyCollection<DnsEndPoint> _hosts;
        private readonly string _replicaSetName;
        private readonly ClusterType _type;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterSettings" /> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="hosts">The hosts.</param>
        /// <param name="replicaSetName">The name of the replica set.</param>
        internal ClusterSettings(ClusterType type, IEnumerable<DnsEndPoint> hosts, string replicaSetName)
        {
            Ensure.IsNotNull("hosts", hosts);
            Ensure.IsGreaterThan("hosts.Count", hosts.Count(), 0);

            _hosts = hosts.ToList().AsReadOnly();
            _replicaSetName = replicaSetName;
            _type = type;

            if (!string.IsNullOrEmpty(_replicaSetName))
            {
                if (type == ClusterType.Unknown)
                {
                    _type = ClusterType.ReplicaSet;
                }
                else if (type != ClusterType.ReplicaSet)
                {
                    throw new ArgumentException("When specifying a replica set name, only ClusterType.Unknown and ClusterType.ReplicaSet are valid.", "type");
                }
            }

            if (_hosts.Count > 1 && _type == ClusterType.StandAlone)
            {
                throw new ArgumentException("Multiple hosts cannot be specified when using ClusterType.StandAlone.", "hosts");
            }
        }

        // public properties
        /// <summary>
        /// Gets the connection mode.
        /// </summary>
        public ClusterConnectionMode ConnectionMode
        {
            get { return _hosts.Count == 1 ? ClusterConnectionMode.Single : ClusterConnectionMode.Multiple; }
        }

        /// <summary>
        /// Gets the hosts.
        /// </summary>
        public IEnumerable<DnsEndPoint> Hosts
        {
            get { return _hosts; }
        }

        /// <summary>
        /// Gets the name of the replica set.
        /// </summary>
        public string ReplicaSetName
        {
            get { return _replicaSetName; }
        }

        /// <summary>
        /// Gets the cluster type.
        /// </summary>
        public ClusterType Type
        {
            get { return _type; }
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
            return string.Format("{{ Type: '{0}', Hosts: [{1}], ReplicaSetName: '{2}' }}",
                _type,
                string.Join(", ", _hosts.Select(x => "'" + x.ToString() + "'")),
                _replicaSetName);
        }

        // public static methods
        /// <summary>
        /// Creates the specified callback.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <returns>The built settings.</returns>
        public static ClusterSettings Create(Action<Builder> callback)
        {
            var builder = new Builder();
            callback(builder);
            return builder.Build();
        }

        /// <summary>
        /// Used to build up <see cref="ClusterSettings"/>.
        /// </summary>
        public class Builder
        {
            private const string __defaultHost = "localhost";
            private const int __defaultPort = 27017;

            private List<DnsEndPoint> _hosts;
            private string _replicaSetName;
            private ClusterType _type;

            internal Builder()
            {
                _hosts = new List<DnsEndPoint>();
            }

            internal ClusterSettings Build()
            {
                var hosts = _hosts.ToList();
                if (hosts.Count == 0)
                {
                    hosts.Add(new DnsEndPoint(__defaultHost, __defaultPort));
                }
                return new ClusterSettings(_type, hosts, _replicaSetName);
            }

            /// <summary>
            /// Adds the host.
            /// </summary>
            /// <param name="host">The host.</param>
            /// <param name="port">The port.</param>
            public void AddHost(string host, int port = __defaultPort)
            {
                Ensure.IsNotNull("host", host);
                _hosts.Add(new DnsEndPoint(host, port));
            }

            /// <summary>
            /// Adds the host.
            /// </summary>
            /// <param name="host">The host.</param>
            public void AddHost(DnsEndPoint host)
            {
                Ensure.IsNotNull("host", host);
                _hosts.Add(host);
            }

            /// <summary>
            /// Adds the hosts.
            /// </summary>
            /// <param name="hosts">The hosts.</param>
            public void AddHosts(params DnsEndPoint[] hosts)
            {
                AddHosts((IEnumerable<DnsEndPoint>)hosts);
            }

            /// <summary>
            /// Adds the hosts.
            /// </summary>
            /// <param name="hosts">The hosts.</param>
            public void AddHosts(IEnumerable<DnsEndPoint> hosts)
            {
                Ensure.IsNotNull("hosts", hosts);
                _hosts.AddRange(hosts);
            }

            /// <summary>
            /// Clears the hosts.
            /// </summary>
            public void ClearHosts()
            {
                _hosts.Clear();
            }

            /// <summary>
            /// Sets the name of the replica set.
            /// </summary>
            /// <param name="replicaSetName">Name of the replica set.</param>
            public void SetReplicaSetName(string replicaSetName)
            {
                _replicaSetName = replicaSetName;
            }

            /// <summary>
            /// Sets the cluster type.
            /// </summary>
            /// <param name="type">The cluster type.</param>
            public void SetType(ClusterType type)
            {
                _type = type;
            }
        }
    }
}
