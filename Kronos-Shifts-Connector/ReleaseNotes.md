# Release Notes

### 27th August 2021

------

##### Shift swap eligibility filtering

The connector now supports shift swap filtering. This means that an employee looking to swap a shift will only be shown shifts that they are permitted to work. 

You will need to do the following to get this feature to work:

- A new configuration setting has been added that needs to be given a value - **FutureSwapEligibilityDays**, this is the number of days in the future the connector will query for eligible shifts to swap. (Be advised that the greater the number of the days the bigger the impact on performance).

- You will also need to ensure that your workforce integration has swap request eligibility enabled. You can do this easily in the graph explorer with this request: 

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