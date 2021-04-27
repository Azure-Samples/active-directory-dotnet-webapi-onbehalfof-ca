<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/Site.Master" CodeBehind="Index.aspx.cs" Inherits="TodoListWebApp.TodoList.Index" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h4>Todo List</h4>
    <a href="Create.aspx">Create Task</a>
    <br /> <br />
        <div>
           <asp:GridView runat="server" ID="grdTodoList" AutoGenerateColumns="false" HeaderStyle-BackColor="cornflowerblue" HeaderStyle-ForeColor="White" Width="900px" OnRowDeleting="grdTodoList_RowDeleting">
                <Columns>  
                        <asp:BoundField DataField="ID" HeaderText="ID" ReadOnly="true" />  
                        <asp:BoundField DataField="Title" HeaderText="Title" />  
                        <asp:BoundField DataField="Owner" HeaderText="Owner" ReadOnly="true" />  
                        <asp:BoundField DataField="DisplayName" HeaderText="DisplayName" ReadOnly="true" />  
                        <asp:CommandField ShowDeleteButton="true" HeaderText="Action" ControlStyle-CssClass="btn btn-danger"/> 
                </Columns>  
           </asp:GridView>
        </div>
</asp:Content>