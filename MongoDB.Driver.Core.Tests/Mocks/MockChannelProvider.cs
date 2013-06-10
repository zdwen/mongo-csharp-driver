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
    public class MockChannelProvider : ChannelProviderBase
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly DnsEndPoint _dnsEndPoint;

        public MockChannelProvider(DnsEndPoint dnsEndPoint, IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _dnsEndPoint = dnsEndPoint;
        }

        public override DnsEndPoint DnsEndPoint
        {
            get { return _dnsEndPoint; }
        }

        public override void Initialize()
        {
            // YAY!!!
        }

        public override IChannel GetChannel(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return new MockChannel(_connectionFactory.Create(_dnsEndPoint));
        }
    }
}