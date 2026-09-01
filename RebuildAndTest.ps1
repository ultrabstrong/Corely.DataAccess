dotnet tool restore
dotnet csharpier format .
dotnet clean Corely.DataAccess.sln --verbosity minimal
dotnet build Corely.DataAccess.sln --verbosity minimal
dotnet test --solution Corely.DataAccess.sln --coverage
