# DotNetApp

**Miscellaneous tools for .NET application development.**

---

## DotNetApp.Collections

Implements a `ListBinding` class with which you can automatically update a target list by binding it with one or more source collections that implement `INotifyCollectionChanged` interface.

Implements a `ListProxy` class which provides a proxy for a list where individual `IList<T>` interface members can be overriden using delegates.

---

## DotNetApp.EntityFrameworkCore

Extend Entity Framework DbContext scaffolding to make DbContext use [`ChangeTrackingStrategy.ChangingAndChangedNotifications`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.modelbuilder.haschangetrackingstrategy?view=efcore-3.1) and to make each entity class implement `INotifyPropertyChanging` and `INotifyPropertyChanged` interfaces.

### Getting started

1. Add new **Console App** project (here called `MyScaffoldingStartupProject`) which will be used as a startup project for Entity Framework Core DbContext scaffolding
2. Add reference to the target project (here called `MyTargetProject`) to which Entity Framework Core context and models would be generated
3. Install NuGet packages using NuGet Package Manager Console:

    ```PowerShell
    Install-Package DotNetApp.EntityFrameworkCore -ProjectName MyScaffoldingStartupProject
    Install-Package Microsoft.EntityFrameworkCore.Tools -ProjectName MyScaffoldingStartupProject
    Install-Package Microsoft.EntityFrameworkCore.SqlServer -ProjectName MyScaffoldingStartupProject
    ```

4. Update `Program.cs` file to include design-time services configuration:

    ```C#
    using Microsoft.EntityFrameworkCore.Design;
    using Microsoft.Extensions.DependencyInjection;
    using DotNetApp.EntityFrameworkCore;

    namespace MyScaffoldingStartupProject
    {
        class Program
        {
            static void Main(string[] args)
            {
            }
        }

        class CustomDesignTimeServices : IDesignTimeServices
        {
            public void ConfigureDesignTimeServices(IServiceCollection services)
            {
                services.AddCSharpNotificationEntityImplementation();
            }
        }
    }
    ```

5. Run [`Scaffold-DbContext`](https://docs.microsoft.com/en-us/ef/core/miscellaneous/cli/powershell#scaffold-dbcontext) using NuGet Package Manager Console (change arguments to match your configuration):

    ```PowerShell
    Scaffold-DbContext "Server=(localdb)\mssqllocaldb;Database=Blogging;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -Project MyTargetProject -StartupProject MyScaffoldingStartupProject -OutputDir Models -ContextDir Context -Verbose -UseDatabaseNames
    ```
