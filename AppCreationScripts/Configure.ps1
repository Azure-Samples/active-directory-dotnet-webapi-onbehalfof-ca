[CmdletBinding()]
param(
    [PSCredential] $Credential,
    [Parameter(Mandatory=$False, HelpMessage='Tenant ID (This is a GUID which represents the "Directory ID" of the AzureAD tenant into which you want to create the apps')]
    [string] $tenantId,
    [Parameter(Mandatory=$False, HelpMessage='Azure environment to use while running the script (it defaults to AzureCloud)')]
    [string] $azureEnvironmentName
)

#Requires -Modules AzureAD -RunAsAdministrator

<#
 This script creates the Azure AD applications needed for this sample and updates the configuration files
 for the visual Studio projects from the data in the Azure AD applications.

 Before running this script you need to install the AzureAD cmdlets as an administrator. 
 For this:
 1) Run Powershell as an administrator
 2) in the PowerShell window, type: Install-Module AzureAD

 There are four ways to run this script. For more information, read the AppCreationScripts.md file in the same folder as this script.
#>

# Create a password that can be used as an application key
Function ComputePassword
{
    $aesManaged = New-Object "System.Security.Cryptography.AesManaged"
    $aesManaged.Mode = [System.Security.Cryptography.CipherMode]::CBC
    $aesManaged.Padding = [System.Security.Cryptography.PaddingMode]::Zeros
    $aesManaged.BlockSize = 128
    $aesManaged.KeySize = 256
    $aesManaged.GenerateKey()
    return [System.Convert]::ToBase64String($aesManaged.Key)
}

# Create an application key
# See https://www.sabin.io/blog/adding-an-azure-active-directory-application-and-key-using-powershell/
Function CreateAppKey([DateTime] $fromDate, [double] $durationInYears, [string]$pw)
{
    $endDate = $fromDate.AddYears($durationInYears) 
    $keyId = (New-Guid).ToString();
    $key = New-Object Microsoft.Open.AzureAD.Model.PasswordCredential
    $key.StartDate = $fromDate
    $key.EndDate = $endDate
    $key.Value = $pw
    $key.KeyId = $keyId
    return $key
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
# Example: GetRequiredPermissions "Microsoft Graph"  "Graph.Read|User.Read"
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
    if ($requiredApplicationPermissions)
    {
        AddResourcePermission $requiredAccess -exposedPermissions $sp.AppRoles -requiredAccesses $requiredApplicationPermissions -permissionType "Role"
    }
    return $requiredAccess
}


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

Function ReplaceInLine([string] $line, [string] $key, [string] $value)
{
    $index = $line.IndexOf($key)
    if ($index -ige 0)
    {
        $index2 = $index+$key.Length
        $line = $line.Substring(0, $index) + $value + $line.Substring($index2)
    }
    return $line
}

Function ReplaceInTextFile([string] $configFilePath, [System.Collections.HashTable] $dictionary)
{
    $lines = Get-Content $configFilePath
    $index = 0
    while($index -lt $lines.Length)
    {
        $line = $lines[$index]
        foreach($key in $dictionary.Keys)
        {
            if ($line.Contains($key))
            {
                $lines[$index] = ReplaceInLine $line $key $dictionary[$key]
            }
        }
        $index++
    }

    Set-Content -Path $configFilePath -Value $lines -Force
}
<#.Description
   This function creates a new Azure AD scope (OAuth2Permission) with default and provided values
#>  
Function CreateScope( [string] $value, [string] $userConsentDisplayName, [string] $userConsentDescription, [string] $adminConsentDisplayName, [string] $adminConsentDescription)
{
    $scope = New-Object Microsoft.Open.AzureAD.Model.OAuth2Permission
    $scope.Id = New-Guid
    $scope.Value = $value
    $scope.UserConsentDisplayName = $userConsentDisplayName
    $scope.UserConsentDescription = $userConsentDescription
    $scope.AdminConsentDisplayName = $adminConsentDisplayName
    $scope.AdminConsentDescription = $adminConsentDescription
    $scope.IsEnabled = $true
    $scope.Type = "User"
    return $scope
}

<#.Description
   This function creates a new Azure AD AppRole with default and provided values
#>  
Function CreateAppRole([string] $types, [string] $name, [string] $description)
{
    $appRole = New-Object Microsoft.Open.AzureAD.Model.AppRole
    $appRole.AllowedMemberTypes = New-Object System.Collections.Generic.List[string]
    $typesArr = $types.Split(',')
    foreach($type in $typesArr)
    {
        $appRole.AllowedMemberTypes.Add($type);
    }
    $appRole.DisplayName = $name
    $appRole.Id = New-Guid
    $appRole.IsEnabled = $true
    $appRole.Description = $description
    $appRole.Value = $name;
    return $appRole
}

Set-Content -Value "<html><body><table>" -Path createdApps.html
Add-Content -Value "<thead><tr><th>Application</th><th>AppId</th><th>Url in the Azure portal</th></tr></thead><tbody>" -Path createdApps.html

$ErrorActionPreference = "Stop"

Function ConfigureApplications
{
<#.Description
   This function creates the Azure AD applications for the sample in the provided Azure AD tenant and updates the
   configuration files in the client and service project  of the visual studio solution (App.Config and Web.Config)
   so that they are consistent with the Applications parameters
#> 
    $commonendpoint = "common"
    
    if (!$azureEnvironmentName)
    {
        $azureEnvironmentName = "AzureCloud"
    }

    # $tenantId is the Active Directory Tenant. This is a GUID which represents the "Directory ID" of the AzureAD tenant
    # into which you want to create the apps. Look it up in the Azure portal in the "Properties" of the Azure AD.

    # Login to Azure PowerShell (interactive if credentials are not already provided:
    # you'll need to sign-in with creds enabling your to create apps in the tenant)
    if (!$Credential -and $TenantId)
    {
        $creds = Connect-AzureAD -TenantId $tenantId -AzureEnvironmentName $azureEnvironmentName
    }
    else
    {
        if (!$TenantId)
        {
            $creds = Connect-AzureAD -Credential $Credential -AzureEnvironmentName $azureEnvironmentName
        }
        else
        {
            $creds = Connect-AzureAD -TenantId $tenantId -Credential $Credential -AzureEnvironmentName $azureEnvironmentName
        }
    }

    if (!$tenantId)
    {
        $tenantId = $creds.Tenant.Id
    }

    

    $tenant = Get-AzureADTenantDetail
    $tenantName =  ($tenant.VerifiedDomains | Where { $_._Default -eq $True }).Name

    # Get the user running the script to add the user as the app owner
    $user = Get-AzureADUser -ObjectId $creds.Account.Id

   # Create the service AAD application
   Write-Host "Creating the AAD application (ToDoListService-OBO-CA-ASP.NET)"
   # create the application 
   $service1AadApplication = New-AzureADApplication -DisplayName "ToDoListService-OBO-CA-ASP.NET" `
                                                   -HomePage "http://localhost:44322" `
                                                   -PublicClient $False

   $serviceIdentifierUri = 'api://'+$service1AadApplication.AppId
   Set-AzureADApplication -ObjectId $service1AadApplication.ObjectId -IdentifierUris $serviceIdentifierUri

   # create the service principal of the newly created application 
   $currentAppId = $service1AadApplication.AppId
   $serviceServicePrincipal = New-AzureADServicePrincipal -AppId $currentAppId -Tags {WindowsAzureActiveDirectoryIntegratedApp}

   # add the user running the script as an app owner if needed
   $owner = Get-AzureADApplicationOwner -ObjectId $service1AadApplication.ObjectId
   if ($owner -eq $null)
   { 
        Add-AzureADApplicationOwner -ObjectId $service1AadApplication.ObjectId -RefObjectId $user.ObjectId
        Write-Host "'$($user.UserPrincipalName)' added as an application owner to app '$($serviceServicePrincipal.DisplayName)'"
   }

    # rename the user_impersonation scope if it exists to match the readme steps or add a new scope
    $scopes = New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.OAuth2Permission]
   
    if ($scopes.Count -ge 0) 
    {
        # add all existing scopes first
        $service1AadApplication.Oauth2Permissions | foreach-object { $scopes.Add($_) }

        $scope = $service1AadApplication.Oauth2Permissions | Where-Object { $_.Value -eq "User_impersonation" }

        if ($scope -ne $null) 
        {
            $scope.Value = "access_as_user"
        }
        else 
        {
            # Add scope
            $scope = CreateScope -value "access_as_user"  `
                -userConsentDisplayName "Access ToDoListService-OBO-CA-ASP.NET"  `
                -userConsentDescription "Allow the application to access ToDoListService-OBO-CA-ASP.NET on your behalf."  `
                -adminConsentDisplayName "Access ToDoListService-OBO-CA-ASP.NET"  `
                -adminConsentDescription "Allows the app to have the same access to information in the directory on behalf of the signed-in user."
            
            $scopes.Add($scope)
        }        
    }
     
    # add/update scopes
    Set-AzureADApplication -ObjectId $service1AadApplication.ObjectId -OAuth2Permission $scopes

   Write-Host "Done creating the service application (ToDoListService-OBO-CA-ASP.NET)"

   # URL of the AAD application in the Azure portal
   # Future? $servicePortalUrl = "https://portal.azure.com/#@"+$tenantName+"/blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/"+$serviceAadApplication.AppId+"/objectId/"+$serviceAadApplication.ObjectId+"/isMSAApp/"
   $servicePortalUrl = "https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/CallAnAPI/appId/"+$service1AadApplication.AppId+"/objectId/"+$service1AadApplication.ObjectId+"/isMSAApp/"
   Add-Content -Value "<tr><td>service</td><td>$currentAppId</td><td><a href='$servicePortalUrl'>ToDoListService-OBO-CA-ASP.NET</a></td></tr>" -Path createdApps.html


   # Create the service AAD application
   Write-Host "Creating the AAD application (ToDoListService-ASP.NET)"
   # Get a 2 years application key for the service Application
   $pw = ComputePassword
   $fromDate = [DateTime]::Now;
   $key = CreateAppKey -fromDate $fromDate -durationInYears 2 -pw $pw
   $serviceAppKey = $pw
   # create the application 
   $serviceAadApplication = New-AzureADApplication -DisplayName "ToDoListService-ASP.NET" `
                                                   -HomePage "http://localhost:44321" `
                                                   -PasswordCredentials $key `
                                                   -PublicClient $False

   $serviceIdentifierUri = 'api://'+$serviceAadApplication.AppId
   Set-AzureADApplication -ObjectId $serviceAadApplication.ObjectId -IdentifierUris $serviceIdentifierUri

   # create the service principal of the newly created application 
   $currentAppId = $serviceAadApplication.AppId
   $serviceServicePrincipal = New-AzureADServicePrincipal -AppId $currentAppId -Tags {WindowsAzureActiveDirectoryIntegratedApp}

   # add the user running the script as an app owner if needed
   $owner = Get-AzureADApplicationOwner -ObjectId $serviceAadApplication.ObjectId
   if ($owner -eq $null)
   { 
        Add-AzureADApplicationOwner -ObjectId $serviceAadApplication.ObjectId -RefObjectId $user.ObjectId
        Write-Host "'$($user.UserPrincipalName)' added as an application owner to app '$($serviceServicePrincipal.DisplayName)'"
   }

    # rename the user_impersonation scope if it exists to match the readme steps or add a new scope
    $scopes = New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.OAuth2Permission]
   
    if ($scopes.Count -ge 0) 
    {
        # add all existing scopes first
        $serviceAadApplication.Oauth2Permissions | foreach-object { $scopes.Add($_) }

        $scope = $serviceAadApplication.Oauth2Permissions | Where-Object { $_.Value -eq "User_impersonation" }

        if ($scope -ne $null) 
        {
            $scope.Value = "access_as_user"
        }
        else 
        {
            # Add scope
            $scope = CreateScope -value "access_as_user"  `
                -userConsentDisplayName "Access ToDoListService-ASP.NET"  `
                -userConsentDescription "Allow the application to access ToDoListService-ASP.NET on your behalf."  `
                -adminConsentDisplayName "Access ToDoListService-ASP.NET"  `
                -adminConsentDescription "Allows the app to have the same access to information in the directory on behalf of the signed-in user."
            
            $scopes.Add($scope)
        }        
    }
     
    # add/update scopes
    Set-AzureADApplication -ObjectId $serviceAadApplication.ObjectId -OAuth2Permission $scopes

   Write-Host "Done creating the service application (ToDoListService-ASP.NET)"

   # URL of the AAD application in the Azure portal
   # Future? $servicePortalUrl = "https://portal.azure.com/#@"+$tenantName+"/blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/"+$serviceAadApplication.AppId+"/objectId/"+$serviceAadApplication.ObjectId+"/isMSAApp/"
   $servicePortalUrl = "https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/CallAnAPI/appId/"+$serviceAadApplication.AppId+"/objectId/"+$serviceAadApplication.ObjectId+"/isMSAApp/"
   Add-Content -Value "<tr><td>service</td><td>$currentAppId</td><td><a href='$servicePortalUrl'>ToDoListService-ASP.NET</a></td></tr>" -Path createdApps.html

   $requiredResourcesAccess = New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.RequiredResourceAccess]

   # Add Required Resources Access (from 'service' to 'service')
   Write-Host "Getting access from 'service' to 'service'"
   $requiredPermissions = GetRequiredPermissions -applicationDisplayName "ToDoListService-OBO-CA-ASP.NET" `
                                                -requiredDelegatedPermissions "access_as_user" `

   $requiredResourcesAccess.Add($requiredPermissions)


   Set-AzureADApplication -ObjectId $serviceAadApplication.ObjectId -RequiredResourceAccess $requiredResourcesAccess
   Write-Host "Granted permissions."

   # Create the client AAD application
   Write-Host "Creating the AAD application (TodoListClient)"
   # create the application 
   $clientAadApplication = New-AzureADApplication -DisplayName "TodoListClient" `
                                                  -ReplyUrls "https://login.microsoftonline.com/common/oauth2/nativeclient" `
                                                  -PublicClient $True

   # create the service principal of the newly created application 
   $currentAppId = $clientAadApplication.AppId
   $clientServicePrincipal = New-AzureADServicePrincipal -AppId $currentAppId -Tags {WindowsAzureActiveDirectoryIntegratedApp}

   # add the user running the script as an app owner if needed
   $owner = Get-AzureADApplicationOwner -ObjectId $clientAadApplication.ObjectId
   if ($owner -eq $null)
   { 
        Add-AzureADApplicationOwner -ObjectId $clientAadApplication.ObjectId -RefObjectId $user.ObjectId
        Write-Host "'$($user.UserPrincipalName)' added as an application owner to app '$($clientServicePrincipal.DisplayName)'"
   }


   Write-Host "Done creating the client application (TodoListClient)"

   # URL of the AAD application in the Azure portal
   # Future? $clientPortalUrl = "https://portal.azure.com/#@"+$tenantName+"/blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/"+$clientAadApplication.AppId+"/objectId/"+$clientAadApplication.ObjectId+"/isMSAApp/"
   $clientPortalUrl = "https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/CallAnAPI/appId/"+$clientAadApplication.AppId+"/objectId/"+$clientAadApplication.ObjectId+"/isMSAApp/"
   Add-Content -Value "<tr><td>client</td><td>$currentAppId</td><td><a href='$clientPortalUrl'>TodoListClient</a></td></tr>" -Path createdApps.html

   $requiredResourcesAccess = New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.RequiredResourceAccess]

   # Add Required Resources Access (from 'client' to 'service')
   Write-Host "Getting access from 'client' to 'service'"
   $requiredPermissions = GetRequiredPermissions -applicationDisplayName "ToDoListService-ASP.NET" `
                                                -requiredDelegatedPermissions "access_as_user" `

   $requiredResourcesAccess.Add($requiredPermissions)


   Set-AzureADApplication -ObjectId $clientAadApplication.ObjectId -RequiredResourceAccess $requiredResourcesAccess
   Write-Host "Granted permissions."

   # Create the webApp AAD application
   Write-Host "Creating the AAD application (TodoListWebApp-aspnet)"
   # Get a 2 years application key for the webApp Application
   $pw = ComputePassword
   $fromDate = [DateTime]::Now;
   $key = CreateAppKey -fromDate $fromDate -durationInYears 2 -pw $pw
   $webAppAppKey = $pw
   # create the application 
   $webAppAadApplication = New-AzureADApplication -DisplayName "TodoListWebApp-aspnet" `
                                                  -HomePage "https://localhost:44379/" `
                                                  -LogoutUrl "https://localhost:44379/signout-oidc" `
                                                  -ReplyUrls "https://localhost:44379/", "https://localhost:44379/signin-oidc" `
                                                  -IdentifierUris "https://$tenantName/TodoListWebApp-aspnet" `
                                                  -PasswordCredentials $key `
                                                  -PublicClient $False

   # create the service principal of the newly created application 
   $currentAppId = $webAppAadApplication.AppId
   $webAppServicePrincipal = New-AzureADServicePrincipal -AppId $currentAppId -Tags {WindowsAzureActiveDirectoryIntegratedApp}

   # add the user running the script as an app owner if needed
   $owner = Get-AzureADApplicationOwner -ObjectId $webAppAadApplication.ObjectId
   if ($owner -eq $null)
   { 
        Add-AzureADApplicationOwner -ObjectId $webAppAadApplication.ObjectId -RefObjectId $user.ObjectId
        Write-Host "'$($user.UserPrincipalName)' added as an application owner to app '$($webAppServicePrincipal.DisplayName)'"
   }


   Write-Host "Done creating the webApp application (TodoListWebApp-aspnet)"

   # URL of the AAD application in the Azure portal
   # Future? $webAppPortalUrl = "https://portal.azure.com/#@"+$tenantName+"/blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/"+$webAppAadApplication.AppId+"/objectId/"+$webAppAadApplication.ObjectId+"/isMSAApp/"
   $webAppPortalUrl = "https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/CallAnAPI/appId/"+$webAppAadApplication.AppId+"/objectId/"+$webAppAadApplication.ObjectId+"/isMSAApp/"
   Add-Content -Value "<tr><td>webApp</td><td>$currentAppId</td><td><a href='$webAppPortalUrl'>TodoListWebApp-aspnet</a></td></tr>" -Path createdApps.html

   $requiredResourcesAccess = New-Object System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.RequiredResourceAccess]

   # Add Required Resources Access (from 'webApp' to 'service')
   Write-Host "Getting access from 'webApp' to 'service'"
   $requiredPermissions = GetRequiredPermissions -applicationDisplayName "ToDoListService-ASP.NET" `
                                                -requiredDelegatedPermissions "access_as_user" `

   $requiredResourcesAccess.Add($requiredPermissions)


   Set-AzureADApplication -ObjectId $webAppAadApplication.ObjectId -RequiredResourceAccess $requiredResourcesAccess
   Write-Host "Granted permissions."

   # Configure known client applications for service 
   Write-Host "Configure known client applications for the 'service'"
   $knowApplications = New-Object System.Collections.Generic.List[System.String]
    $knowApplications.Add($serviceAadApplication.AppId)
    $knowApplications.Add($clientAadApplication.AppId)
    $knowApplications.Add($webAppAadApplication.AppId)
   Set-AzureADApplication -ObjectId $serviceAadApplication.ObjectId -KnownClientApplications $knowApplications
   Write-Host "Configured."


   # Update config file for 'service'
   $configFile = $pwd.Path + "\..\TodoListService\Web.config"
   Write-Host "Updating the sample code ($configFile)"
   ReplaceSetting -configFilePath $configFile -key "ida:TenantId" -newValue $tenantId
   #ReplaceSetting -configFilePath $configFile -key "ida:Audience" -newValue $serviceAadApplication.IdentifierUris
   ReplaceSetting -configFilePath $configFile -key "ida:ClientSecret" -newValue $serviceAppKey
   ReplaceSetting -configFilePath $configFile -key "ida:ClientID" -newValue $serviceAadApplication.AppId
   ReplaceSetting -configFilePath $configFile -key "ida:CAProtectedResourceScope" -newValue ("api://"+$service1AadApplication.AppId+"/access_as_user")

   # Update config file for 'client'
   $configFile = $pwd.Path + "\..\TodoListClient\App.config"
   Write-Host "Updating the sample code ($configFile)"
   ReplaceSetting -configFilePath $configFile -key "ida:TenantId" -newValue $tenantId
   ReplaceSetting -configFilePath $configFile -key "ida:ClientId" -newValue $clientAadApplication.AppId
   #ReplaceSetting -configFilePath $configFile -key "todo:TodoListResourceId" -newValue $serviceAadApplication.IdentifierUris
   ReplaceSetting -configFilePath $configFile -key "todo:TodoListServiceScope" -newValue ("api://"+$serviceAadApplication.AppId+"/.default")

   # Update config file for 'webApp'
   $configFile = $pwd.Path + "\..\TodoListWebApp\Web.config"
   Write-Host "Updating the sample code ($configFile)"
   ReplaceSetting -configFilePath $configFile -key "ida:TenantId" -newValue $tenantId
   ReplaceSetting -configFilePath $configFile -key "ida:ClientId" -newValue $webAppAadApplication.AppId
   #ReplaceSetting -configFilePath $configFile -key "ida:RedirectUri" -newValue $webAppAadApplication.ReplyUrls
   ReplaceSetting -configFilePath $configFile -key "ida:ClientSecret" -newValue $webAppAppKey
   ReplaceSetting -configFilePath $configFile -key "ida:TodoListServiceScope" -newValue ("api://"+$serviceAadApplication.AppId+"/access_as_user")
   
   Write-Host ""
   Write-Host -ForegroundColor Green "------------------------------------------------------------------------------------------------" 
   Write-Host "IMPORTANT: Please follow the instructions below to complete a few manual step(s) in the Azure portal":
   Write-Host "- For 'service'"
   Write-Host "  - Navigate to '$servicePortalUrl'"
   Write-Host "  - Navigate to the Manifest and set 'accessTokenAcceptedVersion' to '2' instead of 'null'" -ForegroundColor Red 
   Write-Host "- For 'service'"
   Write-Host "  - Navigate to '$servicePortalUrl'"
   Write-Host "  - Navigate to the Manifest and set 'accessTokenAcceptedVersion' to '2' instead of 'null'" -ForegroundColor Red 
   Write-Host "- For 'client'"
   Write-Host "  - Navigate to '$clientPortalUrl'"
   Write-Host "  - [Optional] If you are a tenant admin, you can navigate to the API Permissions page and select 'Grant admin consent for (your tenant)'" -ForegroundColor Red 

   Write-Host -ForegroundColor Green "------------------------------------------------------------------------------------------------" 
     
   Add-Content -Value "</tbody></table></body></html>" -Path createdApps.html  
}

# Pre-requisites
if ((Get-Module -ListAvailable -Name "AzureAD") -eq $null) { 
    Install-Module "AzureAD" -Scope CurrentUser 
}

Import-Module AzureAD

# Run interactively (will ask you for the tenant ID)
ConfigureApplications -Credential $Credential -tenantId $TenantId