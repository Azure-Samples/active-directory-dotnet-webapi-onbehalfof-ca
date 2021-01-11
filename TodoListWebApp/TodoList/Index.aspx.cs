using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using TodoListWebApp.Models;

namespace TodoListWebApp.TodoList
{
    public partial class Index : System.Web.UI.Page
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
            if (response != null && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string responseContent = response.Content.ReadAsStringAsync().Result;
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                List<TodoItem> toDoArray = serializer.Deserialize<List<TodoItem>>(responseContent);
                grdTodoList.DataSource = toDoArray;
                grdTodoList.DataBind();
            }
        }
        private async Task PrepareAuthenticatedClientAsync()
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
             var result = await Common.GetAccessTokenForUserAsync();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        protected void grdTodoList_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {

        }

        protected void grdTodoList_RowEditing(object sender, GridViewEditEventArgs e)
        {

        }
    }
}