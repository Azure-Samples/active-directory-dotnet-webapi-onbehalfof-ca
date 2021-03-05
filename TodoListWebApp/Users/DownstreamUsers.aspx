<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DownstreamUsers.aspx.cs" Inherits="TodoListWebApp.DownstreamUsers" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
        <% if (Context.User.Identity.IsAuthenticated)
        {  %>
        <h4>List of Users</h4>
        <table class="tableClass">
        <thead>
            <tr>
                <th>Userprincipal Name</th>
            </tr>
        </thead>
        <tbody>
            <%foreach (var user in lstUsers)
            {%>
                <tr>
                    <td><%: user%></td>
                </tr>
           <% }%>
        </tbody>
    </table>
    <% }
    else
    {%>
    Sign-in to see user details.
    <%} %>
</asp:Content>
