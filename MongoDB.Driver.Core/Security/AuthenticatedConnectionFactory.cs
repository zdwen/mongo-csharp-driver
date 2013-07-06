using System.Linq;
using System.Net;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Security
{
    /// <summary>
    /// Creates <see cref="AuthenticatedConnection"/>s.
    /// </summary>
    public class AuthenticatedConnectionFactory : IConnectionFactory
    {
        // private fields
        private readonly AuthenticationSettings _settings;
        private readonly IConnectionFactory _wrapped;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticatedConnectionFactory" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="wrapped">The wrapped connection factory.</param>
        public AuthenticatedConnectionFactory(AuthenticationSettings settings, IConnectionFactory wrapped)
        {
            _settings = settings;
            _wrapped = wrapped;
        }

        // public methods
        /// <summary>
        /// Creates a connection for the specified address.
        /// </summary>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <returns>A connection.</returns>
        public IConnection Create(DnsEndPoint dnsEndPoint)
        {
            var connection = _wrapped.Create(dnsEndPoint);

            // only want to add in this overhead if we have credentials
            if (_settings.Credentials.Any())
            {
                return new AuthenticatedConnection(_settings, connection);
            }

            return connection;
        }
    }
}