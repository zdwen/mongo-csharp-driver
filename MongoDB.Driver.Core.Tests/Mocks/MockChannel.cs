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
using MongoDB.Driver.Core.Protocol.Messages;

namespace MongoDB.Driver.Core.Mocks
{
    internal class MockChannel : ChannelBase
    {
        private readonly IConnection _connection;

        public MockChannel(IConnection connection)
        {
            _connection = connection;
        }

        public override ConnectionId ConnectionId
        {
            get { return _connection.Id; }
        }

        public override DnsEndPoint DnsEndPoint
        {
            get { return _connection.DnsEndPoint; }
        }

        public override ReplyMessage Receive(ChannelReceiveArgs parameters)
        {
            return _connection.Receive();
        }

        public override void Send(IRequestPacket message)
        {
            _connection.Send(message);
        }
    }
}