using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using TodoListService.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Globalization;
using System.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Web;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using TodoListService.DAL;
using System.Web.Http.Cors;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Exceptions;

namespace TodoListService.Controllers
{
    [Authorize]
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class AccessCaApiController : ApiController
    {
        //
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        // The App Key is a credential used by the application to authenticate to Azure AD.
        // The Tenant is the name of the Azure AD tenant in which this application is registered.
        // The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        // The Authority is the sign-in URL of the tenant.
        //
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["ida:AppKey"];

        //
        // To authenticate to the Graph API, the app needs to know the Grah API's App ID URI.
        // To contact the Me endpoint on the Graph API we need the URL as well.
        //
        private static string caResourceId = ConfigurationManager.AppSettings["ida:CAProtectedResource"];

        // Error Constants
        const String SERVICE_UNAVAILABLE = "temporarily_unavailable";
        const String INTERACTION_REQUIRED = "interaction_required";


        // GET: api/ConditionalAccess
        public async Task<string> Get()
        {
            var scopeClaim = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/scope");
            if (scopeClaim==null || !scopeClaim.Value.Contains("user_impersonation"))
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "The Scope claim does not contain 'user_impersonation' or scope claim not found" });
            }

            AuthenticationResult result = null;

            //
            //   Use ADAL to get a token On Behalf Of the current user.  To do this we will need:
            //      The Resource ID of the service we want to call.
            //      The current user's access token, from the current request's authorization header.
            //      The credentials of this application.
            //      The username (UPN or email) of the user calling the API
            //
            ClientCredential clientCred = new ClientCredential(clientId, appKey);
            var bootstrapContext = ClaimsPrincipal.Current.Identities.First().BootstrapContext as System.IdentityModel.Tokens.BootstrapContext;
            string userName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Upn) != null ? ClaimsPrincipal.Current.FindFirst(ClaimTypes.Upn).Value : ClaimsPrincipal.Current.FindFirst(ClaimTypes.Email).Value;
            string userAccessToken = bootstrapContext.Token;
            UserAssertion userAssertion = new UserAssertion(bootstrapContext.Token, "urn:ietf:params:oauth:grant-type:jwt-bearer", userName);

            string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);
            string userId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            AuthenticationContext authContext = new AuthenticationContext(authority, new DbTokenCache(userId));

            // In the case of a transient error, retry once after 1 second, then abandon.
            // Retrying is optional.  It may be better, for your application, to return an error immediately to the user and have the user initiate the retry.
            bool retry = false;
            int retryCount = 0;

            do
            {
                retry = false;
                try
                {
                    result = await authContext.AcquireTokenAsync(caResourceId, clientCred, userAssertion);
                }
                catch (AdalClaimChallengeException ex)
                {
                    HttpResponseMessage myMessage = new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, ReasonPhrase = INTERACTION_REQUIRED, Content = new StringContent(ex.Claims) };
                    throw new HttpResponseException(myMessage);
                }
                catch (AdalServiceException ex)
                {
                    if (ex.ErrorCode == "invalid_grant")
                    {
                        return ex.Message;
                        //HttpResponseMessage myMessage = new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, ReasonPhrase = INTERACTION_REQUIRED, Content = new StringContent(ex.Message) };
                        //throw new HttpResponseException(myMessage);
                    }
                    if (ex.ErrorCode == INTERACTION_REQUIRED )
                    {
                        HttpResponseMessage myMessage = new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, ReasonPhrase = INTERACTION_REQUIRED, Content = new StringContent(ex.Message) };

                        throw new HttpResponseException(myMessage);
                    }
                }
                catch (AdalException ex)
                {
                    if (ex.ErrorCode == SERVICE_UNAVAILABLE)
                    {
                        // Transient error, OK to retry.
                        retry = true;
                        retryCount++;
                        Thread.Sleep(1000);
                    }
                }
            } while ((retry == true) && (retryCount < 1));

            // Access token is available in result; 
            String oboAccessToken = result.AccessToken;

            //
            // We can now use this  access token to accesss our Conditional-Access protected Web API using On-behalf-of
            // Use this code below to call the downstream Web API OBO
            //

            // e.g.
            // private HttpClient httpClient = new HttpClient();
            // httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            // HttpResponseMessage response = await httpClient.GetAsync(WebAPI2HttpEndpoint (App ID URI + "/endpoint");

            return "protected API successfully called";
        }
    }
}
