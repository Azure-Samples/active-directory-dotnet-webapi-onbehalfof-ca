# Variables for the registration of the AAD applications

# Registration for the Downstream web API
# ----------------------------------------
	# friendly name for the application, for example 'DownstreamCAService' 
	# Application Type is 'Web Application and/or Web API' (that is a private application)
	# sign-on URL, is the base URL for the sample, which is by default https://localhost:44302.
	$downstreamWebApiName = "DownstreamService-OBO-CA"
	$downstreamWebApiIsPublicClient = $false
	$downstreamWebApiBaseUrl= "https://localhost:44302/"
	$downstreamWebApiAppIdURI = "https://$tenantName/$downstreamWebApiName"

# Registration for the TodoListService web API
# ----------------------------------------
	# friendly name for the application, for example 'TodoListService' 
	# Apllication Type is 'Web Application and/or Web API' (private application). 
	# For the sign-on URL, is the base URL for the sample, which is by default https://localhost:44321. 
	# App ID URI, is https://<your_tenant_name>/TodoListService
	# Add key of 1 or 2 years
	#To add the "Downstream web API" is a requested resources for this application
	$todoListServiceWebApiName = "TodoListService-OBO-CA"
	$todoListServiceWebApiIsPublicClient = $false
	$todoListServiceWebApiBaseUrl= "https://localhost:44321/"
	$todoListServiceWebApiAppIdURI = "https://$tenantName/$todoListServiceWebApiName"

# Registration for the TodoListClien app
# ---------------------------------------
	# friendly name for the application, for example 'TodoListClient-DotNet' 
	# Apllication Type is 'Native' (that is public application). 
	# For the redirect URL, this is https://TodoListClient (not be used in this sample, but it needs to be defined nonetheless)
	# App ID URI, is https://<your_tenant_name>/TodoListService
	# "TodoListService" is a requested resource for this application (Required Permissions)
	$todoListClientName = "TodoListClient-DotNet-OBO-CA"
	$todoListClientIsPublicClient = $true
	$todoListClientRedirectUri= "https://TodoListClient"

# Registration for the TodoListClient JavaScript app
# ---------------------------------------------
	# friendly name for the application, for example 'TodoListSPA-OBO' 
	# Apllication Type is 'Web app / API' (that is private application). 
	# For the redirect URL, this is http://localhost:16969/
	# App ID URI, is https://<your_tenant_name>/TodoListSPA-OBO
	# "TodoListService" is a requested resource for this application (Required Permissions)
	$todoListSPAClientName = "TodoListSPA-OBO-CA"
	$todoListSPAClientIsPublicClient = $false
	$todoListSPAClientRedirectUri= "http://localhost:16969/"
	$todoListSPAClientAppIdURI = "https://$tenantName/$todoListSPAClientName"
