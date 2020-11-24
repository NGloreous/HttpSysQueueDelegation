# HttpSys Queue Delegation
ASP.NET Core 5 has [added support](https://github.com/dotnet/aspnetcore/issues/21163) for a new Windows feature which allows a process to delegate processing of requests it receives to other request queues. This enables the ability to route requests based on any headers to other processes running on the same machine without adding significant latency to the overall processing of the request.

This repository provides a simple example of how to use queue delegation.

# Samples

There are three sample applications in this repository. Two applications are just simple hello world applications and the third is an example of an application that uses the new HttpSys queue delegation feature. 

## AspnetcoreHello

This is a simple hello world ASP.NET Core application. It supports running within IIS and as a standalone console app. 

## AspnetframeworkHello

This is a simple hello world ASP.NET Framework application. It supports running with IIS. 

## Delegator

This application delegates requests to one or more other applications. Its behavior is to round robin requests between all the configured destinations which are specified in [appsettings.json]( https://github.com/NGloreous/HttpSysQueueDelegation/blob/main/Delegator/appsettings.json).

# Building

You need to install the .NET 5.0 SDK. To build in Visual Studio, you need use 2019 and make sure it’s up to date.

# Running

The delegator only works on versions of Windows that support queue delegation. At the time of writing this you’ll need to install a recent preview build. 

## AspnetcoreHello

You need to install the .NET 5.0 Runtime or the .NET Core Runtime Hosting Bundle if you want to run in IIS.

### Command line

AspnetcoreHello.exe --urls http://*:7000

### IIS

Follow [these instructions](https://docs.microsoft.com/en-us/aspnet/core/tutorials/publish-to-iis?view=aspnetcore-5.0&tabs=visual-studio) to host an ASP.NET Core application in IIS. Set the site name to **AspnetCoreHello** and the port to **7001**.

## AspnetframeworkHello

You need to have .NET Framework 4.7.2 or higher installed. Follow [these instructions]( https://docs.microsoft.com/en-us/iis/application-frameworks/scenario-build-an-aspnet-website-on-iis/configuring-step-1-install-iis-and-asp-net-modules) to host an ASP.NET Framework application in IIS. Set the site name to **AspnetFrameworkHello** and the port to **7002**.

## Delegator

You need to install the .NET 5.0 Runtime.

**NOTE:** If you want to delegate to IIS applications, for now you will need to run the delegator as **SYSTEM**. You can use the trick [here](https://stackoverflow.com/a/78691/2487788) to do that.

Delegator.exe --urls http://+:8080
