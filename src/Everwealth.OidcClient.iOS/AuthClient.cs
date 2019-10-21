using IdentityModel.OidcClient;

namespace Everwealth.OidcClient
{
    /// <summary>
    /// Primary class for performing authentication and authorization operations with Auth0 using the
    /// underlying <see cref="IdentityModel.OidcClient.OidcClient"/>.
    /// </summary>
    public class AuthClient : AuthClientBase
    {
        /// <summary>
        /// Creates a new instance of the OIDC Client.
        /// </summary>
        /// <param name="options">The <see cref="OidcClientOptions"/> specifying the configuration for the Auth0 OIDC Client.</param>
        public AuthClient(OidcClientOptions options)
            : base(options)
        {
            options.Browser = options.Browser ?? new AutoSelectBrowser();
        }
    }
}
