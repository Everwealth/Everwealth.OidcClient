using System;
using System.Threading.Tasks;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;

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

        public void SetBrowser(IBrowser browser)
        {
            OidcClient.Options.Browser = browser;
        }

        public override async Task<LoginResult> DetouredLoginAsync(string detourUrl, LoginRequest request = null)
        {
            if (request == null) request = new LoginRequest();

            if (OidcClient.Options.Browser is WKWebViewBrowser browser)
            {
                var authState = await OidcClient.PrepareLoginAsync();

                var browserOptions = new ExtendedBrowserOptions(authState.StartUrl, OidcClient.Options.RedirectUri)
                {
                    Timeout = TimeSpan.FromSeconds(request.BrowserTimeout),
                    DisplayMode = request.BrowserDisplayMode,
                    LoadDetourUrl = true,
                    DetourUrl = detourUrl
                };

                if (OidcClient.Options.ResponseMode == OidcClientOptions.AuthorizeResponseMode.FormPost)
                {
                    browserOptions.ResponseMode = OidcClientOptions.AuthorizeResponseMode.FormPost;
                }
                else
                {
                    browserOptions.ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect;
                }

                var browserResult = await browser.InvokeAsync(browserOptions);

                if (browserResult.ResultType == BrowserResultType.Success)
                {
                    var result = await ProcessResponseAsync(
                        browserResult.Response,
                        authState,
                        request.BackChannelExtraParameters);

                    return result;
                }

                return new LoginResult(browserResult.Error ?? browserResult.ResultType.ToString());
            }

            await OidcClient.Options.Browser.InvokeAsync(new BrowserOptions(detourUrl, "null"));
            return new LoginResult("UserCancel");
        }
    }
}
