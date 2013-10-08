using System;
using System.Linq;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Configuration
{
    [TestFixture]
    public class DbConnectionStringTests
    {
        [Test]
        public void With_one_host_and_no_port()
        {
            var subject = new DbConnectionString("mongodb://localhost");

            Assert.AreEqual(1, subject.Hosts.Count());
            Assert.AreEqual("localhost", subject.Hosts.Single().Host);
            Assert.AreEqual(27017, subject.Hosts.Single().Port);
        }

        [Test]
        public void With_one_host_and_port()
        {
            var subject = new DbConnectionString("mongodb://localhost:27092");

            Assert.AreEqual(1, subject.Hosts.Count());
            Assert.AreEqual("localhost", subject.Hosts.Single().Host);
            Assert.AreEqual(27092, subject.Hosts.Single().Port);
        }

        [Test]
        public void With_two_hosts_and_one_port()
        {
            var subject = new DbConnectionString("mongodb://localhost:27092,remote");

            Assert.AreEqual(2, subject.Hosts.Count());
            Assert.AreEqual("localhost", subject.Hosts.ElementAt(0).Host);
            Assert.AreEqual(27092, subject.Hosts.ElementAt(0).Port);
            Assert.AreEqual("remote", subject.Hosts.ElementAt(1).Host);
            Assert.AreEqual(27017, subject.Hosts.ElementAt(1).Port);
        }

        [Test]
        public void With_two_hosts_and_one_port2()
        {
            var subject = new DbConnectionString("mongodb://localhost,remote:27092");

            Assert.AreEqual(2, subject.Hosts.Count());
            Assert.AreEqual("localhost", subject.Hosts.ElementAt(0).Host);
            Assert.AreEqual(27017, subject.Hosts.ElementAt(0).Port);
            Assert.AreEqual("remote", subject.Hosts.ElementAt(1).Host);
            Assert.AreEqual(27092, subject.Hosts.ElementAt(1).Port);
        }

        [Test]
        public void With_two_hosts_and_two_ports()
        {
            var subject = new DbConnectionString("mongodb://localhost:30000,remote:27092");

            Assert.AreEqual(2, subject.Hosts.Count());
            Assert.AreEqual("localhost", subject.Hosts.ElementAt(0).Host);
            Assert.AreEqual(30000, subject.Hosts.ElementAt(0).Port);
            Assert.AreEqual("remote", subject.Hosts.ElementAt(1).Host);
            Assert.AreEqual(27092, subject.Hosts.ElementAt(1).Port);
        }

        [Test]
        public void With_three_hosts()
        {
            var subject = new DbConnectionString("mongodb://localhost,remote,foreign");

            Assert.AreEqual(3, subject.Hosts.Count());
            Assert.AreEqual("localhost", subject.Hosts.ElementAt(0).Host);
            Assert.AreEqual(27017, subject.Hosts.ElementAt(0).Port);
            Assert.AreEqual("remote", subject.Hosts.ElementAt(1).Host);
            Assert.AreEqual(27017, subject.Hosts.ElementAt(1).Port);
            Assert.AreEqual("foreign", subject.Hosts.ElementAt(2).Host);
            Assert.AreEqual(27017, subject.Hosts.ElementAt(2).Port);
        }

        [Test]
        [TestCase("mongodb://localhost")]
        [TestCase("mongodb://localhost/")]
        public void When_nothing_is_specified(string connectionString)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.IsNull(subject.AuthMechanism);
            Assert.IsNull(subject.AuthSource);
            Assert.IsNull(subject.ConnectTimeout);
            Assert.IsNull(subject.DatabaseName);
            Assert.IsNull(subject.FSync);
            Assert.IsNull(subject.Ipv6);
            Assert.IsNull(subject.Journal);
            Assert.IsNull(subject.MaxIdleTime);
            Assert.IsNull(subject.MaxLifeTime);
            Assert.IsNull(subject.MaxPoolSize);
            Assert.IsNull(subject.MinPoolSize);
            Assert.IsNull(subject.Password);
            Assert.IsNull(subject.ReadPreference);
            Assert.IsNull(subject.ReadPreferenceTags);
            Assert.IsNull(subject.ReplicaSet);
            Assert.IsNull(subject.SecondaryAcceptableLatency);
            Assert.IsNull(subject.SocketTimeout);
            Assert.IsNull(subject.Ssl);
            Assert.IsNull(subject.SslVerifyCertificate);
            Assert.IsNull(subject.Username);
            Assert.IsNull(subject.UuidRepresentation);
            Assert.IsNull(subject.WaitQueueMultiple);
            Assert.IsNull(subject.WaitQueueTimeout);
            Assert.IsNull(subject.W);
            Assert.IsNull(subject.WTimeout);
        }

        [Test]
        public void When_everything_is_specified()
        {
            var connectionString = @"mongodb://user:pass@localhost1,localhost2:30000/test?" +
                "authMechanism=GSSAPI;" +
                "authSource=admin;" +
                "connectTimeout=15ms;" +
                "fsync=true;" +
                "ipv6=false;" +
                "j=true;" +
                "maxIdleTime=10ms;" +
                "maxLifeTime=5ms;" +
                "maxPoolSize=20;" +
                "minPoolSize=15;" +
                "readPreference=primary;" +
                "readPreferenceTags=dc:1;" +
                "replicaSet=funny;" +
                "secondaryAcceptableLatency=50ms;" +
                "socketTimeout=40ms;" +
                "ssl=false;" +
                "sslVerifyCertificate=true;" +
                "uuidRepresentation=standard;" +
                "waitQueueMultiple=10;" +
                "waitQueueTimeout=60ms;" +
                "w=4;" +
                "wtimeout=20ms";

            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual("GSSAPI", subject.AuthMechanism);
            Assert.AreEqual("admin", subject.AuthSource);
            Assert.AreEqual(TimeSpan.FromMilliseconds(15),subject.ConnectTimeout);
            Assert.AreEqual("test", subject.DatabaseName);
            Assert.AreEqual(true, subject.FSync);
            Assert.AreEqual(false, subject.Ipv6);
            Assert.AreEqual(true, subject.Journal);
            Assert.AreEqual(TimeSpan.FromMilliseconds(10), subject.MaxIdleTime);
            Assert.AreEqual(TimeSpan.FromMilliseconds(5), subject.MaxLifeTime);
            Assert.AreEqual(20, subject.MaxPoolSize);
            Assert.AreEqual(15, subject.MinPoolSize);
            Assert.AreEqual("pass", subject.Password);
            Assert.AreEqual(ReadPreferenceMode.Primary, subject.ReadPreference);
            Assert.AreEqual(new ReplicaSetTagSet().Add("dc", "1"), subject.ReadPreferenceTags.Single());
            Assert.AreEqual("funny", subject.ReplicaSet);
            Assert.AreEqual(TimeSpan.FromMilliseconds(50), subject.SecondaryAcceptableLatency);
            Assert.AreEqual(TimeSpan.FromMilliseconds(40), subject.SocketTimeout);
            Assert.AreEqual(false, subject.Ssl);
            Assert.AreEqual(true, subject.SslVerifyCertificate);
            Assert.AreEqual("user", subject.Username);
            Assert.AreEqual(GuidRepresentation.Standard, subject.UuidRepresentation);
            Assert.AreEqual(10, subject.WaitQueueMultiple);
            Assert.AreEqual(TimeSpan.FromMilliseconds(60), subject.WaitQueueTimeout);
            Assert.AreEqual(WriteConcern.WValue.Parse("4"), subject.W);
            Assert.AreEqual(TimeSpan.FromMilliseconds(20), subject.WTimeout);
        }

        [Test]
        [TestCase("mongodb://localhost?authMechanism=GSSAPI", "GSSAPI")]
        [TestCase("mongodb://localhost?authMechanism=MONGODB-CR", "MONGODB-CR")]
        public void When_authMechanism_is_specified(string connectionString, string authMechanism)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(authMechanism, subject.AuthMechanism);
        }

        [Test]
        [TestCase("mongodb://localhost?authSource=admin", "admin")]
        [TestCase("mongodb://localhost?authSource=awesome", "awesome")]
        public void When_authSource_is_specified(string connectionString, string authSource)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(authSource, subject.AuthSource);
        }

        [Test]
        [TestCase("mongodb://localhost?connectTimeout=15ms", 15)]
        [TestCase("mongodb://localhost?connectTimeoutMS=15", 15)]
        [TestCase("mongodb://localhost?connectTimeout=15", 1000 * 15)]
        [TestCase("mongodb://localhost?connectTimeout=15s", 1000 * 15)]
        [TestCase("mongodb://localhost?connectTimeout=15m", 1000 * 60 * 15)]
        [TestCase("mongodb://localhost?connectTimeout=15h", 1000 * 60 * 60 * 15)]
        public void When_connect_timeout_is_specified(string connectionString, int milliseconds)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(TimeSpan.FromMilliseconds(milliseconds), subject.ConnectTimeout);
        }

        [Test]
        [TestCase("mongodb://localhost/awesome", "awesome")]
        [TestCase("mongodb://localhost/awesome/", "awesome")]
        public void When_a_database_name_is_specified(string connectionString, string db)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(db, subject.DatabaseName);
        }

        [Test]
        [TestCase("mongodb://localhost?fsync=true", true)]
        [TestCase("mongodb://localhost?fsync=false", false)]
        public void When_fsync_is_specified(string connectionString, bool fsync)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(fsync, subject.FSync);
        }

        [Test]
        [TestCase("mongodb://localhost?ipv6=true", true)]
        [TestCase("mongodb://localhost?ipv6=false", false)]
        public void When_ipv6_is_specified(string connectionString, bool ipv6)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(ipv6, subject.Ipv6);
        }

        [Test]
        [TestCase("mongodb://localhost?j=true", true)]
        [TestCase("mongodb://localhost?j=false", false)]
        public void When_j_is_specified(string connectionString, bool j)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(j, subject.Journal);
        }

        [Test]
        [TestCase("mongodb://localhost?maxIdleTime=15ms", 15)]
        [TestCase("mongodb://localhost?maxIdleTimeMS=15", 15)]
        [TestCase("mongodb://localhost?maxIdleTime=15", 1000 * 15)]
        [TestCase("mongodb://localhost?maxIdleTime=15s", 1000 * 15)]
        [TestCase("mongodb://localhost?maxIdleTime=15m", 1000 * 60 * 15)]
        [TestCase("mongodb://localhost?maxIdleTime=15h", 1000 * 60 * 60 * 15)]
        public void When_maxIdleTime_is_specified(string connectionString, int milliseconds)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(TimeSpan.FromMilliseconds(milliseconds), subject.MaxIdleTime);
        }

        [Test]
        [TestCase("mongodb://localhost?maxLifeTime=15ms", 15)]
        [TestCase("mongodb://localhost?maxLifeTimeMS=15", 15)]
        [TestCase("mongodb://localhost?maxLifeTime=15", 1000 * 15)]
        [TestCase("mongodb://localhost?maxLifeTime=15s", 1000 * 15)]
        [TestCase("mongodb://localhost?maxLifeTime=15m", 1000 * 60 * 15)]
        [TestCase("mongodb://localhost?maxLifeTime=15h", 1000 * 60 * 60 * 15)]
        public void When_maxLifeTime_is_specified(string connectionString, int milliseconds)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(TimeSpan.FromMilliseconds(milliseconds), subject.MaxLifeTime);
        }

        [Test]
        [TestCase("mongodb://localhost?maxPoolSize=-1", -1)]
        [TestCase("mongodb://localhost?maxPoolSize=0", 0)]
        [TestCase("mongodb://localhost?maxPoolSize=1", 1)]
        [TestCase("mongodb://localhost?maxPoolSize=20", 20)]
        public void When_maxPoolSize_is_specified(string connectionString, int maxPoolSize)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(maxPoolSize, subject.MaxPoolSize);
        }

        [Test]
        [TestCase("mongodb://localhost?minPoolSize=-1", -1)]
        [TestCase("mongodb://localhost?minPoolSize=0", 0)]
        [TestCase("mongodb://localhost?minPoolSize=1", 1)]
        [TestCase("mongodb://localhost?minPoolSize=20", 20)]
        public void When_minPoolSize_is_specified(string connectionString, int minPoolSize)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(minPoolSize, subject.MinPoolSize);
        }

        [Test]
        [TestCase("mongodb://a:yes@localhost", "yes")]
        [TestCase("mongodb://a:password@localhost", "password")]
        public void When_password_is_specified(string connectionString, string password)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(password, subject.Password);
        }

        [Test]
        [TestCase("mongodb://localhost?readPreference=primary", ReadPreferenceMode.Primary)]
        [TestCase("mongodb://localhost?readPreference=primaryPreferred", ReadPreferenceMode.PrimaryPreferred)]
        [TestCase("mongodb://localhost?readPreference=secondaryPreferred", ReadPreferenceMode.SecondaryPreferred)]
        [TestCase("mongodb://localhost?readPreference=secondary", ReadPreferenceMode.Secondary)]
        [TestCase("mongodb://localhost?readPreference=nearest", ReadPreferenceMode.Nearest)]
        public void When_readPreference_is_specified(string connectionString, ReadPreferenceMode readPreference)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(readPreference, subject.ReadPreference);
        }

        [Test]
        public void When_one_set_of_readPreferenceTags_is_specified()
        {
            var subject = new DbConnectionString("mongodb://localhost?readPreferenceTags=dc:east,rack:1");

            var tagSet = new ReplicaSetTagSet()
                .Add("dc", "east")
                .Add("rack", "1");

            Assert.AreEqual(1, subject.ReadPreferenceTags.Count());
            Assert.AreEqual(tagSet, subject.ReadPreferenceTags.Single());
        }

        [Test]
        public void When_two_sets_of_readPreferenceTags_are_specified()
        {
            var subject = new DbConnectionString("mongodb://localhost?readPreferenceTags=dc:east,rack:1&readPreferenceTags=dc:west,rack:2");

            var tagSet1 = new ReplicaSetTagSet()
                .Add("dc", "east")
                .Add("rack", "1");

            var tagSet2 = new ReplicaSetTagSet()
                .Add("dc", "west")
                .Add("rack", "2");

            Assert.AreEqual(2, subject.ReadPreferenceTags.Count());
            Assert.AreEqual(tagSet1, subject.ReadPreferenceTags.ElementAt(0));
            Assert.AreEqual(tagSet2, subject.ReadPreferenceTags.ElementAt(1));
        }

        [Test]
        [TestCase("mongodb://localhost?replicaSet=yeah", "yeah")]
        public void When_replicaSet_is_specified(string connectionString, string replicaSet)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(replicaSet, subject.ReplicaSet);
        }

        [Test]
        [TestCase("mongodb://localhost?secondaryAcceptableLatency=15ms", 15)]
        [TestCase("mongodb://localhost?secondaryAcceptableLatencyMS=15", 15)]
        [TestCase("mongodb://localhost?secondaryAcceptableLatency=15", 1000 * 15)]
        [TestCase("mongodb://localhost?secondaryAcceptableLatency=15s", 1000 * 15)]
        [TestCase("mongodb://localhost?secondaryAcceptableLatency=15m", 1000 * 60 * 15)]
        [TestCase("mongodb://localhost?secondaryAcceptableLatency=15h", 1000 * 60 * 60 * 15)]
        public void When_secondaryAcceptableLatency_is_specified(string connectionString, int milliseconds)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(TimeSpan.FromMilliseconds(milliseconds), subject.SecondaryAcceptableLatency);
        }

        [Test]
        [TestCase("mongodb://localhost?socketTimeout=15ms", 15)]
        [TestCase("mongodb://localhost?socketTimeoutMS=15", 15)]
        [TestCase("mongodb://localhost?socketTimeout=15", 1000 * 15)]
        [TestCase("mongodb://localhost?socketTimeout=15s", 1000 * 15)]
        [TestCase("mongodb://localhost?socketTimeout=15m", 1000 * 60 * 15)]
        [TestCase("mongodb://localhost?socketTimeout=15h", 1000 * 60 * 60 * 15)]
        public void When_socketTimeout_is_specified(string connectionString, int milliseconds)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(TimeSpan.FromMilliseconds(milliseconds), subject.SocketTimeout);
        }

        [Test]
        [TestCase("mongodb://localhost?ssl=true", true)]
        [TestCase("mongodb://localhost?ssl=false", false)]
        public void When_ssl_is_specified(string connectionString, bool ssl)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(ssl, subject.Ssl);
        }

        [Test]
        [TestCase("mongodb://localhost?sslVerifyCertificate=true", true)]
        [TestCase("mongodb://localhost?sslVerifyCertificate=false", false)]
        public void When_sslVerifyCertificate_is_specified(string connectionString, bool sslVerifyCertificate)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(sslVerifyCertificate, subject.SslVerifyCertificate);
        }

        [Test]
        [TestCase("mongodb://yes@localhost", "yes")]
        [TestCase("mongodb://username@localhost", "username")]
        public void When_username_is_specified(string connectionString, string username)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(username, subject.Username);
        }

        [Test]
        [TestCase("mongodb://localhost?uuidRepresentation=standard", GuidRepresentation.Standard)]
        [TestCase("mongodb://localhost?guids=standard", GuidRepresentation.Standard)]
        [TestCase("mongodb://localhost?uuidRepresentation=csharpLegacy", GuidRepresentation.CSharpLegacy)]
        [TestCase("mongodb://localhost?guids=csharpLegacy", GuidRepresentation.CSharpLegacy)]
        [TestCase("mongodb://localhost?uuidRepresentation=javaLegacy", GuidRepresentation.JavaLegacy)]
        [TestCase("mongodb://localhost?guids=javaLegacy", GuidRepresentation.JavaLegacy)]
        [TestCase("mongodb://localhost?uuidRepresentation=pythonLegacy", GuidRepresentation.PythonLegacy)]
        [TestCase("mongodb://localhost?guids=pythonLegacy", GuidRepresentation.PythonLegacy)]
        public void When_uuidRepresentation_is_specified(string connectionString, GuidRepresentation representation)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(representation, subject.UuidRepresentation);
        }

        [Test]
        [TestCase("mongodb://localhost?w=0", "0")]
        [TestCase("mongodb://localhost?w=1", "1")]
        [TestCase("mongodb://localhost?w=majority", "majority")]
        public void When_w_is_specified(string connectionString, string w)
        {
            var subject = new DbConnectionString(connectionString);
            var expectedW = WriteConcern.WValue.Parse(w);

            Assert.AreEqual(expectedW, subject.W);
        }

        [Test]
        [TestCase("mongodb://localhost?wtimeout=15ms", 15)]
        [TestCase("mongodb://localhost?wtimeoutMS=15", 15)]
        [TestCase("mongodb://localhost?wtimeout=15", 1000 * 15)]
        [TestCase("mongodb://localhost?wtimeout=15s", 1000 * 15)]
        [TestCase("mongodb://localhost?wtimeout=15m", 1000 * 60 * 15)]
        [TestCase("mongodb://localhost?wtimeout=15h", 1000 * 60 * 60 * 15)]
        public void When_wtimeout_is_specified(string connectionString, int milliseconds)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(TimeSpan.FromMilliseconds(milliseconds), subject.WTimeout);
        }

        [Test]
        [TestCase("mongodb://localhost?waitQueueMultiple=-1", -1)]
        [TestCase("mongodb://localhost?waitQueueMultiple=0", 0)]
        [TestCase("mongodb://localhost?waitQueueMultiple=1", 1)]
        [TestCase("mongodb://localhost?waitQueueMultiple=20", 20)]
        public void When_waitQueueMultiple_is_specified(string connectionString, int waitQueueMultiple)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(waitQueueMultiple, subject.WaitQueueMultiple);
        }

        [Test]
        [TestCase("mongodb://localhost?waitQueueTimeout=15ms", 15)]
        [TestCase("mongodb://localhost?waitQueueTimeoutMS=15", 15)]
        [TestCase("mongodb://localhost?waitQueueTimeout=15", 1000 * 15)]
        [TestCase("mongodb://localhost?waitQueueTimeout=15s", 1000 * 15)]
        [TestCase("mongodb://localhost?waitQueueTimeout=15m", 1000 * 60 * 15)]
        [TestCase("mongodb://localhost?waitQueueTimeout=15h", 1000 * 60 * 60 * 15)]
        public void When_waitQueueTimeout_is_specified(string connectionString, int milliseconds)
        {
            var subject = new DbConnectionString(connectionString);

            Assert.AreEqual(TimeSpan.FromMilliseconds(milliseconds), subject.WaitQueueTimeout);
        }

        [Test]
        public void When_uknown_options_exist()
        {
            var subject = new DbConnectionString("mongodb://localhost?one=1;two=2");

            Assert.AreEqual(2, subject.AllUnknownOptionNames.Count());
            Assert.IsTrue(subject.AllUnknownOptionNames.Contains("one"));
            Assert.IsTrue(subject.AllUnknownOptionNames.Contains("two"));
            Assert.AreEqual("1", subject.GetOption("one"));
            Assert.AreEqual("2", subject.GetOption("two"));
        }
    }
}