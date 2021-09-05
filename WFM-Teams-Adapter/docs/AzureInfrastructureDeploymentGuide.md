

[[_TOC_]]

# Azure Infrastructure Deployment Guide

## Overview

This document has been created to help IT Administrators deploy the necessary infrastructure to an Azure Resource Group using the supplied Azure Resource Manager (ARM) template.

## Prerequisites

The points noted below are the minimal requirements in order to be able to properly deploy and configure the WFM Teams Adapter application with specifics for the Blue Yonder WFM Connector (other WFM providers may have different requirements):

* The IT Admin has a functional understating of Blue Yonder WFM 2020.3+, Microsoft Azure and the Microsoft Teams Shifts App.
* The IT Admin is a tenant admin in the Microsoft Azure tenant where the solution is to be deployed. This level of permission is required for the following activities:
  * Create and configure an App Registration in Active Directory
  * Grant Admin consent for application permissions for the App Registration
  * Deploy the resources to the Resource Group in Azure
  * Configure the Azure KeyVault
* A super-user account has been created in Blue Yonder at the root enterprise level that has the following roles: RWA API Access (required for the retail web api) and Store Manager (required for the site manager api).
* Users must be created in Azure Active Directory manually prior to connecting any team and must be given a user principal name (upn) that can be automatically mapped to the login name of the user in Blue Yonder. User to user mapping is controlled by a configuration app setting in the Functions App in Azure UserPrincipalNameFormatString. This setting is a comma delimited list of upn's where {0} represents the part of the upn that must correspond to the login name in Blue Yonder. Thus a user with the Blue Yonder login name john.doe must have a Teams upn of john.doe@somedomain.com and the UserPrincipalNameFormatString should be set to to {0}@somedomain.com.  
* Teams must be created before they can be connected to the integration. 
  * Only users that have been added to the team and that can be successfully mapped will have their schedule synced across.
  * Users can be added to the team at any time.
  * Users should only be removed from a team after all their current and future shifts have been deleted/reassigned in Blue Yonder and these shift changes have synced across to the Teams Shifts app.
* All time zones configured in Blue Yonder need to be manually added to the timezones Azure Storage table (details below). 



## Register Azure AD Application

This integration uses the Microsoft Graph APIs to access information about users (FLWs & FLMs), teams and schedules from the Microsoft Teams Shifts App. In order to be able to do this, the application must be registered in Azure AD and the required permissions need to be granted.

1. Log in to the Azure Portal and navigate to Azure Active Directory and select **App registrations**.
2. Click on **+ New Registration** and:
   - **Name**: supply a name e.g. Shifts-Blue Yonder Integration
   - **Supported account type**: normally the default of single tenant should be sufficient
   - **Redirect URI**: this is important but will be set after deployment of the ARM template and the functions application

![Register An Application](images/08-RegisterAADApplication.png)

3. Click on the Register button.
4. When the app is registered, you'll be taken to the app's "Overview" page. Copy the **Application (client) ID**; we will need it later.

![App Registration Overview](images/09-AppRegistrationOverview.png)

5. In the side panel in the Manage section, click the **Certificates & secrets** section. In the Client secrets section, click on **+ New client secret**. Add a description (name of the secret) and select *Recommended: 6 months* for Expires (don't forget to set a reminder to create a new secret and update the value in KeyVault near the expiry date otherwise the application will simply stop working) and click **Add**.
6. Copy the secret value because it will be needed later and this is the one and only time the secret will be visible.
7. Navigate to the **Authentication** page that can be found in the left panel under *Manage* in the figure under step 4.
8. Click **+ Add a platform** and select Web and enter the required value for *Redirect URIs* which can be anything for now as it will be updated later and select **Access tokens** and **ID tokens** under *Implicit grant* 

![Configre Web](images/10-AppRegistrationConfigureWeb.png)

9. Click the Configure button.
10. Next click API permissions in the left panel and select the following permissions:

| Scope                              | Application/Delegated | Description                                                  |
| ---------------------------------- | --------------------- | ------------------------------------------------------------ |
| User.Read.All                      | Application           | Allows the application to read the full profile for users    |
| Group.Read.All                     | Application           | Allows the application to read all group (team) data, specifically it allows the application to obtain team and membership data. |
| Schedule.ReadWriteAll              | Application           | Allows the application to create/update a schedule (and all the items within it) for the Team. |
| UserShiftPreferences.ReadWrite.All | Application           | Allows the application to read and write the shift preferences (availability) for the team members. |

11. Click the Grant admin consent for... option to grant the required consent to the Application.



## Deploy The Application Infrastructure To Microsoft Azure

The src folder contains a WfmTeams.Adapter.Infrastructure project which contains the Azure Resource Manager (ARM) template that will create all the required resources for the application in a single resource group.

1. In your Azure subscription create a new resource group to host the application components.
2. Open this file (AzureInfrastructureDeploymentGuide.md) in **your** GitHub repo and edit the url of the Deploy to Azure button below to refer to your own forked repo e.g. change *OfficeDev* to the name of your repo.

[![Deploy to Azure](https://azuredeploy.net/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAndy65%2FMicrosoft-Teams-Shifts-WFM-Connectors%2Fmaster%2FWFM-Teams-Adapter%2Fsrc%2FWfmTeams.Adapter.Infrastructure%2Fazuredeploy.json)  

3. After saving the change, reopen this file from your repo and click the button to start the deployment process which will display the following screen:

