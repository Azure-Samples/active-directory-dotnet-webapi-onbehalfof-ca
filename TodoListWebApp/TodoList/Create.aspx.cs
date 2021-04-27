using System;
using System.Security.Claims;

namespace TodoListWebApp.TodoList
{
    public partial class Create : System.Web.UI.Page
    {
        TodoListServiceMethods ToDoListService = new TodoListServiceMethods();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                lblOwner.Text = ClaimsPrincipal.Current.FindFirst("preferred_username").Value;
            }
        }

        protected void btnCreate_Click(object sender, EventArgs e)
        {
            string title = txtTitle.Text;
            bool isSuccess = ToDoListService.Add(title);
            if (isSuccess)
            {
                Response.Redirect("Index.aspx", false);
            }
        }
    }
}