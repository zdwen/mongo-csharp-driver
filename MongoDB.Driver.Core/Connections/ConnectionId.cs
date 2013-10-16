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
    /// Represents the identify of a connection.
    /// </summary>
    public sealed class ConnectionId : IEquatable<ConnectionId>
    {
        // private fields
        private readonly ServerId _serverId;
        private readonly string _value;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionId"/> class.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        public ConnectionId(ServerId serverId)
            : this(serverId, "*" + IdGenerator<IConnection>.GetNextId().ToString() + "*")
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerId" /> class.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="value">The value.</param>
        public ConnectionId(ServerId serverId, string value)
        {
            Ensure.IsNotNull("serverId", serverId);
            Ensure.IsNotNull("value", value);

            _serverId = serverId;
            _value = value;
        }

        // public properties
        /// <summary>
        /// Gets the server identifier.
        /// </summary>
        public ServerId ServerId
        {
            get { return _serverId; }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public string Value
        {
            get { return _value; }
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
            return Equals(obj as ConnectionId);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(ConnectionId other)
        {
            if (other == null)
            {
                return false;
            }

            return _serverId.Equals(other._serverId)
                && _value.Equals(other._value);
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
                .Hash(_serverId)
                .Hash(_value)
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
            return string.Format("{0}:{1}", _serverId, _value);
        }
    }
}