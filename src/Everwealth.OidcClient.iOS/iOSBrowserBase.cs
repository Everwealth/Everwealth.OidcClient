﻿using IdentityModel.OidcClient.Browser;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Everwealth.OidcClient
{
    /// <summary>
    /// Provides common <see cref="IBrowser"/> logic for iOS platform.
    /// </summary>
    public abstract class IOSBrowserBase : IBrowser
    {
        /// <inheritdoc/>
        public Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(options.StartUrl))
                throw new ArgumentException("Missing StartUrl", nameof(options));

            if (string.IsNullOrWhiteSpace(options.EndUrl))
                throw new ArgumentException("Missing EndUrl", nameof(options));
            
            return Launch(options, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<BrowserResult> InvokeAsync(ExtendedBrowserOptions options, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(options.StartUrl))
                throw new ArgumentException("Missing StartUrl", nameof(options));

            if (string.IsNullOrWhiteSpace(options.EndUrl))
                throw new ArgumentException("Missing EndUrl", nameof(options));

            return Launch(options, cancellationToken);
        }

        /// <summary>
        /// Launch a browser with the options and URL specified by the <see cref="BrowserOptions"/>.
        /// </summary>
        /// <param name="options"></param>
        /// <returns>A <see cref="Task"/> that will contain a <see cref="BrowserResult"/> with details of
        /// wether the launch process succeeded or not by way of a <see cref="BrowserResultType"/>.</returns>
        protected abstract Task<BrowserResult> Launch(BrowserOptions options, CancellationToken cancellationToken);

        internal static BrowserResult Canceled()
        {
            return new BrowserResult
            {
                ResultType = BrowserResultType.UserCancel
            };
        }

        internal static BrowserResult UnknownError(string error)
        {
            return new BrowserResult
            {
                ResultType = BrowserResultType.UnknownError,
                Error = error
            };
        }

        internal static BrowserResult Success(string response)
        {
            return new BrowserResult
            {
                Response = response,
                ResultType = BrowserResultType.Success
            };
        }
    }
}