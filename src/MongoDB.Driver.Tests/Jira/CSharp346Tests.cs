﻿/* Copyright 2010-2014 MongoDB Inc.
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

using System.Linq;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Jira.CSharp346
{
    [TestFixture]
    public class CSharp346Tests
    {
#pragma warning disable 618
        [Test]
        public void TestOneIPv6Address()
        {
            var connectionString = "mongodb://[::1:]/?w=1";
            var url = new MongoUrl(connectionString);
            Assert.AreEqual("[::1:]", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(true, url.SafeMode.Enabled);
        }

        [Test]
        public void TestOneIPv6AddressWithCredential()
        {
            var connectionString = "mongodb://username:password@[::1:]/?w=1";
            var url = new MongoUrl(connectionString);
            Assert.AreEqual("[::1:]", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual("username", url.Username);
            Assert.AreEqual("password", url.Password);
            Assert.AreEqual(true, url.SafeMode.Enabled);
        }

        [Test]
        public void TestOneIPv6AddressWithPort()
        {
            var connectionString = "mongodb://[::1:]:1234/?w=1";
            var url = new MongoUrl(connectionString);
            Assert.AreEqual("[::1:]", url.Server.Host);
            Assert.AreEqual(1234, url.Server.Port);
            Assert.AreEqual(true, url.SafeMode.Enabled);
        }

        [Test]
        public void TestTwoIPv6Addresses()
        {
            var connectionString = "mongodb://[::1:],[::2:]/?w=1";
            var url = new MongoUrl(connectionString);
            var servers = url.Servers.ToArray();
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("[::1:]", servers[0].Host);
            Assert.AreEqual(27017, servers[0].Port);
            Assert.AreEqual("[::2:]", servers[1].Host);
            Assert.AreEqual(27017, servers[1].Port);
            Assert.AreEqual(true, url.SafeMode.Enabled);
        }

        [Test]
        public void TestTwoIPv6AddressesWithCredential()
        {
            var connectionString = "mongodb://username:password@[::1:],[::2:]/?w=1";
            var url = new MongoUrl(connectionString);
            var servers = url.Servers.ToArray();
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("[::1:]", servers[0].Host);
            Assert.AreEqual(27017, servers[0].Port);
            Assert.AreEqual("[::2:]", servers[1].Host);
            Assert.AreEqual(27017, servers[1].Port);
            Assert.AreEqual("username", url.Username);
            Assert.AreEqual("password", url.Password);
            Assert.AreEqual(true, url.SafeMode.Enabled);
        }

        [Test]
        public void TestTwoIPv6AddressesWithPorts()
        {
            var connectionString = "mongodb://[::1:]:1234,[::2:]:2345/?w=1";
            var url = new MongoUrl(connectionString);
            var servers = url.Servers.ToArray();
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("[::1:]", servers[0].Host);
            Assert.AreEqual(1234, servers[0].Port);
            Assert.AreEqual("[::2:]", servers[1].Host);
            Assert.AreEqual(2345, servers[1].Port);
            Assert.AreEqual(true, url.SafeMode.Enabled);
        }
#pragma warning restore
    }
}
