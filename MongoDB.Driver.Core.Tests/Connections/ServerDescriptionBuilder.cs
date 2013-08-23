using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace MongoDB.Driver.Core.Connections
{
    public class ServerDescriptionBuilder
    {
        private TimeSpan _averagePingTime;
        private ServerBuildInfo _buildInfo;
        private DnsEndPoint _dnsEndPoint;
        private int _maxDocumentSize;
        private int _maxMessageSize;
        private ReplicaSetInfo _replicaSetInfo;
        private ServerStatus _status;
        private ServerType _type;

        public static ServerDescription Build(Action<ServerDescriptionBuilder> builder)
        {
            var builderInstance = new ServerDescriptionBuilder();

            builder(builderInstance);

            return builderInstance.Build();
        }

        public static ServerDescription Alter(ServerDescription current, Action<ServerDescriptionBuilder> builder)
        {
            var builderInstance = new ServerDescriptionBuilder(current);

            builder(builderInstance);

            return builderInstance.Build();
        }

        public static ServerDescription Connecting(DnsEndPoint dnsEndPoint)
        {
            return Build(b =>
            {
                b.DnsEndPoint(dnsEndPoint);
            });
        }

        private ServerDescriptionBuilder()
        {
            _dnsEndPoint = new DnsEndPoint("localhost", 27017);
            _averagePingTime = TimeSpan.FromMilliseconds(10);
            _buildInfo = new ServerBuildInfo(64, "blah", "blah", "1.0.0");
            _maxDocumentSize = 1024 * 4;
            _maxMessageSize = 1024 * 8;
            _replicaSetInfo = null;
            _status = ServerStatus.Connecting;
            _type = ServerType.Unknown;
        }

        private ServerDescriptionBuilder(ServerDescription current)
        {
            _dnsEndPoint = current.DnsEndPoint;
            _averagePingTime = current.AveragePingTime;
            _buildInfo = current.BuildInfo;
            _maxDocumentSize = current.MaxDocumentSize;
            _maxMessageSize = current.MaxMessageSize;
            _replicaSetInfo = current.ReplicaSetInfo;
            _status = current.Status;
            _type = current.Type;
        }

        public void AveragePingTime(TimeSpan pingTime)
        {
            _averagePingTime = pingTime;
        }

        public void DnsEndPoint(DnsEndPoint dnsEndPont)
        {
            _dnsEndPoint = dnsEndPont;
        }

        public void MaxDocumentSize(int size)
        {
            _maxDocumentSize = size;
        }

        public void MaxMessageSize(int size)
        {
            _maxMessageSize = size;
        }

        public void ReplicaSetInfo(string name, DnsEndPoint primary, params DnsEndPoint[] secondariesAndArbiter)
        {
            ReplicaSetInfo(new ReplicaSetInfo(name, primary, new [] { primary }.Concat(secondariesAndArbiter), null, null));
        }

        public void ReplicaSetInfo(ReplicaSetInfo info)
        {
            _replicaSetInfo = info;
        }

        public void Status(ServerStatus status)
        {
            _status = status;
        }

        public void Type(ServerType type)
        {
            _type = type;
        }

        private ServerDescription Build()
        {
            return new ServerDescription(
                _averagePingTime,
                _buildInfo,
                _dnsEndPoint,
                _maxDocumentSize,
                _maxMessageSize,
                _replicaSetInfo,
                _status,
                _type);
        }
    }
}