# Cloneable

This is a fork of https://github.com/Nickztar/Cloneable which is a fork of https://github.com/mostmand/Cloneable - Which doesn't seem to be maintained...

Auto generate Clone method using C# Source Generator

There are times you want to make a clone of an object. You can implement a clone method, but when a developer adds a new Field or Property the clone method should be changed too. Another way is to use reflection which is not performant. 
This source generator saves your time by generating the boilerplate code for cloning an object.

### Installing Cloneable
You should install [Cloneable with NuGet](https://www.nuget.org/packages/Cloneable.Generator):

    Install-Package LoDaTek.Cloneable
    
Or via the .NET Core command line interface:

    dotnet add package LoDaTek.Cloneable

Either commands, from Package Manager Console or .NET Core CLI, will download and install Cloneable and all required dependencies.

### Usage

You can add clone method to a class by making it partial and adding the attribute `Cloneable` on top of it. An example is provided in Cloneable.Sample project.

The generator is an incremental source generator, so you need Visual Studio 2022 17.3 or the .NET 6.0.4xx SDK or later. The generator is verified with the .NET 10 and .NET 11 SDKs.

Here is a simple example:

```csharp

using LoDaTek.Cloneable.Core;

[Cloneable]
public partial class Foo
{
    public string A { get; set; }
    public int B { get; set; }
}
```

For more examples please visit the [sample project](https://github.com/Nickztar/Cloneable/tree/master/Cloneable.Sample).
Or view some of the tests [test project](https://github.com/Nickztar/Cloneable/tree/master/Cloneable.Test).
