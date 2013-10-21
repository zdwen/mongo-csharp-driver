using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Mocks
{
    internal class MockChannelProvider : ChannelProviderBase
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly DnsEndPoint _dnsEndPoint;
        private readonly ServerId _serverId;

        public MockChannelProvider(ServerId serverId, DnsEndPoint dnsEndPoint, IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _dnsEndPoint = dnsEndPoint;
            _serverId = serverId;
        }

        public override DnsEndPoint DnsEndPoint
        {
            get { return _dnsEndPoint; }
        }

        public override ServerId ServerId
        {
            get { return _serverId; }
        }

        public override void Initialize()
        {
            // YAY!!!
        }

        public override IChannel GetChannel(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return new MockChannel(_connectionFactory.Create(_serverId, _dnsEndPoint));
        }
    }
}