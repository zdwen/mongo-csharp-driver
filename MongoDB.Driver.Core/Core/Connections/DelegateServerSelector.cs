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

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Selects a server based on a provided delegate.
    /// </summary>
    public class DelegateServerSelector : IServerSelector
    {
        // private fields
        private readonly string _description;
        private readonly Func<ServerDescription, bool> _selector;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateServerSelector" /> class.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="selector">The selector.</param>
        public DelegateServerSelector(string description, Func<ServerDescription, bool> selector)
        {
            _description = description;
            _selector = selector;
        }

        // public methods
        /// <summary>
        /// Selects a server from the provided servers.
        /// </summary>
        /// <param name="serverDescriptions">The server descriptions.</param>
        /// <returns>The selected server or <c>null</c> if none match.</returns>
        public IEnumerable<ServerDescription> SelectServers(IEnumerable<ServerDescription> serverDescriptions)
        {
            return serverDescriptions.Where(_selector);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return _description;
        }
    }
}