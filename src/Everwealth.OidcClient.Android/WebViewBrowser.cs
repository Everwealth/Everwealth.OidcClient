using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
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
        private string[] _restartFlowRoutes;
        private readonly string[] _viewableUrls;

        /// <summary>
        /// Create a new instance of <see cref="WebViewBrowser"/> for a given <see cref="Context"/>.
        /// </summary>
        /// <param name="context"><see cref="Context"/> provided to any subsequent callback.</param>
        /// <param name="restartFlowRoutes">If one of these routes is hit after the initial request, the flow will be restarted. Routes should be formatted excluding the domain eg. /signin</param>
        public WebViewBrowser(Context context = null, string[] restartFlowRoutes = null, string[] viewableUrls = null)
            : base(context)
        {
            _restartFlowRoutes = restartFlowRoutes;
            _viewableUrls = viewableUrls;
        }

        /// <inheritdoc/>
        protected override void OpenBrowser(Android.Net.Uri uri, Context context = null)
        {
            var intent = new Intent(context, typeof(WebViewActivity));
            //intent.AddFlags(ActivityFlags.NoHistory);

            if (IsNewTask)
                intent.AddFlags(ActivityFlags.NewTask);

            // Send uri through to activity
            intent.PutExtra(WebViewActivity.EXTRA_URL, uri.ToString());
            intent.PutExtra(WebViewActivity.RESTART_PATHS, _restartFlowRoutes);

            context.StartActivity(intent);
        }

        protected override void OpenBrowser(Android.Net.Uri startUri, Android.Net.Uri detouredUri, Context context = null)
        {
            var intent = new Intent(context, typeof(WebViewActivity));
            //intent.AddFlags(ActivityFlags.NoHistory);

            if (IsNewTask)
                intent.AddFlags(ActivityFlags.NewTask);

            // Send uri through to activity
            intent.PutExtra(WebViewActivity.EXTRA_URL, startUri.ToString());
            intent.PutExtra(WebViewActivity.EXTRA_DETOUR_URL, detouredUri.ToString());
            intent.PutExtra(WebViewActivity.RESTART_PATHS, _restartFlowRoutes);
            intent.PutExtra(WebViewActivity.VIEWABLE_URLS, _viewableUrls);
            context.StartActivity(intent);
        }
    }

    [Activity(Theme = "@android:style/Theme.Material.Light")]
    public class WebViewActivity : Activity
    {
        public const string EXTRA_URL = "extra.url";
        public const string EXTRA_DETOUR_URL = "extra.detoururl";
        public const string RESTART_PATHS = "restart.paths";
        public const string VIEWABLE_URLS = "extra.viewableurls";

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
            string detourUrl = Intent.GetStringExtra(EXTRA_DETOUR_URL);
            string[] restartPaths = Intent.GetStringArrayExtra(RESTART_PATHS);
            string[] viewableUrls = Intent.GetStringArrayExtra(VIEWABLE_URLS);
            WebView webView = FindViewById<WebView>(Resource.Id.webview);
            ProgressBar progressDialog = FindViewById<ProgressBar>(Resource.Id.progressBar);
            var webViewClient = new CustomSchemeWebViewClient(webView, progressDialog, url, restartPaths, OnSuccessLogin, detourUrl, viewableUrls, this);
            webView.SetWebViewClient(webViewClient);
            
            WebSettings webSettings = webView.Settings;
            webSettings.JavaScriptEnabled = true;
            Title = "";
            ActionBar.SetBackgroundDrawable(new ColorDrawable(Color.White));
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_close_black_24dp);
            webView.LoadUrl(string.IsNullOrEmpty(detourUrl) ? url : detourUrl);
        }

        private void OnSuccessLogin()
        {
            Finish();
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
            private readonly WebView _webView;
            private readonly ProgressBar _progressDialog;
            private readonly Uri _startUrl;
            private readonly string[] _restartPaths;
            private readonly Action _onSuccess;
            private readonly string[] _viewableUrls;
            private readonly Uri _detourUrl;

            public Context Context { get; }

            public CustomSchemeWebViewClient(WebView webView,
                                             ProgressBar progressDialog,
                                             string startUrl,
                                             string[] restartPaths,
                                             Action onSuccess,
                                             string detourUrl,
                                             string[] viewableUrls,
                                             Context context)
            {
                _webView = webView;
                _progressDialog = progressDialog;
                _startUrl = new Uri(startUrl);
                _restartPaths = restartPaths;
                _onSuccess = onSuccess;
                _viewableUrls = viewableUrls;
                Context = context;
                _detourUrl = string.IsNullOrEmpty(detourUrl) ? null : new Uri(detourUrl);
                
            }

            public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
            {
                if (request?.Url != null && request.Url.Scheme != "http" && request.Url.Scheme != "https")
                {
                    ActivityMediator.Instance.Send(request.Url.ToString());
                    _onSuccess?.Invoke();
                    return true;
                }

                if (request.Url is Android.Net.Uri url)
                {
                    if (url.Scheme != "http" && url.Scheme != "https")
                    {
                        Console.WriteLine("URL loading overriden: Hit url {0}", url);
                        ActivityMediator.Instance.Send(request.Url.ToString());
                        _onSuccess?.Invoke();
                        return true;
                    }
                    else if (_restartPaths is string[] restartPaths
                        && url.Host == _startUrl.Host
                        && string.IsNullOrEmpty(url.Query)
                        && restartPaths.Contains(url.Path, StringComparer.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Policy Decision: We hit a redirect route, starting a new session {0}", url);
                        _webView.LoadUrl(_startUrl.ToString());
                        return true;
                    }
                    else if (_viewableUrls != null && _viewableUrls.Any(x => Android.Net.Uri.Parse(x).Host == url.Host))
                    {
                        Console.WriteLine("Policy Decision: Other Url {0} Opening in external browser", url);

                        var i = new Intent(Intent.ActionView);
                        i.SetData(url);
                        i.AddFlags(ActivityFlags.NoHistory);
                        Context.StartActivity(i);
                        return true;
                        
                    }
                }
                _progressDialog.Visibility = ViewStates.Visible;
                return base.ShouldOverrideUrlLoading(view, request);
            }

            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                var uri = Android.Net.Uri.Parse(url);
                if (uri != null && uri.Scheme != "http" && uri.Scheme != "https")
                {
                    ActivityMediator.Instance.Send(uri.ToString());
                    _onSuccess?.Invoke();
                    return true;
                }

                if (uri.Scheme != "http" && uri.Scheme != "https")
                {
                    Console.WriteLine("URL loading overriden: Hit url {0}", url);
                    ActivityMediator.Instance.Send(url);
                    _onSuccess?.Invoke();
                    return true;
                }
                else if (_restartPaths is string[] restartPaths
                    && uri.Host == _startUrl.Host
                    && string.IsNullOrEmpty(uri.Query)
                    && restartPaths.Contains(uri.Path, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Policy Decision: We hit a redirect route, starting a new session {0}", url);
                    _webView.LoadUrl(_startUrl.ToString());
                    return true;
                }
                else if (_viewableUrls != null && _viewableUrls.Any(x => Android.Net.Uri.Parse(x).Host == uri.Host))
                {
                    Console.WriteLine("Policy Decision: Other Url {0} Opening in external browser", url);

                    var i = new Intent(Intent.ActionView);
                    i.SetData(uri);
                    i.AddFlags(ActivityFlags.NoHistory);
                    Context.StartActivity(i);
                    return true;

                }
                _progressDialog.Visibility = ViewStates.Visible;
                return base.ShouldOverrideUrlLoading(view, url);
            }

            public override void OnPageFinished(WebView view, string url)
            {
                base.OnPageFinished(view, url);
                _progressDialog.Visibility = ViewStates.Invisible;
            }
        }
    }
}
