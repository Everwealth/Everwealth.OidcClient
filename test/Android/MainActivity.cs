using Android.App;
using Android.Content;
using Android.OS;
using Android.Text.Method;
using Android.Widget;
using Everwealth.OidcClient;
using IdentityModel.OidcClient;
using System;

namespace AndroidTestApp
{
    [Activity(Label = "Android OIDC Test", MainLauncher = true, Icon = "@drawable/icon", LaunchMode = Android.Content.PM.LaunchMode.SingleTask)]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "com.everwealth.app",
        DataHost = "",
        DataPathPrefix = "/auth/callback")]
    public class MainActivity : AuthClientActivity
    {
        private AuthClient _authClient;
        private Action<string> writeLine;
        private Action clearText;
        private TextView _userDetailsTextView;
        private string accessToken;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            _authClient = new AuthClient(new OidcClientOptions
            {
                //Authority = Resources.GetString(Resource.String.auth0_domain),
                Authority = "",
                ClientId = "",
                Scope = "openid",
                RedirectUri = "com.everwealth.app://auth/callback",
            }, this);

            SetContentView(Resource.Layout.Main);
            FindViewById<Button>(Resource.Id.LoginButton).Click += LoginButtonOnClick;
            FindViewById<Button>(Resource.Id.LogoutButton).Click += LogoutButtonOnClick;
            FindViewById<Button>(Resource.Id.UserButton).Click += UserButtonOnClick;

            _userDetailsTextView = FindViewById<TextView>(Resource.Id.UserDetailsTextView);
            _userDetailsTextView.MovementMethod = new ScrollingMovementMethod();

            writeLine = (s) => _userDetailsTextView.Text += s + "\n";
            clearText = () => _userDetailsTextView.Text = "";
        }

        private async void LoginButtonOnClick(object sender, EventArgs eventArgs)
        {
            clearText();
            writeLine("Starting login...");

            var loginResult = await _authClient.LoginAsync();

            if (loginResult.IsError)
            {
                writeLine($"An error occurred during login: {loginResult.Error}");
                return;
            }

            accessToken = loginResult.AccessToken;

            writeLine($"id_token: {loginResult.IdentityToken}");
            writeLine($"access_token: {loginResult.AccessToken}");
            writeLine($"refresh_token: {loginResult.RefreshToken}");

            writeLine($"name: {loginResult.User.FindFirst(c => c.Type == "name")?.Value}");
            writeLine($"email: {loginResult.User.FindFirst(c => c.Type == "email")?.Value}");

            foreach (var claim in loginResult.User.Claims)
            {
                writeLine($"{claim.Type} = {claim.Value}");
            }
        }

        private async void LogoutButtonOnClick(object sender, EventArgs e)
        {
            clearText();
            writeLine("Starting logout...");

            var result = await _authClient.LogoutAsync();
            accessToken = null;
            writeLine($"Logout successful: {result}");
        }

        private async void UserButtonOnClick(object sender, EventArgs e)
        {
            clearText();

            if (string.IsNullOrEmpty(accessToken))
            {
                writeLine("You need to be logged in to get user info");
                return;
            }

            writeLine("Getting user info...");
            var userInfoResult = await _authClient.GetUserInfoAsync(accessToken);

            if (userInfoResult.IsError)
            {
                writeLine($"An error occurred getting user info: {userInfoResult.Error}");
                return;
            }

            foreach (var claim in userInfoResult.Claims)
            {
                writeLine($"{claim.Type} = {claim.Value}");
            }
        }
    }
}