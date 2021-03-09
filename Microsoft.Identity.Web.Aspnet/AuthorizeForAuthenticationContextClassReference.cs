using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
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
        public static void EnsureUserHasAuthenticationContextClassReference(string acrsValue, string additionalInfo = null)
        {
            AuthenticationConfig authenticationConfig = new AuthenticationConfig();
           
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
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;

                //string message = string.Format(CultureInfo.InvariantCulture, "The presented access tokens had insufficient claims. Please request for claims requested in the WWW-Authentication header and try again.");
                var base64str = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"access_token\":{\"acrs\":{\"essential\":true,\"value\":\"" + acrsValue + "\"}}}"));

                // Create response header as per https://tools.ietf.org/html/rfc6750#section-3.1
                var authenticateHeader = CommonUtil.CreateResponseHeader(authenticationConfig, base64str);

                context.Response.Headers.Add("WWW-Authenticate", authenticateHeader);

               
                string message = $"The claim 'acrs' is either missing or does not have the value(s) '{acrsValue}'.Please redirect the user to the authorization server for additional processing.";
                
                // Create response content with error details.
                InsufficientClaimsResponse insufficientClaimsResponse = CommonUtil.CreateErrorResponseMessage(message, additionalInfo);
                if (insufficientClaimsResponse != null)
                {
                    context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(insufficientClaimsResponse));
                }

                context.Response.End();

                throw new UnauthorizedAccessException(message);
            }
        }
    }
}
