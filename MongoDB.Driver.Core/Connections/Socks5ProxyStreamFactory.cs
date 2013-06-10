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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Connects through a SOCKS5 Proxy.
    /// </summary>
    /// <remarks>
    /// Details are here: http://www.ietf.org/rfc/rfc1928.txt
    /// </remarks>
    public class Socks5StreamProxy : IStreamFactory
    {
        private readonly DnsEndPoint _proxyDnsEndPoint;
        private readonly IStreamFactory _wrapped;

        public Socks5StreamProxy(DnsEndPoint proxyDnsEndPoint, IStreamFactory wrapped)
        {
            _proxyDnsEndPoint = proxyDnsEndPoint;
            _wrapped = wrapped;
        }

        public Stream Create(DnsEndPoint dnsEndPoint)
        {
            // use the proxy endpoint because that is what we are talking to...
            var stream = _wrapped.Create(_proxyDnsEndPoint);

            NegotiateAuth(stream);

            SendCommand(stream, Socks.CMD_CONNECT, dnsEndPoint);

            return stream;
        }

        private byte[] GetDestinationAddressBytes(byte addressType, string host)
        {
            switch (addressType)
            {
                case Socks.ADDRTYPE_DOMAIN_NAME:
                    var bytes = new byte[host.Length + 1];
                    bytes[0] = Convert.ToByte(host.Length);
                    Encoding.ASCII.GetBytes(host).CopyTo(bytes, 1);
                    return bytes;
                case Socks.ADDRTYPE_IPV4:
                case Socks.ADDRTYPE_IPV6:
                    return IPAddress.Parse(host).GetAddressBytes();
                default:
                    throw new NotSupportedException("Unsupported address type.");
            }
        }

        private byte GetDestinationAddressType(DnsEndPoint destination)
        {
            IPAddress ipAddress;
            if (!IPAddress.TryParse(destination.Host, out ipAddress))
            {
                return Socks.ADDRTYPE_DOMAIN_NAME;
            }

            switch (ipAddress.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    return Socks.ADDRTYPE_IPV4;
                case AddressFamily.InterNetworkV6:
                    return Socks.ADDRTYPE_IPV6;
                default:
                    throw new NotSupportedException(string.Format("The host {0} is not a supported address type.", destination.Host));
            }
        }

        private byte[] GetDestinationPortBytes(int port)
        {
            var bytes = new byte[2];
            bytes[0] = Convert.ToByte(port / 256);
            bytes[1] = Convert.ToByte(port % 256);
            return bytes;
        }

        private void HandleSocksError(byte[] reply)
        {
            throw new Exception("Unhandled exception occured.");
        }

        private void NegotiateAuth(Stream stream)
        {
            var request = new byte[3];
            request[0] = Socks.VERSION_NUMBER;
            request[1] = 1; // only support 1 auth method, anonymous
            request[2] = Socks.AUTH_METHOD_NO_AUTHENTICATION_REQUIRED;

            stream.Write(request, 0, request.Length);

            var reply = new byte[2];
            stream.Read(reply, 0, reply.Length);
            var acceptedAuthMethod = reply[1];
            if (acceptedAuthMethod == Socks.AUTH_METHOD_REPLY_NO_ACCEPTABLE_METHODS)
            {
                throw new NotSupportedException("SOCKS proxy requires authentication.");
            }
        }

        private void SendCommand(Stream stream, byte command, DnsEndPoint destination)
        {
            var addressType = GetDestinationAddressType(destination);
            var addressBytes = GetDestinationAddressBytes(addressType, destination.Host);
            var portBytes = GetDestinationPortBytes(destination.Port);

            byte[] request = new byte[4 + addressBytes.Length + 2];
            request[0] = Socks.VERSION_NUMBER;
            request[1] = command;
            request[2] = Socks.RESERVED;
            request[3] = addressType;
            addressBytes.CopyTo(request, 4);
            portBytes.CopyTo(request, 4 + addressBytes.Length);
            stream.Write(request, 0, request.Length);

            var reply = new byte[256];
            stream.Read(reply, 0, reply.Length);
            var replyCode = reply[1];
            if (replyCode != Socks.CMD_REPLY_SUCCEEDED)
            {
                HandleSocksError(reply);
            }
        }

        private static class Socks
        {
            public const string PROXY_NAME = "SOCKS5";
            public const int DEFAULT_PORT = 1080;

            public const byte VERSION_NUMBER = 5;
            public const byte RESERVED = 0x00;
            public const byte AUTH_NUMBER_OF_AUTH_METHODS_SUPPORTED = 1;
            public const byte AUTH_METHOD_NO_AUTHENTICATION_REQUIRED = 0x00;
            public const byte AUTH_METHOD_GSSAPI = 0x01;
            public const byte AUTH_METHOD_USERNAME_PASSWORD = 0x02;
            public const byte AUTH_METHOD_IANA_ASSIGNED_RANGE_BEGIN = 0x03;
            public const byte AUTH_METHOD_IANA_ASSIGNED_RANGE_END = 0x7f;
            public const byte AUTH_METHOD_RESERVED_RANGE_BEGIN = 0x80;
            public const byte AUTH_METHOD_RESERVED_RANGE_END = 0xfe;
            public const byte AUTH_METHOD_REPLY_NO_ACCEPTABLE_METHODS = 0xff;
            public const byte CMD_CONNECT = 0x01;
            public const byte CMD_BIND = 0x02;
            public const byte CMD_UDP_ASSOCIATE = 0x03;
            public const byte CMD_REPLY_SUCCEEDED = 0x00;
            public const byte CMD_REPLY_GENERAL_SOCKS_SERVER_FAILURE = 0x01;
            public const byte CMD_REPLY_CONNECTION_NOT_ALLOWED_BY_RULESET = 0x02;
            public const byte CMD_REPLY_NETWORK_UNREACHABLE = 0x03;
            public const byte CMD_REPLY_HOST_UNREACHABLE = 0x04;
            public const byte CMD_REPLY_CONNECTION_REFUSED = 0x05;
            public const byte CMD_REPLY_TTL_EXPIRED = 0x06;
            public const byte CMD_REPLY_COMMAND_NOT_SUPPORTED = 0x07;
            public const byte CMD_REPLY_ADDRESS_TYPE_NOT_SUPPORTED = 0x08;
            public const byte ADDRTYPE_IPV4 = 0x01;
            public const byte ADDRTYPE_DOMAIN_NAME = 0x03;
            public const byte ADDRTYPE_IPV6 = 0x04;
        }
    }
}