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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core
{
    /// <summary>
    /// Represents the result of an IsMaster command.
    /// </summary>
    [Serializable]
    [BsonSerializer(typeof(CommandResultSerializer))]
    public class IsMasterResult : CommandResult
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandResult"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        public IsMasterResult(BsonDocument response)
            : base(response)
        {
        }

        // public properties
        /// <summary>
        /// Gets the type of the server.
        /// </summary>
        /// <value>
        /// The type of the server.
        /// </value>
        public ServerType ServerType
        {
            get
            {
                if (IsReplicaSetMember)
                {
                    if (Response.GetValue("ismaster", false).ToBoolean())
                    {
                        return ServerType.ReplicaSetPrimary;
                    }

                    if (Response.GetValue("secondary", false).ToBoolean())
                    {
                        return ServerType.ReplicaSetSecondary;
                    }

                    if (Response.GetValue("arbiterOnly", false).ToBoolean())
                    {
                        return ServerType.ReplicaSetArbiter;
                    }

                    return ServerType.ReplicaSetOther;
                }

                if (Response.Contains("msg") && Response["msg"] == "isdbgrid")
                {
                    return ServerType.ShardRouter;
                }

                return ServerType.StandAlone;
            }
        }

        // private properties
        private bool IsReplicaSetMember
        {
            get
            {
                return
                    Response.Contains("setName") ||
                    Response.GetValue("isreplicaset", false).ToBoolean();
            }
        }


        // public methods
        /// <summary>
        /// Gets the size of the max document.
        /// </summary>
        /// <param name="maxDocumentSizeDefault">The max document size default.</param>
        /// <returns></returns>
        public int GetMaxDocumentSize(int maxDocumentSizeDefault)
        {
            return Response.GetValue("maxBsonObjectSize", maxDocumentSizeDefault).ToInt32();
        }

        /// <summary>
        /// Gets the size of the max message.
        /// </summary>
        /// <param name="maxDocumentSizeDefault">The max document size default.</param>
        /// <param name="maxMessageSizeDefault">The max message size default.</param>
        /// <returns></returns>
        public int GetMaxMessageSize(int maxDocumentSizeDefault, int maxMessageSizeDefault)
        {
            return Response.GetValue(
                "maxMessageSizeBytes",
                Math.Max(maxMessageSizeDefault, GetMaxDocumentSize(maxDocumentSizeDefault) + 1024))
                .ToInt32();
        }

        /// <summary>
        /// Gets the replica set info.
        /// </summary>
        /// <param name="addressFamily">The address family.</param>
        /// <returns></returns>
        public ReplicaSetInfo GetReplicaSetInfo(AddressFamily addressFamily)
        {
            if (!IsReplicaSetMember)
            {
                return null;
            }

            var name = Response.Contains("setName") ? Response["setName"].AsString : null;
            DnsEndPoint primary;
            if (!Response.Contains("primary") || !TryParseDnsEndPoint(Response["primary"].AsString, addressFamily, out primary))
            {
                primary = null;
            }

            var hosts = GetInstanceAddressesFromNamedResponseElement(addressFamily, "hosts");
            var passives = GetInstanceAddressesFromNamedResponseElement(addressFamily, "passives");
            var arbiters = GetInstanceAddressesFromNamedResponseElement(addressFamily, "arbiters");

            var tags = new Dictionary<string, string>();
            if (Response.Contains("tags"))
            {
                foreach (var tag in Response["tags"].AsBsonDocument)
                {
                    tags.Add(tag.Name, tag.Value.ToString());
                }
            }

            int? version = null;
            if (Response.Contains("setVersion"))
            {
                version = Response.GetValue("setVersion").AsInt32;
            }

            return new ReplicaSetInfo(name, primary, hosts.Concat(passives).Concat(arbiters), tags, version);
        }

        // private methods
        private IEnumerable<DnsEndPoint> GetInstanceAddressesFromNamedResponseElement(AddressFamily addressFamily, string elementName)
        {
            if (!Response.Contains(elementName))
            {
                return Enumerable.Empty<DnsEndPoint>();
            }

            var dnsEndPoints = new List<DnsEndPoint>();
            foreach (var hostName in Response[elementName].AsBsonArray)
            {
                DnsEndPoint dnsEndPoint;
                if (TryParseDnsEndPoint(hostName.AsString, addressFamily, out dnsEndPoint))
                {
                    dnsEndPoints.Add(dnsEndPoint);
                }
            }

            return dnsEndPoints;
        }

        private bool TryParseDnsEndPoint(string value, AddressFamily addressFamily, out DnsEndPoint dnsEndPoint)
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