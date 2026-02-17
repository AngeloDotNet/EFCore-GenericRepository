# Entity Framework Core Generic Repository

<img src="https://img.shields.io/github/actions/workflow/status/angelodotnet/EFCore-GenericRepository/dotnet.yml?branch=main&style=for-the-badge" />
<img src="https://img.shields.io/github/license/angelodotnet/EFCore-GenericRepository?style=for-the-badge" />

Collection of a generic implementation of Entity Framework Core for .NET 10 mostly used in my private and/or work projects thus avoiding the duplication of repetitive code.

## 🏷️ Introduction

The Entity Framework Core Generic Repository is a design pattern that provides a generic implementation of the repository pattern using Entity Framework Core. It allows developers to perform CRUD (Create, Read, Update, Delete) operations on entities without having to write repetitive code for each entity type.

## 🛠️ Installation

### Prerequisites

- .NET 10.0 SDK (latest version)

### Setup

The package is available on [NuGet](https://www.nuget.org/packages/NET10Library.EFCore), using the following command in the .NET CLI:

```shell
dotnet add package NET10Library.EFCore
```

## 🚀 Getting Started

To get started with the Entity Framework Core Generic Repository, follow these steps:

1 - Register the repository in your dependency injection container

```csharp
services.AddRepositoryRegistration<YourDbContext>();
```

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ⭐ Give a Star

Don't forget that if you find this project helpful, please give it a ⭐ on GitHub to show your support and help others discover it.

## 🤝 Contributing

Contributions are always welcome. Feel free to report issues and submit pull requests to the repository, following the steps below:

1. Fork the repository
2. Create a feature branch (starting from the develop branch)
3. Make your changes
4. Submit a pull requests (targeting develop)

## 🩺 Testing

<img width="734" height="243" alt="image" src="https://github.com/user-attachments/assets/cbea06f9-c9ba-4881-ba79-8ae1baa79035" />