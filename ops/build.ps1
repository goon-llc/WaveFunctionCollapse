dotnet restore
dotnet build --configuration Release --no-restore
dotnet run --project ./WFCTests/WFCTests.csproj
dotnet pack --no-build --configuration Release