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
using System.Net.Sockets;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    internal class DnsCacheResolver : TypedDbDependencyResolver<DnsCache>
    {
        public static readonly DbConfigurationProperty ExpireAfterProperty = new DbConfigurationProperty("dns.expiration", typeof(TimeSpan));
        public static readonly DbConfigurationProperty UseIpv6Property = new DbConfigurationProperty("dns.ipv6", typeof(bool));

        protected override DnsCache Resolve(IDbConfigurationContainer container)
        {
            var props = container.Resolve<IDbConfigurationPropertyProvider>();

            TimeSpan? expireAfter;
            props.TryGetValue<TimeSpan?>(ExpireAfterProperty, out expireAfter);

            bool? ipv6;
            props.TryGetValue<bool?>(UseIpv6Property, out ipv6);

            if (expireAfter.HasValue && ipv6.HasValue)
            {
                return new DnsCache(expireAfter.Value, AddressFamily.InterNetworkV6);
            }
            else if (expireAfter.HasValue)
            {
                return new DnsCache(expireAfter.Value);
            }
            else if (ipv6.HasValue)
            {
                return new DnsCache(AddressFamily.InterNetworkV6);
            }

            return new DnsCache();
        }
    }
}