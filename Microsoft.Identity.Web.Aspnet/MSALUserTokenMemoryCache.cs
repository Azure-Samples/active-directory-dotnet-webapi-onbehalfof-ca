using System.Runtime.Caching;
using Microsoft.Identity.Client;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.Aspnet
{
    public class MSALUserTokenMemoryCache : IMsalTokenCacheProvider
    {
        /// <summary>
        /// The backing MemoryCache instance
        /// </summary>
        internal readonly MemoryCache memoryCache = MemoryCache.Default;

        /// <summary>
        /// The duration till the tokens are kept in memory cache. In production, a higher value, upto 90 days is recommended.
        /// </summary>
        private readonly DateTimeOffset cacheDuration = DateTimeOffset.Now.AddHours(48);

        /// <summary>
        /// Once the user signes in, this will not be null and can be obtained via a call to Thread.CurrentPrincipal
        /// </summary>
        internal ClaimsPrincipal SignedInUser;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSALPerUserMemoryTokenCache"/> class.
        /// </summary>
        /// <param name="tokenCache">The client's instance of the token cache.</param>
        public MSALUserTokenMemoryCache(ITokenCache tokenCache)
        {
            InitializeAsync(tokenCache, ClaimsPrincipal.Current);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MSALPerUserMemoryTokenCache"/> class.
        /// </summary>
        /// <param name="tokenCache">The client's instance of the token cache.</param>
        /// <param name="user">The signed-in user for whom the cache needs to be established.</param>
        public MSALUserTokenMemoryCache(ITokenCache tokenCache, ClaimsPrincipal user)
        {
            InitializeAsync(tokenCache, user);
        }

        public async Task InitializeAsync(ITokenCache tokenCache)
        {
            await InitializeAsync(tokenCache, ClaimsPrincipal.Current);
        }

        /// <summary>Initializes the cache instance</summary>
        /// <param name="tokenCache">The ITokenCache passed through the constructor</param>
        /// <param name="user">The signed-in user for whom the cache needs to be established..</param>
        public Task InitializeAsync(ITokenCache tokenCache, ClaimsPrincipal user)
        {
            SignedInUser = user;

            if (tokenCache == null)
            {
                throw new ArgumentNullException(nameof(tokenCache));
            }

            tokenCache.SetBeforeAccessAsync(OnBeforeAccessAsync);
            tokenCache.SetAfterAccessAsync(OnAfterAccessAsync);


            return Task.CompletedTask;
        }

        /// <summary>
        /// Explores the Claims of a signed-in user (if available) to populate the unique Id of this cache's instance.
        /// </summary>
        /// <returns>The signed in user's object.tenant Id , if available in the ClaimsPrincipal.Current instance</returns>
        internal Task<string> GetMsalAccountIdAsync()
        {
            if (SignedInUser != null)
            {
                return Task.FromResult(SignedInUser.GetMsalAccountId());
            }
            return Task.FromResult<string>(null);
        }

        /// <summary>
        /// Clears the TokenCache's copy of this user's cache.
        /// </summary>
        public async Task ClearAsync()
        {
            memoryCache.Remove(await GetMsalAccountIdAsync());
        }

        public Task ClearAsync(string homeAccountId)
        {
            memoryCache.Remove(homeAccountId);
            return Task.CompletedTask;
        }

        //();
        /// <summary>
        /// Triggered right after MSAL accessed the cache.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private async Task OnAfterAccessAsync(TokenCacheNotificationArgs args)
        {
            SetSignedInUserFromNotificationArgs(args);

            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                string cacheKey = args.Account?.HomeAccountId?.Identifier;

                if (string.IsNullOrEmpty(cacheKey))
                    cacheKey = await GetMsalAccountIdAsync();

                if (string.IsNullOrWhiteSpace(cacheKey))
                    return;

                // Ideally, methods that load and persist should be thread safe.MemoryCache.Get() is thread safe.
                this.memoryCache.Set(cacheKey, args.TokenCache.SerializeMsalV3(), cacheDuration);
            }
        }

        /// <summary>
        /// Triggered right before MSAL needs to access the cache. Reload the cache from the persistence store in case it changed since the last access.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private async Task OnBeforeAccessAsync(TokenCacheNotificationArgs args)
        {
            string cacheKey = args.Account?.HomeAccountId?.Identifier;

            if (string.IsNullOrWhiteSpace(cacheKey))
                cacheKey = await GetMsalAccountIdAsync();

            if (string.IsNullOrWhiteSpace(cacheKey))
                return;

            byte[] tokenCacheBytes = (byte[])this.memoryCache.Get(cacheKey);
            args.TokenCache.DeserializeMsalV3(tokenCacheBytes, shouldClearExistingCache: true);
        }

        /// <summary>
        /// To keep the cache, ClaimsPrincipal and Sql in sync, we ensure that the user's object Id we obtained by MSAL after
        /// successful sign-in is set as the key for the cache.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void SetSignedInUserFromNotificationArgs(TokenCacheNotificationArgs args)
        {
            if (SignedInUser == null && args.Account != null)
            {
                SignedInUser = args.Account.ToClaimsPrincipal();
            }
        }
    }
}
