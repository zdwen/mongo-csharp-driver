using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Support;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents the identify of a server.
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

            return _clusterId.Equals(other._clusterId)
                && _address.Equals(other._address);
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
            return string.Format("{0}:{1}:{2}", _clusterId, _address.Host, _address.Port);
        }
    }
}