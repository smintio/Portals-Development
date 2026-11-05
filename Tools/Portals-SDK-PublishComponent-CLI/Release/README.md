The Portals-SDK-PublishComponent-CLI tool
=========================================

1. [Description](#user-content-description)
1. [Download](#user-content-download)
1. [Environment variable](#user-content-environment-variable)
1. [App settings](#user-content-app-settings)
1. [Parameters](#user-content-parameters)
1. [Publish and deploy](#user-content-publish-and-deploy)

Current version of this document is: 1.4.0 (as of 15th of September, 2026)

## Description

The purpose of the Portals-SDK-PublishComponent-CLI tool is to enable developers with a short cycle to publish and deploy components to Smint.io.

This document contains all the steps needed to configure and run the Smint.io component publishing tool.

**The tool covers frontend and backend components, but they are not delivered the same way.** Publishing a *frontend*
component is self-service. Publishing a *backend* component directly to a Smint.io **production** system is not
supported for third parties — see [Backend components](#user-content-backend-components) below.

Please note that [access to the SDKs is restricted](../../../Overview/README.md#user-content-getting-access-to-the-sdks).


## Download

Download the ZIP archive suitable for your operating system to a convenient location.

Extract the archive.

## Environment variable

Create an environment variable named `SMINT_IO_SDK_HOME` pointing to the location from the previous step.
Doing so will allow the publishing tool to be invoked from different locations.
- For Windows `%SMINT_IO_SDK_HOME%\SmintIo.Portals.SDK.PublishComponent.CLI.exe`
- For Linux `$SMINT_IO_SDK_HOME/SmintIo.Portals.SDK.PublishComponent.CLI`

## App settings

You can manage three different environments for your component development efforts (`Development`, `Staging` and `Production`).
For each of those environments, you can specify different connection data:

- appsettings.json is the template, and holds the settings the environment files have in common
- appsettings.Development.json contains settings for local development (inherits from appsettings.json, if a setting is not given)
- appsettings.Staging.json contains settings for staging developed components (inherits from appsettings.json, if a setting is not given)
- appsettings.Production.json contains settings for publishing to production (inherits from appsettings.json, if a setting is not given)

The `-env` argument described under [Parameters](#user-content-parameters) picks which of the
three environment files applies.

Sample `appsettings.Staging.json`

```json
{
  "SmintIo": {
    "ApiUrl": "https://someone-staging.portalsapib.smint.io",
    "Auth": {
      "Authority": "https://someone-staging.smint.io/",
      "ClientId": "someone",
      "ClientSecret": "********-****-****-****-4a215b643134"
    },
    "AuthorizationHeader": "Basic ************************************************************************YTX6fdRh"
  }
}
```

Please request your specific `SmintIo` configuration `ApiUrl` and `Auth` values from [support@smint.io](mailto:support@smint.io).

The `AuthorizationHeader` value is used during publishing of a frontend component to authorize the CLI tool against the client private npm feed.

Keep in mind that the `AuthorizationHeader` value is not provided by Smint.io.

*Note: by default, the authorization process needs to listen to port `43450` on your machine. In the unlikely case that this port is not
available on your machine, you can override the `RedirectUrl` setting inherited from `appsettings.json` and set another port there. Please
also note that in this case you will also need to contact us at [support@smint.io](mailto:support@smint.io) and tell us the new redirect
URL, because we need to whitelist the new redirect URL on our side before you can use it.*

## Parameters

The CLI tool has parameters that can be listed by executing the binary with one of the arguments `/?`, `/help` or `--help`

Example: `%SMINT_IO_SDK_HOME%\SmintIo.Portals.SDK.PublishComponent.CLI.exe /?`

- The only parameter that has to be specified is for the desired environment to which to deploy.
    - This can be done by running the tool with `-env` or `--environment` argument followed by the environment name.
    - Possible values are `Development`, `Staging` or `Production`.
    - The parameter can be skipped if the `ASPNETCORE_ENVIRONMENT` environment variable is set with a desired environment value.
- Optional parameter is `-dir` or `--directory`
    - Use this in case the working directory is different from the directory where the binary is being executed.
- Optional parameter is `-all` or `--all-tenants`
    - Intended for use by Smint.io staff only.
- Lastly, `--version` shows the tool version

## Publish and deploy

### Frontend components

The target component's npm "package.json" file can be extended with additional "scripts" properties.

```json
"scripts": {
    "smint-io-pc:development": "npm publish && npm info --json | %SMINT_IO_SDK_HOME%\\SmintIo.Portals.SDK.PublishComponent.CLI.exe -env development",
    "smint-io-pc": "npm publish && npm info --json | %SMINT_IO_SDK_HOME%\\SmintIo.Portals.SDK.PublishComponent.CLI.exe -env staging",
    "smint-io-pc:production": "npm publish && npm info --json | %SMINT_IO_SDK_HOME%\\SmintIo.Portals.SDK.PublishComponent.CLI.exe -env production"
}
```

Those three are exactly what the `ui-example-hello-world-1` starter ships, so copying the starter
gets you them already — note that the unsuffixed `smint-io-pc` is the **staging** one.

The CLI executable requires the value of `npm info --json` as standard input so that it can determine the component name, version, and npm repository.

#### When the target component is ready for publish:

In the component folder, issue a single command to push your component to a npm repository and then deploy it to Smint.io from there.

```console
npm run smint-io-pc:development
```

*Please note that Smint.io must be able to retrieve your component from the npm repository you publish it to. A public
registry needs no further setup; for a private feed, the `AuthorizationHeader` setting described above is what lets the
tool authenticate against it.*

*Please note that the examples target a Windows environment. For Linux, the correct environment variable syntax must be used.*

#### The two halves fail independently

That one command is two steps: `npm publish` pushes the package to the registry, and the CLI then registers it with
Smint.io. They can fail separately, and the recovery is not the same.

**If `npm publish` succeeded and the registration failed, do not run the whole command again.** The version number has
already been consumed in the registry, and republishing it will fail on the version alone. Rerun only the second half:

```console
npm info --json | %SMINT_IO_SDK_HOME%\SmintIo.Portals.SDK.PublishComponent.CLI.exe -env development
```

Two failures worth recognising:

- *No permissions to publish the component*, immediately after a successful `npm publish`, can simply mean the registry
  has not made the new version visible yet. Give the identical command one retry before treating it as a permissions
  problem.
- *Missing component name*, or an `E404` reporting that your package is not in the npm registry, usually means the
  package folder has no `.npmrc`. The registration step runs `npm info`, which resolves the registry from the folder it
  runs in — not from `publishConfig` in `package.json`. Without an `.npmrc` pointing at the right registry, `npm info`
  asks the public registry, finds nothing, and the CLI stops with nothing to register — while `npm publish` has already
  succeeded. The `ui-example-hello-world-1` starter ships an `.npmrc` for exactly this reason; copy it when you scaffold
  a new component. It holds no credentials — those belong in your user-level `.npmrc`.

### Backend components

> **Publishing a backend component directly to a Smint.io production system is not supported for
> third parties.** A backend component is trusted server-side code running inside the Smint.io
> platform, so every backend component reaches production through Smint.io, after a **code
> review**. This applies to components from Smint.io, from Solution Partners and from Enterprise
> customers alike.
>
> The commands below are for a development environment that Smint.io has set up for you. They do
> not replace the review. Please get in touch at [support@smint.io](mailto:support@smint.io) when
> you *start* a backend component — see
> [how a component reaches a Smint.io system](../../../Backend/docs/smintio-backend-component-delivery.md#user-content-how-a-component-reaches-a-smintio-system).

Like for frontend components, a single command can be used to publish and deploy your backend component to Smint.io.

Under a command prompt in the component folder run

```console
%SMINT_IO_SDK_HOME%\SmintIo.Portals.SDK.PublishComponent.CLI.exe -env development
```

Behind the scenes the publish component tool will look for `*.csproj` file to compile, package and deploy the component.

Please note that all project and external package references will be included in the package.

For the development environment only, the component will be compiled using the debug build profile. All other environments will use the release build profile.

Optionally, backend developers can register the tool as a global CLI tool:

#### Register the tool as a global CLI tool

1. Install as a global tool
    - dotnet tool install --global --add-source `<SMINT-IO-PRIVATE-SOURCE-FEED>` SmintIo.Portals.SDK.PublishComponent.CLI
1. Confirm the message
    - You can invoke the tool using the following command: smint-io-pc
    - Tool 'smintio.portals.sdk.publishcomponent.cli' (version '4.0.0.0') was successfully installed
	- The version number may vary
	- You can then invoke the tool using the following command: `smint-io-pc`
1. Go to the .NET global tools folder
	- For Windows, go to `%USERPROFILE%\.dotnet\tools`
	- For Linux, go to `$HOME/.dotnet/tools`
1. Go to folder `.store\smintio.portals.sdk.publishcomponent.cli\4.0.0.0\smintio.portals.sdk.publishcomponent.cli\4.0.0.0\tools\net10.0\any`
	- The version number may vary
1. Edit the applicable development, staging, and/or production app settings files similarly to [App settings](#user-content-app-settings)
1. Useful links
	- [Install and use a .NET global tool using the .NET CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-use)
	- [dotnet tool install](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install)

#### Uninstall the global CLI tool

1. Run
	- dotnet tool uninstall --global SmintIo.Portals.SDK.PublishComponent.CLI
1. Confirm the message
	- 'Tool 'smintio.portals.sdk.publishcomponent.cli' (version '4.0.0.0') was successfully uninstalled.'

#### Publish using the registered global CLI tool

If you registered our tool as a global CLI tool, you can use this simplified command for publishing to Smint.io:

```console
smint-io-pc -env development
```

Contributors
============

- Yosif Velev, Smint.io GmbH
- Reinhard Holzner, Smint.io GmbH