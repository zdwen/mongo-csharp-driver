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
using System.Threading;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core
{
    /// <summary>
    /// Filters the servers by an allowed latency emanating from the first server.
    /// </summary>
    public class LatencyLimitingServerSelector : IServerSelector
    {
        // private fields
        private readonly TimeSpan _allowedLatency;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LatencyLimitingServerSelector" /> class.
        /// </summary>
        public LatencyLimitingServerSelector()
            : this(TimeSpan.FromMilliseconds(15))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LatencyLimitingServerSelector" /> class.
        /// </summary>
        /// <param name="allowedLatency">The allowed latency.</param>
        public LatencyLimitingServerSelector(TimeSpan allowedLatency)
        {
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero("allowedLatency", allowedLatency);

            _allowedLatency = allowedLatency;
        }

        // public methods
        /// <summary>
        /// Selects a server from the provided servers.
        /// </summary>
        /// <param name="servers">The servers.</param>
        /// <returns>
        /// The selected server or <c>null</c> if none match.
        /// </returns>
        public IEnumerable<ServerDescription> SelectServers(IEnumerable<ServerDescription> servers)
        {
            if (_allowedLatency == TimeSpan.FromMilliseconds(Timeout.Infinite))
            {
                return servers;
            }

            TimeSpan? first = null;
            return servers.OrderBy(s => s.AveragePingTime)
                .TakeWhile(s =>
                {
                    if (first.HasValue)
                    {
                        return s.AveragePingTime < first.Value.Add(_allowedLatency);
                    }
                    else
                    {
                        first = s.AveragePingTime;
                        return true;
                    }
                });
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("an allowed latency of {0}", _allowedLatency);
        }
    }
}