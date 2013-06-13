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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Xml;
using MongoDB.Bson;

namespace MongoDB.Driver.Core.Connections
{
    internal static class IsMasterResultHelper
    {
        public const int MAX_DOCUMENT_SIZE = 4 * 1024 * 1024; // 4MiB
        public const int MAX_MESSAGE_LENGTH = 16000000; // 16MB (not 16 MiB!)

        public static int GetMaxDocumentSize(BsonDocument isMasterResult, int maxDocumentSizeDefault)
        {
            return isMasterResult.GetValue("maxBsonObjectSize", maxDocumentSizeDefault).ToInt32();
        }

        public static int GetMaxMessageSize(BsonDocument isMasterResult, int maxDocumentSizeDefault, int maxMessageSizeDefault)
        {
            return isMasterResult.GetValue(
                    "maxMessageSizeBytes",
                    Math.Max(maxMessageSizeDefault, GetMaxDocumentSize(isMasterResult, maxDocumentSizeDefault) + 1024))
                    .ToInt32();
        }

        public static ServerType GetServerType(BsonDocument isMasterResult)
        {
            if (IsReplicaSetMember(isMasterResult))
            {
                if (isMasterResult.GetValue("ismaster", false).ToBoolean())
                {
                    return ServerType.ReplicaSetPrimary;
                }

                if (isMasterResult.GetValue("secondary", false).ToBoolean())
                {
                    return ServerType.ReplicaSetSecondary;
                }

                if (isMasterResult.GetValue("arbiterOnly", false).ToBoolean())
                {
                    return ServerType.ReplicaSetArbiter;
                }

                return ServerType.ReplicaSetOther;
            }

            if (isMasterResult.Contains("msg") && isMasterResult["msg"] == "isdbgrid")
            {
                return ServerType.ShardRouter;
            }

            return ServerType.StandAlone;
        }

        public static ReplicaSetInfo GetReplicaSetInfo(AddressFamily addressFamily, BsonDocument isMasterResult)
        {
            if (!IsReplicaSetMember(isMasterResult))
            {
                return null;
            }

            var name = isMasterResult.Contains("setName") ? isMasterResult["setName"].AsString : null;
            DnsEndPoint primary;
            if (!isMasterResult.Contains("primary") || !TryParseDnsEndPoint(isMasterResult["primary"].AsString, addressFamily, out primary))
            {
                primary = null;
            }

            var hosts = GetInstanceAddressesFromNamedResponseElement(addressFamily, isMasterResult, "hosts");
            var passives = GetInstanceAddressesFromNamedResponseElement(addressFamily, isMasterResult, "passives");
            var arbiters = GetInstanceAddressesFromNamedResponseElement(addressFamily, isMasterResult, "arbiters");

            var tags = new Dictionary<string, string>();
            if (isMasterResult.Contains("tags"))
            {
                foreach (var tag in isMasterResult["tags"].AsBsonDocument)
                {
                    tags.Add(tag.Name, tag.Value.ToString());
                }
            }

            var version = isMasterResult.GetValue("setVersion", null).AsNullableInt32;

            return new ReplicaSetInfo(name, primary, hosts.Concat(passives).Concat(arbiters), tags, version);
        }

        private static IEnumerable<DnsEndPoint> GetInstanceAddressesFromNamedResponseElement(AddressFamily addressFamily, BsonDocument isMasterResult, string elementName)
        {
            if (!isMasterResult.Contains(elementName))
            {
                return Enumerable.Empty<DnsEndPoint>();
            }

            var dnsEndPoints = new List<DnsEndPoint>();
            foreach (var hostName in isMasterResult[elementName].AsBsonArray)
            {
                DnsEndPoint dnsEndPoint;
                if (TryParseDnsEndPoint(hostName.AsString, addressFamily, out dnsEndPoint))
                {
                    dnsEndPoints.Add(dnsEndPoint);
                }
            }

            return dnsEndPoints;
        }

        private static bool IsReplicaSetMember(BsonDocument isMasterResult)
        {
            return isMasterResult.Contains("setName") ||
                isMasterResult.GetValue("isreplicaset", false).ToBoolean();
        }

        private static bool TryParseDnsEndPoint(string value, AddressFamily addressFamily, out DnsEndPoint dnsEndPoint)
        {
            // don't throw ArgumentNullException if value is null
            dnsEndPoint = null;
            if (value != null)
            {
                var match = Regex.Match(value, @"^(?<host>(\[[^]]+\]|[^:\[\]]+))(:(?<port>\d+))?$");
                if (match.Success)
                {
                    string host = match.Groups["host"].Value;
                    string portString = match.Groups["port"].Value;
                    int port = (portString == "") ? 27017 : XmlConvert.ToInt32(portString);
                    dnsEndPoint = new DnsEndPoint(host, port, addressFamily);
                }
            }

            return dnsEndPoint != null;
        }
    }
}