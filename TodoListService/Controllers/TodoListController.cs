/*
 The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using Microsoft.Identity.Client;

//using TodoListService.Utils;
using Microsoft.Identity.Web.Aspnet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using TodoListService.DAL;

// The following using statements were added for this sample.
using TodoListService.Models;

//using TodoList.Shared;

namespace TodoListService.Controllers
{
    [Authorize]
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class TodoListController : ApiController
    {
        private static TokenAcquisition _tokenAcquisition = null;

        //
        // To authenticate to the Graph API, the app needs to know the Grah API's App ID URI.
        // To contact the Me endpoint on the Graph API we need the URL as well.
        //
        private static string graphUserUrl = ConfigurationManager.AppSettings["ida:GraphUserUrl"];

        private static IEnumerable<string> requestedScopes = new List<string> { ConfigurationManager.AppSettings["ida:GraphScope"] };

        private static string acrsValue= ConfigurationManager.AppSettings["ida:AcrsValue"];
        //
        // To Do items list for all users.  Since the list is stored in memory, it will go away if the service is cycled.
        //
        private TodoListServiceContext db = new TodoListServiceContext();

        // Error Constants
        private const String SERVICE_UNAVAILABLE = "temporarily_unavailable";

        // GET api/todolist
        public IEnumerable<TodoItem> Get()
        {
            //
            // The Scope claim tells you what permissions the client application has in the service.
            // In this case we look for a scope value of access_as_user, or full access to the service as the user.
            //
            if (!ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/scope").Value.Contains("access_as_user"))
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "The Scope claim does not contain 'access_as_user' or scope claim not found" });
            }

            // A user's To Do list is keyed off of the NameIdentifier claim, which contains an immutable, unique identifier for the user.
            Claim subject = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name);

            return from todo in db.TodoItems
                   where todo.Owner == subject.Value
                   select todo;
        }

        // POST api/todolist
        public async Task Post(TodoItem todo)
        {
            if (!ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/scope").Value.Contains("access_as_user"))
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "The Scope claim does not contain 'access_as_user' or scope claim not found" });
            }

            //
            // Call the Graph API On Behalf Of the user who called the To Do list web API.
            //
            string dsiplayName = string.Empty;
            UserProfile profile = await CallGraphAPIOnBehalfOfUser();

            if (profile != null)
            {
                dsiplayName = profile.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(todo.Title))
            {
                db.TodoItems.Add(new TodoItem { Title = todo.Title, Owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value, DisplayName = dsiplayName });
                db.SaveChanges();
            }
        }

        public static async Task<UserProfile> CallGraphAPIOnBehalfOfUser()
        {
            UserProfile profile = null;
            string accessToken = null;
            AuthenticationResult result = null;

            // In the case of a transient error, retry once after 1 second, then abandon.
            // Retrying is optional.  It may be better, for your application, to return an error immediately to the user and have the user initiate the retry.
            bool retry = false;
            int retryCount = 0;

            do
            {
                retry = false;
                try
                {
                    //_tokenAcquisition = new TokenAcquisition(SetOptions.SetMicrosoftIdOptions(), SetOptions.SetConClientAppOptions());
                    _tokenAcquisition = new TokenAcquisition(new AuthenticationConfig());
                    result = await _tokenAcquisition.GetUserTokenOnBehalfOfAsync(requestedScopes);
                    accessToken = result.AccessToken;
                }
                catch (MsalException ex)
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

            if (accessToken == null)
            {
                // An unexpected error occurred.
                return (null);
            }

            //
            // Call the Graph API and retrieve the user's profile.
            //
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, graphUserUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await client.SendAsync(request);

            //
            // Return the user's profile.
            //
            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                profile = JsonConvert.DeserializeObject<UserProfile>(responseString);
                return (profile);
            }

            // An unexpected error occurred calling the Graph API.  Return a null profile.
            return (null);
        }

        // Delete api/todolist
        public void Delete(int id)
        {
            // Checks if the access token has acrs claim with acrsValue.
            // If does not exists then throws exception.
            AuthorizeForAuthenticationContextClassReference.EnsureUserHasAuthenticationContextClassReference(acrsValue);
            
            TodoItem todo = db.TodoItems.Find(id);
            db.TodoItems.Remove(todo);
            db.SaveChanges();
        }
    }
}