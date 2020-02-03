using System;
using System.Collections.Generic;
using System.Linq;
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
        private string[] _restartFlowRoutes;
        private readonly string[] _viewableUrls;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="restartFlowRoutes">If one of these routes is hit after the initial request, the flow will be restarted. Routes should be formatted excluding the domain eg. /signin</param>
        public WKWebViewBrowser(string[] restartFlowRoutes = null, string[] viewableUrls = null)
        {
            _restartFlowRoutes = restartFlowRoutes;
            _viewableUrls = viewableUrls;
        }

        /// <inheritdoc/>
        protected override Task<BrowserResult> Launch(BrowserOptions options)
        {
            if (options is ExtendedBrowserOptions extendedOptions)
            {
                extendedOptions.RestartFlowRoutes = _restartFlowRoutes;
                extendedOptions.ViewableUrls = _viewableUrls;
                return Start(extendedOptions);
            }
            return Start(new ExtendedBrowserOptions(options.StartUrl, options.EndUrl, _restartFlowRoutes, _viewableUrls));
        }

        internal static Task<BrowserResult> Start(ExtendedBrowserOptions options)
        {
            var tcs = new TaskCompletionSource<BrowserResult>();

            // Create web view controller
            var browserController = new UINavigationController(new WKWebViewController(options)
            {
                ModalPresentationStyle = UIModalPresentationStyle.FormSheet,
            });
            browserController.PresentationController.Delegate = new DismissablePresentationControllerDelegate();

            async void Callback(string response)
            {
                ActivityMediator.Instance.ActivityMessageReceived -= Callback;

                if (response == "UserCancel")
                {
                    BrowserMediator.Instance.Cancel();
                    tcs.SetResult(Canceled());
                }
                else if (response == "RestartFlow")
                {
                    // Close existing
                    await browserController.DismissViewControllerAsync(true); // Close web view
                    browserController.Dispose();

                    // Launch new
                    FindRootController().PresentViewController(browserController, true, null);
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

    public class DismissablePresentationControllerDelegate : UIAdaptivePresentationControllerDelegate
    {
        public override bool ShouldDismiss(UIPresentationController presentationController)
        {
            return true;
        }

        public override void DidDismiss(UIPresentationController presentationController)
        {
            ActivityMediator.Instance.Cancel();
        }
    }

    public class WKWebViewController : UIViewController, IWKNavigationDelegate
    {
        public WKWebView WebView { get; set; }
        private UIActivityIndicatorView _activityIndicatorView;
        private readonly ExtendedBrowserOptions _options;
        public WKWebViewController(ExtendedBrowserOptions options)
        {
            _options = options;

            var webViewConfig = new WKWebViewConfiguration();
            //webViewConfig.SetUrlSchemeHandler(new CallbackHandler(), endUrl.Scheme);
            WebView = new WKWebView(new CGRect(0, 0, 0, 0), webViewConfig);
            WebView.LoadRequest(new NSUrlRequest(new NSUrl(options.LoadDetourUrl ? options.DetourUrl : options.StartUrl)));
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

            if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                var safeView = new UIView(new CGRect(0, 0, UIApplication.SharedApplication.StatusBarFrame.Size.Width, UIApplication.SharedApplication.StatusBarFrame.Size.Height))
                {
                    BackgroundColor = UIColor.White
                };
                this.Add(safeView);
            }
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
            ActivityMediator.Instance.Cancel();
            await NavigationController.DismissViewControllerAsync(true);
        }

        [Export("webView:decidePolicyForNavigationAction:decisionHandler:")]
        public void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
        {
            if (navigationAction.Request?.Url is NSUrl url)
            {
                if (url.Scheme == new NSUrl(_options.EndUrl).Scheme)
                {
                    Console.WriteLine("Policy Decision: Handled callback with URL {0}", url);
                    ActivityMediator.Instance.Send(url.AbsoluteString);
                }
                else if (_options.RestartFlowRoutes is string[] restartRoutes
                    && url.Host == new NSUrl(_options.StartUrl).Host
                    && string.IsNullOrEmpty(url.Query)
                    && restartRoutes.Contains(url.Path, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Policy Decision: We hit a redirect route, starting a new session {0}", url);
                    WebView.LoadRequest(new NSUrlRequest(new NSUrl(_options.StartUrl)));
                }
                else if (_options.ViewableUrls.Any(x => new NSUrl(x).Host == url.Host))
                { 
                    if (UIApplication.SharedApplication.CanOpenUrl(url))
                    {
                        Console.WriteLine("Policy Decision: We hit other URL, open in Safari");
                        UIApplication.SharedApplication.OpenUrl(url);
                    }
                }
            }
            decisionHandler(WKNavigationActionPolicy.Allow);
        }

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
                Console.WriteLine("Callback Handler: Handled callback with URL {0}", urlSchemeTask.Request.Url);
                ActivityMediator.Instance.Send(urlSchemeTask.Request.Url.AbsoluteString);
            }

            public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
            {
                //urlSchemeTask.DidFailWithError(new NSError()
            }
        }
    }
}
