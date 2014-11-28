Gibraltar Loupe Agent for NLog 2.0
=====================


Adapting your .NET Application's NLog logging to include Loupe
--------------------------------------------------------------

To convert your .NET application's existing use of the NLog logging framework to include Loupe you need:

* (recommended) Download and install the latest version of Loupe Desktop to view the logs
* The Agent.NLog project containing the class file: GibraltarTarget.cs 
* Your NLog configuration file (app.config, NLog.config, NLog.dll.nlog, etc), 
  or source code if programmatic configuration is used.

To add this Gibraltar.Agent.NLog library to your solution or build process
--------------------------------------------------------------------------

1. Check that the Reference to the NLog library points to where you have it on your system or build process.  If needed,
    either add the correct reference and delete the broken one, or else edit the project file directly and correct the path.
2. Confirm that the Reference to the Loupe Agent (Gibraltar.Agent) is valid, or correct it.  It's highly
  recommend you use NuGet to get the latest Loupe Agent. 
3. Build this project and either add this project to your application as a dependency or copy the
    Gibraltar.Agent.NLog.dll library it builds to wherever you keep external library dependencies.
4. Also include the Gibraltar.Agent.dll and Gibraltar.Packager.exe in your distribution and installation in the
    directory for your application executable.

Adjust your configuration to use the GibraltarTarget
----------------------------------------------------

Loupe is intended as a top-level catch-all to collect all of your logging in one managed place so you can filter
as needed dynamically during analysis.  To do this, modify the <code>extensions</code>, <code>targets</code>, and
<code>rules</code> sections of your NLog configuration as follows:

1. Include Gibraltar.Agent.NLog as an extension:

```XML
<add assembly="Gibraltar.Agent.NLog" />
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
"Debug" instead.  Also, see the example App.config file in the Gibraltar.Agent.NLog project,
and consult the <a href="http://www.nlog-project.org/config.html">NLog configuration documentation</a> as needed.

Make sure to log something to Loupe early in your application to load the Loupe
Agent and initialize its automatic monitoring features.  For example:</p>

```C#
mainLogger.LogInfo("Entering application.");
```
