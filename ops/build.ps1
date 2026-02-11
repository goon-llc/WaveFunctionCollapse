dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --no-build --verbosity normal
dotnet pack --no-build --configuration Release