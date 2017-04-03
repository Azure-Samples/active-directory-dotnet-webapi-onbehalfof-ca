---
services: active-directory
platforms: dotnet
author: danieldobalian
---

# .NET Native App Accessing Web API with downstream CA-Protected Web API

## About this Sample 

In this sample, a native client calls a web API, and then the web API calls another downstream conditional access protected web API after obtaining a token to act on behalf of the original user.  The sample uses the Active Directory Authentication Library (ADAL) in the native client to obtain a token for the user to call the first web API, and also in the first web API to get a token to act on behalf of the user to call the second web API.  Both flows use the oAuth 2.0 protocol obtain the tokens.  

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).

> Looking for previous versions of this code sample? Check out the tags on the [releases](../../releases) GitHub page.

## Topology 

This sample has 3 components:

**Native App**: Simple Windows Desktop ToDo List app that allows sign in, force MFA, and add a new task.  Once a user is signed in, the app exposes two options.  When you hit **force MFA**, the client app calls Web API 1 to do an on-behalf-of flow to a CA-Protected downstream Web API 2.  The service returns an error that has to be passed back to the client and handled properly. When you select **add ToDo item**, the Web API 1 will do On-Behalf-Of to the Microsoft Graph rather than the CA-protected api. 

**Web API 1**: Service that Native app sends requests to and requires authorization via OWIN framework.  Then will perform OBO to Web API 2 if forcing MFA or OBO to the Microsoft Graph if adding a new ToDo item.

**Web API 2**: No code for this piece.  We register the app and requests tokens solely to demonstrate the conditioanl access error.  

## How to Run this Sample

To run this sample you will need:
- Visual Studio 2013/2015
- An Internet connection
- An Azure Active Directory (Azure AD) tenant. For more information on how to get an Azure AD tenant, please see [How to get an Azure AD tenant](https://azure.microsoft.com/en-us/documentation/articles/active-directory-howto-tenant/) 
- A user account in your Azure AD tenant. This sample will not work with a Microsoft account, so if you signed in to the Azure portal with a Microsoft account and have never created a user account in your directory before, you need to do that now.
- A premium subscription to Azure AD.  Azure AD Conditional Access requires a premium subscription.  Azure AD offers a [one month free trial](https://azure.microsoft.com/en-us/trial/get-started-active-directory/) you can use for this sample.  

### Step 1: Clone or download the code

From your shell or command line: 

`git clone https://github.com/Azure-Samples/active-directory-dotnet-webapi-onbehalfof-ca.git`

### Step 2:  Register the sample with your Azure Active Directory tenant

There are two projects in this sample.  Each needs to be separately registered in your Azure AD tenant.

#### Register the Downstream web API 

1. Sign into the [Azure Portal](https://portal.azure.com). 
2. On the top bar, click on your account and under the **Directory** list, choose the Active Directory tenant where you wish to register your application.
3. Click on **More Services** in the left hand nav, and choose **Azure Active Directory**.
4. Click on **App registrations** and choose **Add**.
5. Enter a friendly name for the application, for example 'DownstreamCAService' and select 'Web Application and/or Web API' as the Application Type. For the sign-on URL, enter the base URL for the sample, which is by default `https://localhost:44302`. Click on **Create** to create the application.
6. While still in the Azure portal, choose your application, click on **Settings** and choose **Properties**.
7. Find the Application ID value and copy it to the clipboard.
8. For the App ID URI, enter https://\<your_tenant_name\>/TestAPI, replacing \<your_tenant_name\> with the name of your Azure AD tenant. 

#### Register the TodoListService web API

1. Under **App registrations** and choose **Add**.
2. Enter a friendly name for the application, for example 'TodoListService' and select 'Web Application and/or Web API' as the Application Type. For the sign-on URL, enter the base URL for the sample, which is by default `https://localhost:44321`. Click on **Create** to create the application.
3. While still in the Azure portal, choose your application, click on **Settings** and choose **Properties**.
4. Find the Application ID value and copy it to the clipboard.
5. For the App ID URI, enter https://\<your_tenant_name\>/TodoListService, replacing \<your_tenant_name\> with the name of your Azure AD tenant. 
6. From the Settings menu, choose **Keys** and add a key - select a key duration of either 1 year or 2 years. When you save this page, the key value will be displayed, copy and save the value in a safe location - you will need this key later to configure the project in Visual Studio - this key value will not be displayed again, nor retrievable by any other means, so please record it as soon as it is visible from the Azure Portal.
7. To add the downstream api to your request resources, select **Required Permissions**, then hit **Add**, and find your downstream api you added earlier.

#### Register the TodoListClient app
1. Under **App registrations** and choose **Add**.
2. Enter a friendly name for the application, for example 'TodoListClient-DotNet' and select 'Native' as the Application Type. For the redirect URI, enter `https://TodoListClient`. Please note that the Redirect URI will not be used in this sample, but it needs to be defined nonetheless. Click on **Create** to create the application.
3. While still in the Azure portal, choose your application, click on **Settings** and choose **Properties**.
4. Find the Application ID value and copy it to the clipboard.
5. Configure Permissions for your application - in the Settings menu, choose the 'Required permissions' section, click on **Add**, then **Select an API**, and type 'TodoListService' in the textbox. Then, click on  **Select Permissions** and select 'Access TodoListService'.

#### Configure known client applications
For the middle tier web API to be able to call the downstream web API, the user must grant the middle tier permission to do so in the form of consent.  Because the middle tier has no interactive UI of its own, you need to explicitly bind the client app registration in Azure AD with the registration for the web API, which merges the consent required by both the client & middle tier into a single dialog. You can do so by adding the "Client ID" of the client app, to the manifest of the web API in the `knownClientApplications` property. Here's how:

1. Navigate to your 'TodoListService' app registration, and open the manifest editor.
2. In the manifest, locate the `knownClientApplications` array property, and add the Client ID of your client application as an element.  Your code should look like the following after you're done:
    `"knownClientApplications": ["94da0930-763f-45c7-8d26-04d5938baab2"]`
3. Save the TodoListService manifest by clicking the "Save" button.


### Step 3:  Configure the sample to use your Azure AD tenant

#### Configure the TodoListService project

1. Open the solution in Visual Studio 2013.
2. Open the `web.config` file.
3. Find the app key `ida:Tenant` and replace the value with your AAD tenant name.
4. Find the app key `ida:Audience` and replace the value with the App ID URI you registered earlier, for example `https://<your_tenant_name>/TodoListService`.
5. Find the app key `ida:ClientId` and replace the value with the Client ID for the TodoListService from the Azure portal.
6. Find the app key `ida:AppKey` and replace the value with the key for the TodoListService from the Azure portal.
7. Find the app key `ida:CAProtectedResource` and make sure this is the same value as the **App ID URI** you set in the Azure Portal. 

#### Configure the TodoListClient project

1. Open `app.config'.
2. Find the app key `ida:Tenant` and replace the value with your AAD tenant name.
3. Find the app key `ida:ClientId` and replace the value with the Client ID for the TodoListClient from the Azure portal.
4. Find the app key `ida:RedirectUri` and replace the value with the Redirect URI for the TodoListClient from the Azure portal, for example `http://TodoListClient`.
5. Find the app key `todo:TodoListResourceId` and replace the value with the  App ID URI of the TodoListService, for example `https://<your_tenant_name>/TodoListService`
6. Find the app key `todo:TodoListBaseAddress` and replace the value with the base address of the TodoListService project.

### Step 4: Create and link a Conditional Access Policy

1. Inside the **Azure Active Directory** blade, select the **Conditional access** button near the bottom of the list. 
2. Go ahead and select **Add** and name your policy.
3. Select the **Users and groups** button, select **All Users** in the **Include** tab.
4. Select the **Cloud apps**, then hit the **Select apps** radio button in the **Include** tab, and select the Web API we create in the previous step.
5. Select the **Conditions** button, then hit **Client apps**, and enable **Configure** as well as select the **Select client apps** radio button and enable **Browser** and **Mobile apps and desktop clients**.
6. Finally, select the **Grant** button and hit **Allow access**. Then check the **Require multi-factor authentication** button for these tests.
7. Enable the policy and save. Access to your Web API now requires multi-factor authentication!

### Step 5:  Trust the IIS Express SSL certificate

Since the web API is SSL protected, the client of the API (the web app) will refuse the SSL connection to the web API unless it trusts the API's SSL certificate.  Use the following steps in Windows Powershell to trust the IIS Express SSL certificate.  You only need to do this once.  If you fail to do this step, calls to the TodoListService will always throw an unhandled exception where the inner exception message is:

"The underlying connection was closed: Could not establish trust relationship for the SSL/TLS secure channel."

To configure your computer to trust the IIS Express SSL certificate, begin by opening a Windows Powershell command window as Administrator.

Query your personal certificate store to find the thumbprint of the certificate for `CN=localhost`:

```
PS C:\windows\system32> dir Cert:\LocalMachine\My


    Directory: Microsoft.PowerShell.Security\Certificate::LocalMachine\My


Thumbprint                                Subject
----------                                -------
C24798908DA71693C1053F42A462327543B38042  CN=localhost
```

Next, add the certificate to the Trusted Root store:

```
PS C:\windows\system32> $cert = (get-item cert:\LocalMachine\My\C24798908DA71693C1053F42A462327543B38042)
PS C:\windows\system32> $store = (get-item cert:\Localmachine\Root)
PS C:\windows\system32> $store.Open("ReadWrite")
PS C:\windows\system32> $store.Add($cert)
PS C:\windows\system32> $store.Close()
```

You can verify the certificate is in the Trusted Root store by running this command:

`PS C:\windows\system32> dir Cert:\LocalMachine\Root`

### Step 6:  Run the sample

Clean the solution, rebuild the solution, and run it.  You might want to go into the solution properties and set both projects as startup projects, with the service project starting first.

Explore the sample by signing in, adding items to the To Do list, removing the user account, and starting again.  The To Do list service will take the user's access token, received from the client, and use it to get another access token so it can act On Behalf Of the user in the Graph API.  This sample does not cache the user's access token at the To Do list service, so it requests a new access token on every request.  The service could cache the access token in a database, for example, for better performance, and it could cache the refresh token so that it could obtain access tokens for the user even when the user is not present.

## About the Code

In the Native App:

**MainWindow.xaml.cs**: The method ForceMFA method to understand how the client handles the claim param.  Here you can see the necessary client code to handle a conditional access claims challenge and construct a new request. 

In the Web Service:

**MFAController.cs**: Checkout the HTTP GET endpoint in this controller to see how we initiate and handle the `interaction_required` error. Specifically, the code attempts to get a token for the downstream service on behalf of the user.  When the claims challenge is generated, this method will parse the exception and return back to the client the claims parameter so it can step up.  


## How To Recreate This Sample

First, in Visual Studio 2013/2015 create an empty solution to host the  projects.  Then, follow these steps to create each project.

### Creating the TodoListService Project

1. In the solution, create a new ASP.Net MVC web API project called TodoListService and while creating the project, click the Change Authentication button, select Organizational Accounts, Cloud - Single Organization, enter the name of your Azure AD tenant, and set the Access Level to Single Sign On.  You will be prompted to sign-in to your Azure AD tenant.  NOTE:  You must sign-in with a user that is in the tenant; you cannot, during this step, sign-in with a Microsoft account.
2. Add the pre-release Active Directory Authentication Library (ADAL) NuGet, Microsoft.IdentityModel.Clients.ActiveDirectory, version 2.6.1-alpha (or higher), to the project.
3. In the `Models` folder add a new class called `TodoItem.cs`.  Copy the implementation of TodoItem from this sample into the class.
4. In the `Models` folder add a new class called `UserProfile.cs`.  Copy the implementation of UserProfile from this sample into the class.
5. Add a new, empty, Web API 2 controller called `TodoListController`.
6. Copy the implementation of the TodoListController from this sample into the controller.  Don't forget to add the `[Authorize]` attribute to the class.
7. In `TodoListController` resolving missing references by adding `using` statements for `System.Collections.Concurrent`, `TodoListService.Models`, `System.Security.Claims`.
9. In `web.config` create keys for `ida:AADInstance`, `ida:Tenant`, `ida:ClientId`, and `ida:AppKey`,and set them accordingly.  For the public Azure cloud, the value of `ida:AADInstance` is `https://login.windows.net/{0}`.
8. In `web.config`, in `<appSettings>`, create keys for `ida:GraphResourceId` and `ida:GraphUserUrl` and set the values accordingly.  For the public Azure AD, the value of `ida:GraphResourceId` is `https://graph.windows.net`, and the value of `ida:GraphUserUrl` is `https://graph.windows.net/{0}/me?api-version=2013-11-08`.

### Creating the TodoListClient Project

1. In the solution, create a new Windows --> WPF Application called TodoListClient.
2. Add the (stable) Active Directory Authentication Library (ADAL) NuGet, Microsoft.IdentityModel.Clients.ActiveDirectory, version 1.0.3 (or higher) to the project.
3. Add  assembly references to `System.Net.Http`, `System.Web.Extensions`, and `System.Configuration`.
4. Add a new class to the project called `TodoItem.cs`.  Copy the code from the sample project file of same name into this class, completely replacing the code in the file in the new project.
5. Add a new class to the project called `CacheHelper.cs`.  Copy the code from the sample project file of same name into this class, completely replacing the code in the file in the new project.
6. Add a new class to the project called `CredManCache.cs`.  Copy the code from the sample project file of same name into this class, completely replacing the code in the file in the new project.
7. Copy the markup from `MainWindow.xaml' in the sample project into the file of same name in the new project, completely replacing the markup in the file in the new project.
8. Copy the code from `MainWindow.xaml.cs` in the sample project into the file of same name in the new project, completely replacing the code in the file in the new project.
9. In `app.config` create keys for `ida:AADInstance`, `ida:Tenant`, `ida:ClientId`, `ida:RedirectUri`, `todo:TodoListResourceId`, and `todo:TodoListBaseAddress` and set them accordingly.  For the public Azure cloud, the value of `ida:AADInstance` is `https://login.windows.net/{0}`.

Finally, in the properties of the solution itself, set both projects as startup projects.