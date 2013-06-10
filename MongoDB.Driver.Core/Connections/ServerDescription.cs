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
using System.Net;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// A description of a server.
    /// </summary>
    public sealed class ServerDescription
    {
        // private fields
        private readonly DnsEndPoint _dnsEndPoint;
        private readonly TimeSpan _averagePingTime;
        private readonly ServerBuildInfo _buildInfo;
        private readonly int _maxDocumentSize;
        private readonly int _maxMessageSize;
        private readonly ReplicaSetInfo _replicaSetInfo;
        private readonly ServerStatus _status;
        private readonly ServerType _type;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerDescription" /> class.
        /// </summary>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <param name="averagePingTime">The average ping time.</param>
        /// <param name="buildInfo">The build info.</param>
        /// <param name="maxDocumentSize">The maximum document size.</param>
        /// <param name="maxMessageSize">The maximum message size.</param>
        /// <param name="replicaSetInfo">The replica set info.</param>
        /// <param name="status">The status.</param>
        /// <param name="type">The type.</param>
        public ServerDescription(
            TimeSpan averagePingTime,
            ServerBuildInfo buildInfo,
            DnsEndPoint dnsEndPoint,
            int maxDocumentSize, 
            int maxMessageSize,
            ReplicaSetInfo replicaSetInfo,
            ServerStatus status,
            ServerType type)
        {
            Ensure.IsNotNull("dnsEndPoint", dnsEndPoint);

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
    }
}