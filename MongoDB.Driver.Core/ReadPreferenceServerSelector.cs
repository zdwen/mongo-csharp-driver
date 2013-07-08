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

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadPreferenceServerSelector" /> class.
        /// </summary>
        /// <param name="readPreference">The read preference.</param>
        public ReadPreferenceServerSelector(ReadPreference readPreference)
        {
            _readPreference = readPreference;
        }

        // public properties
        /// <summary>
        /// Gets the description of the server selector.
        /// </summary>
        public override string Description
        {
            get { return string.Format("Read Preference {0}", _readPreference.ToString()); }
        }

        /// <summary>
        /// Selects a server from the connected servers.
        /// </summary>
        /// <param name="servers">The servers.</param>
        /// <returns>The selected server or <c>null</c> if none match.</returns>
        protected override ServerDescription SelectServerFromConnectedServers(IEnumerable<ServerDescription> servers)
        {
            ServerDescription selected;
            switch(_readPreference.ReadPreferenceMode)
            {
                case ReadPreferenceMode.Primary:
                    return servers.FirstOrDefault(n => n.Type.CanWrite());
                case ReadPreferenceMode.PrimaryPreferred:
                    selected = servers.FirstOrDefault(n => n.Type.CanWrite());
                    if (selected == null)
                    {
                        selected = SelectOne(servers.Where(n => n.Type == ServerType.ReplicaSetSecondary));
                    }
                    return selected;
                case ReadPreferenceMode.Secondary:
                    return SelectOne(servers.Where(n => n.Type == ServerType.ReplicaSetSecondary));
                case ReadPreferenceMode.SecondaryPreferred:
                    selected = SelectOne(servers.Where(n => n.Type == ServerType.ReplicaSetSecondary));
                    if (selected == null)
                    {
                        selected = servers.FirstOrDefault(n => n.Type.CanWrite());
                    }
                    return selected;
                case ReadPreferenceMode.Nearest:
                    return SelectOne(servers.Where(n => n.Type.CanWrite() || n.Type == ServerType.ReplicaSetSecondary));
            }

            throw new InvalidOperationException(string.Format("Invalid ReadPreferenceMode {0}", _readPreference.ReadPreferenceMode));
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

        private ServerDescription SelectOne(IEnumerable<ServerDescription> servers)
        {
            TimeSpan? first = null;
            var tagSets = _readPreference.TagSets;
            if (tagSets == null)
            {
                tagSets = new[] { new ReplicaSetTagSet() };
            }
            foreach (var tagSet in tagSets)
            {
                var selected = servers.Where(n => DoesTagSetMatchServer(tagSet, n))
                    .OrderBy(n => n.AveragePingTime)
                    .TakeWhile(n => // take while doesn't behave like it should...
                    {
                        if (first.HasValue)
                        {
                            return n.AveragePingTime < first.Value.Add(_readPreference.SecondaryAcceptableLatency);
                        }
                        else
                        {
                            first = n.AveragePingTime;
                            return true;
                        }
                    })
                    .RandomOrDefault();

                // stop processing now that we've found a matching one...
                if (selected != null)
                {
                    return selected;
                }
            }

            return null;
        }
    }
}