using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.Aspnet
{
    /// <summary>
    /// Identity specific constants used throughout the assembly
    /// </summary>
    public static class IdentityConstants
    {
        public const string ScopeOfflineAccess = "offline_access";
        public const string ScopeProfile = "profile";
        public const string ScopeOpenId = "openid";

        public const string DefaultScopes = "openid profile offline_access";

        public const string PreferredUserName = "preferred_username";
    }
}
