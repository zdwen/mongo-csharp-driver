/* Copyright 2010-2013 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using MongoDB.Driver.Core.Protocol;

namespace MongoDB.Driver.Core.Connections
{
    internal class PipelinedChannelProvider : ChannelProviderBase
    {
        // private fields
        private readonly IConnectionFactory _connectionFactory;
        private readonly DnsEndPoint _dnsEndPoint;
        private readonly Holder[] _holders;

        // constructors
        public PipelinedChannelProvider(DnsEndPoint dnsEndPoint, IConnectionFactory connectionFactory, int numberOfConcurrentConnections)
        {
            _dnsEndPoint = dnsEndPoint;
            _connectionFactory = connectionFactory;

            _holders = new Holder[numberOfConcurrentConnections];
        }

        // public properties
        public override DnsEndPoint DnsEndPoint
        {
            get { return _dnsEndPoint; }
        }

        // public methods
        public override void Initialize()
        {
            for (int i = 0; i < _holders.Length; i++)
            {
                _holders[i] = new Holder();
            }
        }

        public override IChannel GetChannel(TimeSpan timeout, System.Threading.CancellationToken cancellationToken)
        {
            return new PipelinedChannel(this);
        }

        // private methods
        private ReplyMessage ReceiveMessage(ReceiveMessageParameters parameters)
        {
            var holderIndex = parameters.RequestId % _holders.Length;
            var holder = _holders[holderIndex];

            var mre = new ManualResetEventSlim();
            holder.AddWaiter(parameters.RequestId, mre);

            mre.Wait();

            ReplyMessage message;
            if (!holder.Replies.TryGetValue(parameters.RequestId, out message))
            {
                throw new Exception("Big problems 3!");
            }

            return message;
        }

        private void SendMessage(IRequestMessage message)
        {
            var holderIndex = message.RequestId % _holders.Length;
            var holder = _holders[holderIndex];

            lock (holder.SendLock)
            {
                if (holder.Connection == null || !holder.Connection.IsOpen)
                {
                    holder.Connection = _connectionFactory.Create(_dnsEndPoint);
                    holder.Connection.Open();
                }
                
                holder.Connection.SendMessage(message);
            }
        }

        private class Holder
        {
            private readonly object _receiveLock = new object();
            private int _numberWaiting;

            public readonly object SendLock = new object();
            public readonly ConcurrentDictionary<int, ReplyMessage> Replies = new ConcurrentDictionary<int, ReplyMessage>();
            public readonly ConcurrentDictionary<int, ManualResetEventSlim> Waiters = new ConcurrentDictionary<int, ManualResetEventSlim>();
            public IConnection Connection;

            public void AddWaiter(int requestId, ManualResetEventSlim mre)
            {
                if (!Waiters.TryAdd(requestId, mre))
                {
                    throw new Exception("Big problems!");
                }

                int oldNumberWaiting;
                lock (_receiveLock)
                {
                    oldNumberWaiting = _numberWaiting;
                    _numberWaiting++;
                }

                if (oldNumberWaiting == 0)
                {
                    ThreadPool.QueueUserWorkItem(_ => ReceiveMessage());
                }
            }

            private void ReceiveMessage()
            {
                int numberWaiting;
                do
                {
                    var message = Connection.ReceiveMessage();

                    lock (_receiveLock)
                    {
                        _numberWaiting--;
                        numberWaiting = _numberWaiting;
                    }

                    if (!Replies.TryAdd(message.ResponseTo, message))
                    {
                        throw new Exception("Big problems 2!");
                    }

                    ManualResetEventSlim mre;
                    if (Waiters.TryRemove(message.ResponseTo, out mre))
                    {
                        mre.Set();
                    }
                }
                while (numberWaiting > 0);
            }
        }

        private class PipelinedChannel : ChannelBase
        {
            private readonly PipelinedChannelProvider _provider;

            public PipelinedChannel(PipelinedChannelProvider provider)
            {
                _provider = provider;
            }

            public override DnsEndPoint DnsEndPoint
            {
                get { return _provider.DnsEndPoint; }
            }

            public override ReplyMessage ReceiveMessage(ReceiveMessageParameters parameters)
            {
                return _provider.ReceiveMessage(parameters);
            }

            public override void SendMessage(IRequestMessage message)
            {
                _provider.SendMessage(message);
            }
        }
    }
}