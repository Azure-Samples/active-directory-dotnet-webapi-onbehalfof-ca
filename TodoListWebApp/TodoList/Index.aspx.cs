using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;

//using TodoList.Shared;
using TodoListWebApp.Models;

namespace TodoListWebApp.TodoList
{
    public partial class Index : System.Web.UI.Page
    {
        private List<TodoItem> toDoArray = new List<TodoItem>();
        private TodoListServiceMethods ToDoListService = new TodoListServiceMethods();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                CallAPI();
            }
        }

        /// <summary>
        /// Call method to bind gridview.
        /// Handles MSALUIRequiredException by calling IncrementalConsentExceptionHandler method.
        /// </summary>
        private void CallAPI()
        {
            try
            {
                GridViewBind();
            }
            catch (Exception ex)
            {
                //MicrosoftIdentityExceptionHandler.IncrementalConsentExceptionHandler(ex);
                throw ex;
            }
        }

        /// <summary>
        /// Binds gridview by retreiving tasks list.
        /// </summary>
        private void GridViewBind()
        {
            toDoArray = (List<TodoItem>)ToDoListService.GetAsync().Result;
            if (toDoArray != null)
            {
                grdTodoList.DataSource = toDoArray;
                grdTodoList.DataBind();
            }
        }

        protected void grdTodoList_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            GridViewRow row = (GridViewRow)grdTodoList.Rows[e.RowIndex];

            var id = Convert.ToInt32(row.Cells[0].Text);
            ToDoListService.Delete(id);
            GridViewBind();
        }
    }
}