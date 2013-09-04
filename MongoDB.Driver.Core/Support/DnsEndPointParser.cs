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

using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Xml;

namespace MongoDB.Driver.Core.Support
{
    internal static class DnsEndPointParser
    {
        public static bool TryParse(string value, AddressFamily addressFamily, out DnsEndPoint dnsEndPoint)
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
