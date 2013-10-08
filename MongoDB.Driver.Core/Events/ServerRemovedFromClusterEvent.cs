using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Occurs when a server is removed from a cluster.
    /// </summary>
    public class ServerRemovedFromClusterEvent
    {
        // private fields
        private readonly string _clusterId;
        private readonly string _serverId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerRemovedFromClusterEvent"/> class.
        /// </summary>
        /// <param name="clusterId">The cluster identifier.</param>
        /// <param name="serverId">The server identifier.</param>
        public ServerRemovedFromClusterEvent(string clusterId, string serverId)
        {
            _clusterId = clusterId;
            _serverId = serverId;
        }

        // public properties
        /// <summary>
        /// Gets or sets the cluster identifier.
        /// </summary>
        public string ClusterId
        {
            get { return _clusterId; }
        }

        /// <summary>
        /// Gets or sets the server identifier.
        /// </summary>
        public string ServerId
        {
            get { return _serverId; }
        }
    }
}
