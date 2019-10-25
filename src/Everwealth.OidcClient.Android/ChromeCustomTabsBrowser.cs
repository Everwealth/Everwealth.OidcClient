using System.Collections.Generic;
using Android.Content;
using Android.Support.CustomTabs;

namespace Everwealth.OidcClient
{
    /// <summary>
    /// Implements browser integration using Chrome Custom Tabs.
    /// </summary>
    public class ChromeCustomTabsBrowser : AndroidBrowserBase
    {
        private ChromeCustomTabsServiceConnection _customTabsServiceConnection;

        /// <summary>
        /// Create a new instance of <see cref="ChromeCustomTabsBrowser"/> for a given <see cref="Context"/>.
        /// </summary>
        /// <param name="context"><see cref="Context"/> provided to any subsequent callback.</param>
        public ChromeCustomTabsBrowser(Context context = null)
            : base(context)
        {
            //_customTabsServiceConnection = new ChromeCustomTabsServiceConnection();
            //var packageName = CustomTabsClient.GetPackageName(context, new List<string>() { TrustedWebUtils.ExtraLaunchAsTrustedWebActivity }, false);
            //CustomTabsClient.BindCustomTabsService(context, packageName, _customTabsServiceConnection);
        }

        /// <inheritdoc/>
        protected override void OpenBrowser(Android.Net.Uri uri, Context context = null)
        {
            //using (var builder = new CustomTabsIntent.Builder(_customTabsServiceConnection.Session))
            using (var builder = new CustomTabsIntent.Builder())
            using (var customTabsIntent = builder.Build())
            {
                customTabsIntent.Intent.AddFlags(ActivityFlags.NoHistory);
                if (IsNewTask)
                    customTabsIntent.Intent.AddFlags(ActivityFlags.NewTask);
                customTabsIntent.LaunchUrl(context, uri);

                //TrustedWebUtils.LaunchAsTrustedWebActivity(context, customTabsIntent, uri);
            }
        }
    }

    public class ChromeCustomTabsServiceConnection : CustomTabsServiceConnection
    {
        public CustomTabsClient Client;
        public CustomTabsSession Session;
        public CustomTabsIntent Intent;

        public override void OnCustomTabsServiceConnected(ComponentName name, CustomTabsClient client)
        {
            Client = client;
            Client.Warmup(0L);
            Session = Client.NewSession(callback: null);
        }

        public override void OnServiceDisconnected(ComponentName name)
        {
            Client = null;
        }
    }

    public class ChromeCustomTabsCallback : CustomTabsCallback
    {

    }
}