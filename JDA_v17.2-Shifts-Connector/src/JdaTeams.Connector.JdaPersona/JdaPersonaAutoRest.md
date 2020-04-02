# JdaPersonaClient
> see https://aka.ms/autorest

To run the generator:
> call `autorest JdaPersonaAutoRest.md`

``` yaml
input-file: JdaPersonaOpenApi.json
csharp:
  - namespace: JdaTeams.Connector.JdaPersona
    override-client-name: JdaPersonaClient
    output-folder: .
    output-file: JdaPersonaClient.generated.cs
    add-credentials: false
```