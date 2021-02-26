using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.Identity.Web.Aspnet
{
    public class AuthorizeForAuthenticationContextClassReference
    {
        /// <summary>
        /// Checks if the access token has acrs claim with acrsValue.
        /// If does not exists then adds WWW-Authenticate and throws UnauthorizedAccessException exception.
        /// </summary>
        /// <param name="acrsValue"></param>
        /// <param name="httpResponse"></param>
        public static void EnsureUserHasAuthenticationContextClassReference(string acrsValue)
        {
            AuthenticationConfig authenticationConfig= new AuthenticationConfig();
            HttpContext context = HttpContext.Current;
            ClaimsPrincipal claimsPrincipal = ClaimsPrincipal.Current;
            
            string authenticationContextClassReferencesClaim = "acrs";

            if (context == null || context.User == null || claimsPrincipal.Claims == null || !claimsPrincipal.Claims.Any())
            {
                throw new ArgumentNullException("No Usercontext is available to pick claims from");
            }

            Claim acrsClaim = claimsPrincipal.FindFirst(authenticationContextClassReferencesClaim);

            if (acrsClaim == null || acrsClaim.Value != acrsValue)
            {
                var base64str = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"access_token\":{\"acrs\":{\"essential\":true,\"value\":\""+ acrsValue + "\"}}}"));

                context.Response.Headers.Add("WWW-Authenticate", $"Bearer realm=\"\"; authorization_uri=\"https://login.microsoftonline.com/common/oauth2/authorize\"; client_id=\"" + authenticationConfig.ClientId + "\"; error=\"insufficient_claims\"; claims=\"" + base64str + "\"; cc_type=\"authcontext\"");
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;

                string message = string.Format(CultureInfo.InvariantCulture, "The presented access tokens had insufficient claims. Please request for claims requested in the WWW-Authentication header and try again.");
                
                context.Response.Write("insufficient_claims");

                context.Response.End();

                throw new UnauthorizedAccessException(message);
            }
        }
    }
}
