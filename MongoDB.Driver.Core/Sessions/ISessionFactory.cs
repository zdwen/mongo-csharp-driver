using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Sessions
{
    /// <summary>
    /// Creates <see cref="ISession"/>s.
    /// </summary>
    public interface ISessionFactory : IDisposable
    {
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        IDbConfigurationContainer Configuration { get; }

        /// <summary>
        /// Gets the cluster.
        /// </summary>
        ICluster Cluster { get; }

        /// <summary>
        /// Begins the session.
        /// </summary>
        /// <returns>A session.</returns>
        ISession BeginSession();
    }
}