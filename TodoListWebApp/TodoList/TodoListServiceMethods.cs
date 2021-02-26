using Microsoft.Identity.Web.Aspnet;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TodoListWebApp.Models;

namespace TodoListWebApp.TodoList
{
    public class TodoListServiceMethods
    {
        private HttpClient _httpClient = new HttpClient();
        private string _TodoListBaseAddress = "https://localhost:44321";

        /// <summary>
        /// Retrieve all todo items
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<TodoItem>> GetAsync()
        {
            await PrepareAuthenticatedClientAsync();
            HttpResponseMessage response = _httpClient.GetAsync(_TodoListBaseAddress + "/api/todolist").Result;
            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                string content = response.Content.ReadAsStringAsync().Result;
                IEnumerable<TodoItem> toDoArray = JsonConvert.DeserializeObject<List<TodoItem>>(content);
                return toDoArray;
            }

            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

        /// <summary>
        /// Add a new ToDo item
        /// </summary>
        /// <param name="todo"></param>
        /// <returns></returns>
        public bool Add(string TodoText)
        {
            PrepareAuthenticatedClientAsync().ConfigureAwait(false);
            var jsoncontent = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("Title", TodoText) });
            var response = _httpClient.PostAsync($"{ _TodoListBaseAddress}/api/todolist", jsoncontent).Result;

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return true;
            }

            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

        /// <summary>
        /// Delete a ToDo item
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public void Delete(int id)
        {
            PrepareAuthenticatedClientAsync().ConfigureAwait(false);
            var response = _httpClient.DeleteAsync($"{ _TodoListBaseAddress}/api/todolist/{id}").Result;

            var responseContent = response.Content.ReadAsStringAsync().Result;

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden && responseContent == Constants.InsufficientClaims)
            {
                 throw new WebApiMsalUiRequiredException(responseContent, response);
            }
            else
            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }
        public async Task<List<string>> GetAllUsersAsync()
        {
            List<string> users = new List<string>();
            await PrepareAuthenticatedClientAsync();
            var response = _httpClient.GetAsync(_TodoListBaseAddress + "/api/AccessCaApi/Get").Result;
            List<string> lstUsers = new List<string>();

            var responseContent = response.Content.ReadAsStringAsync().Result;

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                string content = response.Content.ReadAsStringAsync().Result;
                lstUsers = JsonConvert.DeserializeObject<List<string>>(content);
                return lstUsers;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden && responseContent == Constants.InsufficientClaims)
            {
               
                throw new WebApiMsalUiRequiredException(responseContent, response);
            }
            else
                throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");

        }
        /// <summary>
        /// Retrieves the Access Token for the Web API.
        /// Sets Authorization and Accept headers for the request.
        /// </summary>
        /// <returns></returns>
        private async Task PrepareAuthenticatedClientAsync()
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            TokenAcquisition tokenAcquisition = new TokenAcquisition(new AuthenticationConfig());

            // Retrieves the Access Token for the Web API
            string[] scopes = new[] { ConfigurationManager.AppSettings["ida:TodoListServiceScope"] };
            var result = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}