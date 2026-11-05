Developing Smint.io Portals backend components
==============================================

Current version of this document is: 3.5.1 (as of 15th of September, 2026)

This is the guide to building the server-side half of Smint.io Portals: connectors, data
adapters, data processors, task handlers, portal templates, resources and identity providers.
It explains what each component type is for, which one you actually need, the questions to
settle before you write anything, and how a component is built, tested and delivered.

The examples in this repository are working components you can copy. Start from
[Hello World](Connectors/Connector-HelloWorld/) for the shape of a connector and a data adapter,
then read [Picturepark](Connectors/Connector-Picturepark/) or
[SharePoint](Connectors/Connector-SharePoint/) for how a real external system is handled.

You can build your own backend components at any time, based on our *Smint.io Portals SDKs*.
Access to them is restricted and is arranged with Smint.io — see
[getting access to the SDKs](../Overview/README.md#user-content-getting-access-to-the-sdks).

1. [The backend component types](#user-content-the-backend-component-types)
1. [Which component do you actually need?](#user-content-which-component-do-you-actually-need)
1. [Productized or custom?](#user-content-productized-or-custom)
1. [Every component has the same three parts](#user-content-every-component-has-the-same-three-parts)
1. [Reference documents](#user-content-reference-documents)
1. [Examples in this repository](#user-content-examples-in-this-repository)
1. [Before you start: the questions to answer](#user-content-before-you-start-the-questions-to-answer)
1. [Connectors](#user-content-connectors)
1. [Data adapters](#user-content-data-adapters)
1. [Live connection or internal index](#user-content-live-connection-or-internal-index)
1. [Data adapter public API interfaces](#user-content-data-adapter-public-api-interfaces)
1. [Custom public API interfaces](#user-content-custom-public-api-interfaces)
1. [Building, testing and delivery](#user-content-building-testing-and-delivery)
1. [Checklist before you ship](#user-content-checklist-before-you-ship)
1. [Questions](#user-content-questions)

## The backend component types

| Component type | What it does | Reference |
|---|---|---|
| **Connector** | establishes and maintains the trust context to an external system, and describes that system's schema | [the connector contract](docs/smintio-connector-reference.md) |
| **Data adapter** | reads — and where applicable writes — the external system's data, and publishes it to the platform as public API interfaces | [the public API interfaces](docs/smintio-data-adapter-interfaces.md) |
| **Data processor** | hooks into a data adapter's operations and changes what goes in or what comes out | [data processors](docs/smintio-data-processors.md) |
| **Task handler** | a state machine for the task framework — approval workflows, requests, reviews | [task handlers](docs/smintio-task-handlers.md) |
| **Portal template** | describes a whole portal, so a new one can be created from it | [portal templates and resources](docs/smintio-portal-templates-and-resources.md) |
| **Resource** | a typed piece of portal content or styling — a string, a text, an image, a menu item, an email, a font, a style | [portal templates and resources](docs/smintio-portal-templates-and-resources.md) |
| **Identity provider** | federates the authentication of portal end users to an external identity system | [identity providers](docs/smintio-identity-providers.md) |

UI components and page templates are **frontend** components and are built quite differently —
see [developing frontend components](../Frontend/Legacy/).

A connector and a data adapter are always two separate components. The connector holds the
credentials and the schema; the data adapter reads or writes the data. They are separate because several
data adapters routinely share one connector — an asset adapter, an upload adapter, a collections
adapter and a customer-specific variant can all sit on the same authenticated connection.

## Which component do you actually need?

The most expensive mistake in this area is building a connector when something smaller was the
answer. Work down this list and stop at the first that fits:

1. **Nothing.** The external system speaks a protocol one of the shipped connectors already
   handles, and what you need is configuration. Ask before you build.
2. **A data processor.** The data already reaches the portal, and something about its shape, its
   naming or its reachability is wrong. A data processor is a small project — a startup, a
   configuration, the processor itself and a resource file — and by far the cheapest of these
   options.
3. **A second data adapter on an existing connector.** The external system is already integrated
   and you need a different view of it — upload, collections, products, a different mapping. The
   connector is reused unchanged and hands you its API client by injection.
4. **A custom public API interface on an existing data adapter.** The data is there; only the
   operation is missing.
5. **A new connector and data adapter.** An external system nothing integrates yet.

## Productized or custom?

Once you know you are building a connector and a data adapter, there is a second question, and
it decides how much work the whole thing is. **Settle it before you write anything.**

| | **Productized** | **Custom** |
|---|---|---|
| What it is | a general integration with an external system, usable by any portal | a purpose-built interface that one custom UI component consumes |
| Public API interfaces | the standard ones, above all **`IAssets`** — the main asset interaction interface | **your own**, published only for your own UI component |
| Who consumes it | the standard portal experience: search pages, asset detail, download, collections | your custom UI component, and nothing else |
| Connector meta-model | **required** | **not required** |
| Live connection or integration layer | a real decision you have to make | usually does not apply |
| Shared test suite | **use it in full** | do not — write your own tests against the test driver |

### Why the meta-model is only needed for a productized component

The meta-model exists so that **a portal administrator can point a component at a metadata
attribute in the editor**, and so that the platform can then interpret, index, filter, format
and translate that value without knowing anything about your external system. It is the schema
that turns an untyped bag of values into something the *generic* parts of the portal can work
with.

A custom interface consumed by a custom UI component has no such problem. **The interface
itself publishes the data model.** You generate its TypeScript declaration with the
[Data Adapter Exporter CLI](../Tools/Portals-DataAdapter-SDK-DataAdapterExporter-CLI/Release/),
your component deserializes against that declaration, and both ends know the shape at compile
time. Nothing generic ever has to interpret the payload, so there is nothing for a meta-model
to describe.

So: **custom to custom needs no meta-model.** Do not build one out of a sense of completeness —
it is a substantial piece of work whose only purpose is to serve consumers you do not have.

### What a custom connector still has to do

A custom connector is a real connector: it still authenticates, still refreshes its
authorization, still hands a client to its data adapter, and still declares its key, name and
configuration. What it can skip is `GetConnectorMetamodelAsync` returning a described schema,
the `MetamodelMessages` resource file, the integration-layer decision, and the whole
`AssetDataObject` mapping surface.

If your component later has to serve the standard portal experience as well, that is a
productized component — and retrofitting a meta-model onto a custom one is not a small change.
Ask which of the two it is at the start, in those words.

## Every component has the same three parts

Whatever the type, a backend component is up to three classes:

| | |
|---|---|
| **The startup** | a stateless, long-lived singleton implementing `IComponentStartup` (or the type-specific interface deriving from it). It carries the component's key, its localized name and description, its logo and icon, and the types of the other two classes. The platform reads it without instantiating anything |
| **The configuration** | a plain settings class implementing `IComponentConfiguration`, whose properties become the form a Smint.io Portals administrator fills in. See [the annotations](docs/smintio-backend-annotations.md) |
| **The component** | the short-lived, stateful thing that actually does the work, constructed by dependency injection with the configuration injected. A purely declarative component — a plain resource, for instance — has none, and its startup leaves `ComponentImplementation` `null` |

Plus **translatable resources** — a `ConfigurationMessages.resx` per culture, and a
`MetamodelMessages.resx` when the component describes a schema, which a
[custom component](#user-content-productized-or-custom) does not.

**Each component has its own configuration** — the connector has one, and so does every data
adapter, data processor, identity provider, portal template and resource. Nothing has to be
piled onto the connector: the connector's configuration is for establishing the trust context
and pointing at the right tenant, and anything only one data adapter interprets — a
customer-specific setting, a limit, a naming scheme, the key it verifies a signed link with —
belongs in *that adapter's* configuration class, where it can also differ from adapter to
adapter. All component configurations are stored encrypted at rest in the Smint.io database, and
a saved secret is never transmitted back for display — so a credential or a signing secret is a
legitimate configuration property, as long as the property is
[named so that it is recognised as a secret](docs/smintio-backend-annotations.md#user-content-configuration-is-stored-encrypted).
See
[which component's configuration a setting belongs in](docs/smintio-backend-annotations.md#user-content-which-components-configuration-a-setting-belongs-in).

This is the same three-part shape every Smint.io Portals component has, described in general in
[what are Smint.io Portals components](../Overview/README.md#user-content-the-startup-class) —
including why a *frontend* component has no separate startup or configuration class. What is
written above is the backend rendering of it.

`IComponentStartup.Key` is worth its own paragraph. **Component keys are globally unique and
issued by Smint.io**, so ask for yours at the start of the work — not when you are ready to ship.
See [getting a component into a Smint.io Portals system](../Overview/README.md#user-content-getting-a-component-into-a-smintio-portals-system).

## Reference documents

| Document | What it covers |
|---|---|
| [Recipes: how do I …?](docs/smintio-backend-recipes.md) | worked answers to the common tasks — validating credentials, dropdowns, rate limiting, paging, the integration layer, persistent state, custom interfaces, permissions, signed links, data processors |
| [The connector contract](docs/smintio-connector-reference.md) | every member of `IConnectorStartup` and `IConnector`, the setup methods, the call order, the authentication flows, the API client, the project layout |
| [The connector meta-model](docs/smintio-connector-metamodel.md) | describing the external system's schema: data types, entities, properties, enums, indexing, semantic types, form groups, translation, the converter, the lifecycle |
| [The data adapter public API interfaces](docs/smintio-data-adapter-interfaces.md) | the full catalogue, the base classes and what they leave abstract, parameters and results, long-running methods, permissions, configuration marker interfaces, custom interfaces |
| [The asset data model](docs/smintio-asset-data-model.md) | what a data adapter returns and what the portal does with it: content types, binaries, identifiers, folders, references, composites, raw data, paging, filters |
| [Backend component annotations](docs/smintio-backend-annotations.md) | the configuration form: data types, labels, resource files, validation, layout, visibility, dynamic allowed values |
| [Building, testing and publishing](docs/smintio-backend-component-delivery.md) | the project setup, the test drivers, the shared test suite, the publish CLI, versioning |
| [Data processors](docs/smintio-data-processors.md) | the contract, the types, every lifecycle hook point, how a processor is attached |
| [Identity providers](docs/smintio-identity-providers.md) | the contract, OIDC and SAML starting points, external user groups, pass-through authentication |
| [Portal templates and resources](docs/smintio-portal-templates-and-resources.md) | the two templating component types, the live-clone pattern, resource types, resource assets |
| [Task handlers](docs/smintio-task-handlers.md) | the state machine contract, task types, states and actions, the call sequence |
| [Frontend component annotations](../Frontend/Legacy/docs/smintio-annotations.md) | the TypeScript counterpart, when you are also building a UI component |

## Examples in this repository

All of them build from `Portals-SmintIo-BackendComponents.sln`.

#### Hello World — the minimal pair

Backed by artificial data, heavily commented, and the only example whose tests run anywhere
immediately, because it needs no credentials. **Start here.**

- [Connector](Connectors/Connector-HelloWorld/) · [Data adapter](DataAdapters/DataAdapter-HelloWorld/) · [Test driver](DataAdapters/Portals-HelloWorld-TestDriver/)

#### Picturepark — a live connection

Data is requested from the external system on the fly. Shows a real meta-model builder, search
facets derived from the source schema, a dynamic allowed-values provider for the channel, and a
second data adapter — for external users — sharing the same connector.

- [Connector](Connectors/Connector-Picturepark/) · [Data adapter](DataAdapters/DataAdapter-Picturepark/) · [Test driver](DataAdapters/Portals-Picturepark-TestDriver/)

#### Microsoft SharePoint — indexed through the integration layer

Content is analyzed and indexed by Smint.io rather than queried live. The only example
implementing the integration layer provider, and the clearest walkthrough of an OAuth2
authorization code flow.

- [Connector](Connectors/Connector-SharePoint/) · [Connector README](Connectors/Connector-SharePoint/README.md) · [Data adapter](DataAdapters/DataAdapter-SharePoint/) · [Test driver README](DataAdapters/Portals-Sharepoint-TestDriver/README.md)

Also: [how to implement a connector](Connectors/README.md) and
[how to implement a data adapter](DataAdapters/README.md).

## Before you start: the questions to answer

A backend component is quick to write and expensive to change once it is out. The component
`Key`, the persisted names of its configuration properties, and the integration mode are all
effectively permanent as soon as a portal has been configured against it.

So please settle the following before you create the first file. If you are building the
component for a customer, for a colleague or for a project team, ask them all of it in one go —
it is a short conversation, and it saves a rewrite.

*Identity*

| Question | What it decides | If it is wrong |
|---|---|---|
| Which component type is this, and what does it do in one sentence? | the SDK you reference, the contract you implement, and the document you work from | a different contract is a rewrite, not an edit. The most common error is building a connector where a data processor was the answer — see [which component do you actually need](#user-content-which-component-do-you-actually-need) |
| **Productized or custom?** | whether you need a meta-model, an integration mode, the standard interfaces and the shared test suite — or none of them. See [productized or custom](#user-content-productized-or-custom) | it is the difference between a large piece of work and a small one, and retrofitting a meta-model onto a custom component is not a small change |
| Has Smint.io issued the component key? | the value of `IComponentStartup.Key` | it is globally unique and permanent. Ask for it at the start |
| Is this a new component or a change to an existing one? | whether you start a fresh project or add to one | never rename a released component. Portals in the field are configured against the old key |

*How the integration works* — for a connector and data adapter

| Question | What it decides |
|---|---|
| How does the external system authenticate? | the connector's base class and its setup method. Note that an OAuth2 connector usually still uses the `Setup` method, not `Redirect` — see [choosing an authentication flow](docs/smintio-connector-reference.md#user-content-choosing-an-authentication-flow) |
| Which public API interfaces does the data adapter publish? | the reachable surface, and how much you have to implement. Declare what you support, not the widest interface that compiles — see [the catalogue](docs/smintio-data-adapter-interfaces.md#user-content-the-catalogue) |

*Only for a productized component*

| Question | What it decides |
|---|---|
| Live connection, or indexed through the Smint.io integration layer? | which configuration marker interfaces your data adapter implements, whether you write an integration layer provider, and how the data stays current — see [live connection or internal index](#user-content-live-connection-or-internal-index) |
| Can the external system's schema be read through its API, and is its metadata translated? | how the [meta-model](docs/smintio-connector-metamodel.md) is built — and, occasionally, whether the integration is viable at all |

*Configuration and language*

| Question | What it decides |
|---|---|
| Which settings must the administrator be able to change, and which are advanced? | your configuration properties. **The C# property name is the persisted name**, so a rename after release orphans every saved configuration. Start with few — you can add, you cannot remove |
| Which of those should be a dropdown the component fills rather than a text field? | your dynamic allowed-values providers. A configuration where the administrator picks the channel or the site from a list is a different product from one where they paste an identifier |
| Which languages? | whether labels stay on attributes or move into resource files. More than two languages means resource files from the start; retrofitting them touches every property |
| Does the component describe a schema with translatable labels? | whether you ship a `MetamodelMessages.resx`. Leaving it out when you need it makes those translations fail silently. A custom component does not need one |

*Delivery*

| Question | What it decides |
|---|---|
| How will it be tested against the real system? | whether the test drivers are useful. They need real credentials to be worth anything, and a redirect-flow connector needs its redirect URI registered on the external side |
| Who is going to review it, and when? | **a backend component reaches a Smint.io production system only through Smint.io, after a code review.** Plan that in rather than discovering it at the end — see [delivery](#user-content-building-testing-and-delivery) |

## Connectors

A connector has exactly two jobs: **establish and maintain a trust context** to the external
system, and **describe that system's schema**. It does not read assets, and it should not hold
network connections open — a connector instance is short-lived and there will be many of them.

`IConnectorStartup` is the entry point the platform discovers. It declares the connector's key,
its name and description, its logo and icon, which configuration class to present, which
component class to instantiate, the resource files, and the connector-specific members: the data
adapters to create by default, the setup method, whether setting it up needs an IT
administrator, and a link to your setup documentation.

`IConnector` is the connector itself. The methods, in the order the platform calls them when an
administrator configures it:

| Method | What you do |
|---|---|
| `PerformPostConfigurationChecksAsync` | validate the configuration against the external system, and put what the rest of the flow needs onto the authorization values — the identity server URL, a discovered API URL, values in the key-value store. Throw `ExternalDependencyException` when the configuration does not hold up |
| `GetRedirectUrlAsync` | build the authorization URL. Only for the `Redirect` setup method |
| `InitializeAuthorizationValuesAsync` | produce the authorization values used from now on |
| `PerformPostAuthorizationChecksAsync` | now that you can call the external system, confirm it works |
| `GetConnectorMetamodelAsync` | build and return the meta-model |

and, in steady state, `RefreshAuthorizationValuesAsync` before a token expires, `WarmCachesAsync`
on restart and every fifteen minutes, and `ConfigureServicesForDataAdapter` each time a data
adapter is constructed.

```C#
public override async Task PerformPostConfigurationChecksAsync(AuthorizationValuesModel authorizationValuesModel)
{
    ...
    authorizationValuesModel.KeyValueStore.Remove(ApiUrlKey);
    authorizationValuesModel.KeyValueStore.Add(ApiUrlKey, apiUrl);

    authorizationValuesModel.IdentityServerUrl = identityServerUrl;
    ...
}
```

```C#
public override Task<AuthorizationValuesModel> InitializeAuthorizationValuesAsync(
    string originalSecret, string secret, AuthorizationValuesModel bootstrapAuthorizationValuesModel)
{
    bootstrapAuthorizationValuesModel.AccessToken = _configuration.AccessToken;

    return Task.FromResult(bootstrapAuthorizationValuesModel);
}
```

For authorization, `OAuth2Connector` and `OAuth2AuthenticationCodeFlowWithPKCEConnector` are
prefabricated base classes to start from; implementing `IConnector` directly is equally normal
and is what most connectors against API-key systems do. **The full contract, the four
authentication flows with worked code, the API client base classes and the project layout are in
[the connector contract](docs/smintio-connector-reference.md).**

### The connector meta-model

Building the meta-model is the connector's second job. Generally speaking, it describes what
types of *object* exist in the external system and what properties they have — in other words,
the _custom data_ that is delivered. Smint.io Portals uses it to interpret everything your data
adapter returns.

Each type of object is represented by one `EntityModel`, and each of its fields by one
`PropertyModel`. How many entities you end up with depends entirely on the system: a simple file
store may need only one, while a system that distinguishes images, videos and products will have
one per type. Entities and their properties are fully translatable.

You build one in an `IMetamodelBuilder` and return it from `GetConnectorMetamodelAsync`.

Five things about it are worth knowing before you start:

- **The meta-model is a filter, not just a description.** A value whose property was never
  declared is dropped on conversion. If a field does not appear in the portal, check the
  declaration before you debug the conversion.
- **It keeps up with the external system on its own.** `GetConnectorMetamodelAsync` runs when the
  connector configuration is set up, regularly afterwards, and on an administrator action. A new field, a
  renamed label or a new enum value is picked up by a later refresh, and whatever that implies for
  an indexed source, such as re-indexing, is scheduled automatically. Nobody has to set the
  configuration up again for an ordinary schema change to arrive.
- **The meta-model identifier is the index identity.** Ordinary schema evolution does not go
  through it, and it must not churn. But **changing it triggers a full re-index**, which is
  exactly what you want when a change makes everything already indexed uninterpretable — and it
  is expensive, so it is agreed rather than slipped in.
- **Your entity keys are rewritten** to be unique per connector configuration, so the same
  connector configured twice yields two different key sets. Never hard-code an entity key.
- **Every entity automatically gets three properties**, an id and a list and a detail display
  name.

**The full reference is [the connector meta-model](docs/smintio-connector-metamodel.md)**: the
data types, the `EntityModel` and `PropertyModel` members, entity types and inheritance, enum
entities, full-text indexing, semantic types, form groups, translation, the converter that
applies the meta-model to a source payload, the lifecycle, and
[the identifier and forcing a full re-index](docs/smintio-connector-metamodel.md#user-content-the-identifier-and-forcing-a-full-re-index).

For a worked example against a real system, read the
[SharePoint meta-model walkthrough](Connectors/Connector-SharePoint/README.md#meta-model-structure).

## Data adapters

A data adapter is a *facade* for the external system. It uses the connector's authenticated
client to read — and where applicable write — data, and it maps that data into the platform's
own model.

The connecting point between the two is `ConfigureServicesForDataAdapter`:

```C#
// in the connector
public override void ConfigureServicesForDataAdapter(ServiceCollection services)
{
    services.AddTransient(_ => CreateMyClient());     // returns an IMyClient
}
```

which lets the data adapter take `IMyClient` as a constructor parameter.

**The client is the data adapter's way of calling the external system — and it must not expose
any secrets.** Access tokens, refresh tokens, client secrets and API keys stay inside the
connector and inside the client's own implementation. Do not put them on the client interface,
do not return them from a method, and do not hand out a raw authorization header for the caller
to attach itself. The data adapter should be able to do its job without ever holding a
credential, and anything it does hold can end up in a log line, an exception or a response.

That rule is about authenticating to the external system, and it is not a reason to leave the
data adapter without settings of its own. **A data adapter has its own configuration class**,
pointed at by `ConfigurationImplementation` on its startup and injected into its constructor:
the output formats and metadata attributes an administrator picks, plus any property you add —
a customer-specific setting, a limit, a naming scheme, or the key the adapter verifies a signed,
time-limited link with. Configurations are stored encrypted at rest, so a secret of the
adapter's own belongs there rather than on the connector.

`IDataAdapterStartup` adds four members to the common startup contract: the `ConnectorKey` it
belongs to, the `Permissions` it declares (normally `null`), the `PublicApiInterfaces` it
publishes, and the `MetamodelMessages` resource file when it has one.

Derive the adapter itself from `AssetsDataAdapterBaseImpl` for an asset source, or
`DataAdapterBaseImpl` for anything else. The base class implements most of the surface and
leaves eleven members abstract — that list is the work.

Most implementations split the class across `partial` files by capability, in folders named
`Search`, `Read`, `Random`, `Download` and `IntegrationLayer`, with the constructor and the
feature-support methods in the file named after the class. It is not required and it is worth
doing.

**Which methods you have to fill, what the objects you return have to look like, and which of
them you may leave out** is in [the asset data model](docs/smintio-asset-data-model.md). **Which
interfaces exist and how to publish your own** is in
[the public API interfaces](docs/smintio-data-adapter-interfaces.md).

## Live connection or internal index

Smint.io offers two integration modes, and the choice is structural.

**Live connection.** Data is fetched on demand. This requires the external system to be
feature-rich — a fully translatable meta-model, faceted search, acceptable latency — because
every portal request becomes a request to it. Picturepark is the example in this repository.

**Internal index.** Selected data is analyzed and its metadata captured; thumbnails and video,
audio and document renditions are generated and stored by Smint.io for offline use. Further
synchronisation happens through tokens, webhooks or timed intervals. Please note that Smint.io
does not store original assets. SharePoint is the example in this repository.

Choose the live connection when the external system can keep up, and the index when it cannot,
or when renditions have to be generated. The decision shows up in your configuration class as
one of the integration layer marker interfaces, and in your adapter as whether you implement
`IAssetsIntegrationLayerApiProvider`.

## Data adapter public API interfaces

Each Smint.io Portals backend or frontend component can tie itself to public API interfaces
published by Smint.io Portals data adapters. Data adapters can also define *custom permissions*
to facilitate fine-grained access management by the Smint.io Portals administrator.

This is done by requesting a data adapter public API interface through the configuration of the
backend or frontend component:

#### .NET example (for Smint.io Portals backend components)

```C#
using SmintIo.Portals.DataAdapterSDK.DataAdapters.Interfaces.Assets;

...

[DisplayName("en", "Data source for auto completion", IsDefault = true)]
[DisplayName("de", "Daten-Quelle für die Auto-Vervollständigung")]
[Description("en", "The data source to query the search bar auto completion suggestions from.", IsDefault = true)]
[Description("de", "Die Daten-Quelle, aus der die Vorschläge für die Auto-Vervollständigung für die Such-Eingabeleiste geladen werden.")]
[FormGroup("s-search-bar")]
public IAssetsSearch SearchBarAutoCompletion { get; set; }
```

Once the component is instantiated, you can call methods of that public API interface:

```C#
var searchAssetsResult = await _portalsContext.PublicApiInterfaceExecutionWrapper
    .WrapPublicApiInterfaceExecutionAsync<SearchAssetsParameters, SearchAssetsResult, IAssetsSearch>(
        _configuration.SearchBarAutoCompletion,
        nameof(IAssetsSearch.SearchAssetsAsync),
        parameters)
    .ConfigureAwait(false);
```

*Side note: you could also invoke the method of the target's public API interface directly.
However, we ask you to use this way of calling other public API interfaces, as this method
performs permission checks, script executions and other potentially required operations. We are
looking at ways to make this less manual in future; until then, please use the wrapper.*

### Calling the same interface from a UI component

A Smint.io Portals **UI component** ties itself to the same public API interfaces, with the
TypeScript equivalent of the annotations above — and all the wiring from frontend to backend
is done for you, without any further work involved. The TypeScript side is shown in
[data adapter public API interfaces](../Frontend/Legacy/README.md#user-content-data-adapter-public-api-interfaces)
in the frontend guide, and every interface, parameter, result and enumeration is listed as
TypeScript in [the frontend data adapter reference](../Frontend/Legacy/docs/smintio-data-adapter-reference.md).

**The full catalogue of interfaces is in
[the data adapter public API interfaces](docs/smintio-data-adapter-interfaces.md).**

## Custom public API interfaces

In the above example, `IAssetsSearch` is a standard public API interface provided by Smint.io
Portals.

*The great thing is*: if you develop your own Smint.io Portals data adapter, you can publish your
own custom public API interfaces as well. This enables you to develop any custom functionality
required using the Smint.io Portals component framework and runtime.

The pieces involved — the interface deriving from `IDataAdapterInterface`, the parameter and
result objects, the custom permission, and the declaration on the startup — are described in
[custom public API interfaces](docs/smintio-data-adapter-interfaces.md#user-content-custom-public-api-interfaces).

For use of your custom public API interfaces in a Smint.io Portals UI component you will need its
TypeScript public API interface definition. Use the
[Smint.io Portals Data Adapter Exporter CLI tool](../Tools/Portals-DataAdapter-SDK-DataAdapterExporter-CLI/Release/)
to generate it directly from your data adapter assembly:

```console
SmintIo.Portals.DataAdapterSDK.DataAdapterExporter.CLI.exe -s [Data-Adapter-Assembly-DLL] -t [Output-Filename]
```

For example:

```console
SmintIo.Portals.DataAdapterSDK.DataAdapterExporter.CLI.exe -s SmintIo.Portals.DataAdapter.Picturepark.MyCustomPictureparkInterfaces.dll -t .\IMyCustomPictureparkInterfaces.ts
```

This is how the result might look:

```typescript
import type { IDataAdapterInterface } from '@smintio/portals-component-sdk';
import type { IAssetIdsParameters } from '@smintio/portals-component-sdk';
import type { IDataAdapterParameterObject } from '@smintio/portals-component-sdk';
import type { IAssetIdentifier } from '@smintio/portals-component-sdk';
import type { IDataAdapterResult } from '@smintio/portals-component-sdk';

export interface IMyCustomPictureparkInterfaces extends IDataAdapterInterface
{
    markAssetsAsDeletedAsync(parameters: IMarkAssetsAsDeletedParameters) : Promise<IMarkAssetsAsDeletedResult>;
}
export interface IMarkAssetsAsDeletedParameters extends IDataAdapterParameterObject, IAssetIdsParameters
{
}
export interface IMarkAssetsAsDeletedResult extends IDataAdapterResult
{
}
```

*You see that this is just a TypeScript interface definition file (in this case even a very
simple one) that you can directly use in your Smint.io Portals UI component. You may also publish
the interface as an npm package for further comfort.*

## Building, testing and delivery

A backend component is an ordinary .NET 10 class library referencing the Smint.io SDK packages.

The path from source to a running portal has three steps, and the third is not self-service:

**1. Write it.** Copy the example closest to what you are building.

**2. Test it outside the platform.** The test drivers construct your real connector and data
adapter against the real external system, with in-memory stand-ins for everything the platform
normally provides — no server, no database, no portal. A connector that fails in the test driver
would have failed in the platform.

For a **productized** component, inherit the shared test suite rather than writing the basic
tests: `SmintIo.Portals.Connector.Test` and `SmintIo.Portals.DataAdapter.Test` ship abstract
test classes that already assert what a correct component does, and you supply a fixture and the
sample data. For a **custom** component, do not — most of what the suite asserts is about the
standard asset surface you deliberately do not implement. Use the test drivers directly and
write the tests your interface actually needs.

**3. Hand it to Smint.io.**

> **Publishing a backend component directly to a Smint.io production system is not supported for
> third parties.** A backend component runs as trusted server-side code inside the Smint.io
> platform, so every component — from Smint.io, from a Solution Partner or from an Enterprise
> customer — goes through Smint.io and through a **code review** before it reaches production.
> That review is a security requirement, not a formality.

So plan for it: build the component, prove it with tests, then get in touch at
[support@smint.io](mailto:support@smint.io) to arrange the review and the rollout. Raise it when
you *start* the component, not when you are ready to ship — the review is much cheaper against a
component that was written knowing it was coming.

**The full detail — the project file, getting the packages, the test drivers, the shared test
suite, what the review looks at, and versioning — is in
[building, testing and delivery](docs/smintio-backend-component-delivery.md).**

## Checklist before you ship

- [ ] the component `Key` is the one Smint.io issued, and unchanged from the released version
- [ ] `<Version>` bumped in the project file
- [ ] `TargetFramework` is `net10.0`, and the SDK package versions match across your projects
- [ ] every configuration property has a `DisplayName` with exactly one default culture, and a
      `ConfigurationMessages` resource file backs them
- [ ] no configuration property renamed since the last release
- [ ] `PublicApiInterfaces` lists exactly what you implement and want reachable
- [ ] the client the connector hands to the data adapter **exposes no tokens, secrets or API
      keys** — see [data adapters](#user-content-data-adapters)
- [ ] the external client registered in the connector's `ConfigureServicesForDataAdapter`, not in
      the startup's `ConfigureServices`
- [ ] custom permissions, if any, declared in all three places: the constant, the interface
      method, the startup
- [ ] no credentials committed anywhere in the project or its test settings
- [ ] the solution builds and the test driver runs green against the real system

*Additionally, for a productized component:*

- [ ] `MetamodelMessages` declared if the component ships translatable meta-model labels
- [ ] the meta-model identifier distinguishes differently scoped configurations, derives from
      nothing that moves on its own, and — if this version changes it — you have said so, because
      it triggers a full re-index
- [ ] every property emitted into `rawData` is declared in the meta-model — anything else is
      dropped
- [ ] both feature-support methods answer truthfully, and unsupported methods throw
      `NotImplementedException`
- [ ] `PermissionUuids` set on every asset and folder
- [ ] the inherited shared test suite passes

Then get in touch at [support@smint.io](mailto:support@smint.io) for the review and the rollout.

## Questions

Please do not hesitate to contact us at [support@smint.io](mailto:support@smint.io) if you run
into any issues.

Contributors
============

- Reinhard Holzner, Smint.io GmbH
- Yosif Velev, Smint.io GmbH
