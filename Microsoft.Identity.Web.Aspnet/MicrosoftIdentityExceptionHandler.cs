using Microsoft.Identity.Client;
using System;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Identity.Web.Aspnet
{
    public static class MicrosoftIdentityExceptionHandler
    {
        /// <summary>
        /// Challenges the user if exception is thrown from Web API.
        /// </summary>
        /// <param name="ex"></param>
        public static void HandleExceptionFromWebAPI(Exception ex)
        {
            WebApiMsalUiRequiredException webApiMsalUiRequiredException =
                   ex as WebApiMsalUiRequiredException;
            if (webApiMsalUiRequiredException != null)
            {
                Dictionary<string, string> keyValues = ExtractAuthenticationHeader.ExtractHeaderValues(webApiMsalUiRequiredException.HttpResponseMessage);

                // read the header and checks if it conatins error with insufficient_claims value.
                if (keyValues.ContainsKey(Constants.Error) && keyValues[Constants.Error] == Constants.InsufficientClaims)
                {

                    try
                    {
                        {
                            string redirectUri = HttpContext.Current.Request.Url.ToString();
                            AuthenticationProperties authenticationProperties = new AuthenticationProperties();
                            authenticationProperties.RedirectUri = redirectUri;
                            if (keyValues.ContainsKey(Constants.Claims))
                            {
                                string claims = keyValues[Constants.Claims];
                                authenticationProperties.Dictionary.Add(Constants.Claims, claims);

                            }
                            if (keyValues.ContainsKey(Constants.Scopes))
                            {
                                string scopes = keyValues[Constants.Scopes];
                                authenticationProperties.Dictionary.Add(
                                    Constants.Scope,
                                    scopes);
                            }

                            HttpContext.Current.GetOwinContext().Authentication.Challenge(
                              authenticationProperties,
                              OpenIdConnectAuthenticationDefaults.AuthenticationType);
                        }
                    }
                    catch (Exception exception)
                    {
                        throw exception;
                    }

                }
                else
                {
                    throw ex;
                }
            }
            else
            {
                throw ex;
            }
        }
    }
}
