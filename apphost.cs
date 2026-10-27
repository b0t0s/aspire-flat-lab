#!/usr/bin/env -S dotnet --
#:sdk Aspire.AppHost.Sdk@13.6.1
#:property AspireUseCliBundle=false
#:property NoWarn=$(NoWarn);ASPIRE010
#:package DotNetEnv@3.2.0
#:package Aspire.Hosting.Docker@13.6.1
#:package Aspire.Hosting.JavaScript@13.6.1

#:include src/Dependencies.cs
#:include src/RuntimeManager.cs

#:include src/settings/Secrets.cs
#:include src/settings/EnvironmentSettings.cs

#:include src/extensions/entertainment-services.cs

#:include src/extensions/secret-services.cs
// #:include src/extensions/alts/secret-alt-services.cs
#:include src/extensions/media-services.cs
// #:include src/extensions/alts/media-alt-services.cs
#:include src/extensions/chat-services.cs
// #:include src/extensions/alts/chat-alt-services.cs
#:include src/extensions/knowledge-services.cs
// #:include src/extensions/alts/knowledge-alt-services.cs
#:include src/extensions/smarthome-services.cs
// #:include src/extensions/alts/smarthome-alt-services.cs
#:include src/extensions/development-services.cs
// #:include src/extensions/alts/development-alt-services.cs

// Infra (caddy, zerotier, homepage, adguard, seafile, ...) lives in docker-compose.yml so it survives restarts of this app stack.

using apphost.cs.src;

DotNetEnv.Env.NoClobber().TraversePath().Load();

var builder = DistributedApplication.CreateBuilder(args);
var environmentSettings = new EnvironmentSettings(builder);
var dependencies = builder.AddDependencies(environmentSettings);

builder.AddSecretServices(dependencies, environmentSettings);
builder.AddEntertainmentServices(dependencies, environmentSettings);
builder.AddMediaServices(environmentSettings);
builder.AddChatServices(dependencies, environmentSettings);
builder.AddDevelopmentServices(environmentSettings);
builder.AddSmartHomeServices(environmentSettings);
builder.AddKnowledgeServices(dependencies, environmentSettings);

var compose = builder.AddDockerComposeEnvironment("flat-lab");

builder.ApplyLabConventions(compose, environmentSettings.Shared.DataRoot);
builder.CascadeAutoStartThroughDependencies();

builder.Build().Run();
