using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core
{
    public class ClusterBuilder
    {
        public ICluster BuildCluster()
        {
            var config = new DbConfiguration();
            Configure(config);

            return config.BuildCluster();
        }

        protected virtual void Configure(DbConfiguration configuration)
        {
            var connString = Environment.GetEnvironmentVariable("MONGO_URI") ?? "mongodb://localhost";

            configuration.ConfigureWithConnectionString(connString);
        }
    }
}