using System;
using IdentityModel.OidcClient.Browser;

namespace Everwealth.OidcClient
{
    public class ExtendedBrowserOptions : BrowserOptions
    {
        public ExtendedBrowserOptions(string startUrl, string endUrl) : base(startUrl, endUrl)
        {
        }

        public ExtendedBrowserOptions(string startUrl, string endUrl, string[] restartFlowRoutes) : base(startUrl, endUrl)
        {
            RestartFlowRoutes = restartFlowRoutes;
        }

        /// <summary>
        /// If one of these routes is hit after the initial request, the flow will be restarted.
        /// Routes should be formatted excluding the domain eg. /signin
        /// </summary>
        public string[] RestartFlowRoutes { get; set; }
    }
}
