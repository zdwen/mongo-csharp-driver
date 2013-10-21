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
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents the identity of a server.
    /// </summary>
    public sealed class ServerId : IEquatable<ServerId>
    {
        // private fields
        private readonly ClusterId _clusterId;
        private readonly DnsEndPoint _address;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerId"/> class.
        /// </summary>
        public ServerId(ClusterId clusterId, DnsEndPoint address)
        {
            Ensure.IsNotNull("clusterId", clusterId);
            Ensure.IsNotNull("address", address);

            _clusterId = clusterId;
            _address = address;
        }

        // public properties
        /// <summary>
        /// Gets the cluster identifier.
        /// </summary>
        public ClusterId ClusterId
        {
            get { return _clusterId; }
        }

        /// <summary>
        /// Gets the address.
        /// </summary>
        public DnsEndPoint Address
        {
            get { return _address; }
        }

        // public methods
        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ServerId);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(ServerId other)
        {
            if (other == null)
            {
                return false;
            }

            return _clusterId.Equals(other._clusterId) && 
                _address.Equals(other._address);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_clusterId)
                .Hash(_address)
                .GetHashCode();
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0},{1}:{2}", _clusterId, _address.Host, _address.Port);
        }
    }
}