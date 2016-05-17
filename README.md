# WebApi.RequestLogging

WebApi.RequestLogging allows you to easily add [NLog](http://nlog-project.org/)-based request logging to [Web API](http://www.asp.net/web-api) projects.

NuGet package:
* [WebApi.RequestLogging](https://www.nuget.org/packages/WebApi.RequestLogging/)



## Usage

Add `config.EnableRequestLogging();` to your `WebApiConfig.cs` file.



## Log levels

WebApi.RequestLogging selects log levels depending on the HTTP status code and the HTTP method.

| Status code | Safe methods | Unsafe methods |
| ----------- | ------------ | -------------- |
| 2xx, 3xx    | Debug        | Info           |
| 4xx         | Warn         | Error          |
| 5xx         | Fatal        | Fatal          |

Safe methods are `GET`, `HEAD`, `OPTIONS` and `TRACE`. All other methods such as `POST`, `PUT` and `DELETE` are considered unsafe.



## Sample project

The source code includes a sample project that uses demonstrates the usage of WebApi.RequestLogging. You can build and run it using Visual Studio 2015. By default the instance will be hosted by IIS Express at `http://localhost:7675/`.
