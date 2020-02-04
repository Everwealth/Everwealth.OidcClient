using System;
using System.Collections.Generic;
using IdentityModel.OidcClient.Browser;

namespace Everwealth.OidcClient
{
    public class ExtendedBrowserOptions : BrowserOptions
    {
        public ExtendedBrowserOptions(string startUrl, string endUrl) : base(startUrl, endUrl)
        {
        }

        public ExtendedBrowserOptions(string startUrl, string endUrl, string[] restartFlowRoutes, string[] viewableUrls, Dictionary<object,object> headers) : base(startUrl, endUrl)
        {
            RestartFlowRoutes = restartFlowRoutes;
            ViewableUrls = viewableUrls;
            Headers = headers;
        }

        /// <summary>
        /// If one of these routes is hit after the initial request, the flow will be restarted.
        /// Routes should be formatted excluding the domain eg. /signin
        /// </summary>
        public string[] RestartFlowRoutes { get; set; }

        /// <summary>
        /// Whitelisted external URLs that could be available in the identity flow
        /// </summary>
        public string[] ViewableUrls { get; set; }
        public Dictionary<object, object> Headers { get; set; }

        /// <summary>
        /// An boolean that determines whether the detour URL should be loaded.
        /// </summary>
        public bool LoadDetourUrl { get; set; }

        /// <summary>
        /// A URL that may be triggered before the startUrl.
        /// </summary>
        public string DetourUrl { get; set; }
    }
}
