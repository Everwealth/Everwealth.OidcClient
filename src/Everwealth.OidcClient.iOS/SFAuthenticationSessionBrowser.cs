﻿using Foundation;
using IdentityModel.OidcClient.Browser;
using SafariServices;
using System.Threading.Tasks;

namespace Everwealth.OidcClient
{
    /// <summary>
    /// Implements the Browser <see cref="IBrowser"/> using <see cref="SFAuthenticationSession"/> for support on iOS 11.
    /// </summary>
    public class SFAuthenticationSessionBrowser : IOSBrowserBase
    {
        /// <inheritdoc/>
        protected override Task<BrowserResult> Launch(BrowserOptions options)
        {
            return Start(options);
        }

        internal static Task<BrowserResult> Start(BrowserOptions options)
        {
            var tcs = new TaskCompletionSource<BrowserResult>();

            SFAuthenticationSession sfWebAuthenticationSession = null;
            sfWebAuthenticationSession = new SFAuthenticationSession(
                new NSUrl(options.StartUrl),
                options.EndUrl,
                (callbackUrl, error) =>
                {
                    tcs.SetResult(CreateBrowserResult(callbackUrl, error));
                    sfWebAuthenticationSession.Dispose();
                });

            sfWebAuthenticationSession.Start();

            return tcs.Task;
        }

        private static BrowserResult CreateBrowserResult(NSUrl callbackUrl, NSError error)
        {
            if (error == null)
            {
                BrowserMediator.Instance.Success();
                return Success(callbackUrl.AbsoluteString);
            }

            if (error.Code == (long)SFAuthenticationError.CanceledLogin)
            {
                BrowserMediator.Instance.Cancel();
                return Canceled();
            }

            BrowserMediator.Instance.Cancel();
            return UnknownError(error.ToString());
        }
    }
}
