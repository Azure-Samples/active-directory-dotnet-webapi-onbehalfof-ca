<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/Site.Master" CodeBehind="Create.aspx.cs" Inherits="TodoListWebApp.TodoList.Create" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h3>Create Task</h3>
    <div class="row">
        <div class="col-1"><label title="Title">Title</label></div>
        <div class="col-1"><asp:TextBox ID="txtTitle" runat="server" required="required" ></asp:TextBox></div>
    </div>
    <div class="row">
        <div class="col-1"><label title="Owner">Owner</label></div>
        <div class="col-1"><asp:Label ID="lblOwner" runat="server"></asp:Label></div>
    </div>
    <div class="m-3">
        <asp:Button ID="btnCreate" runat="server" Text="Add Task" OnClick="btnCreate_Click" class="btn btn-primary "/>
    </div>
    <div>
        <a href="Index.aspx">Back to List</a>
    </div>
</asp:Content>