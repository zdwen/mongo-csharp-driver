using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Occurs when a cluster is opened.
    /// </summary>
    public class ClusterOpenedEvent
    {
        // private fields
        private readonly string _clusterId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterOpenedEvent"/> class.
        /// </summary>
        /// <param name="clusterId">The cluster identifier.</param>
        public ClusterOpenedEvent(string clusterId)
        {
            _clusterId = clusterId;
        }

        // public properties
        /// <summary>
        /// Gets the cluster identifier.
        /// </summary>
        public string ClusterId
        {
            get { return _clusterId; }
        }
    }
}
