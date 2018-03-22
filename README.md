Gibraltar Loupe Agent for NLog 2.0 and Later
=====================

This agent extends NLog to send messages to the [Loupe Agent](https://nuget.org/packages/Gibraltar.Agent/) so you can
use any viewer for Loupe to review the agent's information and have it stored & analyzed by Loupe Server.

Adapting your .NET Application's NLog logging to include Loupe
--------------------------------------------------------------

To convert your .NET application's existing use of the NLog logging framework to include Loupe you need:

* (recommended) Download and install the latest version of Loupe Desktop to view the logs
* The Loupe Agent for NLog  from NuGet or the Agent.NLog project containing the class file: GibraltarTarget.cs
* Your NLog configuration file (app.config, NLog.config, NLog.dll.nlog, etc),
  or source code if programmatic configuration is used.

To add this agent to your solution or build process
--------------------------------------------------------------------------

Just add the appropriate Loupe Agent for NLog NuGet package to your project
  * [Loupe Agent for NLog 2 From NuGet](https://www.nuget.org/packages/Gibraltar.Agent.NLog2/) for NLog 2 & 3
  * [Loupe Agent for NLog 4 From NuGet](https://www.nuget.org/packages/Gibraltar.Agent.NLog4/) for NLog 4 & later

This will automatically add the Loupe Agent if it hasn't been previously added.

Adjust your configuration to use the GibraltarTarget
----------------------------------------------------

Loupe is intended as a top-level catch-all to collect all of your logging in one managed place so you can filter
as needed dynamically during analysis.  To do this, modify the _extensions_, _targets_, and
_rules_ sections of your NLog configuration as follows:

1. Include Gibraltar.Agent.NLog as an extension:

For NLog 4 & Later:
```XML
<add assembly="Gibraltar.Agent.NLog4" />
```

For NLog 2 & 3:
```XML
<add assembly="Gibraltar.Agent.NLog2" />
```

2. Add the GibraltarTarget as a target:

```XML
<target name="Gibraltar" xsi:type="Gibraltar" />
```
(or drop the "xsi:" prefix--just type="Gibraltar"--if not using explicit namespaces)

3. Add a rule sending all logging to the GibraltarTarget:

```XML
<logger name="*" minlevel="Trace" writeTo="Gibraltar" />
```
(we recommend that it be the first rule- or at least before any "final" rules - to avoid missing any).

If you find minlevel="Trace" too voluminous for normal collection, the minlevel may be set at
"Debug" instead.  Also, see the example App.config file in the Gibraltar.Agent.NLog4 project,
and consult the [NLog configuration documentation](http://nlog-project.org) as needed.

Make sure to log something to Loupe early in your application to load the Loupe
Agent and initialize its automatic monitoring features.  For example:</p>

```C#
mainLogger.LogInfo("Entering application.");
```

Building the Agent
------------------

This project is designed for use with Visual Studio 2017 with NuGet package restore enabled.
When you build it the first time it will retrieve dependencies from NuGet.

Contributing
------------

Feel free to branch this project and contribute a pull request to the development branch. 
If your changes are incorporated into the master version they'll be published out to NuGet for
everyone to use!