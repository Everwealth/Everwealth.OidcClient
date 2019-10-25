using System;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using IdentityModel.OidcClient.Browser;
using UIKit;
using WebKit;

namespace Everwealth.OidcClient
{
    /// <summary>
    /// Implements the <see cref="IBrowser"/> interface using <see cref="WKWebView"/> for support on iOS 10 and earlier.
    /// </summary>
    public class WKWebViewBrowser : IOSBrowserBase
    {
        /// <inheritdoc/>
        protected override Task<BrowserResult> Launch(BrowserOptions options)
        {
            return Start(options);
        }

        internal static Task<BrowserResult> Start(BrowserOptions options)
        {
            var tcs = new TaskCompletionSource<BrowserResult>();

            // Create web view controller
            var browserController = new UINavigationController(new WKWebViewController(new NSUrl(options.StartUrl), new NSUrl(options.EndUrl))
            {
                ModalPresentationStyle = UIModalPresentationStyle.FormSheet,
            });

            async void Callback(string response)
            {
                ActivityMediator.Instance.ActivityMessageReceived -= Callback;

                if (response == "UserCancel")
                {
                    BrowserMediator.Instance.Cancel();
                    tcs.SetResult(Canceled());
                }
                else
                {
                    BrowserMediator.Instance.Success();
                    await browserController.DismissViewControllerAsync(true); // Close web view
                    browserController.Dispose();
                    tcs.SetResult(Success(response));
                }
            }

            ActivityMediator.Instance.ActivityMessageReceived += Callback;

            // Launch web view
            FindRootController().PresentViewController(browserController, true, null);

            return tcs.Task;
        }

        private static UIViewController FindRootController()
        {
            var vc = UIApplication.SharedApplication.KeyWindow.RootViewController;
            while (vc.PresentedViewController != null)
                vc = vc.PresentedViewController;
            return vc;
        }
    }

    public class WKWebViewController : UIViewController, IWKNavigationDelegate
    {
        public WKWebView WebView { get; set; }
        private UIActivityIndicatorView _activityIndicatorView;

        public WKWebViewController(NSUrl startUrl, NSUrl endUrl)
        {
            var webViewConfig = new WKWebViewConfiguration();
            webViewConfig.SetUrlSchemeHandler(new CallbackHandler(), endUrl.Scheme);
            WebView = new WKWebView(new CGRect(0, 0, 0, 0), webViewConfig);
            WebView.LoadRequest(new NSUrlRequest(startUrl));
            WebView.WeakNavigationDelegate = this;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Set up bar button
            var closeBarButton = new UIButton(UIButtonType.System);
            closeBarButton.TouchUpInside += Cancelled;
            closeBarButton.SetImage(UIImage.FromFile(@"close"), UIControlState.Normal);
            closeBarButton.Frame = new CGRect(0, 0, 32, 32);
            closeBarButton.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;
            closeBarButton.Layer.CornerRadius = 16;
            closeBarButton.BackgroundColor = new UIColor(red: 0.95f, green: 0.95f, blue: 0.95f, alpha: 1.0f);
            NavigationItem.LeftBarButtonItem = new UIBarButtonItem(closeBarButton);

            NavigationController.NavigationBar.SetBackgroundImage(new UIImage(), UIBarMetrics.Default);
            NavigationController.NavigationBar.ShadowImage = new UIImage();
            NavigationController.NavigationBar.Translucent = true;

            _activityIndicatorView = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.Gray);
            _activityIndicatorView.HidesWhenStopped = true;
            WebView.AddSubview(_activityIndicatorView);
            _activityIndicatorView.StartAnimating();
            _activityIndicatorView.Alpha = 0;
        }

        public override void LoadView()
        {
            View = WebView;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            _activityIndicatorView.Center = View.Center;
            UIView.Animate(0.15, () =>
            {
                _activityIndicatorView.Alpha = 1;
            });
        }

        private async void Cancelled(object sender, EventArgs e)
        {
            ActivityMediator.Instance.Send("UserCancel");
            await NavigationController.DismissViewControllerAsync(true);
        }

        //[Export("webView:didStartProvisionalNavigation:")]
        //public void DidStartProvisionalNavigation(WKWebView webView, WKNavigation navigation)
        //{
        //    _activityIndicatorView.Center = View.Center;
        //    _activityIndicatorView.StartAnimating();
        //}

        [Export("webView:didFinishNavigation:")]
        public void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
        {
            _activityIndicatorView.StopAnimating();
            _activityIndicatorView.Center = View.Center;
        }

        [Export("webView:didFailNavigation:withError:")]
        public void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error)
        {
            _activityIndicatorView.StopAnimating();
            _activityIndicatorView.Center = View.Center;
        }

        private class CallbackHandler : NSObject, IWKUrlSchemeHandler
        {
            public void StartUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
            {
                ActivityMediator.Instance.Send(urlSchemeTask.Request.Url.AbsoluteString);
            }

            public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
            {
                //urlSchemeTask.DidFailWithError(new NSError()
            }
        }
    }
}
