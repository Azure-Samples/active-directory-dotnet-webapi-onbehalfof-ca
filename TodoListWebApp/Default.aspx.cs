using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web.UI;

namespace TodoListWebApp
{
    public partial class _Default : Page
    {
        protected IEnumerable<Claim> _claims = Enumerable.Empty<Claim>();

        // Defines list of claim types that will be displayed after successfull sign-in.
        private string[] printClaims = { "name", "preferred_username", "http://schemas.microsoft.com/identity/claims/tenantid", "http://schemas.microsoft.com/identity/claims/objectidentifier" };
        protected void Page_Load(object sender, EventArgs e)
        {
            if (ClaimsPrincipal.Current.Identity.IsAuthenticated)
            {
                _claims = ClaimsPrincipal.Current.Claims.Where(x => printClaims.Contains(x.Type));
            }
        }
    }
}