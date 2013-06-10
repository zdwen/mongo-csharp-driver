using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core.Mocks
{
    public class MockReplicaSet : IClusterableServerFactory
    {
        private readonly string _replicaSetName;
        private readonly Dictionary<DnsEndPoint, ServerHelper> _servers;

        public MockReplicaSet(string replicaSetName)
        {
            _servers = new Dictionary<DnsEndPoint, ServerHelper>();
            _replicaSetName = replicaSetName;
        }

        public IEnumerable<DnsEndPoint> Members
        {
            get { return _servers.Values.Select(x => x.Server.DnsEndPoint); }
        }

        public string ReplicaSetName
        {
            get { return _replicaSetName; }
        }

        public IClusterableServer Create(DnsEndPoint dnsEndPoint)
        {
            if (!_servers.ContainsKey(dnsEndPoint))
            {
                var server = _servers.Values.SingleOrDefault(x => x.AlternateServer != null && dnsEndPoint.Equals(x.AlternateServer.DnsEndPoint));
                if (server != null)
                {
                    return server.AlternateServer;
                }

                throw new ArgumentException(string.Format("Unknown server {0}", dnsEndPoint));
            }

            return _servers[dnsEndPoint].Server;
        }

        public void AddAlternateDnsEndPoint(DnsEndPoint canonical, DnsEndPoint alternate)
        {
            var server = _servers[canonical];
            server.AlternateServer = new MockServer(alternate);
            server.AlternateServer.SetNextDescription(ServerDescriptionBuilder.Alter(server.ConnectedDescription, b =>
            {
                b.DnsEndPoint(alternate);
            }));
        }

        public void AddMember(ServerType type, DnsEndPoint dnsEndPoint)
        {
            var server = CreateServer(type, dnsEndPoint);
            _servers.Add(dnsEndPoint, server);
        }

        public void ApplyChanges()
        {
            ReconfigureReplicaSet();
            foreach (var server in _servers.Values)
            {
                server.ApplyChanges();
            }

            foreach (var server in _servers.Values)
            {
                server.RaiseDescriptionChangedEvent();
            }
        }

        public void ChangeServerType(DnsEndPoint dnsEndPoint, ServerType type)
        {
            var server = _servers[dnsEndPoint];
            if (type == ServerType.ReplicaSetPrimary)
            {
                var oldPrimary = _servers.Values.SingleOrDefault(x => x.ConnectedDescription.Type == ServerType.ReplicaSetPrimary);
                if (oldPrimary != null)
                {
                    oldPrimary.ConnectedDescription = ServerDescriptionBuilder.Alter(oldPrimary.ConnectedDescription, b =>
                    {
                        b.Type(ServerType.ReplicaSetSecondary);
                    });
                }
            }
            server.ConnectedDescription = ServerDescriptionBuilder.Alter(server.ConnectedDescription, b =>
            {
                b.Type(type);
            });
        }

        public void Kill(DnsEndPoint dnsEndPoint)
        {
            var server = _servers[dnsEndPoint];
            server.Disconnect();
        }

        public void Remove(DnsEndPoint dnsEndPoint)
        {
            _servers.Remove(dnsEndPoint);
        }

        public void Revive(DnsEndPoint dnsEndPoint)
        {
            var server = _servers[dnsEndPoint];
            server.Connect();
        }

        public void RaiseChangedEvent(DnsEndPoint dnsEndPoint)
        {
            var server = _servers[dnsEndPoint];
            server.Connect();
        }

        private ServerHelper CreateServer(ServerType type, DnsEndPoint dnsEndPoint)
        {
            var server = new MockServer(dnsEndPoint);
            var helper = new ServerHelper
            {
                ConnectedDescription = ServerDescriptionBuilder.Alter(server.Description, b => 
                {
                    b.DnsEndPoint(dnsEndPoint);
                    b.Status(ServerStatus.Connected);
                    b.Type(type);
                }),
                Server = new MockServer(dnsEndPoint),
            };

            helper.Connect();
            return helper;
        }

        private void ReconfigureReplicaSet()
        {
            var primary = _servers.Values.First(x => x.ConnectedDescription.Type == ServerType.ReplicaSetPrimary);
            var hosts = _servers.Values.Where(x => x.ConnectedDescription.Type.IsReplicaSetMember());
            var replicaSetInfo = new ReplicaSetInfo(
                _replicaSetName,
                primary != null ? primary.Server.DnsEndPoint : null,
                hosts.Select(x => x.Server.DnsEndPoint),
                new Dictionary<string, string>());

            foreach (var server in _servers.Values)
            {
                bool amIPrimary = primary != null && primary.Server.DnsEndPoint.Equals(server.Server.DnsEndPoint);
                server.ConnectedDescription = ServerDescriptionBuilder.Alter(server.ConnectedDescription, b =>
                {
                    b.ReplicaSetInfo(replicaSetInfo);
                    if (server.ConnectedDescription.Type.IsReplicaSetMember())
                    {
                        b.Type(amIPrimary ? ServerType.ReplicaSetPrimary : ServerType.ReplicaSetSecondary);
                    }
                });
            }
        }

        private class ServerHelper
        {
            public ServerDescription ConnectedDescription { get; set; }

            public bool IsConnected { get; set; }

            public int Port
            {
                get { return Server.DnsEndPoint.Port; }
            }

            public MockServer AlternateServer { get; set; }

            public MockServer Server { get; set; }

            public void ApplyChanges()
            {
                Server.SetNextDescription(IsConnected ? ConnectedDescription : ServerDescriptionBuilder.Connecting(Server.DnsEndPoint));
                if (AlternateServer != null)
                {
                    var alt = ServerDescriptionBuilder.Alter(IsConnected ? ConnectedDescription : ServerDescriptionBuilder.Connecting(AlternateServer.DnsEndPoint), b =>
                    {
                        b.DnsEndPoint(AlternateServer.DnsEndPoint);
                    });
                    AlternateServer.SetNextDescription(alt);
                }
            }

            public void Connect()
            {
                IsConnected = true;
                Server.SetNextDescription(ConnectedDescription);
                if (AlternateServer != null)
                {
                    AlternateServer.SetNextDescription(ServerDescriptionBuilder.Alter(ConnectedDescription, b =>
                    {
                        b.DnsEndPoint(AlternateServer.DnsEndPoint);
                    }));
                }
            }

            public void Disconnect()
            {
                IsConnected = false;
                Server.SetNextDescription(ServerDescriptionBuilder.Connecting(Server.DnsEndPoint));
                if (AlternateServer != null)
                {
                    AlternateServer.SetNextDescription(ServerDescriptionBuilder.Connecting(AlternateServer.DnsEndPoint));
                }
            }

            public void RaiseDescriptionChangedEvent()
            {
                Server.ApplyChanges();
                if (AlternateServer != null)
                {
                    AlternateServer.ApplyChanges();
                }
            }
        }
    }
}