// Configuration of the Azure AD Application for this TodoList Single Page application
// Note that changing popUp to false will produce a completely different UX based on redirects instead of popups.
var config = {
    tenant: "[Enter tenant name, e.g. contoso.onmicrosoft.com]",
    clientId: "[Enter client ID as obtained from Azure Portal for this SPA, e.g. 7cee0f68-5051-41f6-9e45-80463d21d65d]",
    redirectUri: "http://localhost:16969/",
    popUp: true
}

// Configuration of the Azure AD Application for the WebAPI called by this single page application (TodoListService)
var webApiConfig = {
    resourceId: "[Enter App ID URI of TodoListService, e.g. https://contoso.onmicrosoft.com/TodoListService]",
    resourceBaseAddress: "https://localhost:44321/",
}
