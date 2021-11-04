# Release Notes

### 4th November 2021

------

**Manager Create, Update and Delete shifts in Teams**

A manager can now modify a schedule within Teams allowing them to create new shifts, edit existing shifts and remove shifts completely. All changes will sync across to Kronos as they are made.

To enable this feature please set the value of *'AllowManagersToModifyScheduleInTeams'* to true when deploying. 

Please consider the following:

- A manager has to share changes directly using the Share button. Drafting entities within Teams using the Save button is actively blocked
- A manager cannot create a shift with activities and cannot edit activities within Teams. 
- A manager cannot create a transfer shift
- A manager cannot edit a transfer shift

**Manager can Create Open shifts**

A manager can now create open shifts within Teams in the same way as above. This also enables managers to create open shifts in teams with a slot count.

To enable this feature please set the value of *'AllowManagersToModifyScheduleInTeams'* to true when deploying. 

Please consider the following:

- A manager cannot draft an open shift creation using the Save button

- A manager cannot ever update or delete open shifts in Teams, please make these changes within Kronos
- A manager cannot create an open shift with activities within Teams
- A manager cannot create an open shift which includes a shift transfer

**Additional Note Syncing Supported** 

We now support syncing even more types of notes between Kronos and Teams: shift notes, sender swap request notes, recipient swap request notes, manager swap request notes, manager open shift request notes and manager time off request notes.

Syncing these notes requires a relevant commentText value to be created in Kronos during deployment. Please refer to the section titled *Configuration to Enable Syncing of Notes* within the readme for steps on how to do this.

Please note that open shifts share the commentText value used for shift notes.

**Bugfixes**

- Improvements to how we handle graph tokens allowing us to retry failed requests in the event of an expired token

- Fixed the issue where a users shifts would not be deleted in the old team if they move to a new team 

  *We are aware of an issue that will prevent this fix from working on existing shifts. For example:*

  *1- A shift is created and stored in cache before this fix is deployed*

  *2- The fix is then deployed in your environment*

  *3- The user then changes team*

  *4- The existing shift will fail to be deleted*

  *This will only be a problem for existing shifts, newly created shifts will have the TeamId column in cache populated and be deleted successfully.*

### 30th September 2021

------

**Further Improvements to Transfer Shift Visibility**

We have improved the way that a shift indicates to employees that it contains a shift transfer (all or part of the shift involves a job other than their main job).

- The colour of shifts including a transfer is set as dark-pink. This is configurable in the integration-Api appSettings.
- The display name that is shown for all shift transfers is configurable. This value has a max value of 16 characters and is defaulted to 'TRANSFER'. Whatever value you provide will be followed by the start and end time of the entire shift ie. TRANSFER 08:00 - 15:00 

**Bugfixes**

- Seconds and milliseconds were being added to a time off request when requested on mobile for a partial day
- Open shifts could not be requested when they started at midnight
- Could not request to swap a shift if it had already been swapped once before



### 27th August 2021

------

##### Shift swap eligibility filtering

The connector now supports shift swap filtering. This means that an employee looking to swap a shift will only be shown shifts that they are permitted to work. 

You will need to do the following to get this feature to work:

- A new configuration setting has been added that needs to be given a value - **FutureSwapEligibilityDays**, this is the number of days in the future the connector will query for eligible shifts to swap. (Be advised that the greater the number of the days the bigger the impact on performance).

- You will also need to ensure that your workforce integration has swap request eligibility enabled. This is done automatically with newly created workforce integrations. If you have an existing workforce integration you can patch this in using the graph explorer with this request: 

  ```
  PATCH https://graph.microsoft.com/beta/teamwork/workforceIntegrations/{workforceIntegrationId}
  
  {
  	"eligibilityFilteringEnabledEntities": "SwapRequest"
  }
  ```

##### Syncing of time off request notes

We now support syncing sender time off request notes. At this time we only support syncing sender/requestor notes, this is due to a Shifts bug with manager TOR notes that will hopefully be fixed in the near future.

You will need to do the following to get this feature to work:

- You will need to configure comment categories in Kronos that we can assign to the specific comments when syncing. You can find out how to do this in the **Deployment -> Prerequisites** section of the readme.

- We have two new config values **ManagerTimeOffRequestCommentText** and **SenderTimeOffRequestCommentText** that will need the comment text values created in the previous step. Although manager note syncing is not currently supported I recommend adding this value now.

##### Shift Transfer Improvements

The way the connector handles and displays shift transfers in Teams has been greatly improved in this release. We now clearly identify to the employee when a specific shift contains a job transfer within it using the shift label property.

We have also updated the way activities are generated - we no longer just show the value *TRANSFER* in the event of a shift transfer and actually display the org job path to the employee. 

You will need to do the following to get this feature to work:

- A new config value has been added - **NumberOfOrgJobPathSectionsForActivityName**, this is the number of sections of the org job path you want to display in the activity name. Bear in mind we are constrained to 50 characters.
- Example: ./Contoso/UK/Stores/London/Checkout/Checkout Operator <br/> - A value of 2 would lead to shift transfer activities having a title of: _Checkout - Checkout Operator_ <br/> - A value of 1 would lead to shift transfer activities having a title of: _Checkout Operator_

##### Session Timeout Improvements

Previously the connector cached the Kronos auth token in redis for a predefined amount of time. This led to issues as the session timeout value is actually configurable in Kronos. We now have a setting that asks for your timeout value that we now use for the time to live. Find more info in the ARM tmeplate parameters table in the readme.