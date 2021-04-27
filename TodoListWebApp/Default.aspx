<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="TodoListWebApp._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <% if (Context.User.Identity.IsAuthenticated)
        {  %>
        <table class="tableClass">
        <thead>
            <tr>
                <th>Claim Type</th>
                <th>Value</th>
            </tr>
        </thead>
        <tbody>
            <%foreach (var claim in _claims)
            {%>
                <tr>
                    <td><%: claim.Type%></td>
                    <td><%: claim.Value%></td>
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
