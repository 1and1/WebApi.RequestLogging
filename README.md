# WebApi.RequestLogging

WebApi.RequestLogging allows you to easily add [NLog](http://nlog-project.org/)-based request logging to [Web API](http://www.asp.net/web-api) projects.

NuGet package:
* [WebApi.RequestLogging](https://www.nuget.org/packages/WebApi.RequestLogging/)



## Usage

Add `config.EnableRequestLogging();` to your `WebApiConfig.cs` file.



## Log levels

WebApi.RequestLogging selects log levels depending on the HTTP status code and the HTTP method:

<table>
  <tr>
    <th>Status codes</th>  <th>HEAD</th>  <th>GET, OPTIONS</th>      <th>DELETE</th>  <th>POST, PUT</th>
  </tr>
  <tr>
    <td>1xx</td>           <td colspan="4">Trace</td>
  </tr>
  <tr>
    <td>2xx</td>           <td colspan="2">Debug</td>                <td colspan="2">Info</td>
  </tr>
  <tr>
    <td>3xx, 401</td>      <td colspan="2">Info</td>                 <td colspan="2">Warn</td>
  </tr>
  <tr>
    <td>403, 404, 410</td> <td>Info</td>  <td colspan="2">Warn</td>  <td>Error</td>
  </tr>
    <td>416</td>           <td colspan="2">Trace</td>                <td colspan="2">Error</td>
  <tr>
    <td>Other 4xx</td>     <td colspan="4">Error</td>
  </tr>
  <tr>
    <td>5xx</td>           <td colspan="4">Fatal</td>
  </tr>
</table>



## Sample project

The source code includes a sample project that uses demonstrates the usage of WebApi.RequestLogging. You can build and run it using Visual Studio 2015. By default the instance will be hosted by IIS Express at `http://localhost:7675/`.
