# WFM Teams Adapter Architecture

## Introduction

At the heart of the WFM Teams Adapter is an Azure Functions application that utilizes the serverless capability of functions applications to scale out to as many servers as required in order to process the current workload. This architecture has been proven to scale well under load for a large retail organisation in the UK synchronising more than 600 Teams  serving more than 59,000 users.

![01-WFM Teams Adapter Reference Architecture](images/01-WFM%20Teams%20Adapter%20Reference%20Architecture.png)

### WFM System & API

The WFM system is any enterprise application that can manage a client's workforce and provides labour scheduling capabilities. The extent to which the functionality provided by the Teams Shifts Application can be supported by the Adapter is entirely dependent on the richness of the WFM API layer as this is the sole means by which the adapter communicates with the WFM system.

### Teams Shifts Application

The teams shifts application is an enterprise scale application hosted in the Microsoft Teams client (web, desktop & mobile) providing the following capabilities:

- Shifts Management
  - view, add, edit, delete, unassign
  - shift swap request & approval
  - offer shift request & approval
- Open Shifts Management
  - view, add, edit, delete, assign
  - open shift request & approval
- Time Off Management
  - view
  - time off request & approval
- User Shift Preferences
  - synchronisation from WFM
  - user edit
- Time Clock
  - start/end shift
  - start/end break

### Azure Functions

As mentioned the whole synchronisation and workforce integration is handled by the functions application which has a number of triggers, orchestrators, sub-orchestrators and activity functions driving the whole process of synchronising the supported data items from the WFM system to the Teams Shifts Application as well as handling changes in the Teams Shifts App and applying those changes to the WFM system (if supported). 

### Azure Storage

Table storage is used to store details of the connection between a Team within Microsoft Teams and a business unit within the WFM system. 

Blob storage is used to store the details (including mapped ID's) of all entities successfully synchronised to the Teams Shifts Application.

### Azure KeyVault

KeyVault is used to store the credentials of the API user used when making requests to the WFM API's.

### Azure Cache for Redis

This distributed in-memory cache is used to store transient data that is frequently accessed by the various activity functions including:

1. Mapped user details
2. WFM Authentication Tokens

The transient data is kept fresh with separate orchestration functions.

### Azure AD

The adapter uses application permissions when communicating with the Microsoft Graph API and these are defined in an App Registration in Azure AD.

### Microsoft Graph API

Data is synchronized to the Teams Shifts Application exclusively using the Microsoft Graph API.

## Application Components

The components comprising the solution are as follows:

![02-WFM Teams Adapter Components](images/02-WFM%20Teams%20Adapter%20Components.png)

Other than the functions app itself, the other two key components are the WFM Provider Implementation component and the Microsoft Graph component both of which provide the interfaces with the two external systems being connected.

### WFM Provider Implementation

In order for the adapter to be able to communicate with the WFM Provider in an agnostic way, it is necessary for the WFM Provider component to implement the following interfaces defined in the WFMTeams.Adapter core component:

- **IWfmDataService** - this interface contains a number of methods used to request data required by the Adapter from the WFM Provider
- **IWfmActionService** - this interface contains a number of methods used to push changes from the Teams Shifts Application to the WFM Provider
- **IWfmConfigService** - this interface has a single ConfigureServices method that passes the services collection and configuration root object from the functions app and allows the provider implementation to register its own dependencies and access its specific settings from the collection of functions app settings.

## Orchestrations

All synchronisations from the WFM Provider to Teams are controlled by Durable Orchestration Functions in the adapter with separate orchestrators for Shifts, Open Shifts, Time Off and Availability with two additional orchestrators being used to maintain the transient employee and employee token data in the Redis cache.

The whole process is controlled by a single timer trigger which fires once per minute and has all the logic to work out which orchestrators need to be triggered for which teams according to the various synchronisation intervals specified for them.

![03-WFM Teams Adpater Orchestrations](images/03-WFM%20Teams%20Adpater%20Orchestrations.png)

### Timer Trigger Logic

To maximise the capability of functions applications to scale in/out the synchronisation is performed using orchestration functions executed in parallel as per the following:

1. Test the SuspendAllSyncs app setting and if true, exit doing nothing, else continue with step 2.
2. Fetch the full list of teams to be synchronised
3. Filter the list to only those teams that are enabled for synchronisation (individual teams can be switched on or off as necessary)
4. Execute the EmployeeCacheOrchestrator for all Teams where this is due according to the EmployeeCacheFrequencyMinutes app setting to refresh the mapped employee data in Redis.
5. Execute the ShiftsOrchestrator, OpenShiftsOrchestrator, TimeOffOrchestrator, AvailabilityOrchestrator and the EmployeeTokenRefreshOrchestrator, if the features are enabled, in parallel.

### Execution Spread Logic

In order to ensure that the synchronisation work is spread out evenly over each synchronisation interval the following logic is adopted for each orchestrator separately:

1. Calculate the pending time being the current time less the synchronisation interval
2. Get the list of pending teams i.e. teams whose last execution time for the orchestrator is less than or equal to the pending time sorted in last execution date order ascending.
3. Compute the maximum number of orchestrators that should be started being the count of enabled teams divided by the sync interval for the orchestrator. If the number of pending teams exceeds this maximum take the maximum from the pending.
4. For each remaining team in the pending list, test the status of the orchestrator and only if it is Completed, Failed, Canceled or Termininated is the orchestrator started and the last execution date updated.

## Example Orchestration Process: Shifts Orchestrator

The work of any single orchestrator is divided over a number of configurable past and future weeks (if appropriate) using sub-orchestrators to manage the work for the specific week. Weeks were selected as the unit of work as businesses typically schedule employees on a week by week basis. 

![04-WFM Teams Adapter Shifts Orchestrator](images/04-WFM%20Teams%20Adapter%20Shifts%20Orchestrator.png)

The sub-orchestrators repeatedly call the activity functions until the activity function returns that all work for the week has been completed. This allows the activity function to execute partial jobs and ensures that the execution time of the activity does not exceed the maximum allowed by the functions runtime.

## Example Sync Activity: Shifts Week Activity

Most sync activity functions operate in the same way as specified in the following diagram for the shifts week activity:

![05-WFM Teams Adapter Sync Activity Process](images/05-WFM%20Teams%20Adapter%20Sync%20Activity%20Process.png)

1. Fetch the appropriate records from WFM
2. Fetch the cache of records successfully synced to Teams Shifts
3. Compare the two sets of records to identify new records that new to be created in Teams Shifts, existing records that need to be updated in Teams Shifts and records that are no longer in WFM that now need to be deleted from Teams Shifts.
4. If there are changes, limit the number to be processed in this execution to the configurable maximum (in order to ensure that the activity does not time out).
5. Apply the remaining changes to Teams Shifts.
6. Update the cache of records for the changes successfully applied to Teams Shifts (in this way the cache represents the current state of the records in Teams Shifts).
7. If a subset of changes was taken in 4 return the fact that the processing is not complete to the sub-orchestrator so that it can call the activity again until all records have been processed.

## Workforce Integration

As well as syncing from WFM to Teams, the adapter also registers for workforce integration with Teams and at the time of writing, specifically supports the following integrations:

1.  Shifts*
2.  Open Shifts*
3.  Shift Swap
4.  Open Shift Request
5.  Open Shift Assignment
6.  User Shift Preferences

\* Note: there is currently no support for add, update and delete operations for these entities

Future integrations to be supported include

1. Offer Shift
2. Time Off Requests
3. Time Clock

For each type of integration, Teams calls a single endpoint in the adapter passing an encrypted JSON Batch payload. The adapter decrypts the payload and then iterates through the collection of handlers to identify the one that is written to handle that specific integration. The handler converts the Teams Shifts ID's into WFM ID's by looking them up in the cached records and then delegates the actual work of updating the WFM system to the WFM connector.

![06-WFM Teams Adapter WFI Handling](images/06-WFM%20Teams%20Adapter%20WFI%20Handling.png)

## Authentication

### Microsoft Graph Authentication

The adpater uses Application Permissions exclusively when calling the Microsoft Graph API which requires an App Registration in Active Directory with the following Application API Permissions assigned:

Group.ReadAll

Schedule.ReadWriteAll

Users.ReadAll

UserShiftPreferences.ReadWriteAll

The ClientId and ClientSecret from the App Registration are used to obtain an access token which is retained until it expires whereupon a new one is requested.

## Logging

All logging is done to Azure Application Insights via the ILogger interface injected into the functions by the Azure Functions runtime. Log entries are assigned an event ID and make use of the semantic logging capabilities of AI to facilitate easy querying.

