using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.Aspnet
{
    internal static class OidcConstants
    {
        public const string AdditionalClaims = "claims";
        public const string ScopeOfflineAccess = "offline_access";
        public const string ScopeProfile = "profile";
        public const string ScopeOpenId = "openid";
        public const string PolicyKey = "policy";
    }
}
