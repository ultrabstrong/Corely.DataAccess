# Corely Common
This library helps abstract the persistence mechanism to an implementation detail; keeping the domain layer clean

## Installation
`dotnet add package Corely.DataAccess`

## Getting Started
Corely.DataAccess provides a set of utilities to help you work with data access in your applications. It includes:

- **IRepository**: A generic repository interface for CRUD operations.
- **IReadRepository**: A read-only repository interface for querying data.
- **IUnitOfWork**: An interface for managing transactions and coordinating changes across multiple repositories.
- Base Entity Framework configurations for MSSQL, MySQL, and PostgreSQL.


## Documentation
Details about each utility can be found in the [documentation](https://github.com/ultrabstrong/Corely.DataAccess/blob/master/Docs/index.md).

## Repository
[Corely.DataAccess](https://github.com/ultrabstrong/Corely.DataAccess)

## Contributing
We welcome contributions! Please read our [contributing guidelines](CONTRIBUTING.md) to get started.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
