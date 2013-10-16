using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Occurs when a cluster is closed.
    /// </summary>
    public class ClusterClosedEvent
    {
        // private fields
        private readonly ClusterId _clusterId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterClosedEvent"/> class.
        /// </summary>
        /// <param name="clusterId">The cluster identifier.</param>
        public ClusterClosedEvent(ClusterId clusterId)
        {
            _clusterId = clusterId;
        }

        // public properties
        /// <summary>
        /// Gets the cluster identifier.
        /// </summary>
        public ClusterId ClusterId
        {
            get { return _clusterId; }
        }
    }
}
