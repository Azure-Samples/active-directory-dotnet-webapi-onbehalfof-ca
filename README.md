---
services: active-directory, conditional access
platforms: .NET
author: dadobali
---

# .NET Native App Accessing Web API with downstream CA-Protected Web API

This sample has 3 components:

**Native App**: Simple Windows Desktop ToDo List app that allows sign in, force MFA, and add a new task.  Sign in, then you can use the other two operations.  When you force MFA, Web API 1 will do an on-behalf-of flow to a CA-Protected downstream Web API 2 that requires MFA.  The service returns an error that has to be passed back to the client and handled properly. When you add a ToDo item, the Web API 1 will do On-Behalf-Of to the Microsoft Graph. 

**Web API 1**: Service that Native app can talk to and requires authorization via OWIN framework.  Then will preform OBO to Web API 2 if forcing MFA or to the Microsoft Graph.

**Web API 2**: No code for this app, but it has been registered solely to get tokens for. 

## Steps to Run

All the configs in the app make it ready to run on my tenant. Only config reqd is the client secret in Web.Config, which can be provided upon emailing me. 

## About the Code

In the Native App:

	**MainWindow.xaml.cs**: Checkout the ForceMFA method to understand how the client handles the claim param.

In the Web Service:

	**MFAController.cs**: Checkout the HTTP GET api endpoint to see how we initiate and handle the `interaction_required` error. 

