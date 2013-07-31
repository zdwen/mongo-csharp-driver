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
        private ServerDescription _nextDescription;

        public MockServer(DnsEndPoint dnsEndPoint)
        {
            _dnsEndPoint = dnsEndPoint;
            _connectingDescription = ServerDescriptionBuilder.Build(x =>
            {
                x.DnsEndPoint(dnsEndPoint);
            });

            _currentDescription = _connectingDescription;
            _nextDescription = _connectingDescription;
        }

        public override ServerDescription Description
        {
            get { return Interlocked.CompareExchange(ref _currentDescription, null, null); }
        }

        public DnsEndPoint DnsEndPoint
        {
            get { return _dnsEndPoint; }
        }

        public override event EventHandler<ServerDescriptionChangedEventArgs<ServerDescription>> DescriptionUpdated;

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

            SetNextDescription(disposedDescription);
            ApplyChanges();
        }

        public override void Initialize()
        {
            ApplyChanges();
        }

        public override void Invalidate()
        {
            var next = Interlocked.CompareExchange(ref _nextDescription, null, null);
            SetNextDescription(_connectingDescription);
            ApplyChanges();
            SetNextDescription(next);
        }

        public void ApplyChanges()
        {
            var next = Interlocked.CompareExchange(ref _nextDescription, null, null);
            Interlocked.Exchange(ref _currentDescription, next);
            if (DescriptionUpdated != null)
            {
                DescriptionUpdated(this, new ServerDescriptionChangedEventArgs<ServerDescription>(next));
            }
        }

        public void SetNextDescription(ServerType serverType, ReplicaSetInfo replicaSetInfo)
        {
            var description = ServerDescriptionBuilder.Build(b =>
            {
                b.DnsEndPoint(_dnsEndPoint);
                b.ReplicaSetInfo(replicaSetInfo);
                b.Status(ServerStatus.Connected);
                b.Type(serverType);
            });

            SetNextDescription(description);
        }

        public void SetNextDescription(ServerDescription description)
        {
            Interlocked.Exchange(ref _nextDescription, description);
        }
    }
}