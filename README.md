---
services: active-directory
platforms: dotnet
author: danieldobalian
---

# .NET Native App accessing Web Service that calls a downstream Web API with Conditional Access 

## About this Sample 

The sample uses the Active Directory Authentication Library (ADAL) in a native client to obtain a token for the user to call the first web service, and also in the first web service to get a token to act on behalf of the user to call the second downstream web API.  The downstream web API will have a Conditional Access policy applied that requires MFA.  It is built on [the core On-Behalf-Of sample](https://github.com/Azure-Samples/active-directory-dotnet-webapi-onbehalfof), and thus also accesses the Microsoft Graph On-Behalf-Of the user. 

This sample is intended to explore the implications of using the On-Behalf-Of flow with Conditional Access. We'll walk through code changes necessary to build an app that supports Conditional Access. Quite simply, Azure AD will return state to the web service that needs to hand this back to the client. The client can then use this state to prompt the end-user to satisfy the downstream Conditional Access policy. 

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).  To learn more about Conditional Access, checkout [Conditional Access in Azure AD](https://docs.microsoft.com/en-us/azure/active-directory/active-directory-conditional-access).  

> Looking for previous versions of this code sample? Check out the tags on the [releases](../../releases) GitHub page.

## Topology 

This sample has 3 components:

\[Native App\] - - - \[Web Service\] - - - OnBehalfOf - - - \[Downstream Web API & MS Graph\]

**Native App**: Simple Windows Desktop ToDo List app that allows a user to sign in, satisfy a Conditional Access policy, and add a new task.  Once a user is signed in, the app exposes two options.  When you hit **Satisfy CA**, the client app calls Web Service to do an On-Behalf-Of flow to a CA-Protected downstream Web API 2.  The service returns an error that has to be passed back to the client and handled properly. When you select **add ToDo item**, the Web Service will do On-Behalf-Of to the Microsoft Graph rather than the CA-protected api. 

**Web Service**: Service that Native app sends requests to and requires authorization via OWIN framework.  Then will perform OBO to Web API 2 if forcing MFA or OBO to the Microsoft Graph if adding a new ToDo item.

**Web API 2**: No code for this piece.  We register the app, but never call it. It exists to demonstrate token acquisition when Conditional Access is applied.  

## How to Run this Sample

To run this sample you will need:
- Visual Studio 
- An Internet connection
- An Azure Active Directory (Azure AD) tenant. For more information on how to get an Azure AD tenant, please see [How to get an Azure AD tenant](https://azure.microsoft.com/en-us/documentation/articles/active-directory-howto-tenant/) 
- A user account in your Azure AD tenant with the same domain name as the Azure AD tenant. This sample will not work with a Microsoft account, so if you signed in to the Azure portal with a Microsoft account and have never created a user account in your directory before, you need to do that now.
- A premium subscription to Azure AD.  Azure AD Conditional Access requires a premium subscription.  Azure AD offers a [one month free trial](https://azure.microsoft.com/en-us/trial/get-started-active-directory/) you can use for this sample.  

### Step 1: Clone or download the code

From your shell or terminal: 

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
8. For the App ID URI, enter https://\<your_tenant_name\>/DownstreamAPI, replacing \<your_tenant_name\> with the name of your Azure AD tenant. 
9. Save your changes. 

#### Register the TodoListService Web Service

1. Under **App registrations** and choose **Add**.
2. Enter a friendly name for the application, for example 'TodoListService' and select 'Web Application and/or Web API' as the Application Type. For the sign-on URL, enter the base URL for the sample, which is by default `https://localhost:44321`. Click on **Create** to create the application.
3. While still in the Azure portal, choose your application, click on **Settings** and choose **Properties**.
4. Find the Application ID value and copy it to the clipboard.
5. For the App ID URI, enter https://\<your_tenant_name\>/TodoListService, replacing \<your_tenant_name\> with the name of your Azure AD tenant. 
6. From the Settings menu, choose **Keys** and add a key - select a key duration of either 1 year or 2 years. When you save this page, the key value will be displayed, copy and save the value in a safe location - you will need this key later to configure the project in Visual Studio - this key value will not be displayed again, nor retrievable by any other means, so please record it as soon as it is visible from the Azure Portal.
7. To add the downstream service to your requested resources, select **Required Permissions**, then hit **Add**, and find your downstream api you added earlier. Select **Access DownstreamCAService**.

#### Register the TodoListClient app
1. Under **App registrations** and choose **Add**.
2. Enter a friendly name for the application, for example 'TodoListClient-DotNet' and select 'Native' as the Application Type. For the redirect URI, enter `https://TodoListClient`.  Click on **Create** to create the application.
3. While still in the Azure portal, choose your application, click on **Settings** and choose **Properties**.
4. Find the Application ID value and copy it to the clipboard.
5. Configure Permissions for your application - in the Settings menu, choose the 'Required permissions' section, click on **Add**, then **Select an API**, and type 'TodoListService' in the textbox. Then, click on  **Select** and select 'Access TodoListService'.  Save your changes. 

#### Configure known client applications
For the middle tier web API to be able to call the downstream web API, the user must grant the middle tier permission to do so in the form of consent.  Because the middle tier has no interactive UI of its own, you need to explicitly bind the client app registration in Azure AD with the registration for the web API, which merges the consent required by both the client & middle tier into a single dialog. You can do so by adding the `Client/App ID` of the client app, to the manifest of the web API in the `knownClientApplications` property. Here's how:

1. Navigate to your 'TodoListService' app registration, and open the manifest editor.
2. In the manifest, locate the `knownClientApplications` array property, and add the Client/App ID of your client TodoListClient application as an element.  Your code should look like the following after you're done:
    `"knownClientApplications": ["94da0930-763f-45c7-8d26-04d5938baab2"]`
3. Save the TodoListService manifest by clicking the "Save" button.


### Step 3:  Configure the sample to use your Azure AD tenant

#### Configure the TodoListService project

1. Open the solution in Visual Studio.
2. Open the `web.config` file.
3. Find the app key `ida:Tenant` and replace the value with your AAD tenant name.
4. Find the app key `ida:Audience` and replace the value with the App ID URI you registered earlier, for example `https://<your_tenant_name>/TodoListService`.
5. Find the app key `ida:ClientId` and replace the value with the Client/App ID for the TodoListService from the Azure portal.
6. Find the app key `ida:AppKey` and replace the value with the key/secret for the TodoListService from the Azure portal.
7. Find the app key `ida:CAProtectedResource` and make sure this is the same value as the **App ID URI** of the Downstream Web API you set in the Azure Portal. 

#### Configure the TodoListClient project

1. Open `app.config`.
2. Find the app key `ida:Tenant` and replace the value with your AAD tenant name.
3. Find the app key `ida:ClientId` and replace the value with the Client/App ID for the TodoListClient from the Azure portal.
4. Find the app key `ida:RedirectUri` and replace the value with the Redirect URI for the TodoListClient from the Azure portal, for example `https://TodoListClient`.
5. Find the app key `todo:TodoListResourceId` and replace the value with the  App ID URI of the TodoListService, for example `https://<your_tenant_name>/TodoListService`
6. Find the app key `todo:TodoListBaseAddress` and replace the value with the base address of the TodoListService project.  This is the same value as the **Home page URL** from the Azure portal. 

### Step 4: Create and link a Conditional Access Policy

1. Inside the **Azure Active Directory** blade, select the **Conditional access** button near the bottom of the list. 
2. Go ahead and select **Add** and name your policy.
3. Select the **Users and groups** button, select **All Users** in the **Include** tab.
4. Select the **Cloud apps**, then hit the **Select apps** radio button in the **Include** tab, and select the `DownstreamCAService`.
5. Select the **Conditions** button, then hit **Client apps**, and enable **Configure** as well as select the **Select client apps** radio button and enable **Browser** and **Mobile apps and desktop clients**.
6. Finally, select the **Grant** button and hit **Allow access**. Then check the **Require multi-factor authentication** button for these tests.
7. Enable the policy and save. Access to your Web API now requires multi-factor authentication!


### Step 5:  Run the sample

Clean the solution, rebuild the solution, and run it.  You might want to go into the solution properties and set both projects as startup projects, with the service project starting first.

Explore the sample by signing in, adding items to the To Do list, removing the user account, and starting again.  The To Do list service will take the user's access token, passed from the client, and use it to perform the On-Behalf-Of flow to access the Graph API.  

If you hit the `Satisfy CA` button, the client app will do the same as above except the downstream API is not Graph.  In this case, it will request a token to the downstream web API with Conditional Access applied. The middletier will encounter an error when attempting On-Behalf-Of, and will return back state in the `claims` parameter so the client can step up.  The user will be prompted to do MFA. 

## About the Code

In the Native App:

**MainWindow.xaml.cs**: The method SignInCA method to understand how the client handles the claim param.  Here you can see the necessary client code to handle a Conditional Access claims challenge and construct a new request. 

In the Web Service:

**AccessCaApiController.cs**: Checkout the HTTP GET endpoint in this controller to see how we initiate and handle the `interaction_required` error. Specifically, the code attempts to get a token for the downstream service On-Behalf-Of the user.  When the claims challenge is generated, this method will parse the exception and return back to the client the claims parameter so it can step up.  

## Feedback

Please feel free to open a github issue if you have any problems runnning the sample or suggestions for improvement. For more general developer support, please post on [StackOverflow](http://stackoverflow.com/questions/tagged/azure-active-directory) with the tag `azure-active-directory`. 