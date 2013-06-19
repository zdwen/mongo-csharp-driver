using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Protocol;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Mocks
{
    public class MockConnection : ConnectionBase
    {
        private readonly List<Tuple<Func<BsonDocument, bool>, Func<IEnumerable<BsonDocument>>>> _responses;
        private Queue<Func<IEnumerable<BsonDocument>>> _responseQueue;
        private readonly DnsEndPoint _dnsEndPoint;
        private bool _isOpen;
        private bool _isServerDead;
        private Action _openCallback;

        public MockConnection(DnsEndPoint dnsEndPoint, List<Tuple<Func<BsonDocument, bool>, Func<IEnumerable<BsonDocument>>>> responses)
        {
            _dnsEndPoint = dnsEndPoint;
            _responses = responses ?? new List<Tuple<Func<BsonDocument, bool>, Func<IEnumerable<BsonDocument>>>>();
            _responseQueue = new Queue<Func<IEnumerable<BsonDocument>>>();
        }

        public override DnsEndPoint DnsEndPoint
        {
            get { return _dnsEndPoint; }
        }

        public override bool IsOpen
        {
            get { return _isOpen; }
        }

        public override void Open()
        {
            if (_openCallback != null)
            {
                _openCallback();
            }
            _isOpen = true;
        }

        public override ReplyMessage ReceiveMessage()
        {
            if (_responseQueue.Count > 0)
            {
                var replyDocs = _responseQueue.Dequeue()();
                return ProtocolHelper.BuildReplyMessage(replyDocs);
            }

            _isOpen = false;
            throw new AssertionException("ReceiveMessage called without any registered response.");
        }

        public override void SendMessage(IRequestMessage message)
        {
            if (_isServerDead)
            {
                _isOpen = false;
                throw new SocketException();
            }

            if (message is BufferedRequestMessage)
            {
                var doc = ProtocolHelper.ReadQueryMessage((BufferedRequestMessage)message);
                // get the last one here in case a response is changed 
                var registeredResponse = _responses.LastOrDefault(x => x.Item1(doc));
                if (registeredResponse != null)
                {
                    _responseQueue.Enqueue(registeredResponse.Item2);
                }
            }
        }

        public void RegisterOpenCallback(Action callback)
        {
            _openCallback = callback;
        }

        public void RegisterResponse(Func<BsonDocument, bool> sent, Func<IEnumerable<BsonDocument>> response)
        {
            _responses.Add(Tuple.Create(sent, response));
        }

        public void ServerHasDied()
        {
            _isServerDead = true;
        }

        protected override void Dispose(bool disposing)
        {
            _isOpen = false;
        }
    }
}