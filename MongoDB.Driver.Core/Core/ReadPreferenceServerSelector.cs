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
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core
{
    /// <summary>
    /// Selects a server from a <see cref="ICluster" /> based on a read preference.
    /// </summary>
    public class ReadPreferenceServerSelector : ConnectedServerSelector
    {
        // private fields
        private readonly ReadPreference _readPreference;
        private readonly LatencyLimitingServerSelector _latencySelector;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadPreferenceServerSelector" /> class.
        /// </summary>
        /// <param name="readPreference">The read preference.</param>
        public ReadPreferenceServerSelector(ReadPreference readPreference)
        {
            _readPreference = readPreference;
            _latencySelector = new LatencyLimitingServerSelector(readPreference.SecondaryAcceptableLatency);
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
            return string.Format("a read preference of {0} and {1}", _readPreference, _latencySelector);
        }

        // protected methods
        /// <summary>
        /// Selects a server from the connected servers.
        /// </summary>
        /// <param name="servers">The servers.</param>
        /// <returns>The selected server or <c>null</c> if none match.</returns>
        protected override IEnumerable<ServerDescription> SelectServersFromConnectedServers(IEnumerable<ServerDescription> servers)
        {
            IEnumerable<ServerDescription> selected;
            switch(_readPreference.ReadPreferenceMode)
            {
                case ReadPreferenceMode.Primary:
                    selected = servers.Where(n => n.Type.CanWrite());
                    break;
                case ReadPreferenceMode.PrimaryPreferred:
                    selected = servers.Where(n => n.Type.CanWrite());
                    if (!selected.Any())
                    {
                        selected = FilterServersByTags(servers.Where(n => n.Type == ServerType.ReplicaSetSecondary));
                    }
                    break;
                case ReadPreferenceMode.Secondary:
                    selected = FilterServersByTags(servers.Where(n => n.Type == ServerType.ReplicaSetSecondary));
                    break;
                case ReadPreferenceMode.SecondaryPreferred:
                    selected = FilterServersByTags(servers.Where(n => n.Type == ServerType.ReplicaSetSecondary));
                    if (!selected.Any())
                    {
                        selected = servers.Where(n => n.Type.CanWrite());
                    }
                    break;
                case ReadPreferenceMode.Nearest:
                    selected = FilterServersByTags(servers.Where(n => n.Type.CanWrite() || n.Type == ServerType.ReplicaSetSecondary));
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Invalid ReadPreferenceMode {0}", _readPreference.ReadPreferenceMode));
            }

            return _latencySelector.SelectServers(selected);
        }

        // private methods
        private bool DoesTagSetMatchServer(ReplicaSetTagSet tagSet, ServerDescription server)
        {
            // an empty tag set matches anything
            if (!server.Type.IsReplicaSetMember() || tagSet.Count == 0)
            {
                return true;
            }

            return tagSet.All(ts => server.ReplicaSetInfo.Tags.Any(x => x.Key == ts.Name && x.Value == ts.Value));
        }

        private IEnumerable<ServerDescription> FilterServersByTags(IEnumerable<ServerDescription> servers)
        {
            var tagSets = _readPreference.TagSets;
            if (tagSets == null)
            {
                tagSets = new[] { new ReplicaSetTagSet() };
            }
            foreach (var tagSet in tagSets)
            {
                var selected = servers.Where(n => DoesTagSetMatchServer(tagSet, n));

                // stop processing now that we've found a matching one...
                if (selected.Any())
                {
                    return selected;
                }
            }

            return Enumerable.Empty<ServerDescription>();
        }
    }
}