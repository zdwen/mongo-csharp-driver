using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Occurs when a cluster is opened.
    /// </summary>
    public class ClusterOpenedEvent
    {
        // private fields
        private readonly ClusterId _clusterId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterOpenedEvent"/> class.
        /// </summary>
        /// <param name="clusterId">The cluster identifier.</param>
        public ClusterOpenedEvent(ClusterId clusterId)
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
