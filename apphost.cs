#!/usr/bin/env -S dotnet --
#:sdk Aspire.AppHost.Sdk@13.6.4
#:property AspireUseCliBundle=false
#:property NoWarn=$(NoWarn);ASPIRE010
#:package DotNetEnv@3.2.0
#:package Aspire.Hosting.Docker@13.6.4
#:package Aspire.Hosting.JavaScript@13.6.4

#:include src/dependencies/LabDependencies.cs
#:include src/dependencies/alts/chat-alt-dependencies.cs
#:include src/dependencies/alts/development-alt-dependencies.cs
#:include src/dependencies/alts/knowledge-alt-dependencies.cs
#:include src/dependencies/alts/smarthome-alt-dependencies.cs
#:include src/dependencies/development-dependencies.cs
#:include src/dependencies/entertainment-dependencies.cs
#:include src/dependencies/irl-dependencies.cs
#:include src/dependencies/knowledge-dependencies.cs
#:include src/dependencies/media-dependencies.cs
#:include src/dependencies/secret-dependencies.cs
#:include src/dependencies/smarthome-dependencies.cs
#:include src/RuntimeManager.cs
#:include src/LabGroups.cs

#:include src/settings/Secrets.cs
#:include src/settings/EnvironmentSettings.cs

#:include src/extensions/chat-services.cs
#:include src/extensions/development-services.cs
#:include src/extensions/entertainment-services.cs
#:include src/extensions/irl-services.cs
#:include src/extensions/knowledge-services.cs
#:include src/extensions/media-services.cs
#:include src/extensions/secret-services.cs
#:include src/extensions/smarthome-services.cs

#:include src/extensions/alts/chat-alt-services.cs
#:include src/extensions/alts/development-alt-services.cs
#:include src/extensions/alts/knowledge-alt-services.cs
#:include src/extensions/alts/smarthome-alt-services.cs

// Infra (caddy, zerotier, homepage, adguard, home-assistant, ...) lives in docker-compose.yml so it survives restarts of this app stack.

using apphost.cs.src;

DotNetEnv.Env.NoClobber().TraversePath().Load();

var builder = DistributedApplication.CreateBuilder(args);
var environmentSettings = new EnvironmentSettings(builder);
var dependencies = new LabDependencies(builder, environmentSettings);

builder
    .AddLabGroup(LabGroup.Secrets, LabRole.Main, b => b.AddSecretServices(dependencies, environmentSettings))
    .AddLabGroup(LabGroup.Entertainment, LabRole.Main, b => b.AddEntertainmentServices(dependencies, environmentSettings))
    .AddLabGroup(LabGroup.Media, LabRole.Main, b => b.AddMediaServices(dependencies, environmentSettings))
    .AddLabGroup(LabGroup.Chat, LabRole.Main, b => b.AddChatServices(dependencies, environmentSettings))
    .AddLabGroup(LabGroup.Development, LabRole.Main, b => b.AddDevelopmentServices(dependencies, environmentSettings))
    .AddLabGroup(LabGroup.SmartHome, LabRole.Main, b => b.AddSmartHomeServices(dependencies, environmentSettings))
    .AddLabGroup(LabGroup.Knowledge, LabRole.Main, b => b.AddKnowledgeServices(dependencies, environmentSettings))
    .AddLabGroup(LabGroup.Irl, LabRole.Main, b => b.AddIrlServices(dependencies, environmentSettings))
    .AddLabGroup(LabGroup.Chat, LabRole.Alt, b => b.AddChatAltServices(dependencies, environmentSettings))
    .AddLabGroup(LabGroup.Development, LabRole.Alt, b => b.AddDevelopmentAltServices(dependencies, environmentSettings))
    .AddLabGroup(LabGroup.SmartHome, LabRole.Alt, b => b.AddSmartHomeAltServices(dependencies, environmentSettings))
    .AddLabGroup(LabGroup.Knowledge, LabRole.Alt, b => b.AddKnowledgeAltServices(dependencies, environmentSettings))
    .ApplyLabModes();

var compose = builder.AddDockerComposeEnvironment("flat-lab");

builder.ApplyLabConventions(compose, environmentSettings.Shared.DataRoot);
builder.CascadeAutoStartThroughDependencies();

builder.Build().Run();
