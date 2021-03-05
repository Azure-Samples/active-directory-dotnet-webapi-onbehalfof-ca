using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Aspnet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace TodoListDownstreamService.Controllers
{
    public class CallGraphController : ApiController
    {
        TokenAcquisition _tokenAcquisition = null;
        private static string graphUserUrl = ConfigurationManager.AppSettings["ida:GraphUserUrl"];
        private static IEnumerable<string> requestedScopes = new List<string> { ConfigurationManager.AppSettings["ida:GraphScope"] };

        const String INTERACTION_REQUIRED = "interaction_required";
        // GET api/CallGraph
        public async Task<IEnumerable<string>> GetAsync()
        {
            List<string> userList = new List<string>();
            string accessToken = null;
            AuthenticationResult result = null;
            _tokenAcquisition = new TokenAcquisition(new AuthenticationConfig());
            try
            {
                result = await _tokenAcquisition.GetUserTokenOnBehalfOfAsync(requestedScopes).ConfigureAwait(false);
                accessToken = result.AccessToken;

                if (accessToken == null)
                {
                    // An unexpected error occurred.
                    return (null);
                }

                IEnumerable<User> users = await CallGraphApiOnBehalfOfUser(accessToken);
                userList = users.Select(x => x.UserPrincipalName).ToList();
                return userList;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
        private static async Task<IEnumerable<User>> CallGraphApiOnBehalfOfUser(string accessToken)
        {
            // Call the Graph API and retrieve the user's profile.
            GraphServiceClient graphServiceClient = GetGraphServiceClient(accessToken);
            IGraphServiceUsersCollectionPage users = await graphServiceClient.Users.Request()
                                                      .Filter($"accountEnabled eq true")
                                                      .Select("id, userPrincipalName")
                                                      .GetAsync();
            if (users != null)
            {

                return users;
            }
            throw new Exception();
        }
        /// <summary>
        /// Prepares the authenticated client.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        private static GraphServiceClient GetGraphServiceClient(string accessToken)
        {
            try
            {
                GraphServiceClient graphServiceClient = new GraphServiceClient(graphUserUrl,
                                                                     new DelegateAuthenticationProvider(
                                                                         async (requestMessage) =>
                                                                         {
                                                                             await Task.Run(() =>
                                                                             {
                                                                                 requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
                                                                             });
                                                                         }));
                return graphServiceClient;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}