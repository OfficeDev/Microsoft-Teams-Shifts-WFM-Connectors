# Deployment Guide

## Overview

This document has been created to help IT Administrators deploy, configure and use the **Shifts-Blue Yonder (aka JDA) Integration application** in a Microsoft Azure environment.

**Blue Yonder Workforce Management (formerly JDA)** is a Workforce Management (WFM) system designed for First Line Managers (FLMs) and First Line Workers (FLWs). It provides various capabilities to create and maintain the schedules of FLWs in organizations with multiple business units. First Line Workers can access their schedule, create schedule requests for Time Off, Open Shifts, Shift Swaps, etc.. FLMs can create, and access their FLWs' schedules, schedule requests and approve those.
**Shifts App in Microsoft Teams** is built mobile first for fast and effective time management and communication for FLWs in teams. Shifts lets FLWs and FLMs use their mobile devices to manage schedules and keep in touch.  
**Shifts-Blue Yonder Integration application** syncs shift data only between Blue Yonder WFM (v17.2) and Microsoft Shifts App in Teams thus enabling both FLWs and FLMs to see their assigned shifts.

## Prerequisites

The points noted below are the minimal requirements in order to properly configure the Shifts-Blue Yonder Integration application:

* The IT Admin has a functional understating of Blue Yonder WFM 17.2, Microsoft Azure and the Microsoft Teams Shifts App. IT Admin has the client admin role in Blue Yonder and is a tenant admin in the Microsoft Azure tenant where the solution is to be deployed. 
* Blue Yonder WFM serves as single source of truth for shifts. 
  * Shifts should only be created/edited/deleted in Blue Yonder WFM by the FLMs and not in the Shifts App.  
  * **Open Shifts, Time Off and Availability are not in scope for this integration**.
  * **Shift swap, Offer Shift and Time Off Requests are similarly out of scope for this integration**.
  * The Shifts App in Teams can be used by the FLWs to view their schedules only.
* Users must be created in Azure/Teams manually prior and must have a user principal name (upn) that can automatically be mapped to the login name of the user in Blue Yonder. User to user mapping is controlled by a configuration app setting in the functions app in Azure UserPrincipalNameFormatString. This setting is a comma delimited list of upn's where {0} represents the part of the upn that must correspond to the login name in Blue Yonder. Thus a user with the Blue Yonder login name john.doe must have a Teams upn of john.doe@somedomain.com and the UserPrincipalNameFormatString should be set to to {0}@somedomain.com.  
* Teams must be created before they can be connected to the integration.
  * Only the shifts of users that have been added to the team will be synced across.
  * Users can be added to the team at any time.
  * Users should only be removed from a team after all their current and future shifts have been deleted/reassigned in Blue Yonder and these shift changes have synced across to the Teams Shifts app.
* All time zones configured in Blue Yonder need to be manually added to the timezones Azure Storage table (details below). 

## Solution Overview

The Shifts-Blue Yonder Integration application is a Microsoft Azure Functions v2 application developed using .NET Core 2.1 with supporting components developed using .NET Standard 2.0.  The core function of the application of synchronising shifts from Blue Yonder into Microsoft Teams Shift is driven by a single eternal orchestrator. Teams are connected to the application via a Teams tab application.

![](.\images\ReferenceArchitecture.png)

1. Azure Functions - contains the entire logic of the connector - developed in C#
2. Azure Table Storage - the list of connected teams is stored in a table called **teams** with the following schema:

| Field          | Description                                                  |
| -------------- | ------------------------------------------------------------ |
| PartitionKey   | Always has the value teams                                   |
| RowKey         | Stores the identifier (guid) of the team that is connected.  |
| TimeStamp      | The date and time the record was last modified               |
| BaseAddress    | The base address of the api connection to Blue Yonder - in theory each team can reference a different instance of Blue Yonder, however, in practice the design assumes that a single instance of the connector will connect teams within a single tenant to a single instance of Blue Yonder |
| StoreId        | The integer ID of the connected store (business unit) in Blue Yonder |
| StoreName      | The name of the connected store (business unit) in Blue Yonder |
| TeamName       | The name of the team in Microsoft Teams the store (business unit) is connected to |
| TimeZoneInfoId | The standard* name of the time zone assigned to the store (business unit) in Blue Yonder |

All entries on this table are created programmatically by the connector at the time of connection.

3. Azure Table Storage - the mappings between Blue Yonder's user-definable time zone names and the standard time zone names that are required to actually convert times between different time zones is stored in a table called **timezones** which has the following schema:

| Field          | Description                                                  |
| -------------- | ------------------------------------------------------------ |
| PartitionKey   | Always has the value timezones                               |
| RowKey         | Stores the name values assigned in Blue Yonder to the different time zones |
| TimeStamp      | The date and time the record was last modified               |
| TimeZoneInfoId | The standard* name of the time zone                          |

\* *Note*: A list of all supported timezones and their standard names can be found here: [TimeZones](timezones.md) and full details on how to create and populate this table are presented [below](#Populate The Timezones Table).

4. Azure Blob Storage - a container named **app** is used to store a single html file named index.html that contains the html, javascript and images required for the tab application when connecting a team to Blue Yonder.
5. Azure Blob Storage - a container named **shifts** is used to cache the collection of shifts synced to the connected team's schedule per store per week
6. Azure KeyVault - this is used to store the username and password of the api user used to get shift and other data from Blue Yonder and the access and refresh tokens required for interacting with the Graph API when creating/updating/deleting shifts in Teams
7. Application Insights - all logging within the connector is to application insights which has powerful querying tools for diagnosing issues and monitoring overall performance.

## Sync Process

The sync process is controlled per team by an instance of an eternal TeamOrchestrator, one per team which uses a timer to schedule its next execution according to the sync frequency defined in the configuration settings of the Functions app. The orchestrator determines the number of weeks to be synchronised (controlled by configuration settings for the Functions app) and fans out execution by creating in parallel an instance of the WeekOrchestrator which itself calls an instance of the WeekActivity:

![](.\images\FanOut.png)

The WeekActivity is where the real work of the synchronisation happens as follows:

 ![](.\images\WeekActivity.png)

## Deployment

The following explains the necessary steps to deploy the Blue Yonder Integration application.

### Prerequisites

1. Access to the Blue Yonder Workforce Management (v17.2) system to integrate
   - API endpoint URL.
   - API username and password (must have access to the retail web apis).
2. Access to Microsoft Teams with permissions to be able to upload a tab application into the application store for the tenant
3. Access to Microsoft Azure Tenant with sufficient permissions to be able to:
   - Create and configure an App Registration in Active Directory
   - Grant Admin consent for user, group and application permissions to the app registration
   - Create a resource group within a new or existing subscription to host the application components
   - Deploy the resources to the resource group
   - Configure the Azure Storage
   - Configure the Azure KeyVault

### Register Azure AD Application

This integration uses the Microsoft Graph APIs to access information about users (FLWs & FLMs), teams and schedules from the Microsoft Teams Shifts App. In order to be able to do this, the application must be registered in Azure AD and the required permissions need to be granted.

1. Log in to the Azure Portal and navigate to Azure Active Directory and select **App registrations**
2. Click on **+ New Registration** and:
   - **Name**: supply a name e.g. Shifts-Blue Yonder Integration
   - **Supported account type**: normally the default of single tenant should be sufficient
   - **Redirect URI**: this is important but will be set after deployment of the ARM template and the functions application

![](.\images\AppRegistration1.png)

3. Click on the *Register* button
4. When the app is registered, you'll be taken to the app's "Overview" page. Copy the **Application (client) ID**; we will need it later.

![](.\images\AppRegistration2.png)

5. In the side panel in the Manage section, click the **Certificates & secrets** section. In the Client secrets section, click on **+ New client secret**. Add a description (name of the secret) and select *Never* for Expires. Click **Add**

![](.\images\AppRegistration3.png)

6. Once the client secret is created, copy its *Value*; we will need it later
7. Navigate to the **Authentication** page that can be found in the left panel under *Manage* in the figure under step 4.
8. Click **+ Add a platform** and select Web and enter the required value for *Redirect URIs* which can be anything for now as it will be updated later and select **Access tokens** and **ID tokens** under *Implicit grant*

![](.\images\AppRegistration4.png)



9. Click the Configure button.
10. Next click API permissions in the left panel and select the following permissions:

| Scope               | Application/Delegated | Description                                                  |
| ------------------- | --------------------- | ------------------------------------------------------------ |
| User.Read.All       | Delegated             | Allows the application to read the full profile for users    |
| Group.ReadWrite.All | Delegated             | Allows the application to read and write all group (team) data, specifically it allows the application to obtain team and membership data as well as create a schedule for a team and create/update/delete scheduling groups and shifts within the team's schedule. |
| offline_access      | Delegated             | Allows the application to obtain a refresh token from the Graph API that can be used to automatically refresh the access token. |

![](.\images\AppRegistration5.png)

11. Click the Grant admin consent for... option to grant the required consent to the delegated user the application runs as.

### Deploy The Application to Microsoft Azure

The following steps should be followed in order to correctly deploy the required resources to your Azure subscription.

1. In your Azure subscription create a new resource group to host the application components.
2. In GitHub, fork the Microsoft Repo (https://github.com/OfficeDev/Microsoft-Teams-Shifts-WFM-Connectors) to your own account which will allow you to make changes to your copy of the code without impacting the main repository and redeploy when necessary.
3. Open this Readme in your forked copy and edit the url of the Deploy to Azure button below to refer to your own forked repo e.g. change *OfficeDev* to the name of your repo.

[![Deploy to Azure](https://azuredeploy.net/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FOfficeDev%2FMicrosoft-Teams-Shifts-WFM-Connectors%2Fmaster%2FJDA_v17.2-Shifts-Connector%2Fsrc%2FJdaTeams.Connector.Infrastructure%2Fazuredeploy.json)  

5. After saving the change, reopen the readme from your repo and click the button to start the deployment process which will display the following screen

![](.\images\Deployment1.png)#

6. Select a subscription and the resource group created in 1.
7. Fill in the values for the parameters of the ARM Template as per the table below:

| Parameter Name                    | Required | Description                                                  |
| --------------------------------- | :------: | ------------------------------------------------------------ |
| Client Id                         |   Yes    | The application id copied during the app registration section above (step 4). |
| Client Secret                     |   Yes    | The client secret value copied during the app registration section above (step 6). |
| Frequency Seconds                 |   Yes    | The shifts in Blue Yonder will be synchronised to Teams every X seconds defined here. |
| Past Weeks                        |   Yes    | The number of weeks in the past that will be synchronised.   |
| Future Weeks                      |   Yes    | The number of weeks in the future will be synchronised.      |
| Consumption Plan Name             |    No    | If not supplied the consumption plan will be created with the name {ResourceGroupName}pln. |
| Functions App Name                |    No    | If not supplied the functions app will be created with the name {ResourceGroupName}fun. |
| Functions Storage Account Name    |    No    | If not supplied the functions storage account will be created with the name {ResourceGroupName}fns. |
| Application Storage Account Name  |    No    | If not supplied the application storage account will be created with the name {ResourceGroupName}aps. |
| Application Insights Name         |    No    | If not supplied the application insights instance will be created with the name {ResourceGroupName}ain. |
| Key Vault Name                    |    No    | If not supplied the KeyVault will be created with the name {ResourceGroupName}kev. |
| Delete Shifts Cache After Days    |   Yes    | The number of days after which the cached shifts will be automatically deleted. |
| Clear Schedule Enabled            |   Yes    | If a team is disconnected and reconnected this ensures that any existing shifts in the team's schedule are deleted (to prevent duplicates from being created). |
| Draft Shifts Enabled              |   Yes    | If set to true, shifts will be created in teams as drafts and published together at the end of the week sync. This reduces the number of notifications that are sent by teams to the users. |
| Jda Base Address                  |    No    | If supplied the user will not be prompted to enter a URL when connecting a team via the tab application. |
| Shifts App Url                    |   Yes    | This is a fixed URL that is used to provide a link to the shifts app in Teams from the tab page. |
| Start Day Of Week                 |   Yes    | The number of the day representing the start day of the week (0 = Sunday) |
| User Principal Name Format String |   Yes    | The format string used to automatically map users from Blue Yonder to users in Teams. N.B. this assumes that accounts are created with matching values as described in the Prerequisites at the top of this document. |

8. Click Review + Create followed by Create and wait for all elements of the template to be fully deployed.

### Post ARM Template Deployment Steps

The following actions are required to complete the deployment and configuration of the integration application:

1. [Access policy setup in Azure Key Vault](#Access Policy Setup In Azure KeyVault)
3. [Set up the redirect URIs](#Set Up The Redirect URIs)
4. [Configure Azure Storage](#Configure Azure Storage)
5. [Populate the timezones table](#Populate The Timezones Table)
5. [Compile and upload the Index.html page](#Compile And Upload The Index.html Page)
6. [Deploy the Functions application](#Deploy The Functions Application)
7. [Create and Upload the Tab App Manifest](#Create And Upload The Tab App Manifest)

#### Access Policy Setup In Azure KeyVault

1. Navigate to the Functions application created by the ARM template deployment. Select **Identity** under *Settings* in the left-hand panel.

![](.\images\ConfigureKeyVault1.png)

2. With the **System Assigned** tab selected switch the *Status* to **On** and Save followed by Yes. Copy the Object ID once the identity has been created in Azure.
3. Navigate to the **Azure KeyVault** resource and select **Access policies** under *Settings*

![](.\images\ConfigureKeyVault2.png)

4. Click **Add Access Policy** and in the *Configure from template*, choose **Secret Management**. Click the link **None selected** next to *Select principle* and paste the Object ID copied from 2 above. Click Select and then click Add to add the new policy.

![](.\images\ConfigureKeyVault3.png)

5. On the summary screen it is important to click the **Save** button to persist these changes.
6. If you wish to be able to view the Secrets stored by the application you will need to create an access policy for your own account also.

#### Set Up The Redirect URIs

It is now necessary to configure the redirect uris for the application as follows:

1. Navigate to the Functions App resource and in the Overview screen copy the URL value:

![](.\images\SetupUris1.png)

2. In Azure AD navigate to the App Registration created above and select Authentication under Manage. Replace the dummy https://updatemelater.com with the URL copied from the functions app and add three more uris based on it as follows:
   1. {copied url}/delegate
   2. {copied url}/consent
   3. {copied url}/app

![](.\images\SetupUris2.png)

3. Then click Save to persist the changes.

#### Configure Azure Storage

Navigate to the application Azure Storage resource (the one ending **aps** if the default naming was used) and click the **Storage Explorer** option and in **Blob Containers** create the following containers:

1. shifts
2. app

And in **Tables** create the following tables

1. teams
2. timezones

#### Populate The Timezones Table

A list of all the time zone names that have been created in Blue Yonder can be obtained from either in the Enterprise application or using the /timezones api.

![](.\images\TimezoneSetup1.png)

The values that are required are from the user-definable Time Zone Name column.

Navigate to the timezones table created above and for each Time Zone Name from Blue Yonder create a row with the following schema:

 ![](.\images\TimezoneSetup2.png)

#### Compile And Upload The Index.html Page

Open the web folder in VS Code, open a terminal in the root and do the following:

1. *npm install* - to install the node packages required to build the page
2. *npm run-script build* - to build and package the complete source into a single **index.html** file using webpack v3.3.0 (this may need to be installed separately depending on your development environment).
3. If built successfully, the **index.html** file will be created in the *dist* folder
4. Upload this file to the **app** blob container created [above](*Configure Azure Storage)

#### Deploy The Functions Application

The functions application code is not currently deployed as part of the ARM template deployment and therefore must be deployed using a manual process from Visual Studio as follows:

1. Right-click the functions application project JdaTeams.Connector.Functions and select Publish

![](.\images\DeployFunctions1.png)

2. Select Azure and click Next
3. Select your account, Subscription, Resource Group and the Functions App created by the ARM template. Ensure Run from package file (recommended) is ticked and click Finish.

![](.\images\DeployFunctions2.png)

4. Check the details on the summary screen and then click Publish to commence the deployment

![](.\images\DeployFunctions3.png)

#### Create And Upload The Tab App Manifest

In order to be able to connect a team to Blue Yonder it is necessary to do so with a tab application dedicated to a particular environment. Tab applications are created by uploading to the Teams app store an app manifest file, an example of which can be found in the app folder.

```json
{
    "$schema": "https://developer.microsoft.com/en-us/json-schemas/teams/v1.5/MicrosoftTeams.schema.json",
    "manifestVersion": "1.5",
    "version": "1.2.0",
    "id": "f8e70947-71d4-483f-91e3-88cb4de36a7a",
    "packageName": "com.jda.shifts.tab",
    "developer": {
        "name": "Microsoft",
        "websiteUrl": "https://www.microsoft.com",
        "privacyUrl": "https://go.microsoft.com/fwlink/?LinkId=521839",
        "termsOfUseUrl": "https://go.microsoft.com/fwlink/?LinkID=206977"
    },
    "icons": {
        "color": "color.png",
        "outline": "outline.png"
    },
    "name": {
        "short": "Shifts",
        "full": "JDA Shifts"
    },
    "description": {
        "short": "JDA Workforce Management Shifts Integration",
        "full": "Links JDA Workforce Management and Microsoft Teams"
    },
    "accentColor": "#FFFFFF",
    "configurableTabs": [
        {
            "configurationUrl": "https://editme.azurewebsites.net/app?theme={theme}",
            "canUpdateConfiguration": false,
            "scopes": [
                "team"
            ]
        }
    ],
    "permissions": [
        "identity"
    ],
    "validDomains": [
        "editme.azurewebsites.net",
        "login.microsoftonline.com"
    ]
}
```

The values that MUST be edited in this file are:

| Field            | Description                                                  |
| ---------------- | ------------------------------------------------------------ |
| id               | Generate a new GUID for each separate instance of the tab application |
| configurationUrl | Enter the url of the app function (see below) replace the ?clientId=default from the copied Url with ?theme={theme} |
| validDomains     | Replace the editme with the name of the Functions app        |

![](.\images\DeployTabApp1.png) 

Zip the following files to create the tab app package to upload to the app store:

1. color.png
2. manifest.json
3. outline.png

To upload the tab application click the **Apps** button in Teams followed by the **Upload a custom app** (if you cannot see this option then you will need a tenant admin to upload the package for you).

![](.\images\DeployTabApp2.png)



# Operations Guide

## App Settings

The application defines the following app settings which can be configured as required. N.B. not all settings are created by the ARM template deployment and if a non-default value is required then it will be necessary to create it and set it to the desired alternative value.

| Setting                           | Default                                                      | Description                                                  |
| --------------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| AdminConsentUrl                   | https://login.microsoftonline.com/common/adminconsent        | Used by the ConsentTrigger.                                  |
| AppBlobName                       | index.html                                                   | The name of the blob retrieved from the App container and returned by the AppTrigger. |
| AppContainerName                  | app                                                          | The name of the blob storage container holding the app blob. |
| AuthorizeUrl                      | https://login.microsoftonline.com/common/oauth2/v2.0/authorize | If the application is registered with Supported account types: Single tenant then the default should be replaced with a single tenant url where _common_ is replaced with the domain name or tenant id. This url is returned to the client in the ConfigTrigger and used by the DelegateTrigger. |
| BaseAddress                       | https://graph.microsoft.com/beta                             | The base address of the microsoft graph api.                 |
| ClearScheduleBatchSize            | 50                                                           | The number of shifts that are deleted in a single operation. |
| ClearScheduleEnabled              | false                                                        | Whether the schedule is automatically cleared when a team first subscribes. |
| ClearScheduleMaxAttempts          | 20                                                           | Sets the maximum iterations that a clear schedule day orchestrator will attempt to delete the shifts for a day. |
| ClearScheduleMaxBatchSize         | 200                                                          | Sets the size of the batch of shifts to delete that is obtained from Teams each iteration. |
| ClientId                          | -                                                            | This is the application ID of the app registration in Azure Active Directory. It is returned from the App Settings by the ConfigTrigger and is used by the ConsentTrigger and DelegateTrigger and when retrieving access and refresh tokens for the graph api. |
| ClientSecret                      | -                                                            | This is a secret generated in the app registration in Azure AD. Used when retrieving access and refresh tokens for the graph api. |
| ConnectionString                  | Set by the ARM Template Deployment                           | The connection string to the Azure Storage account for the application. |
| ContinueOnError                   | true                                                         | If true ensures that errors are caught in the TeamOrchestrator, logged and the orchestrator continues. |
| DraftShiftsEnabled                | false                                                        | When set to true, shifts are initially created as drafts in Teams by the integration then shared to all the team members at the end of the week orchestration. Used to minimise user notifications. |
| FrequencySeconds                  | 900                                                          | The interval at which shifts are fetched from Blue Yonder and synchronised with the Teams Shifts App. |
| FutureWeeks                       | 3                                                            | The number of weeks into the future that are synced from Blue Yonder to Teams. |
| IgnoreCertificateValidation       | false                                                        | Allows server certificate validation to be ignored.          |
| JdaApiPath                        | /data/retailwebapi/api/v1-beta5                              | The root path for the Blue Yonder public apis.               |
| JdaBaseAddress                    | -                                                            | The base address for Blue Yonder cookie authentication and the public apis. |
| JdaCookieAuthPath                 | /data/login                                                  | The root path for the Blue Yonder cookie authentication.     |
| KeyVaultConnectionString          | Set by the ARM Template Deployment                           | Used by the AzureKeyVaultSecretsService to read/write the secrets. |
| LongOperationMaxAttempts          | 8                                                            | Used to set the maximum number of attempts a long operation will be retried in the event that it fails. Used by the share schedule operation. |
| LongOperationRetryIntervalSeconds | 15                                                           | Used to set the interval of time between each attempt at performing the long operation. |
| MaximumDelta                      | 200                                                          | The maximum number of shifts that will be processed in a single delta. |
| MaximumUsers                      | 100                                                          | The maximum number of users to return in a single call to JDA - N.B. all users are fetched, this just sets the batch size. |
| NotifyTeamOnChange                | false                                                        | Used when sharing the schedule to determine whether the whole team (true) is notified of the updated schedule or just affected users (false). |
| PastWeeks                         | 3                                                            | The number of weeks into the past that are synced from JDA to Teams. |
| PollIntervalSeconds               | 10                                                           | The interval at which Teams is polled to see if a new schedule has been fully provisioned. |
| PollMaxAttempts                   | 20                                                           | The maximum number of times Teams is polled with regard to the schedule. |
| RetryIntervalSeconds              | 5                                                            | For operations that need to be retried if they fail, this sets the interval between each attempt. |
| RetryMaxAttempts                  | 5                                                            | The maximum number of times failing operations will be retried before giving up. |
| Scope                             | offline_access Group.ReadWrite.All User.Read.All             | Defines the claims requested for the token used in the graph api operations. |
| ShiftsAppUrl                      | -                                                            | The deep link to the Shifts app used by the Shifts tab page. |
| ShiftsContainerName               | shifts                                                       | The name of the Azure storage blob container where Shifts are cached. |
| StartDayOfWeek                    | DayOfWeek.Monday (1)                                         | The start day of the week as defined in JDA and matched in Teams. |
| TakeCount                         | 1000                                                         | Sets the maximum number of connections that are returned from the team table in Azure storage. |
| TeamTableName                     | teams                                                        | The name of the table used to store team connection information. |
| ThemeMap                          | -                                                            | Allows the manual mapping of JDA Job colours to Teams shift and activity colours and must be in the form {jdathemecode}={teamsthemecode};{jdathemecode]={teamsthemecode}. NB it is not necessary to map all colours, automatic mapping will be used for any not defined in the map. |
| TimeZone                          | GMT Standard Time                                            | The fallback timezone in the event that no mapping can be found for the time zone from Blue Yonder |
| TokenUrl                          | https://login.microsoftonline.com/common/oauth2/v2.0/token   | If the application is registered with Supported account types: Single tenant then the default should be replaced with a single tenant url where _common_ is replaced with the domain name or tenant id. The url used to obtain new access and refresh tokens for the Graph API. |
| TraceEnabled                      | false                                                        | Used for local debugging purposes only to log out the details of all JDA and Graph API calls. N.B. it cannot currently be used in Azure because it logs to file and the application does not have permission to create files. |
| TraceFolder                       | Traces                                                       | The name of the folder where the traces are created - can be a full or a relative (to the application assemblies) path. |
| TraceIgnore                       | vault.azure.net                                              | Does not log details of calls to any of the ; separated urls, N.B. urls containing these values are ignored. |
| UserPrincipalNameFormatString     | {0}                                                          | Used when mapping of JDA user login names to Teams user principal names. This allows JDA users to be specified in the form e.g. andy.maggs and the corresponding Teams user in the form e.g. andy.maggs@replappsource.onmicrosoft.com. In this example the format would be {0}@replappsource.onmicrosoft.com. Multiple , or ; delimited formats can be specified. |

## Application Insights

All application logging is done to Application Insights except Tracing in the AutoRest generated files which is intended only to be used in the local development environment. All logging to Application Insights is done using semantic logging with the following Event ID's:

| EventId | Name             | Log Level | Exception               | Description                                                  | Semantic Data                                                |
| ------- | ---------------- | --------- | ----------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| 2       | Source           | Info      |                         | logs the count of records obtained from source               | activityType, recordCount, teamId, dateValue, storeId        |
| 3       | Delta            | Info      |                         | logs the count of delta items; full, partial and applied     | itemType, stage, createdCount, updatedCount, deletedCount, failedCount, skippedCount, teamId, dateValue |
| 4       | Schedule         | Info      |                         | logs information about a schedule                            | status, isEnabled, timeZone, teamId, workforceIntegrationId  |
| 5       | Employee         | Warning   |                         | logs details when an employee referenced in a shift cannot be obtained from Blue Yonder | status, employeeId, storeId, teamId, weekDate                |
| 6       | Job              | Warning   |                         | logs details of a job associated with a shift that cannot be found in JDA | status, jobId, storeId, teamId, weekDate                     |
| 7       | Department       | Warning   |                         | logs details of a department associated with a job that cannot be found in JDA | status, jobId, storeId, teamId, weekDate                     |
| 8       | Member           | Error     | Exception               | logs details when the mapped team member cannot be obtained from Teams | status, employeeId, loginName, storeId, teamId, weekDate     |
| 9       | Shift            | Error     | Exception               | logs details of any error when creating/updating/deleting shifts in Teams | shiftType, status, operationName, sourceId, employeeId, storeId, teamId, weekDate |
|         |                  | Error     | MicrosoftGraphException | logs details of any microsoft graph errors when creating/updating/deleting shifts in Teams | shiftType, status, operationName, errorCode, errorDescription, errorRequestId, errorDate, sourceId, employeeId, storeId, teamId, weekDate |
| 10      |                  | Trace     |                         | logs details of any shift skipped                            | shiftType, status, operationName, sourceId, employeeId, storeId, teamId, weekDate |
| 11      | Scheduling Group | Error     | Exception               | logs details of any error when creating a new scheduling group in Teams | status, departmentName, sourceId, employeeId, storeId, teamId, weekDate |
|         |                  | Error     | MicrosoftGraphException | logs details of any microsoft graph errors when creating a new scheduling group in Teams | status, operationName, errorCode, errorDescription, errorRequestId, errorDate, sourceId, employeeId, storeId, teamId, weekDate |
|         |                  | Error     | Exception               | logs details of any microsoft graph errors when adding employees to a scheduling group in Teams | status, departmentName, schedulingGroupId, storeId, teamId, weekDate |
| 12      | Clear Schedule   | Info      |                         | logs details at the start of the clear schedule              | clearType, stage, teamId, startDate, endDate                 |
|         |                  | Error     | Exception               | logs details of any error when deleting shifts/open shifts   | shiftType, status, operationName, sourceId, destinationId, employeeId, teamId, dayDate |
|         |                  | Error     | Exception               | logs details of any error fetching the list of shifts to delete | shiftType, status, operationName, teamId, dayDate            |
|         |                  | Various   |                         | logs details at the end of the clear schedule                | clearType, stage, teamId, startDate, deletedCount, iterationCount, isFinished |
|         |                  | Error     | Exception               | logs details of any error when deleting time off records     | status, operationName, teamId, startDate, endDate            |
| 13      | Team             | Error     | Exception               | logs any errors in the team orchestrator and sub-orchstrators | storeId, teamId                                              |
| 14      | Week             | Info      |                         | logs details of the week being processed by the week activity | storeId, teamId, weekDate                                    |

## Connect A Team

Connecting a team is as simple as adding an instance of the tab app to the general channel of the Team:

![](.\images\ConnectTeam1.png)

Click the + button

![](.\images\ConnectTeam2.png)

What you see in the Add a tab dialog depends on what you have used before and you may need to search for the application which may use a different name or icon depending on what was used in the manifest.json for the application.

![](.\images\ConnectTeam3.png)

In the dialog above click the Add button to add the tab app to the team.

![](.\images\ConnectTeam4.png)

Click the Sign in with Microsoft button then enter the Store ID (internal integer ID of the store/business unit), the login name and password of the user who will need sufficient permissions to access the Blue Yonder public apis and have full access to the data for the store. Click Save whereupon the data entered will be validated and if successful the connection will be made, the schedule will be created (may take a few minutes) and the synchronisation of shifts started. 
