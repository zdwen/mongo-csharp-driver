using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Protocol;

namespace MongoDB.Driver.Core.Mocks
{
    public class MockChannel : ChannelBase
    {
        private readonly IConnection _connection;

        public MockChannel(IConnection connection)
        {
            _connection = connection;
        }

        public override DnsEndPoint DnsEndPoint
        {
            get { return _connection.DnsEndPoint; }
        }

        public override ReplyMessage Receive(ChannelReceiveArgs parameters)
        {
            return _connection.Receive();
        }

        public override void Send(IRequestNetworkPacket message)
        {
            _connection.Send(message);
        }
    }
}