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
using System.Linq;
using System.Net;
using System.Text;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// A description of a server.
    /// </summary>
    public sealed class ServerDescription
    {
        // private fields
        private readonly TimeSpan _averagePingTime;
        private readonly ServerBuildInfo _buildInfo;
        private readonly DnsEndPoint _dnsEndPoint;
        private readonly ServerId _id;
        private readonly int _maxDocumentSize;
        private readonly int _maxMessageSize;
        private readonly ReplicaSetInfo _replicaSetInfo;
        private readonly ServerStatus _status;
        private readonly ServerType _type;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerDescription" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="averagePingTime">The average ping time.</param>
        /// <param name="buildInfo">The build info.</param>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <param name="maxDocumentSize">The maximum document size.</param>
        /// <param name="maxMessageSize">The maximum message size.</param>
        /// <param name="replicaSetInfo">The replica set info.</param>
        /// <param name="status">The status.</param>
        /// <param name="type">The type.</param>
        public ServerDescription(
            ServerId id, 
            TimeSpan averagePingTime,
            ServerBuildInfo buildInfo,
            DnsEndPoint dnsEndPoint,
            int maxDocumentSize, 
            int maxMessageSize,
            ReplicaSetInfo replicaSetInfo,
            ServerStatus status,
            ServerType type)
        {
            Ensure.IsNotNull("id", id);
            Ensure.IsNotNull("dnsEndPoint", dnsEndPoint);

            _id = id;
            _averagePingTime = averagePingTime;
            _buildInfo = buildInfo;
            _dnsEndPoint = dnsEndPoint;
            _maxDocumentSize = maxDocumentSize;
            _maxMessageSize = maxMessageSize;
            _replicaSetInfo = replicaSetInfo;
            _status = status;
            _type = type;
        }

        // public properties
        /// <summary>
        /// Gets the average ping time.
        /// </summary>
        public TimeSpan AveragePingTime
        {
            get { return _averagePingTime; }
        }

        /// <summary>
        /// Gets the build info.
        /// </summary>
        public ServerBuildInfo BuildInfo
        {
            get { return _buildInfo; }
        }

        /// <summary>
        /// Gets the DNS end point.
        /// </summary>
        public DnsEndPoint DnsEndPoint
        {
            get { return _dnsEndPoint; }
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        public ServerId Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets the maximum document size.
        /// </summary>
        public int MaxDocumentSize
        {
            get { return _maxDocumentSize; }
        }

        /// <summary>
        /// Gets the maximum message size.
        /// </summary>
        public int MaxMessageSize
        {
            get { return _maxMessageSize; }
        }

        /// <summary>
        /// Gets the replica set info.
        /// </summary>
        public ReplicaSetInfo ReplicaSetInfo
        {
            get { return _replicaSetInfo; }
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        public ServerStatus Status
        {
            get { return _status; }
        }

        /// <summary>
        /// Gets the type.
        /// </summary>
        public ServerType Type
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
            var builder = new StringBuilder();
            builder.AppendFormat("{{ ClusterId: '{0}', EndPoint: '{1}', Server: '{2}', Type: '{3}', Status: '{4}', PingTime: '{5}'", _id.ClusterId.Value, _dnsEndPoint, _buildInfo, _type, _status, _averagePingTime);
            if (_replicaSetInfo != null)
            {
                builder.AppendFormat(", ReplicaSetInfo: {0}", _replicaSetInfo);
            }
            builder.Append(" }");

            return builder.ToString();
        }
    }
}