# Implementing a new WFM Connector

### Introduction

The WFM Teams Universal Adapter has been designed to allow for additional connectors for new WFM systems to be developed and plugged in. In general the adapter project will not need updating provided the adapter supports all desired features. Currently supported scenarios can be found here: [Supported Shifts App Features](../README.md/#supported-shifts-app-features)

The connector you develop will be required to implement the following interfaces:

- **IWfmDataService** - this interface contains a number of methods used to request data required by the Adapter from the WFM Provider
- **IWfmActionService** - this interface contains a number of methods used to push changes from the Teams Shifts Application to the WFM Provider
- **IWfmConfigService** - this interface has a single ConfigureServices method that passes the services collection and configuration root object from the functions app and allows the provider implementation to register its own dependencies and access its specific settings from the collection of functions app settings.

One of the interfaces is not a requirement for all connectors. Please ensure the following interface is implemented if your WFM system requires a separate token for each employee, like the case with BlueYonder:

- **IWfmAuthService** - this interface has two methods HandleFederatedAuthRequest which handles the federated authentication request from the WFM Provider and RefreshEmployeeTokenAsync which is used to refresh the employee token in Redis

### WFM Action Service Return Values

All action service requests must return an WfmResponse object so that the adapter can generate the correct response to return in the event of a WFI call. 

Use the SuccessResponse method when the action was completed with no issues - the newEntityId parameter should be used if in response to the action a new entity has been created such as during an approval.

Use the ErrorResponse method if something goes wrong whilst performing the action in the WFM system. This will prevent any changes within the Shifts app. An error code and message is expected and will be displayed to the user that triggered the action in the Shifts app.

### WFM Data Service Return Values

IWfmDataService methods handle the retrieval of data from the WFM system. Some of the methods will require you to map from the WFM model to our universal adapter models.

### Exceptions

The WfmException class models the only supported type of exception. If your connector is going to throw an exception it must be of this type. 

Please be aware that many of the handlers do not currently catch exceptions - if you expect to throw an exception in one of these handlers they will need to be updated within the adapter project.

### Modifications to Startup.cs

Once your connector implements the four interfaces you will need to modify the Startup.cs file for the WfmTeamsAdapter.Functions project to ensure that the application is registering the required dependencies for your new WFM connector.  

Firstly, navigate to the ProviderType.cs file and create a new enum value for your new WFM provider. You will then need to update the connector options to use this new value.

Next, in the Startup.cs file add a new method:

```c#
private IWfmConfigService ConfigureForYourWfmProvider(IServiceCollection services)
{
	services.AddTransient<IWfmDataService, YourWfmProviderDataService>()
    	.AddTransient<IWfmActionService, YourWfmProviderActionService>()
        .AddTransient<IWfmAuthService, YourWfmProviderAuthService>();

	return new YourWfmProviderConfigService();
}
```

 Finally, you need to edit the switch statement to handle the new ProviderType you just created. The switch statement should call the method above to register all necessary dependencies.