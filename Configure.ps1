# This script creates the Azure AD applications needed for this sample and updates the configuration files
# for the visual Studio projects from the data in the Azure AD applications.
#
# Before running this script you need to install the AzureAD cmdlets as an administrator. 
# For this:
# 1) Run Powershell as an administrator
# 2) in the PowerShell window, type: Install-Module AzureAD
#
# Before you run this script
# 3) With the Azure portal (https://portal.azure.com), choose your active directory tenant, then go to the Properties of the tenant and copy
#    the DirectoryID. This is what we'll use in this script for the tenant ID
# 
# To configurate the applications
# 4) Run the following command:
#      $apps = ConfigureApplications -tenantId [place here the GUID representing the tenant ID]
#    You will be prompted by credentials, be sure to enter credentials for a user who can create applications
#    in the tenant
#
# To execute the samples
# 5) Build and execute the applications. This just works
#
# To cleanup
# 6) Optionnaly if you want to cleanup the applications in the Azure AD, run:
#      CleanUp $apps
#    The applications are un-registered
param([PSCredential]$Credential="", [string]$TenantId="")
Import-Module AzureAD
$ErrorActionPreference = 'Stop'

# Replace the value of an appsettings of a given key in an XML App.Config file.
Function ReplaceSetting([string] $configFilePath, [string] $key, [string] $newValue)
{
    [xml] $content = Get-Content $configFilePath
    $appSettings = $content.configuration.appSettings; 
    $keyValuePair = $appSettings.SelectSingleNode("descendant::add[@key='$key']")
    if ($keyValuePair)
    {
        $keyValuePair.value = $newValue;
    }
    else
    {
        Throw "Key '$key' not found in file '$configFilePath'"
    }
   $content.save($configFilePath)
}

# Updates the config file for a client application
Function UpdateTodoListServiceConfigFile([string] $configFilePath, [string] $tenantId, [string] $clientId, [string] $appKey, [string] $audience, [string] $protectedResource)
{
    ReplaceSetting -configFilePath $configFilePath -key "ida:Tenant" -newValue $tenantId
    ReplaceSetting -configFilePath $configFilePath -key "ida:Audience" -newValue $audience
    ReplaceSetting -configFilePath $configFilePath -key "ida:ClientID" -newValue $clientId
    ReplaceSetting -configFilePath $configFilePath -key "ida:AppKey" -newValue $appKey
    ReplaceSetting -configFilePath $configFilePath -key "ida:CAProtectedResource" -newValue $protectedResource
}

# Updates the config file for a client application
Function UpdateTodoListClientConfigFile([string] $configFilePath, [string] $tenantId, [string] $clientId, [string] $redirectUri, [string] $resourceId, [string] $baseAddress)
{
    ReplaceSetting -configFilePath $configFilePath -key "ida:Tenant" -newValue $tenantId
    ReplaceSetting -configFilePath $configFilePath -key "ida:ClientId" -newValue $clientId
    ReplaceSetting -configFilePath $configFilePath -key "ida:RedirectUri" -newValue $redirectUri
    ReplaceSetting -configFilePath $configFilePath -key "todo:TodoListResourceId" -newValue $resourceId
    ReplaceSetting -configFilePath $configFilePath -key "todo:TodoListBaseAddress" -newValue $baseAddress
}


# Adds the requiredAccesses (expressed as a pipe separated string) to the requiredAccess structure
# The exposed permissions are in the $exposedPermissions collection, and the type of permission (Scope | Role) is 
# described in $permissionType
Function AddResourcePermission($requiredAccess, `
                               $exposedPermissions, [string]$requiredAccesses, [string]$permissionType)
{
        foreach($permission in $requiredAccesses.Trim().Split("|"))
        {
            foreach($exposedPermission in $exposedPermissions)
            {
                if ($exposedPermission.Value -eq $permission)
                 {
                    $resourceAccess = New-Object Microsoft.Open.AzureAD.Model.ResourceAccess
                    $resourceAccess.Type = $permissionType # Scope = Delegated permissions | Role = Application permissions
                    $resourceAccess.Id = $exposedPermission.Id # Read directory data
                    $requiredAccess.ResourceAccess.Add($resourceAccess)
                 }
            }
        }
}

#
# Exemple: GetRequiredPermissions "Microsoft Graph"  "Graph.Read|User.Read"
# See also: http://stackoverflow.com/questions/42164581/how-to-configure-a-new-azure-ad-application-through-powershell
Function GetRequiredPermissions([string] $applicationDisplayName, [string] $requiredDelegatedPermissions, [string]$requiredApplicationPermissions, $servicePrincipal)
{
	# If we are passed the service principal we use it directly, otherwise we find it from the display name (which might not be unique)
	if ($servicePrincipal)
	{
		$sp = $servicePrincipal
	}
	else
    {
		$sp = Get-AzureADServicePrincipal -Filter "DisplayName eq '$applicationDisplayName'"
	}
    $appid = $sp.AppId
    $requiredAccess = New-Object Microsoft.Open.AzureAD.Model.RequiredResourceAccess
    $requiredAccess.ResourceAppId = $appid 
    $requiredAccess.ResourceAccess = New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.ResourceAccess]

    # $sp.Oauth2Permissions | Select Id,AdminConsentDisplayName,Value: To see the list of all the Delegated permissions for the application:
    if ($requiredDelegatedPermissions)
    {
        AddResourcePermission $requiredAccess -exposedPermissions $sp.Oauth2Permissions -requiredAccesses $requiredDelegatedPermissions -permissionType "Scope"
    }
    
    # $sp.AppRoles | Select Id,AdminConsentDisplayName,Value: To see the list of all the Application permissions for the application
    if ($requiredDelegatedPermissions)
    {
        AddResourcePermission $requiredAccess -exposedPermissions $sp.AppRoles -requiredAccesses $requiredApplicationPermissions -permissionType "Role"
    }
    return $requiredAccess
}

# Create a password that can be used as an application key
Function ComputePassword
{
��� $aesManaged = New-Object "System.Security.Cryptography.AesManaged"
��� $aesManaged.Mode = [System.Security.Cryptography.CipherMode]::CBC
��� $aesManaged.Padding = [System.Security.Cryptography.PaddingMode]::Zeros
��� $aesManaged.BlockSize = 128
��� $aesManaged.KeySize = 256
��� $aesManaged.GenerateKey()
��� return [System.Convert]::ToBase64String($aesManaged.Key)
}

# Create an application key
# See https://www.sabin.io/blog/adding-an-azure-active-directory-application-and-key-using-powershell/
Function CreateAppKey([DateTime] $fromDate, [int] $durationInYears, [string]$pw)
{
    $endDate = $fromDate.AddYears($durationInYears) 
    $keyId = (New-Guid).ToString();
    $key = New-Object Microsoft.Open.AzureAD.Model.PasswordCredential($null, $fromDate, $keyId, $endDate, $pw) 
}


Function UnCommentSingleTenantAppConditionalDirective($file)
{
    $lines = [System.IO.File]::ReadAllLines($file)
    if ($lines[0].Contains("#define SingleTenantApp") -And $lines[0].StartsWith("// "))
    {
        $lines[0] = $lines[0].Replace("// ", "")
        [System.IO.File]::WriteAllLines($file, $lines)
    }
}

Function ConfigureApplications
{
<#
.Description
This function creates the Azure AD applications for the sample in the provided Azure AD tenant and updates the
configuration files in the client and service project  of the visual studio solution (App.Config and Web.Config)
so that they are consistent with the Applications parameters
#>
    [CmdletBinding()]
    param(
        [PSCredential] $Credential,
        [Parameter(HelpMessage='Tenant ID (This is a GUID which represents the "Directory ID" of the AzureAD tenant into which you want to create the apps')]
        [string] $tenantId
    )

   process
   {
    # $tenantId is the Active Directory Tenant. This is a GUID which represents the "Directory ID" of the AzureAD tenant 
    # into which you want to create the apps. Look it up in the Azure portal in the "Properties" of the Azure AD. 

    # Login to Azure PowerShell (interactive if credentials are not already provided: 
    # you'll need to sign-in with creds enabling your to create apps in the tenant)
    if (!$Credential)
    {
        $creds = Connect-AzureAD -TenantId $tenantId
    }
    else
    {
        if (!$TenantId)
        {
            $creds = Connect-AzureAD -Credential $Credential
        }
        else
        {
            $creds = Connect-AzureAD -TenantId $tenantId -Credential $Credential
        }
    }

    if (!$tenantId)
    {
        $tenantId = $creds.Tenant.Id
    }
    $tenant = Get-AzureADTenantDetail
    $tenantName =  $tenant.VerifiedDomains[0].Name

    . .\Config.ps1
   
	# Get a 1 year application key for the Downstream Web API Application
    $pw = ComputePassword
    $fromDate = [DateTime]::Now
    $key = CreateAppKey -fromDate $fromDate -durationInYears 1 $pw
    $appKey = $pw

	# Create the Downstreamm Web Api Active Directory Application and it's service principal
    Write-Host "Creating the AAD appplication ($downstreamWebApiName)"
    $downstreamWebApiAadApplication = New-AzureADApplication -DisplayName $downstreamWebApiName `
                                             -HomePage $downstreamWebApiBaseUrl `
                                             -IdentifierUris $downstreamWebApiAppIdURI `
                                             -PasswordCredentials $key `
                                             -PublicClient $downstreamWebApiIsPublicClient
	$downstreamWebApiServicePrincipal = New-AzureADServicePrincipal -AppId $downstreamWebApiAadApplication.AppId -Tags {WindowsAzureActiveDirectoryIntegratedApp}
	Write-Host "Created."

	# Create the TodoListService Active Directory Application and it's service principal
    Write-Host "Creating the AAD appplication ($todoListServiceWebApiName)"
    $todoListServiceWebApiAadApplication = New-AzureADApplication -DisplayName $todoListServiceWebApiName `
                                             -HomePage $todoListServiceWebApiBaseUrl `
                                             -IdentifierUris $todoListServiceWebApiAppIdURI `
                                             -PublicClient $todoListServiceWebApiIsPublicClient
	$todoListServiceWebApiServicePrincipal = New-AzureADServicePrincipal -AppId $todoListServiceWebApiAadApplication.AppId
	Write-Host "Created."

    # Add Required Resources Access (from 'TodoListService' to 'Downstream Web API')
    Write-Host "Getting access from '$todoListServiceWebApiName' to '$downstreamWebApiName'"
    $requiredResourcesAccess = New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.RequiredResourceAccess]
    $downstreamWebApiRequiredPermissions = GetRequiredPermissions -servicePrincipal $downstreamWebApiServicePrincipal `
                                                                -requiredDelegatedPermissions "user_impersonation";
    $requiredResourcesAccess.Add($downstreamWebApiRequiredPermissions)
    Set-AzureADApplication -ObjectId $todoListServiceWebApiAadApplication.ObjectId -RequiredResourceAccess $requiredResourcesAccess
   	Write-Host "Granted."


	# Create the TodoListClient Active Directory Application and it's service principal 
    Write-Host "Creating the AAD appplication ($todoListClientName) and requesting access to '$todoListServiceWebApiName'"
    $todoListClientAadApplication = New-AzureADApplication -DisplayName $todoListClientName `
                                             -ReplyUrls $todoListClientRedirectUri `
                                             -PublicClient $todoListClientIsPublicClient `
											 -RequiredResourceAccess $requiredResourcesAccess
	$todoListClientServicePrincipal = New-AzureADServicePrincipal -AppId $todoListClientAadApplication.AppId
	Write-Host "Created."

    # Add Required Resources Access (from 'TodoListClient' to 'TodoListService')
	Write-Host "Getting access from '$todoListClientName' to '$todoListServiceWebApiName'"
	$requiredResourcesAccess = New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.RequiredResourceAccess]
    $todoListServiceRequiredPermissions = GetRequiredPermissions -servicePrincipal $todoListServiceWebApiServicePrincipal `
                                                                -requiredDelegatedPermissions "user_impersonation";
    $requiredResourcesAccess.Add($todoListServiceRequiredPermissions)
	Set-AzureADApplication -ObjectId $todoListClientAadApplication.ObjectId -RequiredResourceAccess $requiredResourcesAccess
	Write-Host "Granted."

    # Configure TodoListClient as a known client application on the TodoListService
	Write-Host "Configure '$todoListClientName' as a known client application on the '$todoListServiceWebApiName'"
    Set-AzureADApplication -ObjectId $todoListServiceWebApiAadApplication.ObjectId -KnownClientApplications $todoListClientAadApplication.AppId
	Write-Host "Configured."
 
    # Update the config files in the application
    $configFile = $pwd.Path + "\TodoListService\Web.Config"
    Write-Host "Updating the sample code ($configFile)"
    UpdateTodoListServiceConfigFile -configFilePath $configFile `
                            -clientId $todoListServiceWebApiAadApplication.AppId `
                            -appKey $appKey `
                            -tenantId $tenantId `
                            -audience $todoListServiceWebApiAppIdURI `
	                        -protectedResource $downstreamWebApiAadApplication.AppId

    $configFile = $pwd.Path + "\TodoListClient\App.Config"
    Write-Host "Updating the sample code ($configFile)"
    UpdateTodoListClientConfigFile -configFilePath $configFile `
                            -clientId $todoListClientAadApplication.AppId `
                            -tenantId $tenantId `
                            -redirectUri $todoListClientRedirectUri `
                            -baseAddress $todoListServiceWebApiBaseUrl `
	                        -resourceId $todoListServiceWebApiAadApplication.AppId


	Write-Host "REMEMBER: You still need to Create and link a Conditional Access Policy in the Azure portal. See https://github.com/Azure-Samples/active-directory-dotnet-webapi-onbehalfof-ca" -foregroundcolor "Cyan"

    # Completes
    Write-Host "Done."
   }
}

# Run interactively (will ask you for the tenant ID)
ConfigureApplications -Credential $Credential -tenantId $TenantId


# you can also provide the tenant ID and the credentials
# $tenantId = "ID of your AAD directory"
# $apps = ConfigureApplications -tenantId $tenantId 


# When you have built your Visual Studio solution and ran the code, if you want to clean up the Azure AD applications, just 
# run the following command in the same PowerShell window as you ran ConfigureApplications
# . .\CleanUp -Credentials $Credentials