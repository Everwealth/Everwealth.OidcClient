using System;
using System.Threading.Tasks;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Results;

namespace Everwealth.OidcClient
{
    /// <summary>
    /// Interface for performing authentication and authorization operations with an identity server using the
    /// underlying <see cref="IdentityModel.OidcClient.OidcClient"/>.
    /// </summary>
    public interface IAuthClient
    {
        OidcClientOptions Options { get; }

        /// <summary>
        /// Starts a login.
        /// </summary>
        /// <param name="request">Request object.</param>
        /// <returns>A <see cref="LoginResult"/> containing the tokens and claims.</returns>
        Task<LoginResult> LoginAsync(LoginRequest request = null);

        /// <summary>
        /// Starts a session at the provided URL and starts a login if registered restart route is hit.
        /// </summary>
        /// <param name="alternateUrl">URL to be loaded.</param>
        /// <param name="request">Request object.</param>
        /// <returns>A <see cref="LoginResult"/> containing the tokens and claims.</returns>
        Task<LoginResult> DetouredLoginAsync(string detourUrl, LoginRequest request = null);

        /// <summary>
        /// Starts a logout.
        /// </summary>
        /// <param name="request">Request object.</param>
        /// <returns>A <see cref="bool"/> representating if the request was successful.</returns>
        Task<bool> LogoutAsync(LogoutRequest request = null);

        /// <summary>
        /// Generates an <see cref="AuthorizeState"/> containing the URL, state, nonce and code challenge which can
        /// be used to redirect the user to the authorization URL, and subsequently process any response by calling
        /// the <see cref="ProcessResponseAsync"/> method.
        /// </summary>
        /// <param name="extraParameters">Optional extra parameters that need to be passed to the endpoint.</param>
        /// <returns>A <see cref="AuthorizeState"/> with necessary URLs, nonce, state and code verifiers.</returns>
        Task<AuthorizeState> PrepareLoginAsync(object extraParameters = null);

        /// <summary>
        /// Creates a logout URL.
        /// </summary>
        /// <param name="request">Request object.</param>
        /// <returns>A <see cref="string"/> representation of the logout URL.</returns>
        Task<string> PrepareLogoutAsync(LogoutRequest request = null);

        /// <summary>
        /// Process the response from the redirect URI.
        /// </summary>
        /// <param name="data">The data containing the full redirect URI.</param>
        /// <param name="state">The <see cref="AuthorizeState"/> which was generated when the <see cref="PrepareLoginAsync"/>
        /// method was called.</param>
        /// <returns>A <see cref="LoginResult"/> containing the tokens and claims.</returns>
        Task<LoginResult> ProcessResponseAsync(string data, AuthorizeState state, object extraParameters = null);

        /// <summary>
        /// Generates a new set of tokens based on a refresh token. 
        /// </summary>
        /// <param name="refreshToken">Refresh token which was issued during the authorization flow, or subsequent
        /// calls to <see cref="IdentityModel.OidcClient.OidcClient.RefreshTokenAsync"/>.</param>
        /// <param name="extraParameters">Optional extra parameters that need to be passed to the endpoint.</param>
        /// <returns>A <see cref="RefreshTokenResult"/> with the refreshed tokens.</returns>
        Task<RefreshTokenResult> RefreshTokenAsync(string refreshToken, object extraParameters = null);

        /// <summary>
        /// Gets the user claims from the userinfo endpoint.
        /// </summary>
        /// <param name="accessToken">Access token to use in obtaining claims.</param>
        /// <returns>
        /// <returns>A <see cref="UserInfoResult"/> with the user information and claims.</returns>
        /// </returns>
        /// <exception cref="ArgumentNullException">When <paramref name="accessToken"/> is null.</exception>
        /// <exception cref="InvalidOperationException">When no userinfo endpoint specified.</exception>
        Task<UserInfoResult> GetUserInfoAsync(string accessToken);
    }
}