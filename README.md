# Loupe Agent for NLog #

This agent extends NLog to send messages to the [Loupe Agent](https://nuget.org/packages/Gibraltar.Agent/) so you can
use any viewer for Loupe to review the agent's information and have it stored & analyzed by Loupe Server.

## To add this agent to your application ##

Just add the appropriate Loupe Agent for NLog NuGet package to your project
* [Loupe.Agent.NLog](https://www.nuget.org/packages/Loupe.Agent.NLog/) for NLog 4.5 and .NET Core / .NET Standard
* [Loupe Agent for NLog 4](https://www.nuget.org/packages/Gibraltar.Agent.NLog4/) for NLog 4.4 (and .NET 4.5)
* [Loupe Agent for NLog 2](https://www.nuget.org/packages/Gibraltar.Agent.NLog2/) for NLog 2 & 3
  
This will automatically add the correct Loupe Agent if it hasn't been previously added.  
This only has to be done once at the process level of your application, not every place
that NLog is used.

## Adjust your NLog config to include NLog target for Loupe ##

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

## Viewing Logs with Loupe ##

Loupe stores log data in a compact, binary format that supports .NET data types.  This format
is best viewed using either [Loupe Desktop](https://onloupe.com/local-logging/free-net-log-viewer) (free)
or [Loupe Server](https://onloupe.com) (available self-hosted or hosted by Gibraltar Software)
Log files are automatically created, rolled over, and pruned for space and age by the Loupe agent.
For more information, see [Getting Started with Loupe](https://doc.onloupe.com)

## Use with .NET Framework 4.5 and Older ##

Loupe continues to support older versions of .NET with the original Loupe Agent and using the 
agents for NLog 2 and NLog 4.  

### Gibraltar.Agent.NLog4 ###
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

### Gibraltar.Agent.NLog2 ###

Legacy support for NLog ver. 2 and 3

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

## Best Practices ##

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

Additionally, the Loupe Agent will keep your application running in some circumstances
as it has a foreground thread to ensure it has an opportunity to shut down cleanly.  Therefore,
it's important to be sure that a call is made to Log.EndSession on the Loupe agent as your
application is exiting to put Loupe into synchronous logging mode (which is slower) and prepare
it to let the application exit.  In .NET Core this is handled automatically if Loupe is added
as a service to the Host application but if you're using .NET Framework or another situation
where this doens't happen automaticaly, it's recommended that you call Log.EndSession in a
finally clause, such as this:

```C#
private static void Main()
{
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);

    try
    {
        log.Info("Starting application.");
        Application.Run(new TestForm());
    }
    finally
    {
        Log.EndSession("Application shutting down");
    }
}
```

For more information, see [Using NLog with Loupe](https://doc.onloupe.com/ThirdParty_NLog.html) in
the main Loupe documentation.

## Building the Agent ##

This project is designed for use with Visual Studio 2017 with NuGet package restore enabled.
When you build it the first time it will retrieve dependencies from NuGet.

## Contributing ##

Feel free to branch this project and contribute a pull request to the development branch. 
If your changes are incorporated into the master version they'll be published out to NuGet for
everyone to use!