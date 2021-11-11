# BlueYonder Open Api
> see https://aka.ms/autorest

To run the generator:
> call `autorest BlueYonderOpenApiAutoRest.md`

``` yaml
input-file: BlueYonderOpenApi.json
csharp:
  - namespace: WfmTeams.Connector.BlueYonder
    override-client-name: BlueYonderClient
    output-folder: .
    output-file: BlueYonderOpenApi.generated.cs
    add-credentials: false
```
