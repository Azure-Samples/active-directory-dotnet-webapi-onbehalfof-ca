using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.Aspnet
{
    public class ApplicationBuilders
    {
        private IMsalTokenCacheProvider userTokenCache = null;
        internal IMsalTokenCacheProvider UserTokenCache { get => userTokenCache; set => userTokenCache = value; }

        private IMsalTokenCacheProvider applicationtokenCache = null;
        internal IMsalTokenCacheProvider ApplicationtokenCache { get => applicationtokenCache; set => applicationtokenCache = value; }

        public CacheType CacheType { get; set; }

        public ApplicationBuilders(CacheType cacheType)
        {
            this.CacheType = cacheType;
        }

        private void InitializeUserTokenCache(ITokenCache cache)
        {
            switch (this.CacheType)
            {
                case CacheType.DistributedCache:
                    break;

                case CacheType.FileCache:
                    break;

                case CacheType.InMemoryCache:
                    userTokenCache = new MSALUserTokenMemoryCache(cache);
                    break;

                default:
                    break;
            }
        }

        public IConfidentialClientApplication BuildConfidentialClientApplication(AuthenticationConfig config)//, IMsalTokenCacheProvider userTokencacheProvider, IMsalTokenCacheProvider appTokencacheProvider)
        {
            var app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                .WithRedirectUri(config.RedirectUri)
                .WithAuthority(config.Authority)
                .WithClientSecret(config.ClientSecret)
                .Build();

            this.InitializeUserTokenCache(app.UserTokenCache);

            //// Initialize token cache providers
            //if (this.userTokenCache != null)
            //{
            //    await this.userTokenCache.InitializeAsync(app.UserTokenCache);
            //}

            //if (this.applicationtokenCache != null)
            //{
            //    await this.applicationtokenCache.InitializeAsync(app.AppTokenCache);
            //}

            return app;
        }
    }
}