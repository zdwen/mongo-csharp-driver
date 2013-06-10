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

using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Information about a replica set member.
    /// </summary>
    public sealed class ReplicaSetInfo
    {
        private readonly string _name;
        private readonly DnsEndPoint _primary;
        private readonly List<DnsEndPoint> _members;
        private readonly IEnumerable<KeyValuePair<string, string>> _tags;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicaSetInfo" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="primary">The primary.</param>
        /// <param name="members">The members.</param>
        /// <param name="tags">The tags.</param>
        public ReplicaSetInfo(string name, DnsEndPoint primary, IEnumerable<DnsEndPoint> members, IEnumerable<KeyValuePair<string, string>> tags)
        {
            _name = name;
            _primary = primary;
            _members = members == null ? new List<DnsEndPoint>() : members.ToList();
            _tags = tags ?? Enumerable.Empty<KeyValuePair<string, string>>();
        }

        // public properties
        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the primary.
        /// </summary>
        public DnsEndPoint Primary
        {
            get { return _primary; }
        }

        /// <summary>
        /// Gets the members.
        /// </summary>
        public IEnumerable<DnsEndPoint> Members
        {
            get { return _members; }
        }

        /// <summary>
        /// Gets the tags.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> Tags
        {
            get { return _tags; }
        }
    }
}