using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using MongoDB.Driver.Core.Connections;
using NSubstitute;

namespace MongoDB.Driver.Core.Mocks
{
    public class MockServer : ClusterableServerBase
    {
        private readonly ServerDescription _connectingDescription;
        private readonly DnsEndPoint _dnsEndPoint;

        private ServerDescription _currentDescription;

        public MockServer(DnsEndPoint dnsEndPoint)
        {
            _dnsEndPoint = dnsEndPoint;
            _connectingDescription = ServerDescriptionBuilder.Build(x =>
            {
                x.DnsEndPoint(dnsEndPoint);
                x.Status(ServerStatus.Connecting);
            });

            _currentDescription = ServerDescriptionBuilder.Build(x =>
            {
                x.DnsEndPoint(dnsEndPoint);
                x.Status(ServerStatus.Disconnected);
            });
        }

        public override ServerDescription Description
        {
            get { return Interlocked.CompareExchange(ref _currentDescription, null, null); }
        }

        public DnsEndPoint DnsEndPoint
        {
            get { return _dnsEndPoint; }
        }

        public override event EventHandler<ChangedEventArgs<ServerDescription>> DescriptionChanged;

        public override IChannel GetChannel(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Substitute.For<IChannel>();
        }

        protected override void Dispose(bool disposing)
        {
            var disposedDescription = ServerDescriptionBuilder.Build(x =>
            {
                x.DnsEndPoint(_dnsEndPoint);
                x.Status(ServerStatus.Disposed);
            });

            SetDescription(disposedDescription);
        }

        public override void Initialize()
        {
            SetDescription(_connectingDescription);
        }

        public override void Invalidate()
        {
            SetDescription(_connectingDescription);
        }

        public void SetDescription(ServerDescription description)
        {
            var old = Interlocked.Exchange(ref _currentDescription, description);
            if (DescriptionChanged != null)
            {
                DescriptionChanged(this, new ChangedEventArgs<ServerDescription>(old, description));
            }
        }

        public void SetDescription(ServerType serverType, ReplicaSetInfo replicaSetInfo)
        {
            var description = ServerDescriptionBuilder.Build(b =>
            {
                b.DnsEndPoint(_dnsEndPoint);
                b.ReplicaSetInfo(replicaSetInfo);
                b.Status(ServerStatus.Connected);
                b.Type(serverType);
            });

            SetDescription(description);
        }
    }
}