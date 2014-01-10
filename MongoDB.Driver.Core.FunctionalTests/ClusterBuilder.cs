using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
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
            var connString = new DbConnectionString(Environment.GetEnvironmentVariable("MONGO_URI") ?? "mongodb://localhost");
            configuration.ConfigureWithConnectionString(connString);

            if (connString.Ssl.HasValue && connString.Ssl.Value)
            {
                var sslCertFile = Environment.GetEnvironmentVariable("MONGO_SSL_CERT_FILE");
                if (sslCertFile != null)
                {
                    configuration.ConfigureSsl(ssl =>
                    {
                        var password = Environment.GetEnvironmentVariable("MONGO_SSL_CERT_PASS");
                        X509Certificate cert;
                        if(password == null)
                        {
                            cert = new X509Certificate2(sslCertFile);
                        }
                        else
                        {
                            cert = new X509Certificate2(sslCertFile, password);
                        }
                        ssl.AddClientCertificate(cert);
                    });
                }
            }
        }
    }
}