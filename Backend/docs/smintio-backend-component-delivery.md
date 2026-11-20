Building, testing and delivering a backend component
===================================================

Current version of this document is: 2.2.0 (as of 23rd of October, 2026)

How a Smint.io Portals backend component gets from a C# project to something running in a
portal: the project setup, the test drivers that let you run a connector and a data adapter
without the platform, which tests to write, and how the component reaches a Smint.io system.

**Read [how a component reaches a Smint.io system](#user-content-how-a-component-reaches-a-smintio-system)
before you plan the work.** A backend component runs as trusted server-side code inside the
Smint.io platform, so it does not go live self-service — it goes through Smint.io and through a
code review. That is a security requirement and it shapes the schedule.

1. [The project](#user-content-the-project)
1. [Getting the SDK packages](#user-content-getting-the-sdk-packages)
1. [The test driver — running a component outside the platform](#user-content-the-test-driver--running-a-component-outside-the-platform)
1. [Which tests to write](#user-content-which-tests-to-write)
1. [The shared test suite](#user-content-the-shared-test-suite)
1. [Testing a custom component](#user-content-testing-a-custom-component)
1. [Generating TypeScript for a custom interface](#user-content-generating-typescript-for-a-custom-interface)
1. [How a component reaches a Smint.io system](#user-content-how-a-component-reaches-a-smintio-system)
1. [What the review looks at](#user-content-what-the-review-looks-at)
1. [Versioning](#user-content-versioning)
1. [A checklist before you hand it over](#user-content-a-checklist-before-you-hand-it-over)
1. [Things that are easy to get wrong](#user-content-things-that-are-easy-to-get-wrong)

## The project

A backend component is an ordinary .NET class library. **Target `net10.0`.**

A connector project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>MyCompany.Portals.Connector.MyService</AssemblyName>
    <Version>1.0.0</Version>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="SmintIo.Portals.ConnectorSDK" Version="…" />
  </ItemGroup>
</Project>
```

A data adapter project references the data adapter SDK, plus the connector project it belongs
to — but **not at runtime**: the connector is already loaded by the platform, so the reference
is compile-time only.

```xml
<ItemGroup>
  <PackageReference Include="SmintIo.Portals.DataAdapterSDK" Version="…" />
  <ProjectReference Include="..\..\Connectors\Connector-MyService\Connector-MyService.csproj"
                    Private="false" ExcludeAssets="runtime" />
</ItemGroup>
```

`Private="false"` and `ExcludeAssets="runtime"` are what keep the connector assembly out of the
data adapter's own output. Copy them.

`AssemblyName` and `Version` are read straight out of the project file when you publish, so keep
them accurate. The `AssemblyName` identifies the component for the rest of its life.

A test driver project references the data adapter project and the three test packages — see
below.

Keep all of your projects in one solution. The examples in this repository are laid out as
`Connectors/Connector-<Name>`, `DataAdapters/DataAdapter-<Name>` and
`DataAdapters/Portals-<Name>-TestDriver` under one solution file, and that layout is worth
copying.

## Getting the SDK packages

The SDK packages are **not public** — see
[getting access to the SDKs](../../Overview/README.md#user-content-getting-access-to-the-sdks)
for how access is arranged. These are the packages you will reference:

| Package | For |
|---|---|
| `SmintIo.Portals.ConnectorSDK` | a connector |
| `SmintIo.Portals.DataAdapterSDK` | a data adapter |
| `SmintIo.Portals.SDK.Core` | pulled in by both — the annotations, the data objects, the meta-model |
| `SmintIo.Portals.ConnectorSDK.TestDriver`, `SmintIo.Portals.DataAdapterSDK.TestDriver` | running a component outside the platform |
| `SmintIo.Portals.Connector.Test`, `SmintIo.Portals.DataAdapter.Test` | the shared test suite |

Once you have access, add a `NuGet.Config` next to your solution pointing at the feed. Two of
the example projects in this repository carry one; there are no credentials in it — those belong
in your user-level NuGet configuration.

**Keep the SDK versions aligned across your projects.** A data adapter built against a different
SDK version than its connector is the kind of problem that only shows up at load time.

## The test driver — running a component outside the platform

The test drivers construct your connector and data adapter with in-memory stand-ins for
everything the platform normally provides — cache, persistent storage, HTTP client factory,
logging, portals context — so you can exercise the real component against the real external
system with no server, no database and no portal.

This is the development loop for a backend component. Use it.

```C#
// 1. the connector
var connectorTestDriver = new SetupTestDriver();

await connectorTestDriver.InstantiateConnectorAsync(
    typeof(MyConnectorStartup),
    new MyConnectorConfiguration
    {
        ServiceUrl   = ConfigurationOptions.ServiceUrl,
        ClientId     = ConfigurationOptions.ClientId,
        ClientSecret = ConfigurationOptions.ClientSecret
    });

var connector = (MyConnector)connectorTestDriver.Connector;
var metamodel = connectorTestDriver.ConnectorMetamodel;

// 2. the data adapter, on top of it
var dataAdapterTestDriver = new DataAdapterTestDriver(connector, metamodel);

await dataAdapterTestDriver.InstantiateDataAdapterAsync(
    typeof(MyAssetsDataAdapterStartup),
    new MyAssetsDataAdapterConfiguration { … });

var dataAdapter = (MyAssetsDataAdapter)dataAdapterTestDriver.DataAdapter;
```

Pick the connector test driver that matches your setup method:

| Test driver | For |
|---|---|
| `SetupTestDriver` | `ConnectorSetupMethod.Setup` — credentials from the configuration |
| `OAuth2ConnectorTestDriver` | an OAuth2 connector, constructed with the redirect URI and optionally a refresh token |
| `OAuth2AuthenticationCodeFlowWithPKCETestDriver` | the PKCE flow, same constructor |

Each runs the real lifecycle — `PerformPostConfigurationChecksAsync`, the authorization step,
`PerformPostAuthorizationChecksAsync`, `GetConnectorMetamodelAsync` — so a connector that fails
in the test driver would have failed in the platform.

Two things to know about the redirect flows. The driver opens a **real browser** for the consent
screen, which works most reliably with Google Chrome, and it listens on the redirect URI you
pass — so that URI has to be registered with the external system as an allowed redirect. Pass a
refresh token on later runs to skip the browser step.

`DataAdapterTestDriver` additionally implements `IEntityModelProvider`, which is what lets your
converter resolve meta-model entities without the platform. It is deliberately partial: methods
outside what a data adapter needs throw. That is a harness, not a simulator.

The test driver does **not** validate that your component would be accepted for publishing, and
it does not exercise a portal. Use it to get the integration right, then publish to a
development environment and look at a real portal.

## Which tests to write

This depends on whether the component is **productized** or **custom** — see
[productized or custom](../README.md#user-content-productized-or-custom).

| | Productized | Custom |
|---|---|---|
| The shared test suite | **inherit it in full** | **do not** |
| Your own tests | the sample data the suite needs, plus anything specific | all of them, written directly against the test driver |

The shared suite asserts that a component behaves correctly *as a general integration*: that the
meta-model is well formed, that asset search honours its parameters, that download item mappings
are produced, that feature support is declared truthfully. Almost none of that applies to a
custom component, which implements its own interfaces and no meta-model. Inheriting the suite
there produces a long list of failures that mean nothing, and the temptation is then to suppress
them — which defeats the point for everyone who does need the suite.

## The shared test suite

**For a productized component**, do not write the basic tests yourself.
`SmintIo.Portals.Connector.Test` and `SmintIo.Portals.DataAdapter.Test` ship abstract xUnit test
classes that already assert what a correct component does; you inherit one and supply the sample
data.

| Base class | Asserts |
|---|---|
| `ConnectorStartupTests` | the startup is complete and consistent |
| `ConnectorTests` | the connector's lifecycle behaves |
| `OAuth2ConnectorTests` | the OAuth2 flow behaves |
| `ConnectorMetamodelTests` | the meta-model is well formed |
| `DataAdapterStartupTests` | the data adapter startup is complete |
| `DataAdapterAssetsTests` and its `…ReadTests`, `…SearchTests`, `…ReadRandomTests`, `…SharedTests` parts | asset read, search and random access behave as the platform expects |
| `DataAdapterAssetsLiveConnectionTests` | the same against a live connection |
| `DataAdapterIntegrationLayerTests`, `DataAdapterIntegrationLayerStartupTests`, `DataAdapterAssetsIntegrationLayerTests` | an indexed adapter |

The harness is an xUnit fixture deriving from
`BaseDataAdapterFixture<TConfigurationOptions, TConnector, TDataAdapterConfiguration, TDataAdapter>`,
which reads an `appsettings.json` next to the test assembly, binds your configuration options
out of it, and exposes `Connector`, `Metamodel`, `DataAdapter`, `DataAdapterConfiguration`,
`ServiceProvider` and `AssetOptions`.

```C#
public class MyServiceFixture : BaseDataAdapterFixture<
    MyServiceOptions, MyConnector, MyAssetsDataAdapterConfiguration, MyAssetsDataAdapter>
{
    protected override void BindSections(IConfiguration configuration)
    {
        base.BindSections(configuration);
        configuration.GetSection(MyServiceOptions.Name).Bind(ConfigurationOptions);
    }

    protected override async Task CreateConnectorAsync() { … }   // as above
    public override async Task InitializeAsync() { … }           // connector, then data adapter
}

[CollectionDefinition(nameof(MyServiceFixtureCollection))]
public class MyServiceFixtureCollection : ICollectionFixture<MyServiceFixture> { }

[Collection(nameof(MyServiceFixtureCollection))]
public class MyServiceDataAdapterAssetsTests : DataAdapterAssetsTests
{
    private readonly MyServiceFixture _fixture;

    public MyServiceDataAdapterAssetsTests(MyServiceFixture fixture) => _fixture = fixture;

    protected override bool SupportAssetVersions => false;
    protected override string SampleFormGroupDefinitionId => _fixture.AssetOptions.FormGroupDefinitionId;
    // … the rest of the sample data the suite needs
}
```

The collection fixture is what makes the connector bootstrap once per run rather than once per
test class — which matters when bootstrapping means an OAuth handshake.

Put the sample identifiers, names and query strings in `appsettings.json` rather than in the
test code, so that the same suite can be pointed at a different tenant. Structure the test
project as `Harness/` and `Integration/`, as all three examples in this repository do.

**Do not commit real credentials.** Use user secrets or environment variables for anything
sensitive, and keep the committed `appsettings.json` to placeholders — the Hello World test
driver's is a good model, because the Hello World connector is backed by artificial data and
needs no real credentials at all. That is also what makes it the one example whose tests run
anywhere, immediately.

## Testing a custom component

A custom component publishes its own interfaces, so nothing generic can assert anything useful
about it. Use the test drivers directly and write the tests the interface deserves:

```C#
var connectorTestDriver = new SetupTestDriver();

await connectorTestDriver.InstantiateConnectorAsync(typeof(MyConnectorStartup), connectorConfiguration);

var dataAdapterTestDriver = new DataAdapterTestDriver(
    connectorTestDriver.Connector, connectorTestDriver.ConnectorMetamodel);

await dataAdapterTestDriver.InstantiateDataAdapterAsync(
    typeof(MyDataAdapterStartup), dataAdapterConfiguration);

var dataAdapter = (IMyProductData)dataAdapterTestDriver.DataAdapter;

var result = await dataAdapter.ReadProductAsync(new ReadProductParameters { ProductCode = "…" });
```

What is worth asserting, in rough order of value:

- **the happy path against the real external system** — the thing the component exists for;
- **the shape of the result**, because a UI component deserializes it against the generated
  TypeScript declaration. A field you rename or retype is a breaking change to that component;
- **identifier scoping**, if your parameters or results carry asset, folder or resource
  identifiers — round-trip one through `UnscopeIdentifiers` and `ScopeIdentifiers`;
- **the failure paths** — what the component does when the external system is down, returns an
  error, or returns nothing. These reach a live portal, and a clean failure is the difference
  between a support ticket and an outage;
- **the authentication lifecycle**, at least once: that the connector sets up, authorizes, and
  refreshes.

Your connector is still exercised in full by the test driver, so a connector that would fail in
the platform still fails here — the difference is only in what is asserted about the data
adapter on top.

## Generating TypeScript for a custom interface

If your data adapter publishes a [custom public API
interface](smintio-data-adapter-interfaces.md#user-content-custom-public-api-interfaces) and a UI
component has to call it, generate the TypeScript definition from the built assembly with the
[Data Adapter Exporter CLI](../../Tools/Portals-DataAdapter-SDK-DataAdapterExporter-CLI/Release/):

```console
SmintIo.Portals.DataAdapterSDK.DataAdapterExporter.CLI.exe -s MyDataAdapter.dll -t .\IMyProductData.ts
```

The tool is **win-x64 only**. Re-run it whenever the interface changes; the output is generated
and never hand-edited. Publish it as an npm package if more than one component uses it.

## How a component reaches a Smint.io system

> **Publishing a backend component directly to a Smint.io production system is not supported for
> third parties.** Every backend component reaches production through Smint.io, after a **code
> review**.

This is not an administrative preference. A backend component is **trusted server-side code**: it
runs inside the Smint.io platform, in the same process as everything else, holding customer
credentials and reading customer data. There is no sandbox that would make an unreviewed
component safe, so the review is the control. It applies to components from Smint.io, from
Solution Partners and from Enterprise customers alike.

So the path is:

```
   write the component
        ↓
   prove it with tests, against the real external system
        ↓
   hand it to Smint.io  →  code review  →  rollout
```

**Start the conversation when you start the component, not when you finish it.** Get in touch at
[support@smint.io](mailto:support@smint.io). Two things come out of that conversation that you
need early: the component `Key`, which is issued and permanent, and an understanding of what the
review will look for — both of which are much cheaper to have before the code exists than after.

### What to hand over

- the **source**, in a repository Smint.io can review;
- the **tests**, and a way to run them — which usually means saying what credentials or sandbox
  tenant they need, never the credentials themselves;
- a short note on **what the component does, what it talks to, and what it needs**: the external
  system, the scopes or permissions it requires there, the configuration properties an
  administrator fills in, and any dependency you added;
- the **TypeScript interface declaration**, if it publishes a custom interface a UI component
  consumes.

### About the publish CLI

The [Portals-SDK-PublishComponent-CLI](../../Tools/Portals-SDK-PublishComponent-CLI/Release/) can
compile, package and upload a backend component, and its README documents how. **That route is
for a development environment Smint.io has set up for you, not for production**, and it does not
replace the review. If you have not been given an environment to publish to, you do not need the
tool — build, test, and hand the component over.

## What the review looks at

Knowing this in advance costs nothing and saves a round trip. In rough order of how often it
comes up:

- **Secrets.** No credential in the repository, in `appsettings`, in a test fixture or in a log
  line. And **the client the connector hands to the data adapter must not expose tokens, secrets
  or API keys** — see
  [the API client](smintio-connector-reference.md#user-content-the-api-client).
- **What the component talks to.** Every outbound host, and why. A backend component that calls
  something other than the external system it is a connector for needs an explanation.
- **Dependencies.** Every package you added ships with the component. Fewer is better, and an
  unmaintained or unnecessary one is the most common thing sent back.
- **Error handling.** Failures from the external system turned into `ExternalDependencyException`
  rather than swallowed or allowed to escape raw. Unsupported methods throwing
  `NotImplementedException` rather than returning empty.
- **Permissions.** `PermissionUuids` set on the objects you emit; custom permissions declared in
  all three places; nothing bypassing a permission check.
- **Input handling.** Anything from a configuration property or a request parameter that reaches
  a query, a URL or a file path.
- **Resource use.** Unbounded result sets, requests without a timeout, work done per asset that
  should be done per page.
- **The configuration surface.** Property names you will not want to change later, and settings
  that are actually used.

## Versioning

The `<Version>` in your project file is the component's version, and it is read straight out of
the project file — there is no separate manifest. **Bump it for every hand-over**, not only for
every functional change: a version is how everyone involved refers to what was reviewed and what
was rolled out.

Use ordinary semantic versioning, and treat these as breaking, because they are:

- renaming or removing a configuration property — it orphans every saved configuration;
- changing the component `Key`;
- removing a public API interface from `PublicApiInterfaces`, or a method from a custom
  interface — which also breaks the generated TypeScript a UI component was built against;
- changing what a meta-model entity key means.

Adding a configuration property, adding a method to a custom interface, and adding an interface
to `PublicApiInterfaces` are all safe.

### A new version must fit the configurations already saved

Administrators have saved configurations for your component, in every tenant that uses it, and a
new version reads them. When a new version is published to a Smint.io system, the platform checks
it against all of them, and **rejects the version** if a saved value no longer fits:

- **A configuration property keeps its type.** A property saved as a string cannot become a number,
  and a property that was removed cannot come back later with another type under the same name.
- **A value that is in use stays allowed.** Removing a member from an enum property, or a value
  from an `AllowedValues` list, is rejected while a saved configuration still uses it. Values
  nobody uses may go.

The rejection names the property, the type or the value, and how many saved configurations are
affected — never which tenant they belong to. What stays safe:

- **adding a property** — give it a default, because existing configurations do not have a value
  for it;
- **removing a property** — its saved value is no longer read.

There is no way to rename a property or to migrate a saved value: a rename is a removal plus an
addition, and the value is lost. If a property needs another type, add a new property with a new
name and stop reading the old one.

Two things to say out loud when you hand over a new version:

- **Whether it changes the meta-model identifier.** An ordinary meta-model change reaches existing
  configurations on its own, at the next refresh, and the platform schedules whatever re-indexing
  it implies. **Changing the identifier is different: it triggers a full re-index** of every
  configuration using the connector. That is the right answer when a change makes what is already
  indexed uninterpretable, and it is expensive on a large customer source — so it is agreed in
  advance and stated in the release notes, never discovered afterwards. See
  [the identifier, and forcing a full re-index](smintio-connector-metamodel.md#user-content-the-identifier-and-forcing-a-full-re-index).
- **Anything your component keeps outside itself** — a cache entry, a stored cursor, a file —
  may be read by both the old and the new version around a rollout. Version it, or make it
  tolerant.

## A checklist before you hand it over

- [ ] the component `Key` is the one Smint.io issued, and unchanged from the released version
- [ ] `<Version>` bumped
- [ ] `TargetFramework` is `net10.0`, and the SDK package versions match across your projects
- [ ] the data adapter's project reference to the connector carries `Private="false"` and
      `ExcludeAssets="runtime"`
- [ ] `ConfigurationMessages` is declared and every configuration property has a `DisplayName`
      with exactly one default culture
- [ ] `PublicApiInterfaces` lists every interface you want reachable — and nothing you have not
      implemented
- [ ] the client exposes **no** tokens, secrets or API keys
- [ ] **no credentials anywhere** in the repository, the test settings or the history
- [ ] every dependency you added is one you can justify
- [ ] failures from the external system become `ExternalDependencyException`; unsupported methods
      throw `NotImplementedException`
- [ ] the test driver runs green against the real external system
- [ ] a note describing what the component does, what it talks to and what it needs

*Additionally, for a productized component:*

- [ ] `MetamodelMessages` is declared if you build a meta-model with translatable labels
- [ ] the meta-model identifier distinguishes differently scoped configurations and derives from
      nothing that moves on its own — and if this version changes it, the full re-index that
      causes is flagged in the hand-over
- [ ] both feature-support methods answer truthfully
- [ ] `PermissionUuids` set on every asset and folder
- [ ] the inherited shared test suite runs green

## Things that are easy to get wrong

- **Planning for a self-service production deployment.** There is not one. Build the review into
  the schedule from the start.
- **Leaving a credential in the repository or its history.** The most common reason a component
  is sent back, and the most expensive to undo.
- **Exposing a token on the client interface** so the data adapter can attach it itself. Keep
  credentials inside the connector and inside the client's implementation.
- **Committing an `appsettings.<Env>.json`.** They hold OAuth credentials.
- **Shipping a dependency you did not mean to.** Everything you reference ships with the
  component.
- **Inheriting the shared test suite for a custom component.** Most of what it asserts does not
  apply, and the noise hides the failures that do matter.
- **Skipping the shared test suite for a productized component.** It asserts things that are easy
  to get wrong and hard to notice, and inheriting it costs a class.
- **Changing the meta-model identifier without saying so.** It forces a full re-index of every
  configuration using the connector. Deriving it from anything that moves on its own — a
  timestamp, a token, your connector's version — does that on every release.
- **Treating a configuration property rename as cosmetic.** It is a breaking change, silently.

## Questions

Please do not hesitate to contact us at [support@smint.io](mailto:support@smint.io) if you run
into any issues.

Contributors
============

- Reinhard Holzner, Smint.io GmbH
