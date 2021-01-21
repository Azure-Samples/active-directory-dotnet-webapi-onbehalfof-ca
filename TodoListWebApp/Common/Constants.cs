using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TodoListWebApp
{
    public static class Constants
    {
        public const string Logging_DateTimeformat = "dddd, dd MMMM yyyy";

        public const string ScopeOfflineAccess = "offline_access";
        public const string ScopeProfile = "profile";
        public const string ScopeOpenId = "openid";

        public const string DefaultScopes = "openid profile offline_access";
    }
}