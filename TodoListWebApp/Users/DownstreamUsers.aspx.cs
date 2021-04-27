using Microsoft.Identity.Web.Aspnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TodoListWebApp.TodoList;

namespace TodoListWebApp
{
    public partial class DownstreamUsers : Page
    {
        protected IEnumerable<string> lstUsers = Enumerable.Empty<string>();
        TodoListServiceMethods ToDoListService = new TodoListServiceMethods();

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                GetAllUsersAsync().ConfigureAwait(false);
            }
            catch(Exception ex)
            {

            }
        }
        private async Task GetAllUsersAsync()
        {
            try
            {
                lstUsers = await ToDoListService.GetAllUsersAsync();
            }
            catch (WebApiMsalUiRequiredException msalException)
            {
                MicrosoftIdentityExceptionHandler.HandleExceptionFromWebAPI(msalException);
            }
        }
    }
}