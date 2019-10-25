using System;
using System.Diagnostics;
using System.Threading.Tasks;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Results;

namespace Everwealth.OidcClient
{
    /// <summary>
    /// Base class for performing authentication and authorization operations with Auth0 using the
    /// underlying <see cref="IdentityModel.OidcClient.OidcClient"/>.
    /// </summary>
    public abstract class AuthClientBase : IAuthClient
    {
        private readonly OidcClientOptions _options;
        private IdentityModel.OidcClient.OidcClient _oidcClient;
        protected IdentityModel.OidcClient.OidcClient OidcClient
        {
            get
            {
                return _oidcClient ?? (_oidcClient = new IdentityModel.OidcClient.OidcClient(_options));
            }
        }

        /// <summary>
        /// Create a new instance of <see cref="AuthClientBase"/>.
        /// </summary>
        /// <param name="options"><see cref="OidcClientOptions"/> specifying the configuration options for this client.</param>
        protected AuthClientBase(OidcClientOptions options)
        {
            _options = options;
        }

        /// <inheritdoc />
        public Task<LoginResult> LoginAsync(LoginRequest request = null)
        {
            Debug.WriteLine($"Using Callback URL ${_options.RedirectUri}. Ensure this is an Allowed Callback URL for application/client ID ${_options.ClientId}.");

            return OidcClient.LoginAsync(request);
        }

        /// <inheritdoc/>
        public async Task<bool> LogoutAsync(LogoutRequest request = null)
        {
            Debug.WriteLine($"Using Callback URL ${_options.PostLogoutRedirectUri}. Ensure this is an Allowed Logout URL for application/client ID ${_options.ClientId}.");

            try
            {
                await OidcClient.LogoutAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        /// <inheritdoc/>
        public Task<AuthorizeState> PrepareLoginAsync(object extraParameters = null)
        {
            return OidcClient.PrepareLoginAsync(extraParameters);
        }

        /// <inheritdoc/>
        public Task<LoginResult> ProcessResponseAsync(string data, AuthorizeState state)
        {
            return OidcClient.ProcessResponseAsync(data, state);
        }

        /// <inheritdoc/>
        public Task<string> PrepareLogoutAsync(LogoutRequest request = null)
        {
            return OidcClient.PrepareLogoutAsync(request);
        }

        /// <inheritdoc/>
        public Task<RefreshTokenResult> RefreshTokenAsync(string refreshToken, object extraParameters = null)
        {
            return OidcClient.RefreshTokenAsync(refreshToken, extraParameters);
        }

        /// <inheritdoc/>
        public Task<UserInfoResult> GetUserInfoAsync(string accessToken)
        {
            return OidcClient.GetUserInfoAsync(accessToken);
        }
    }
}