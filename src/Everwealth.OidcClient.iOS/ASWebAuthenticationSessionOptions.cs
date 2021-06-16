namespace Everwealth.OidcClient
{
    /// <summary>
    /// Specifies options that can be passed to <see cref="ASWebAuthenticationSessionBrowser"/> implementations.
    /// </summary>
    public class ASWebAuthenticationSessionOptions
    {
        /// <summary>
        /// Specify whether or not EphemeralWebBrowserSessions should be preferred. Defaults to false.
        /// </summary>
        /// <remarks>
        /// Setting <see cref="PrefersEphemeralWebBrowserSession"/> to true will disable Single Sign On (SSO) on iOS 13+.
        /// </remarks>
        public bool PrefersEphemeralWebBrowserSession { get; set; }
    }
}
