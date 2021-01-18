using Microsoft.Identity.Client;
using System;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;

namespace TodoList.Shared
{
    public static class MicrosoftIdentityExceptionHandler
    {
        /// <summary>
        /// Process the exception.
        /// </summary>
        /// <param name="exception">Exception.</param>
        public static void IncrementalConsentExceptionHandler(Exception ex)
        {
            MicrosoftIdentityWebChallengeUserException microsoftIdentityWebChallengeUserException =
                   ex as MicrosoftIdentityWebChallengeUserException;
            if (microsoftIdentityWebChallengeUserException == null)
            {
                microsoftIdentityWebChallengeUserException = ex.InnerException as MicrosoftIdentityWebChallengeUserException;
            }
            if (microsoftIdentityWebChallengeUserException != null && CanBeSolvedByReSignInOfUser(microsoftIdentityWebChallengeUserException.MsalUiRequiredException))
            {
                string redirectUri = HttpContext.Current.Request.Url.ToString();
                AuthenticationProperties authenticationProperties = new AuthenticationProperties();
                authenticationProperties.RedirectUri = redirectUri;
                if (!string.IsNullOrEmpty(microsoftIdentityWebChallengeUserException.MsalUiRequiredException.Claims))
                {
                    authenticationProperties.Dictionary.Add("claims", microsoftIdentityWebChallengeUserException.MsalUiRequiredException.Claims);
                }
                HttpContext.Current.GetOwinContext().Authentication.Challenge(
                   authenticationProperties,
                   OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }

            else
            {
                throw ex;
            }
        }
        
        /// <summary>
        /// Can the exception be solved by re-signing-in the user?.
        /// </summary>
        /// <param name="ex">Exception from which the decision will be made.</param>
        /// <returns>Returns <c>true</c> if the issue can be solved by signing-in
        /// the user, and <c>false</c>, otherwise.</returns>
        private static bool CanBeSolvedByReSignInOfUser(MsalUiRequiredException ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            // ex.ErrorCode != MsalUiRequiredException.UserNullError indicates a cache problem.
            return ex.ErrorCode.ContainsAny(new[] { MsalError.UserNullError, MsalError.InvalidGrantError });
        }
    }
}
