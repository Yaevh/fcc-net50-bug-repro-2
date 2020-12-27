param (
    [Parameter(Mandatory=$false)][string]$name
)
write-host "Are you sure? Updating the database may result in loss of data! Y/N"
$confirm = read-host "Y/N"
switch ($confirm)
{
	"Y" { dotnet ef database update $name --project ./src/Szlem.Persistence.EF --startup-project ./src/Szlem.AspNetCore --verbose }
}
exit
