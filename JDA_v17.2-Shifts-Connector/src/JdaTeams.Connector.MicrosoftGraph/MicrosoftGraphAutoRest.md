# MicrosoftGraphClient
> see https://aka.ms/autorest

To run the generator:
> call `autorest MicrosoftGraphAutoRest.md`

``` yaml
input-file: MicrosoftGraphOpenApi.json
csharp:
  - namespace: JdaTeams.Connector.MicrosoftGraph
    override-client-name: MicrosoftGraphClient
    output-folder: .
    output-file: MicrosoftGraphClient.generated.cs
    add-credentials: false
```