using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Mocks
{
    internal class MockConnectionFactory : IConnectionFactory
    {
        private object _lock = new object();

        private readonly List<Tuple<Func<BsonDocument, bool>, Func<IEnumerable<BsonDocument>>>> _responses;
        private readonly List<MockConnection> _createdConnections;
        private bool _isServerDead;

        public MockConnectionFactory()
        {
            _createdConnections = new List<MockConnection>();
            _responses = new List<Tuple<Func<BsonDocument, bool>, Func<IEnumerable<BsonDocument>>>>();
        }

        public int CreatedConnectionCount
        {
            get
            {
                lock (_lock)
                {
                    return _createdConnections.Count;
                }
            }
        }

        public IConnection Create(ServerId serverId, DnsEndPoint dnsEndPoint)
        {
            lock (_lock)
            {
                var created = new MockConnection(serverId, dnsEndPoint, _responses);
                _createdConnections.Add(created);
                if (_isServerDead)
                {
                    created.ServerHasDied();
                }
                return created;
            }
        }

        public void RegisterOpenCallback(Action callback)
        {
            lock (_lock)
            {
                foreach (var createdConnection in _createdConnections)
                {
                    createdConnection.RegisterOpenCallback(callback);
                }
            }
        }

        public void RegisterResponse(Func<BsonDocument, bool> sent, Func<IEnumerable<BsonDocument>> response)
        {
            lock (_lock)
            {
                _responses.Add(Tuple.Create(sent, response));
                foreach (var createdConnection in _createdConnections)
                {
                    createdConnection.RegisterResponse(sent, response);
                }
            }
        }

        public void RegisterResponse(Func<BsonDocument, bool> sent, Func<BsonDocument> response)
        {
            RegisterResponse(sent, () => new[] { response() });
        }

        public void RegisterResponse(string commandName, Func<BsonDocument> response)
        {
            RegisterResponse(d => d.GetElement(0).Name == commandName, response);
        }

        public void RegisterResponse(string commandName, BsonDocument response)
        {
            RegisterResponse(commandName, () => response );
        }

        public void ServerHasDied()
        {
            lock (_lock)
            {
                _isServerDead = true;
                foreach (var createdConnection in _createdConnections)
                {
                    createdConnection.ServerHasDied();
                }
            }
        }

        public void ServerIsAlive()
        {
            lock (_lock)
            {
                _isServerDead = false;
            }
        }
    }
}