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

            return config.BuildSessionFactory().Cluster;
        }

        protected virtual void Configure(DbConfiguration configuration)
        {
            var settings = GetTestSettings();
            var connString = settings.GetValueOrDefault("ConnectionString", "mongodb://localhost");

            configuration.UseConnectionString(connString);
        }

        protected virtual ITestSettings GetTestSettings()
        {
            return new EnvironmentVariableTestSettings();
        }
    }
}