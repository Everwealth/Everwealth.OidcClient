using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Android.Widget;

namespace Everwealth.OidcClient
{
    /// <summary>
    /// Implements the <see cref="IBrowser"/> interface using the best available option for the current Android version.
    /// </summary>
    public class WebViewBrowser : AndroidBrowserBase
    {
        /// <summary>
        /// Create a new instance of <see cref="WebViewBrowser"/> for a given <see cref="Context"/>.
        /// </summary>
        /// <param name="context"><see cref="Context"/> provided to any subsequent callback.</param>
        public WebViewBrowser(Context context = null)
            : base(context)
        {
        }

        /// <inheritdoc/>
        protected override void OpenBrowser(Android.Net.Uri uri, Context context = null)
        {
            var intent = new Intent(context, typeof(WebViewActivity));
            intent.AddFlags(ActivityFlags.NoHistory);

            if (IsNewTask)
                intent.AddFlags(ActivityFlags.NewTask);

            // Send uri through to activity
            intent.PutExtra(WebViewActivity.EXTRA_URL, uri.ToString());

            context.StartActivity(intent);
        }
    }

    [Activity(Theme = "@android:style/Theme.Material.Light")]
    public class WebViewActivity : Activity
    {
        public const string EXTRA_URL = "extra.url";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_webview);

            // Cookie settings
            CookieSyncManager.CreateInstance(this);
            var cookieManager = CookieManager.Instance;
            cookieManager.RemoveAllCookie();
            //cookieManager.SetAcceptCookie(false);

            string url = Intent.GetStringExtra(EXTRA_URL);
            WebView webView = FindViewById<WebView>(Resource.Id.webview);
            ProgressBar progressDialog = FindViewById<ProgressBar>(Resource.Id.progressBar);
            webView.SetWebViewClient(new CustomSchemeWebViewClient(progressDialog));
            WebSettings webSettings = webView.Settings;
            webSettings.JavaScriptEnabled = true;
            Title = "";
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_close_black_24dp);
            webView.LoadUrl(url);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                // Respond to the action bar's Up/Home button
                case global::Android.Resource.Id.Home:
                    Finish();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public class CustomSchemeWebViewClient : WebViewClient
        {
            private readonly ProgressBar _progressDialog;
            public CustomSchemeWebViewClient(ProgressBar progressDialog)
            {
                _progressDialog = progressDialog;
            }

            public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
            {
                if (request?.Url != null && request.Url.Scheme != "http" && request.Url.Scheme != "https")
                {
                    ActivityMediator.Instance.Send(request.Url.ToString());
                    return true;
                }
                _progressDialog.Visibility = ViewStates.Visible;
                return base.ShouldOverrideUrlLoading(view, request);
            }

            public override void OnPageFinished(WebView view, string url)
            {
                base.OnPageFinished(view, url);
                _progressDialog.Visibility = ViewStates.Invisible;
            }
        }
    }
}
