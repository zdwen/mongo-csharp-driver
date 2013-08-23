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
using System.Net.Sockets;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Caches dns lookups for a period of time.
    /// </summary>
    public sealed class DnsCache
    {
        // private fields
        private readonly ConcurrentDictionary<DnsEndPoint, Entry> _cache;
        private readonly AddressFamily _defaultAddressFamily;
        private readonly TimeSpan _expireAfter;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DnsCache" /> class.
        /// </summary>
        public DnsCache()
            : this(TimeSpan.FromMinutes(60), AddressFamily.InterNetwork)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsCache" /> class.
        /// </summary>
        /// <param name="expireAfter">The expire after.</param>
        public DnsCache(TimeSpan expireAfter)
            : this(expireAfter, AddressFamily.InterNetwork)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsCache" /> class.
        /// </summary>
        /// <param name="defaultAddressFamily">The default address family.</param>
        public DnsCache(AddressFamily defaultAddressFamily)
            : this(TimeSpan.FromMinutes(60), defaultAddressFamily)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsCache" /> class.
        /// </summary>
        /// <param name="expireAfter">The expire after.</param>
        /// <param name="defaultAddressFamily">The default address family.</param>
        public DnsCache(TimeSpan expireAfter, AddressFamily defaultAddressFamily)
        {
            _expireAfter = expireAfter;
            _defaultAddressFamily = defaultAddressFamily;
            _cache = new ConcurrentDictionary<DnsEndPoint, Entry>();
        }

        // public methods
        /// <summary>
        /// Resolves the specified DNS end point.
        /// </summary>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <returns></returns>
        public IPEndPoint Resolve(DnsEndPoint dnsEndPoint)
        {
            Entry entry;
            if (_cache.TryGetValue(dnsEndPoint, out entry))
            {
                if (entry.AddedAtUtc + _expireAfter < DateTime.UtcNow)
                {
                    return entry.IPEndPoint;
                }
            }

            entry = new Entry
            {
                AddedAtUtc = DateTime.UtcNow,
                IPEndPoint = ResolveIPEndPoint(dnsEndPoint, _defaultAddressFamily)
            };

            return _cache.AddOrUpdate(dnsEndPoint, entry, (key, old) => entry).IPEndPoint;
        }

        // private static methods
        private static IPEndPoint ResolveIPEndPoint(DnsEndPoint dnsEndPoint, AddressFamily defaultAddressFamily)
        {
            // TODO: how to handle when the dnsEndPoint.AddressFamily is not an IP based end point?
            var ipAddresses = Dns.GetHostAddresses(dnsEndPoint.Host);

            var addressFamily = dnsEndPoint.AddressFamily;
            if (addressFamily == AddressFamily.Unknown || addressFamily == AddressFamily.Unspecified)
            {
                addressFamily = defaultAddressFamily;
            }
            if (ipAddresses != null && ipAddresses.Length != 0)
            {
                foreach (var ipAddress in ipAddresses)
                {
                    if (ipAddress.AddressFamily == addressFamily)
                    {
                        return new IPEndPoint(ipAddress, dnsEndPoint.Port);
                    }
                }
            }
            var message = string.Format("Unable to resolve host name '{0}'.", dnsEndPoint.Host);
            throw new MongoException(message);
        }

        // nested classes
        private class Entry
        {
            public DateTime AddedAtUtc;
            public IPEndPoint IPEndPoint;
        }
    }
}