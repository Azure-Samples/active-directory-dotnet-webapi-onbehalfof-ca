using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TodoListWebApp.TodoList
{
    public partial class Create : System.Web.UI.Page
    {
        private HttpClient _httpClient = new HttpClient();
        private string todoListBaseAddress = "https://localhost:44321";

        protected void Page_Load(object sender, EventArgs e)
        {
            CallAPI();
        }
        private void CallAPI()
        {
            PrepareAuthenticatedClientAsync().ConfigureAwait(false);
            HttpResponseMessage response = _httpClient.GetAsync(todoListBaseAddress + "/api/todolist").Result;
        }
        private async Task PrepareAuthenticatedClientAsync()
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            var result = await Common.GetAccessTokenForUserAsync();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        }
    }
}