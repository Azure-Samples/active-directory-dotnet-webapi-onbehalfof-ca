<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/Site.Master" CodeBehind="Index.aspx.cs" Inherits="TodoListWebApp.TodoList.Index" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h4>Todo List</h4>
        <div>
            <asp:GridView runat="server" ID="grdTodoList" AlternatingRowStyle-BackColor="lightsteelblue" HeaderStyle-BackColor="cornflowerblue" HeaderStyle-ForeColor="White" Width="900px">
            </asp:GridView>
        </div>
</asp:Content>