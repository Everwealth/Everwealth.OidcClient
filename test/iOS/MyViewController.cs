using Everwealth.OidcClient;
using IdentityModel.OidcClient;
using System;
using UIKit;

namespace iOSTestApp
{
    public partial class MyViewController : UIViewController
    {
        private AuthClient _authClient;
        private Action<string> writeLine;
        private Action clearText;
        private string accessToken;

        public MyViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            _authClient = new AuthClient(new OidcClientOptions
            {
                Flow = OidcClientOptions.AuthenticationFlow.AuthorizationCode,
                ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect,
                Authority = "https://everwealth-stg-sa-idn.azurewebsites.net",
                ClientId = "everwealth.ios",
                Scope = "openid",
                RedirectUri = "com.everwealth.app://auth/callback",
                Browser = new WKWebViewBrowser()
            });

            LoginButton.Clicked += Login;
            UserInfoButton.Clicked += UserInfo;
            LogoutButton.Clicked += Logout;

            writeLine = (s) => TextView.Text += s + "\r\n";
            clearText = () => TextView.Text = "";
        }

        private async void Login(object sender, EventArgs e)
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

        private async void Logout(object sender, EventArgs e)
        {
            clearText();
            writeLine("Starting logout...");

            var result = await _authClient.LogoutAsync();
            accessToken = null;
            writeLine($"Logout successful: {result}");
        }

        private async void UserInfo(object sender, EventArgs e)
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