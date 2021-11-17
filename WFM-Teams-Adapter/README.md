# Introduction 

WFM Teams Universal Adapter is a Microsoft Azure Functions (serverless) application to integrate Workforce Management/Scheduling Systems such as those provided by Blue Yonder, Ceridian, Quinyx, Reflexis, Rotageek, UKG etc. with the Shifts application developed by Microsoft and hosted within Microsoft Teams.

 

![01-WFM Teams Adapter Reference Architecture](docs/images/00-WFM%20Teams%20Adapter.png)



The architecture and operation of the WFM Teams Universal Adapter and WFM Connector are described in detail here: [WFM Teams Adapter Architecture](docs/WFMTeamsAdapterArchitecture.md)



## Supported Shifts App Features

At the current time, the adapter does not support all of the features available in the Shifts App and the following table details the what is and what is not supported:

| Teams Shifts Feature                                         | Supported (Y/N) | Notes                                                        |
| ------------------------------------------------------------ | --------------- | ------------------------------------------------------------ |
| Shift (View)                                                 | **Y**           | Shift changes in the WFM Provider are detected by polling at regular intervals and are synced to Teams Shifts. |
| Shift (Add, Edit, Delete, Unassign)                          | N               | The synchronisation of changes made to Shifts in Teams Shifts to the WFM Provider is not currently supported in the adapter. |
| Shift Swap Request (Create, Cancel, Recipient Approve/Deny, Manager Approve/Deny, Eligibility Filtering) | **Y**           | Shift Swap Requests initiated in Teams Shifts must be completed in Teams Shifts because bi-directional syncing of requests is not currently supported in the adapter. |
| Offer Shift Request (Create, Manager Approve/Deny)           | N               |                                                              |
| Open Shift (View)                                            | **Y**           | Open Shift changes in the WFM Provider are detected by polling at regular intervals and are synced to Teams Shifts. |
| Open Shift (Add, Edit, Delete)                               | N               | The synchronisation of changes made to Open Shifts in Teams Shifts to the WFM Provider is not currently supported in the adapter. |
| Open Shift (Manager Assign)                                  | **Y**           |                                                              |
| Open Shift Request (Create, Manager Approve/Deny)            | **Y**           | Open Shift Requests initiated in Teams Shifts must be completed in Teams Shifts because bi-directional syncing of requests is not currently supported in the adapter. |
| Time Off (View)                                              | **Y**           | Time Off changes in the WFM Provider are detected by polling at regular intervals and are synced to Teams Shifts. |
| Time Off (Add, Edit, Delete)                                 | N               | The synchronisation of changes made to Time Off in Teams Shifts to the WFM Provider is not currently supported in the adapter. |
| Time Off Request (Create, Manager Approve/Deny)              | N               |                                                              |
| User Shift Preferences/Availability (View)                   | **Y**           | Availability changes in the WFM Provider are detected by polling at regular intervals and are synced to Teams Shifts. |
| User Shift Preferences/Availability (User Edit)              | **Y**           | Availability changes made by the User in the Shifts app on the mobile device are synchronised back to the WFM Provider in real-time. |
| Time Clock (Start Shift, End Shift, Start Break, End Break)  | N               |                                                              |



# Deployment Guide

Prior to deploying anything to your Azure environment, you should first Fork the OfficeDev/Microsoft-Teams-Shifts-WFM-Connectors project to your own GitHub repository as this will allow you to make changes to suit your specific requirements should you need to and if you enhance the adapter or fix any bugs you should consider submitting a PR to have them merged into the OfficeDev repository.

Full details of how to deploy the required infrastructure to Microsoft Azure using the included Azure Resource Manager template are provided in: [Azure Infrastructure Deployment Guide](docs/AzureInfrastructureDeploymentGuide.md)

