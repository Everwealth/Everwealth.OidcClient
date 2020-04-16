using Android.App;
using Android.Content;
using IdentityModel.OidcClient.Browser;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Everwealth.OidcClient
{
    /// <summary>
    /// Provides common IBrowser logic for Android.
    /// </summary>
    public abstract class AndroidBrowserBase : IBrowser
    {
        /// <summary>
        /// The <see cref="Context"/> associated with this browser.
        /// </summary>
        protected Context context;

        /// <summary>
        /// Whether this browser should launch a new Android Task.
        /// </summary>
        protected bool IsNewTask;

        /// <summary>
        /// Default constructor for <see cref="AndroidBrowserBase"/> that provides assignment
        /// of context and IsNewTask when called by subclasses.
        /// </summary>
        /// <param name="context">Optional <see cref="Context"/> to provide on subsequent callbacks.</param>
        protected AndroidBrowserBase(Context context = null)
        {
            this.context = context;
            IsNewTask = context == null;
        }

        /// <inheritdoc/>
        public Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(options.StartUrl))
                throw new ArgumentException("Missing StartUrl", nameof(options));

            if (string.IsNullOrWhiteSpace(options.EndUrl))
                throw new ArgumentException("Missing EndUrl", nameof(options));

            var tcs = new TaskCompletionSource<BrowserResult>();

            void Callback(string response)
            {
                ActivityMediator.Instance.ActivityMessageReceived -= Callback;

                var cancelled = response == "UserCancel";
                if (cancelled)
                {
                    BrowserMediator.Instance.Cancel();
                }
                else
                {
                    BrowserMediator.Instance.Success();
                }

                tcs.SetResult(new BrowserResult
                {
                    ResultType = cancelled ? BrowserResultType.UserCancel : BrowserResultType.Success,
                    Response = response
                });

            }

            ActivityMediator.Instance.ActivityMessageReceived += Callback;

            if (options is ExtendedBrowserOptions extendedOptions && extendedOptions.LoadDetourUrl)
            {
                OpenBrowser(Android.Net.Uri.Parse(extendedOptions.StartUrl), Android.Net.Uri.Parse(extendedOptions.DetourUrl), context ?? Application.Context, cancellationToken);
            }
            else
            {
                OpenBrowser(Android.Net.Uri.Parse(options.StartUrl), context ?? Application.Context, cancellationToken);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Open a web browser with the given uri.
        /// </summary>
        /// <param name="uri"><see cref="Uri"/> address to open in the browser.</param>
        /// <param name="context">Optional <see cref="Context"/> associated with the browser.</param>
        protected abstract void OpenBrowser(Android.Net.Uri uri, Context context = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Open a web browser with the given uri.
        /// </summary>
        /// <param name="uri"><see cref="Uri"/> address to open in the browser.</param>
        /// <param name="context">Optional <see cref="Context"/> associated with the browser.</param>
        protected virtual void OpenBrowser(Android.Net.Uri startUri, Android.Net.Uri detourUri, Context context = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}