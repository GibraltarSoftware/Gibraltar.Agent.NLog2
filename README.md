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
* [Loupe.Agent.NLog](https://www.nuget.org/packages/Loupe.Agent.NLog/) for NLog 4.5 and .NET Core
* [Loupe Agent for NLog 4 From NuGet](https://www.nuget.org/packages/Gibraltar.Agent.NLog4/) for NLog 4.4 (and .NET 4.5)
* [Loupe Agent for NLog 2 From NuGet](https://www.nuget.org/packages/Gibraltar.Agent.NLog2/) for NLog 2 & 3
  
This will automatically add the Loupe Agent if it hasn't been previously added.

Adjust your NLog config to include NLog target for Loupe
----------------------------------------------------

Loupe is intended as a top-level catch-all to collect all of your logging in one managed place so you can filter
as needed dynamically during analysis.  To do this, modify the _extensions_, _targets_, and
_rules_ sections of your NLog configuration as follows:

**Loupe.Agent.NLog**
Latest and recommended version with support for .NET Core and improved structured logging capabilities.

1. Install nuget-package:
```
Install-Package Loupe.Agent.NLog
```

2. Update NLog config:
```xml
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
    <extensions>
        <!--Register nuget-package assembly for LoupeTarget class with target type "Loupe" -->
        <add assembly="Loupe.Agent.NLog" />
    </extensions>
    <targets>
        <!--Define a named target using the "Loupe" target type-->
        <target name="Loupe" xsi:type="Loupe" />
    </targets>
    <rules>
        <!--Send all logging to the "Loupe" named target-->
        <logger name="*" minlevel="Trace" writeTo="Loupe" />
    </rules>
</nlog>
```

3. Start logging
```
    NLog.LogManager.GetLogger("App").Info("Starting...");
```

4. Additional options and Structured logging
NLog 4.5 includes additional support for [structured logging](https://github.com/NLog/NLog/wiki/How-to-use-structured-logging):

_Loupe.Agent.NLog_ has the following options to include additional message details:

- _IncludeEventProperties_ - Include LogEvent properties. Default: _false_
- _IncludeMdlc_ - Include NLog Mapped Diagnostic Logical Context (MDLC) properties. Default: _false_
- _IncludeCallSite_ - Capture and include NLog CallSite information. Default: _true_
- _ContextProperties_ - Capture additional properties with help from [NLog Layout-Renderers](https://nlog-project.org/config/?tab=layout-renderers).

Example of using these options:

```xml
    <target name="Loupe" xsi:type="Loupe" includeEventProperties="true" includeMdlc="true">
        <contextProperty name="threadid" layout="${threadid}" />    <!-- Additional context properties -->
    </target>
```

**Gibraltar.Agent.NLog4**
Support NLog 4.4 (and older) along with .NET Framework ver. 4.5

1. Install nuget-package:
```
Install-Package Gibraltar.Agent.NLog4
```

2. Update NLog config:
```xml
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
    <extensions>
        <!--Register nuget-package assembly for LoupeTarget class with target type "Loupe" -->
        <add assembly="Gibraltar.Agent.NLog4" />
    </extensions>
    <targets>
        <!--Define a named target using the "Loupe" target type-->
        <target name="Loupe" xsi:type="Loupe" />
    </targets>
    <rules>
        <!--Send all logging to the "Loupe" named target-->
        <logger name="*" minlevel="Trace" writeTo="Loupe" />
    </rules>
</nlog>
```

3. Start logging
```
    NLog.LogManager.GetLogger("App").Info("Starting...");
```

**Gibraltar.Agent.NLog2**
Legacyg support for NLog ver. 2 and 3

1. Install nuget-package:
```
Install-Package Gibraltar.Agent.NLog2
```

2. Update NLog config:
```xml
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
    <extensions>
        <!--Register nuget-package assembly for LoupeTarget class with target type "Loupe" -->
        <add assembly="Gibraltar.Agent.NLog2" />
    </extensions>
    <targets>
        <!--Define a named target using the "Loupe" target type-->
        <target name="Loupe" xsi:type="Loupe" />
    </targets>
    <rules>
        <!--Send all logging to the "Loupe" named target-->
        <logger name="*" minlevel="Trace" writeTo="Loupe" />
    </rules>
</nlog>
```

3. Start logging
```
    NLog.LogManager.GetLogger("App").Info("Starting...");
```

Best Practises
------------------
We recommend to have the NLog logging-rule for the Loupe-target as the first rule,
or at least before any "final" rules - to avoid missing any relevant logevents.

If you find minlevel="Trace" too voluminous for normal collection, the minlevel may be set at
"Debug" instead.  Also, see the example App.config file in the Gibraltar.Agent.NLog4 project,
and consult the [NLog configuration documentation](http://nlog-project.org) as needed.

Make sure to log something to Loupe early in your application to load the Loupe
Agent and initialize its automatic monitoring features.  For example:</p>

```C#
mainLogger.Info("Entering application.");
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