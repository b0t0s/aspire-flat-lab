#!/usr/bin/env -S dotnet --
#:sdk Aspire.AppHost.Sdk@13.5.4
#:property AspireUseCliBundle=false
#:property NoWarn=$(NoWarn);ASPIRE010
#:package DotNetEnv@3.2.0
#:package Aspire.Hosting.Docker@13.5.4

#:include src/RuntimeManager.cs
#:include src/EnvironmentSettings.cs
#:include src/Secrets.cs
#:include src/Dependencies.cs

#:include src/extensions/media-services.cs
#:include src/extensions/knowledge-services.cs
#:include src/extensions/smarthome-services.cs
#:include src/extensions/development-services.cs
#:include src/extensions/secret-services.cs
#:include src/extensions/entertainment-services.cs
#:include src/extensions/irl-services.cs
#:include src/extensions/chat-services.cs

// Infra (caddy, zerotier, homepage, adguard, seafile, ...) lives in docker-compose.yml so it survives restarts of this app stack.

DotNetEnv.Env.NoClobber().TraversePath().Load();

var builder = DistributedApplication.CreateBuilder(args);
var environmentSettings = new EnvironmentSettings(builder);
var dependencies = builder.AddDependencies(environmentSettings);

builder.AddEntertainmentServices(dependencies, environmentSettings);
builder.AddDevelopmentServices(dependencies, environmentSettings);
builder.AddSmartHomeServices(dependencies, environmentSettings);
builder.AddKnowledgeServices(dependencies, environmentSettings);
builder.AddSecretServices(dependencies, environmentSettings);
builder.AddMediaServices(dependencies, environmentSettings);
builder.AddChatServices(dependencies, environmentSettings);
builder.AddIrlServices(dependencies, environmentSettings);

var compose = builder.AddDockerComposeEnvironment("flat-lab");

builder.ApplyLabConventions(compose, environmentSettings.Shared.DataRoot);
builder.CascadeAutoStartThroughDependencies();

builder.Build().Run();
