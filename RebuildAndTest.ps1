csharpier format .
dotnet clean Corely.DataAccess.sln --verbosity minimal
dotnet build Corely.DataAccess.sln --verbosity minimal
dotnet test --collect:"XPlat Code Coverage"