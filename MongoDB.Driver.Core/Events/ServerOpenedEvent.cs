using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Occurs when a server is opened.
    /// </summary>
    public class ServerOpenedEvent
    {
        // private fields
        private readonly ServerId _serverId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerOpenedEvent" /> class.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        public ServerOpenedEvent(ServerId serverId)
        {
            _serverId = serverId;
        }

        // public properties
        /// <summary>
        /// Gets the server identifier.
        /// </summary>
        public ServerId ServerId
        {
            get { return _serverId; }
        }
    }
}
