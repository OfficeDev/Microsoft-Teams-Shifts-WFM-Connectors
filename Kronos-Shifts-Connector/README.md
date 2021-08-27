# Deployment Guide

## Overview
This document is created to help IT Administrators to deploy, configure, and use the **Shifts-Kronos Integration application** in a Microsoft Azure environment.  
**Kronos Workforce Central (Kronos WFC 8.1)** is a Workforce Management system designed for First Line Managers (FLMs) and First Line Workers (FLWs). Kronos provides various capabilities to handle schedules of FLWs in organizations with multiple departments and job categories. First Line Workers can access their schedule, create schedule requests for Time Offs, Open Shifts, Swap Shifts, etc.. FLMs can create, and access their FLWs' schedules, schedule requests and approve those.  
**Shifts App in Microsoft Teams** keeps FLWs connector and in sync. It's built mobile first for fast and effective time management and communication for teams. Shifts lets FLWs and FLMs use their mobile devices to manage schedules and keep in touch.  
**Shifts-Kronos Integration application** is built to sync data between Kronos Workforce Central (v8.1) and Microsoft Shifts App in Teams in seamless fashion. It helps FLWs access their schedules which are created in Kronos system from Shifts App, and further enables FLMs to access schedule requests which are created in Shifts from Kronos system.

## Considerations
The points noted below are to be considered as best practices to properly leverage the full potential of the Shifts-Kronos Integration application.

* IT Admin has functional understating of Kronos WFC 8.1 and Microsoft Teams Shifts App. IT Admin is also the super user of Kronos – The IT Admin needs to have admin-level access to Shifts as their credentials are required for request approval  
* Kronos WFC serves as single source of truth for all entities 
    * Shifts and Open Shifts should be created in Kronos WFC by the FLMs and not in the Shifts App  
    * **The Offer Shift entity and related functionalities are not in scope of this integration**
    * Shifts App can be used by the FLWs to view their schedules, create requests for Time Offs, Open Shifts, and Swap Shifts  
    * **FLMs can use Kronos WFC for Approval/Rejection workflows but this is also now supported in the Shifts app. FLMs should not approve any requests in the Shifts App as it can cause data inconsistency between Kronos WFC and Shifts.** 
* FLW requests (Open Shift Request, Time Off Request, Swap Shift Request) will be synced from Shifts to Kronos in synchronous manner using Shifts Outbound APIs and Kronos WFC 8.1 data submission (POST) APIs  
* Approved schedules for Shifts, Time Offs, Open Shifts and Swap Shifts will be sync'd from Kronos to Shifts App in asynchronous manner using Kronos WFC 8.1 GET APIs and Shifts/Graph POST APIs  
* Status of requests created in Shifts App and synced to Kronos WFC will be synced back to Shifts App to keep both systems in sync  
* To sync all the requests initiated in Shifts App (by FLWs) to Kronos, SuperUser account credentials are used. Once these are approved in Kronos (by FLMs), their approval status will be synced back to Shifts App. These statuses are synced to Shifts App using Microsoft Graph APIs with Shifts Admin account authorization  
* Users must be created in Azure/Teams prior to User to User mapping step to be performed in Configuration Web App (Config Web App is one of the components of this integration as explained in below sections)  
* Teams and Scheduling groups must be created in Shifts App prior Teams to Department mapping step in Configuration Web App  
* Done button on Configuration Web App should be used only for first time sync  
* First time sync is expected to take longer time since it may sync data for larger time interval. The time would vary based on amount of data i.e. number of users, number of teams, number of entities (such as Shifts, Time Offs, Open Shifts etc.) to be synced and date span of the Time interval for which the sync is happening. So, it may take time to reflect this complete data in Shifts. Done button click will initiate background process to complete the sync  
* FLWs are to be aware of which PayCodes they have access to for creating the Time Off requests. The PayCodes are to be configured ahead of time in the Kronos WFC instances. 
* The list of usable PayCodes is taken from your Kronos instance and updated periodically to mirror any additions or removals. To manually configure PayCodes, please follow the steps outlined in the Troubleshooting section of the Design Document
* Inactive and terminated users and any entities linked to them are not synced to the Shifts app.
* All other syncs are dependant on the user sync. If this sync fails, the others will not run.

## Solution Overview
The Shifts-Kronos Integration application has the following components built using ASP.Net Core 2.2. Those need to be hosted on Microsoft Azure.  
* Configuration Web App  
* Integration Service API  
* Azure Logic App for periodic data sync  
* Kronos WFC solution to retrieve data and post data, part of Integration Service API  

![architecture](images/arch-diagram.png)

1.	Azure Web App Services – For Configuration Web App and the Integration Service API. The Configuration Web App and the Integration Service API are both written in ASP.NET Core technologies
2.	Azure Table Storage – the database account which contains the necessary tables required for the entire Shifts-Kronos Integration to work successfully
3.	Azure Logic App – this is the schedule job that will sync data between Kronos WFC and Shifts App on a configured interval of time or configured number of previous days from current date till number of next days, based on flag passed to APIs for slide dates or complete sync
4.	Azure Key Vault – to store all the connection strings, client Ids, client secrets, access token for accessing graph API (All the data which requires encryption must be the part of key vault)
5.	Kronos Solution – This is custom library project which is the part of Integration Service API. It will be used to query and submit data to Kronos WFC
6.	Application Insights – Capture necessary telemetry at the time of necessary events, and will be used by both the Configuration Web App, the Integration Service API

## Deployment
Following section explains necessary steps to deploy Shifts-Kronos Integration application

### Prerequisites
To begin with, you will need to ensure following perequisites:

1. Kronos WFC 8.1 - Access to Kronos WFC 8.1 System with following details:  
* Kronos WFC endpoint
* SuperUser Name
* SuperUser password  
* You will also need to be aware of the Kronos session timeout value as we use this when caching the session token  
Review and ensure users, org levels and jobs are properly setup in Kronos system

2. Microsoft Teams Shifts App - Access to Teams Deployment with Shifts App
* Tenant ID
* Tenant Admin credentials  
Review and ensure AAD users, Teams, and Scheduling Groups are properly setup in Teams Shifts App  

3. Microsoft Azure environment to host Shifts-Kronos Integration App - An Azure subscription where you can create the following resources:  
* App services
* App service plan
* Azure Table storage account
* Azure Blob storage
* Azure Key Value
* Application Insights

#### Configuration to Enable Syncing of Notes
Kronos requires a **Comment Text** value to be assigned to any comments or notes. This requires you to firstly configure each comment text before adding the chosen values in to the connector appSettings - this will be done when we deploy using the ARM template.

The following settings you must configure are:
   - **SenderTimeOffRequestCommentText** - Used for syncing time off request notes added by the requestor.
   - **ManagerTimeOffRequestCommentText** - Used for syncing time off request notes added by the manager. (Please note we do not currently support manager TOR note syncing, however plan to in the near future).

1. First Log in to Kronos as an admin and navigate to the Comments section under Setup -> CommonSetup.
![Comment Setup Screen](images/figure53.PNG)

2. Now click the **New** button to configure a new comment type. Add the value you want the comment text to be - this can be whatever you like. Next set a code number - again this is up to you. Finally ensure that you select the correct categories for the comment you are setting up and hit **Save**.
![Adding a New Comment Screen](images/figure54.PNG)

3. Repeat this for all of the comment types listed above.

### Register Azure AD Application
This integration app uses [Microsoft Graph APIs](https://developer.microsoft.com/en-us/graph) to access users (FLWs & FLMs) and teams and their schedules from Microsoft Teams Shifts App. To use Microsoft Graph to read and write resources on behalf of a user, this integration app needs to be registered in Azure AD by following steps below.  This is required to use Microsoft identity platform endpoint for authentication and authorization with Microsoft Graph.
1.	Log in to the Azure Portal for your subscription, and go to the "App registrations" blade here
2.	Click on "New registration” and create an Azure AD application
* **Name**: The name of your Teams app - if you are following the template for a default deployment, we recommend "Shifts-Kronos Integration"
* **Supported account types**: Select "Accounts in any organizational directory (Any Azure AD directory - Multitenant)"
* **Redirect URI based on ADAL / MSAL**: The URIs that will be accepted as destinations when returning authentication responses (tokens) after successfully authenticating users

![Azure AD Application Registration](images/figure1.png)

3. Click on the "Register" button
4. When the app is registered, you'll be taken to the app's "Overview" page. Copy the **Application (client) ID**; we will need it later. Verify that the "Supported account types" is set to **Multiple organizations**

![Azure Application Registration Overview page](images/figure2.png)

5. On the side rail in the Manage section, navigate to the "Certificates & secrets" section. In the Client secrets section, click on "+ New client secret". Add a description (Name of the secret) for the secret and select “Never” for Expires. Click "Add"

![Adding a new secret](images/figure3.png)

6. Once the client secret is created, copy its Value; we will need it later
7. Navigate to the Authentication page that can be found in the left blade in the figure under step 4.
8. Under the section that reads *Implicit grant*, make sure that the check boxes for Access tokens and ID tokens are checked. The screen should resemble something like the screenshot that follows:

![Ensuring the proper authentication settings](images/figure4.png)

At this point you have the following unique values:
* Application (client) ID
* Client secret
* Directory (Tenant) ID
* Managed Object ID

We recommend that you copy these values into a text file, using an application like Notepad. You will need these values later during the application deployment process.

### Microsoft Graph API Permissions
The table below outlines the required permissions necessary for the Azure AD application registration to successfully work end-to-end. These Graph API permissions should have their consent provided on the app registration:

**Table 1.** The list of Microsoft Graph API permissions.

|Scope|Application/Delegated|Function|
|-----|---------------------|--------|
|Group.ReadWrite.All|Delegated|Allows the application to create groups and read all group properties and memberships on behalf of the signed-in user (tenant admin)|
|WorkforceIntegration.ReadWrite.All|Delegated|Allows for workforce integrations to be created and registered with Microsoft Graph|
|offline_access|N/A|Enables for the Microsoft Graph token to be automatically refreshed|
|Schedule.ReadWrite.All|Application|Read and write all schedule items|
|User.Read|Delegated|Sign in and read user profile|
|User.Read.All|Delegated|Read all users' full profiles|

## Deploy Application to your Azure Subscription
Here are the following requirements to correctly deploy the **Shifts-Kronos Integration** application to your Azure subscription: 
1. An ARM Template published as part of the Microsoft GitHub package.
2. Ensure to **properly** fork the main Microsoft repo to your account. This helps in three ways:
   1. The forked copy does not impate the master branch in the Microsoft repository
   2. Having a forked copy of the main repository, tenant admins can deploy from the forked repo
   3. If there are changes that are required for your organization, you can always modify the code in your forked copy and re-deploy
3. GitHub package containing the code for the Configuration Web App and the Integration Service API
4. You will be prompted to click on the *Deploy to Azure* button below, and when prompted log in to your Azure subscription

[![Deploy to Azure](https://azuredeploy.net/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAndy65%2FMicrosoft-Teams-Shifts-WFM-Connectors%2Fmaster%2FKronos-Shifts-Connector%2FDeployment%2Fazuredeploy.json)  
5. It will navigate to the form for filling the template parameters  
6. The figure below shows the form once the *Deploy to Azure* button is clicked

![ARM Template form rendering in the Azure portal](images/figure52.png)

7. Select a subscription and a resource group - it is recommended to create a new resource group  
8. Fill in the values for the parameters of the ARM Template. They are defined in the table below:

**Table 2.** The ARM Template parameters
|Parameter Name|Description|
|--------------|-----------|
|baseResourceName|This is a standard name to be used across all resources|
|aadAppClientId|This is the Client ID from the app registration|
|aadAppClientSecret|This is the Client Secret from the app registration|
|managedAadAppObjectId|This is the AAD Object ID of the app registration|
|teamsTenantId|This is the Tenant ID of your Teams tenant, and it can be different than the tenant ID of the Azure sub where the Shifts-Kronos Integration package is deployed|
|firstTimeSyncStartDate|This is the start date of the first-time data sync between Kronos and Shifts App|
|firstTimeSyncEndDate|This is the end date of the first-time data sync between Kronos and Shifts App|
|location|This is the data center for all the resources that will be deployed through the ARM Template. Make sure that you select a location that can host Application Insights, Azure Table Storage, Azure Key Vault, and Redis Cache|
|storageAccountType|This is the storage grade for the Azure table storage account|
|sku|This is the payment tier of the various resources|
|planSize|The size of the hosting plan required for the API web app service and the Configuration Web App service|
|processNumberOfUsersInBatch|When syncing the shift entities between Kronos and Shifts App, the transfer is done based on users in a batch manner. The default value is 100 and can be changed at the time of deployment|
|processNumberOfOrgJobsInBatch|When syncing the open shift entities between Kronos and Shifts App, the transfer is done based on the org job paths in a batch manner. The default value is 50 and can be changed at the time of deployment|
|syncFromPreviousDays|The number of days in the past for subsequent syncs between Kronos and Shifts App|
|syncToNextDays|The number of days in the future for subsequent syncs between Kronos and Shifts App|
|futureSwapEligibilityDays|The number of days in the future to query when checking swap shift eligibility|
|correctDateSpanForOutboundCalls|The number of days in the past and future when it comes to having outbound calls for the Open Shift and Swap Shift Requests|
|numberOfOrgJobPathSectionsForActivityName|This is the number of org job path sections you want to appear as a Teams shift activity name (this is only used when syncing a shift transfer). <br /> Example: ./Contoso/UK/Stores/London/Checkout/Checkout Operator <br /> - A value of 2 would lead to shift transfer activities having a title of: _Checkout - Checkout Operator_ <br /> - A value of 1 would lead to shift transfer activities having a title of: _Checkout Operator_|
|managerTimeOffRequestCommentText|Used for syncing time off request notes added by the manager. (Please note we do not currently support manager TOR note syncing, however plan to in the near future)|
|senderTimeOffRequestCommentText|Used for syncing time off request notes added by the requestor|
|kronosUserName|The Kronos WFC SuperUser name|
|kronosPassword|The Kronos WFC SuperUser password|
|gitRepoUrl|The public GitHub repository URL|
|gitBranch|The specific branch from which the code can be deployed. The recommended value is master, however, at the time of deployment this value can be changed|

8.	Agree to the Azure terms and conditions by clicking on the check box *I agree to the terms and conditions stated above* located at the bottom of the page
9.	Click on *Purchase* to start the deployment
10.	Wait for the deployment to finish. You can check the progress of the deployment from the *Notifications* pane of the Azure portal
11.	Once the deployment has finished, you would have the option to navigate to the resource group to ensure all resources are deployed correctly
12. Upon a successful deployment you would be presented with a screen that shows a large green check mark and a phrase similar to *Your deployment completed successfully*
13. Smoke test – this step is required to ensure that all the code has been properly deployed

* *Note*: With the baseResourceName, the minimum length is 3 characters, maximum length is 24 characters, and it should contain only numbers and letters. Therefore, it is recommended to use a baseResourceName at 8 characters because the names of the resources that are being deployed via the ARM Template will contain strings such as `-logicApp` for the Azure Logic App; `-api` for the Integration Service API; `-config` for the Configuration Web App; etc..

## Post ARM Template Deployment Steps
The following actions are to be done post deployment to ensure that all the information is being exchanged correctly between the resources in the newly created resource group:
1.	Access Policy Setup in Azure Key Vault
2.	Setting up the Redirect URIs
3.	Logout URL setting in App Registration
4.	Uploading Excel files into Azure Blob storage
5.	User creation through the Teams Admin portal
6.	Establishing the necessary recurrence for the Azure Logic App

### Access Policy Setup in Azure Key Vault
Once the ARM Template deployment succeeds, it is important that you still add one more access policy to the Azure KeyVault resource. This time, the policy is to allow you as the Azure Administrator to properly monitor the Azure KeyVault. In order to setup your administrative access to the Azure KeyVault follow the steps below: 
1. Log on to the Azure portal
2. Navigate to the resource group that has been created as part of the ARM template deployment

![Resource group created as part of the ARM Template](images/figure29.png)

3. Navigate inside of the KeyVault resource. You should search for an icon that has a key inside of a circle as shown in the screenshot in the previous step.

![KeyVault resource](images/figure31.png)

4. Once you navigate to the KeyVault resource, on the left hand side in the above screenshot, find an option that reads *Access policies*. This is where you will modify the access policies so that you as an admin can be able to change other access policies as required. Click on the option that reads *Access policies*. Your screen should resemble like something below:

![Current Access policies on the Azure KeyVault](images/figure33.png)
5. The screen that you would see will definitely be different than the one shown here. However, you should still be able to see the option that reads *Add access policy*.  
6. Click on that link and you should be taken here:

![Adding the Azure KeyVault access policy](images/figure34.png)
7. Follow the below instructions before clicking on the *Add* button in blue  
   * Under the label, *Configure from template (optional)* click the dropdown and you should see the following screenshot:

![Selecting the template for setting the access policy](images/figure35.png)
   * Select the first value in the above screenshot.

8. Once you have selected the value that reads *Key, Secret, & Certificate Management*, the screen should resemble something like the following:

![Selecting the template](images/figure36.png)
9. In the above figure, the next step is to select the principal. In this case, the principal is in fact your Azure admin account. Proceed further by clicking on the red star under the label that reads *Select principal*. There should be a rectangular window that opens to the right hand side.

![Searching for the principal](images/figure37.png)
10.  In the text box on the right hand side of the screen, type your Azure admin account email.

![Searching for the Tenant Admin account](images/figure38.png)
11.  From the screenshot above, make sure to select the account that matches the string that you input to search. *Note*: It is recommended to search using the email address as opposed to the name of the account.  
12.  Once you have selected the account, your screen should resemble the below:

![Selecting the Azure admin account](images/figure39.png)
13. On the bottom right hand side of the screen, click on the button in blue that reads *Select*. Your screen should then look like this:

![Prior to clicking the Add button](images/figure40.png)
14.   Now click on the *Add* button, and you would be automatically redirected to the page which is shown in step 4. However, now you would see 4 access policies which would be similar to what is shown below:

![All Azure KeyVault access policies are configured](images/figure41.png)
*Note*: There may be a difference in the names of the applications and the app registration. The main purpose is to ensure that there are a total of ***3*** access policies that are configured.

### Setting up the Redirect URIs
1. Once the ARM Template deployment is successful, there would be an output screen that will show the necessary URL for the Configuration Web App service. Copy that URL into an application such as Notepad  
2. Navigate to the App Registration created earlier as noted in the section *Register Azure AD Application*.
3. In the screenshot under step 4 in the section *Register Azure AD Application*, click on the text that reads *Redirect URIs*.
4. There is a chance that the screen may not have any redirect URIs. You would need to set those now. 

![Redirect URIs already set](images/figure5.png)

5. You need to properly take the Configuration Web App service URL that is deployed as part of the ARM Template deployment and paste that URL here
6. Subsequently, you need to add a new URL; paste that same URL that was copied from the outputs of the ARM Template deployment, and append “/signin-oidc”. By doing so, the tenant admin when logging into the Configuration Web App, will be authenticated using OpenIdConnect 
7. Once all the changes are made, ensure to commit the changes through clicking on the button that reads Save

### Logout URL setting in App Registration
1.	Log on to the Azure portal
2.	Navigate to the application registration recently created (refer to the screenshots above)
3.	On the left-hand side, click on the option that reads Manifest
4.	In the code window that appears, scroll down until you read the JSON attribute: logoutUrl
Refer to the screenshot:

![Application Registration Manifest window](images/figure6.png)

5.	The value for the logoutUrl is the URL of the Configuration Web App service that was deployed through the ARM Template
6.	Copy and paste the URL from step 5 as the value for the logoutUrl
7.	Once the changes have been made, the save button at the top of the screenshot in step 4 will transition from a disabled state to an enabled state
8.	Click on the Save button to properly commit the changes made

### Uploading Excel Template files into Azure Blob Storage
Once the ARM Template deployment is successful, one final operation is to ensure that the Excel template files are uploaded to the Azure Blob storage that has been provisioned through the ARM Template. The steps below outline the procedure:

1.	Log onto the Azure portal, and sign in using your administrator credentials
2.	Navigate to the resource group that was created at the time of ARM Template deployment
3.	Navigate to the storage account that was created from the ARM Template deployment. The screen should resemble the figure below: 

![Storage account overview](images/figure7.png)

4.	Navigate into the containers, by clicking on the link that reads *Containers* from the figure above
5.	Upon navigation to the containers, the ARM Template should provision a blob container called “templates”, and the screen should resemble below:

![Templates blob container](images/figure8.png)
6. Navigate inside of the "templates" blob container, and the screen should resemble the next screenshot below: 

![Navigation inside of the templates blob](images/figure9.png)

7. Initially you would see an empty container. That is due to the fact that you would need to get the necessary files uploaded.
8. On GitHub, there is a sub-directory within the `Kronos-Shifts-Connector` folder called `Excel Templates`. Navigate to that folder, and for additional reference, see the screenshot below: 

![Excel Templates directory on GitHub](images/excel-templates-folder-GitHub.png)

9.  Download both Excel files from the above location.
10. Navigate back to the Azure portal, to the screenshot in step 5, and upload the Excel files accordingly. 

### User Creation through the Teams Admin Portal
1.    Navigate to the [Microsoft Teams Admin Portal](https://admin.teams.microsoft.com)
2.	Sign In with your AAD Tenant Admin credentials
3.	You will be presented with the following view once the sign in is successful:

![Home page of the Teams Admin portal](images/figure10.png)

1. From the figure above, navigate to the Users page by clicking on the option that reads *Users* in the left hand blade. The screen should resemble the following below:

![The users landing page](images/figure11.png)

5. In the text above the table, there is a mention of "Admin center > Users". That is the location where you should go to add users to the tenant. Clicking on the "Admin center > Users" hyperlink in the screenshot above should yield in the page below:

![The page to add or remove users](images/figure12.png)

6. In order to add users, the screenshot in the previous step has a label that reads *Add a user*. Clicking on that label will show a form where you can enter in all the details.

### Setting up the Recurrence and Concurrency for the Azure Logic App
In this section you are going to set 2 aspects of the logic app: recurrence and the concurrency. The reason for setting the recurrence is to have a repeated occurrence of the data sync between Kronos WFC and Microsoft Shifts to happen on a specific interval of time. The reason for setting the concurrency is to prevent the logic app from running multiple times within a specific time window.

1.	Navigate to the resource group that has been create through the ARM Template Deployment
2. Click on the resource which has the text `-logicApp` appended at the end of the name. Your screen should resemble the following:

![Logic app in resource group](images/figure42.png)

3. Navigate to the logic app, and you should be redirected to the following view:  
![Logic app home page on the Azure portal](images/figure43.png)

4. From the screenshot in the previous step, select on the *Edit* label. You should be able to find that label next to a pencil icon. Once you click that, you should then be redirected to a user interface that contains 1 step like the screenshot that follows:

![Logic App Designer](images/figure44.png)

5. Here, you will search for *Recurrence*, as defined below. You will need to type in the text box where the text reads *Search connectors and triggers*.

![Searching for recurrence](images/figure45.png)

6. Click on *Recurrence* as shown in the previous step, and you would be shown a view like so:  
   

![Populating the recurrence schedule for the logic app](images/figure46.png)

7. Next, provide a value for the interval and let the value of frequency be minute. It is recommended to provide the value of 15 such that the logic app will run 4 times within the span of 1 hour. Also, having 15 minute intervals allows for complete syncing of data.

![Establish the 15 minute intervals for the logic app](images/figure47.png)

8. Now, to set the concurrency - in the blue rectangle which reads *Recurrence*, click on the ellipses at the top right hand corner. See below:

![Clicking on the ellipsis](images/figure48.png)

9. Next, click on the option that reads *Settings* (which can be seen in the above figure). You would be shown the following:

![Establishing the concurrency](images/figure49.png)

10. Under the *Concurrency Control* subheader, there is a toggle button near the text that reads *Limit* and switch it from *Off* to *On*  

![Turning on the concurrency control](images/figure50.png)

11. Finally, change the slider value from the above screenshot, from 25 to 1. This will then allow for having 1 run of the logic app to happen every 15 minutes.

![Setting the value for the concurrency control](images/figure51.png)

12. From the above screenshot, click on the *Done* button, and at which point, in the top left corner, under the text that reads *Logic App Designer*, the *Save* button will also be enabled.
13. Click on the *Save* button to commit all the changes.

## Configuration App Workflow
The Configuration Web App serves as a helpful aid to establish the necessary configurations to properly integrate an instance of Kronos WFC v8.1 with Shifts app. It further helps to create mapping between Kronos and Shifts users as well as Kronos departments and Shifts teams.

### Step 1: Capture Kronos WFC Settings and register Shifts Workforce Integration
Access Config Web App. Sign in using your Azure AD Tenant Admin account. 
On successful sign-in you will see home page as shown below. Perform following steps on this page:
* Enter Kronos WFC endpoint, SuperUser name and password and click Submit button. It will validate and store the details in Azure storage
* Click Register link for Workforce Integration Registration, it registers application with Shifts Workforce Integration
By registering a Workforce Integration with Microsoft Graph, Shifts APIs can make real-time outbound calls when an FLW requests to take an Open Shift, or when two FLWs are requesting to Swap Shifts with each other. Without registering a Workforce Integration with Microsoft Graph, Shifts APIs will not make outbound calls, nor would any data be synced from Kronos WFC into Shifts.

![Home page of the Configuration Web App](images/figure13.png)

### Step 2: Kronos users to Shifts users mapping
You can access users mapping screen which will help you map users between Kronos and Shifts. Perform following steps on this page:
1.	Click Export button which downloads the kronos_shift_usermapping.xlsx file to your machine. The Excel file contains the following tabs:  
      1. “Kronos Users” tab contains KronosOrgJobPath, KronosPersonNumber, KronosUserName  
      2. "Shift Users" tab contains ShiftAADObjectId, ShiftUserDisplayName, ShiftUserPrincipalName
2.	Click on the Download Template button. It will download KronosShiftUserMappingTemplate.xlsx to your machine.
3.	Copy user details from kronos_shift_usermapping.xlsx to KronosShiftUserMappingTemplate.xlsx. Ensure user details from both Kronos and Shifts are mapped correctly in this file. Review and ensure the following:  
    1. All the columns are correctly populated 
    2. All users are correctly mapped
* It is a recommended tip that the display names of the users in AAD and in Kronos WFC are matching. For example, if a user in Kronos has their name formatted as LastName, FirstName (i.e. Benson, Sara), then the AAD name structure should be the same as well (i.e. Benson, Sara).
* No missing values
* No duplicate rows
4. When users have been successfully imported, the tenant admins can be able to delete a user mapping through click on the button that reads Delete in the event an erroneous mapping has been established. To re-establish the correct mapping, you can reimport the template and duplicate records will not be imported.

![User to User Mapping Screen](images/figure14.png)

### Step 3: Kronos departments to Shifts teams mapping
Access Kronos departments to Shifts teams mapping screen to map departments to teams:
1.	Click on the Export button. This action downloads the teams_department_usermapping.xlsx file on your machine which includes following details
      1. “Kronos Details” tab contains – KronosOrgJobPath, KronosWorkforceIntegrationId as below

![Sample screenshot from the exported Kronos departments](images/figure15.png)

   2. Need to create the shift teams and scheduling group using above path where second last value in each row is the team name and last value in each row is scheduling group name.

E.g. In below OrgJobPath **Frontend** will be a team name and **Cashier** will be the scheduling group name. Corporate/Grocery/Region 1/District 1/Store 0404/**Frontend/Cashier**.  Navigate to your [Teams instance](https://teams.microsoft.com/) and go to Teams tab, click on Join or create a team, and create a team with above specified name.

![Screenshot for creating a team in Teams](images/figure16.png)

3. Provide the name of the team

![The Teams creation window](images/figure17.png)

4. After successful team creation add the members in the team as per Kronos OrgJobPath  

![User interface in Teams to add users to a team](images/figure18.png)

5. Navigate to Shifts, app create the Schedule of the above created team as shown below:
   1. Log on to Microsoft Teams
   2. Navigate to the Shifts app as shown in the screenshot below:

![The main UI in Teams, then logging into the Shifts app](images/figure19.png)  

6. Once you navigate into Shifts, you can view all the schedules for an entire team or create a new schedule for a team. The figure below shows the schedules that are available for teams that have already been created. To get to that screen, follow the steps below:  
   * In the Shifts app, if you are creating a schedule for a team for the first time, you would be presented with a screen that lists out all the teams in your Teams instance. The screenshot would be like the one below:

![Creating a new schedule for a team](images/figure20.png)

Note: If you have created teams and schedules before, make sure in the figure below, make sure to scroll all the way to the bottom and click on the option that reads New schedule, there should be a plus icon there.

* Once you click on create from step 6, you will be presented with another screen that will enable you to set the time zone for the schedule

![Setting the time zone for the schedule of the team](images/figure21.png)

It is important to note that the time zone should be the same as the time zone of the organisation location in Kronos WFC that you intend to map the team to. This way when entities (i.e. Open Shifts, Time Off, Shifts) are synced, the start and end times are showing the same time in both systems.

* After selecting the time zone and the closest city, proceed to click on the confirm button. You would be taken to a screen that looks like the screen grab that is annotated with the text: *Viewing all schedules*.  

Viewing all schedules  
![Shifts user interface to view all the schedules](images/figure22a.png)

![Creating the scheduling groups in Shifts user interface](images/figure23.png)

Also add the members of corresponding scheduling group by referring the user to user mapping Excel. E.g. Adams, Donald should be added to the Technician scheduling group under the Pharmacy team.

![A Kronos User and the Org Job Path which identifies the user](images/figure24.png)

Note: OrgJobPaths having same second last value are the same teams with different group name (Scheduling Group).  
g.	After creating all the teams, schedules and scheduling groups again download 
teams_department_usermapping.xlsx file on your machine which includes following details  
h.	“Kronos Details” tab contains – KronosOrgJobPath, KronosWorkforceIntegrationId  
i.	“Shift Team Details” tab contains - ShiftTeamId, ShiftTeamDisplayName, SchedulingGroupId, SchedulingGroupDisplayName

1. Click on *Download Template* button. This action downloads the KronosShiftTeamDeptMappingTemplate.xlsx
2. Copy the details from teams_department_usermapping.xlsx file to KronosShiftTeamDeptMappingTemplate.xlsx. Ensure details from both Kronos and Shifts are mapped correctly in this file. Review and ensure following:  
   a.	All the columns are correctly populated. In particular the KronosTimeZone column has been set to the correct standard name of the time zone appropriate for each mapped location. Note, the connector like the Teams Shifts app itself can only support a single time zone per Team/location but as the spreadsheet also contains separate rows for each scheduling group within a Team it will be necessary to duplicate the same time zone name for all such groups.  
   b.	All values are correctly mapped  
   c.	No missing values  
   d.	No duplicate rows. 

*Note*: A list of all supported timezones and their standard names can be found here: [TimeZones](timezones.md)

*Note*: If a team has not been mapped (i.e. Meatpacking team), and the users under the Meatpacking team have been mapped, the data in Kronos WFC for the Meatpacking department will not be synced to the Meatpacking team on Shifts. In other words, those users will be skipped during the sync operations.

![Teams to Department Mapping screen](images/figure25.png)

*Note*: For the Teams and Department Mappings, you should create the teams and the scheduling groups first in the Shifts UI if the number of teams in your Azure AD tenant is less than 10. If on the other hand, the number of teams in your Azure AD tenant is greater, go [here](#automating-the-process-to-create-scheduling-groups) for the necessary instructions.

#### Automating the process to create scheduling groups
It is important to note that you would need to create the necessary scripts using PowerShell or a scripting language that you are comfortable. Also, another key prerequisite, make sure to use the ClientID from earlier to be able to obtain the necessary Graph API token. 

1. Get the list of team IDs that you are a part of. You can find the documentation [here](https://docs.microsoft.com/en-us/graph/api/user-list-joinedteams?view=graph-rest-beta&tabs=http), and the necessary API reference is listed below:

`GET https://graph.microsoft.com/beta/me/joinedTeams`

The permissions that are required for this API are in Table 3 below.

**Table 3.** Required Graph API permissions
Permission type|Permissions (from least to most privileged)
----------------|------------------------
|Delegated (work or school account)|User.Read.All, User.ReadWrite.All|
|Delegated (personal Microsoft account)|Not supported.|
|Application|User.Read.All, User.ReadWrite.All|

*Note* - With the above permissions, please ensure that these permissions are listed with your Azure AD application registration.

2. Obtain a list of the user IDs to be added into the new scheduling group. You can get the list of users from the User to User Mapping exported Excel file as a sample source. The documentation reference is found [here](https://docs.microsoft.com/en-us/graph/api/group-list-members?view=graph-rest-beta&tabs=http). The necessary API reference is also found below: 

`GET https://graph.microsoft.com/beta/groups/{id}/members` where `{id}` is to be substituted with a groupId from the previous step. In the table below, **Table 4**, you will see the Graph API permissions that are required:

**Table 4.** Permissions required for this Graph API.
Permission type|Permissions (from least to most privileged)
----------------|------------------------
|Delegated (work or school account)|User.ReadBasic.All, User.Read.All, Group.Read.All, Directory.Read.All|
|Delegated (personal Microsoft account)|Not supported.|
|Application|Group.Read.All, Directory.Read.All|

3. Create the scheduling group by passing in the necessary team ID and the user IDs. The API documentation can be found [here](https://docs.microsoft.com/en-us/graph/api/schedule-post-schedulinggroups?view=graph-rest-beta&tabs=http), and find the API reference: `POST https://graph.microsoft.com/beta/teams/{teamId}/schedule/schedulingGroups`

The body of the request would be defined as follows:

```json
{
   "displayName": "SampleSchedulingGroupName",
   "isActive": true,
   "userIds": [
      "userId1",
      "userId2",
      "userId3"
   ]
}
```
Replace the `SampleSchedulingGroupName` with the scheduling group name which would be the job code or the last labor level in the Org Job Path. The required permissions are shown in **Table 5**. 

**Table 5.** Required Graph API permissions.
Permission type|Permissions (from least to most privileged)
----------------|------------------------
|Delegated (work or school account)|Group.ReadWrite.All|
|Delegated (personal Microsoft account)|Not supported.|
|Application|Schedule.ReadWrite.All|

*Note*: Anytime that users are to be added or removed, there would need to be a PUT call made. [Here](https://docs.microsoft.com/en-us/graph/api/schedulinggroup-put?view=graph-rest-beta&tabs=http) is the documentation for the resource, and the following is the exact HTTP call:

`PUT https://graph.microsoft.com/beta/teams/{teamId}/schedule/schedulingGroups/{schedulingGroupId}` where the teamId is obtained from step 1, and the schedulingGroupId is obtained from the previous step. The schema for the PUT call is shown below:

```json
{
  "displayName": "Cashiers",
  "isActive": true,
  "userIds": [
    "c5d0c76b-80c4-481c-be50-923cd8d680a1",
    "2a4296b3-a28a-44ba-bc66-0274b9b95851"
  ]
}
```
Once the necessary steps have been done, and you will be able to continue mapping the teams and departments between Shifts and Kronos WFC. 

### Step 4: Configure Kronos WFC

In the current version of the connector, it is necessary to make the following Configuration changes in Kronos WFC:

1. The connector makes calls to the Kronos WFC API using a fixed date format: **M/dd/yyyy**, therefore it is necessary to configure the super user account supplied at deployment time with a locale with the specified date format.
2. When synchronizing shifts, open shifts and time off requests the connector requests data from Kronos with the following request sub-types:
   	1. **Shift Swap Request** (for Shift Swap Requests)
    	2. **Open Shift - Request** (for Open Shift Requests)
    	3. **TOR** (for Time Off Requests)
3. When submitting shift swap requests the connector uses a specific comment type which must be created in Kronos with the name **Other reason**

### Step 5: Perform first time sync

Click on the Done button in Team to Department Mapping screen, which will initiate following workflows:  
a. Kronos to Shifts - User sync
b.	Kronos to Shifts – Open Shift sync  
c.	Kronos to Shifts – Approved or Declined Open Shift Request sync  
d.	Kronos to Shifts – Approved or Declined Swap Shift Request sync  
e.	Kronos to Shifts – Time Off Reason sync  
f.	Kronos to Shifts – Approved or Declined Time Off Sync  
g.	Shifts to Kronos – Time Off Request sync  
h.	Kronos to Shifts – Shifts sync

![Team to Department Mapping screen with Done button](images/figure26.png)

The first-time sync will be done using the parameters of *firstTimeSyncStartDate* and *firstTimeSyncEndDate* which have been defined in **Table 2**: ARM Template parameters. These are the dates that the Configuration Web App will sync the data from Kronos into Shifts. Subsequent data sync operations are handled through the logic app, and explained in the following section: *Data Sync through Logic App*. 

# Data Sync through Logic App
The ARM Template provisions the Azure logic app, and the Azure logic app will execute based upon the sync frequency, sync interval, and sync hour chosen by the tenant admin. There are couple key differences to note between syncing data via the done button, and syncing the data via the Azure logic app. When the logic app is triggered on a scheduled interval, the time period for syncing data will be automatically calculated using the parameters syncFromPreviousDays and syncToNextDays from config file which represent the number of days in the past, and the number of days in the future respectively. The point of reference for the calculations will be based on the current date at which the logic app is executing.
FLW requests (Open Shift Request, Swap Shift Request, and Time Off Request) will be synced from the Shifts App to Kronos in synchronous manner using Shifts Outbound APIs and Kronos WFC 8.1 data submission (POST) APIs.

# Telemetry
Shifts-Kronos Integration application utilizes Azure Application Insights to capture the necessary events and errors. It captures following properties:
* Response from Kronos and Graph APIs – capturing necessary Id values.
  * Success
  * Errors
* Events (sync from Kronos to Shifts App, real-time outbound from Shifts App to Kronos)
  * Method names
    * Parameters passed to methods
  * Timestamps (at the time when events happen)  
* Data properties (i.e. any IDs that are of interest to Graph API calls, or local data retrieval)  
* Tenant ID – where applicable  
* Outbound calls from the Shifts App to the Integration API Service

# Troubleshooting
The following are common issues that tenant admins may encounter while following these steps to deploy the Shifts-Kronos Integration application:
* Problems while deploying to Azure

|Issue|Solution|
|-----|--------|
|Duplicate resource names|Ensure that at the time of deployment, the necessary resources have unique names.|

* Problems while using Configuration Web App, or logic app (data does not sync, most likely due to Graph token expiration)

|Issue|Solution|
|-----|--------|
|Graph token expiration - 401 Unauthorized|The Graph token may expire, and to resolve that: 1. Log out of the Configuration Web App. 2. Log back into the Configuration Web App.|

* Problems while creating Open Shifts
![Problems while creating Open Shifts](images/figure27.png)

|Error|Reason|
|-----|------|
|Sorry your change couldn't be completed|Above scenario may happen due to Kronos business rules: Ex: the number of working hours for the user should not be more than 40 hours for current week otherwise Kronos does not allow to create / submit the open shift request. If such request is initiated, the Workforce Integration sends error to Shifts and Shifts would not allow to submit such open shift request  **Solution:** FLW to choose and submit open shift requests after ensuring number of working hours quota for given duration|

* Problem while creating swap shift requests
![Problem while creating swap Shifts](images/figure28.png)

|Error|Reason|
|-----|------|
|Sorry your change couldn't be completed|Swap Shift Request creation/submission success depends upon business rules on Kronos side so FLWs need to be aware of those before requesting Swap 1. Swap shift is not possible for past date in Shifts. If such request is initiated from Shifts, the Workforce Integration sends error to Shifts. Shifts will display a generic error message 2. Swap shift is not allowed in Kronos if user already has same shift as the requested shift. The Workforce Integration sends error to Shifts. Shifts will display a generic error message. 3. If User1 has requested User2 for a swap shift and user2 has requested the same shift to User 3, then one of the requests will get approved and other will be declined|


|Issue|Reason|
|-----|------|
|Sync not occurring| If nothing is syncing, please launch the Configuration app and reimport your user to user mapping table. |

* Conflict due to source code deployment failure - There are possibilities that the underlying Azure deployment engine may not be able to properly deploy the source code from GitHub into the necessary Web App services. Navigate [here](#continuous-deployment-in-azure-app-services) to properly fix such issues.

* Problem when using the Configuration Web App and/or Integration Service API

|Issue|Solution|
------|--------|
|The specified CGI application encountered an error and the server terminated the process|This issue can be resolved by restarting the Configuration Web App and/or the Integration Service API. The instructions are found [here](#continuous-deployment-in-azure-app-services)|

# Continuous Deployment in Azure App Services
In order to ensure you have the latest build/changes to the app services, you would need to have the code updated for both the Configuration Web App, and the Integration API Service applications. To get the latest changes, please follow the instructions below:
1. Log in to the Azure Portal for your subscription.
2. Navigate to resource group where you have deployed the Shifts-Kronos Integration resources, and you should see something similar to the screengrab below:

![Sample resource group](images/sample-resource-group.png)

3. Navigate to the resource which has the `-config` string appended to the base resource name. In the screenshot above, `k2shifts-config`. When doing so, you should see the below screenshot:

![Configuration app overview](images/config-app-overview.png)

4. On the left hand side of the screenshot in the above step, under the *Deployment* section, there is a label that reads *Deployment Center*. Once you navigate into the deployment center, your screen should resemble the below:

![Deployment center overview](images/deployment-center-overview.png)

5. In the screenshot in the previous step, there is a button that reads *Sync*. Please click on that button, and it will initiate the sync from GitHub and deploy the code.  
6. Once the code has synced successfully, navigate to the overview, which is shown in Step 3.
7. Make sure to click on the button that reads *Restart* in the screenshot in step 3.
8. Repeat the above steps 1-7 for the resource that has the string `-api` appended to the base resource name.

# Best Practices
The following tips are recommended as best practices when it comes to deploying the Shifts-Kronos Integration package from GitHub:
1.	One instance of Kronos WFC corresponds to one instance of Teams
2.	Fork the main Microsoft repo for the following reasons:  
    * Any custom changes that are required, can be made on the forked copy of the Microsoft repository  
    * Easy to deploy changes from the forked copy of the Microsoft repo on to Azure subscription

# Legal notice

Please read the license terms applicable to this [license](https://github.com/OfficeDev/Microsoft-Teams-Shifts-WFM-Connectors/blob/master/LICENSE). In addition to these terms, you agree to the following: 

* You are responsible for complying with all applicable privacy and security regulations, as well as all internal privacy and security policies of your company. You must also include your own privacy statement and terms of use for your app if you choose to deploy or share it broadly. 

* This template includes functionality to provide your company employees with HR information, and it is your responsibility to ensure the data is presented accurately. 

* Use and handling of any personal data collected by your app is your responsibility. Microsoft will not have any access to data collected through your app, and therefore is not responsible for any data related incidents.

# Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com. 

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA. 

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact opencode@microsoft.com with any additional questions or comments. 
