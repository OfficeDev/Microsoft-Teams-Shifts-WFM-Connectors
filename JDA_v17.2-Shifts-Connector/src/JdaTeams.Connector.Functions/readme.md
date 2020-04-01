### Debug
1. Add JdaTeams.Connector.Functions/local.settings.json for environment variable setup eg.
```
{
    "IsEncrypted": false,
    "Values": {
      "AzureWebJobsStorage": "UseDevelopmentStorage=true",
      "FUNCTIONS_WORKER_RUNTIME": "dotnet",
      "MyOptionString": "MyStringValue",
      "MyOptionNumber": 10
    }
}
```
2. Make JdaTeams.Connector.Functions your startup project
3. Hit debug
4. Make a note of the HttpTrigger endpoints that are activated

### Config

To limit sync cycles each of the TeamOrchestratorOptions can be set to appropriate values. The below will only sync this week and then not continue.
```
{
    "IsEncrypted": false,
    "Values": {
      "AzureWebJobsStorage": "UseDevelopmentStorage=true",
      "FUNCTIONS_WORKER_RUNTIME": "dotnet",
      "FrequencySeconds": -1,
      "PastWeeks": 0,
      "FutureWeeks": 0,
      "RetryMaxAttempts": 1
    }
}
```