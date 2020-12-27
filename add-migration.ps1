param (
    [Parameter(Mandatory=$true)][string]$name
)
dotnet ef migrations add $name --project ./src/Szlem.Persistence.EF --startup-project ./src/Szlem.AspNetCore --verbose