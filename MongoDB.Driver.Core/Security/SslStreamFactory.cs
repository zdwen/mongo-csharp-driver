using System.IO;
using System.Net;
using System.Net.Security;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Security
{
    /// <summary>
    /// Creates a <see cref="SslStream"/>.
    /// </summary>
    public class SslStreamFactory : IStreamFactory
    {
        // private fields
        private readonly SslSettings _settings;
        private readonly IStreamFactory _wrapped;

        // constructors
        public SslStreamFactory(SslSettings settings, IStreamFactory wrapped)
        {
            Ensure.IsNotNull("settings", settings);
            Ensure.IsNotNull("wrapped", wrapped);

            _settings = settings;
            _wrapped = wrapped;
        }

        // public methods
        /// <summary>
        /// Creates a stream for the specified dns end point.
        /// </summary>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <returns>A stream.</returns>
        public Stream Create(DnsEndPoint dnsEndPoint)
        {
            var stream = _wrapped.Create(dnsEndPoint);

            var checkCertificateRevocation = _settings.CheckCertificateRevocation;
            var clientCertificateCollection = _settings.ClientCertificates;
            var clientCertificateSelectionCallback = _settings.ClientCertificateSelectionCallback;
            var enabledSslProtocols = _settings.EnabledSslProtocols;
            var serverCertificateValidationCallback = _settings.ServerCertificateValidationCallback;

            var sslStream = new SslStream(stream, false, serverCertificateValidationCallback, clientCertificateSelectionCallback);

            sslStream.AuthenticateAsClient(dnsEndPoint.Host, clientCertificateCollection, enabledSslProtocols, checkCertificateRevocation);

            return sslStream;
        }
    }
}
