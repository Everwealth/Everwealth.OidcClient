using Android.App;
using Android.Content;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Everwealth.OidcClient
{
    /// <summary>
    /// Primary class for performing authentication and authorization operations with Auth0 using the
    /// underlying <see cref="IdentityModel.OidcClient.OidcClient"/>.
    /// </summary>
    public class AuthClient : AuthClientBase
    {
        /// <summary>
        /// Create a new instance of <see cref="AuthClient"/> with a given <see cref="OidcClientOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="OidcClientOptions"/> specifying the configuration to use.</param>
        /// <remarks>options.RedirectUri must match your <see cref="Activity"/> <see cref="IntentFilterPriority"/>
        /// DataScheme, DataPathPrefix and DataHost values.
        /// If not supplied it will presume the convention
        /// <code>$"{Context.PackageName}://{options.Domain}/android/{Context.PackageName}/callback".ToLower();</code>.
        /// Your <see cref="IntentFilterAttribute"/> should have DataScheme, DataPathPrefix and DataHost with values that match.
        /// Alternatively set <see cref="OidcClientOptions"/> RedirectUri and PostLogoutRedirectUri to match your <see cref="IntentFilterAttribute"/>. 
        /// DataScheme must be lower-case or Android will not receive the callbacks.
        /// </remarks>
        public AuthClient(OidcClientOptions options)
            : base(options)
        {
        }

        /// <summary>
        /// Create a new instance of <see cref="AuthClient"/> with a given <see cref="OidcClientOptions"/> and <see cref="Context"/>.
        /// </summary>
        /// <param name="options">The <see cref="OidcClientOptions"/> specifying the configuration to use.</param>
        /// <param name="activity">The <see cref="Activity"/> with the <see cref="IntentFilterAttribute"/> you perform calls to <see cref="AuthClient"/> from.</param>
        /// <remarks>options.RedirectUri must match your IntentFilter attribute's DataScheme, DataPathPrefix and DataHost values.
        /// If not supplied it will first try to detect the registered IntentFilter automatically if your supplied <paramref name="activity"/>.
        /// If it does it will presume the convention
        /// <code>$"{Context.PackageName}://{options.Domain}/android/{Context.PackageName}/callback".ToLower();</code>.
        /// Your <see cref="IntentFilter"/> attribute used to register for callbacks should have DataScheme, DataPathPrefix and DataHost with need values
        /// that match.
        /// Alternatively set the RedirectUri manually to match your IntentFilter. Please note that DataScheme should be lower-case or Android
        /// will not listen to callbacks.
        /// </remarks>
        public AuthClient(OidcClientOptions options, Activity activity)
            : base(options)
        {
            options.Browser = options.Browser ?? new AutoSelectBrowser(activity);

            var defaultRedirectUri = options.RedirectUri == null || options.PostLogoutRedirectUri == null ?
                GetActivityIntentCallbackUri(activity) : null;

            options.RedirectUri = options.RedirectUri ?? defaultRedirectUri;
            options.PostLogoutRedirectUri = options.PostLogoutRedirectUri ?? defaultRedirectUri;
        }

        public void SetBrowser(IBrowser browser)
        {
            OidcClient.Options.Browser = browser;
        }

        /// <summary>
        /// Attempt to find the right <see cref="IntentFilterAttribute"/> for the given
        /// <see cref="Activity"/>.
        /// </summary>
        /// <param name="activity"><see cref="Activity"/> to determine callback Uri from using the associated <see cref="IntentFilterAttribute"/></param>
        /// <returns>A url that can be used as a callback to get to the given <paramref name="activity"/>.</returns>
        private string GetActivityIntentCallbackUri(Activity activity)
        {
            var intents = Attribute
                .GetCustomAttributes(activity.GetType(), typeof(IntentFilterAttribute))
                .OfType<IntentFilterAttribute>()
                .Where(i => IsActionDefaultBrowsable(i) && HasSchemeHostAndPrefix(i))
                .ToList();

            if (intents.Count != 1)
                return null;

            var dataScheme = GetResourcableValue(activity, intents[0].DataScheme);
            var dataHost = GetResourcableValue(activity, intents[0].DataHost);
            var dataPathPrefix = GetResourcableValue(activity, intents[0].DataPathPrefix);

            return $"{dataScheme}://{dataHost}{dataPathPrefix}";
        }

        private bool IsActionDefaultBrowsable(IntentFilterAttribute ifa)
        {
            return ifa.Actions.Contains(Intent.ActionView) &&
                   ifa.Categories.Contains(Intent.CategoryDefault) &&
                   ifa.Categories.Contains(Intent.CategoryBrowsable);
        }

        private bool HasSchemeHostAndPrefix(IntentFilterAttribute ifa)
        {
            return ifa.DataScheme != null && ifa.DataHost != null && ifa.DataPathPrefix != null;
        }

        private string GetResourcableValue(Context context, string value)
        {
            if (!value.StartsWith("@")) return value;
            var resourceId = context.Resources.GetIdentifier(value, null, context.PackageName);
            return resourceId > 0 ? context.Resources.GetString(resourceId) : value;
        }

        public override async Task<LoginResult> DetouredLoginAsync(string detourUrl, LoginRequest request = null)
        {
            if (request == null) request = new LoginRequest();

            if (OidcClient.Options.Browser is WebViewBrowser browser)
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
