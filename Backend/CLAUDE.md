# Building Smint.io Portals backend components

This directory is everything you need to build a **backend component** — a connector, a data
adapter, a data processor, an identity provider, a portal template, a resource or a task handler
— for Smint.io Portals. It is written for an agent doing the work, and for the developer reading
over its shoulder.

| | |
|---|---|
| `README.md` | the guide: what each component type is, the questions to settle, getting started, the anatomy of a project, building, testing, publishing |
| `docs/` | the reference documents — the contracts, the annotations, the meta-model, the asset data model, the public API interfaces, and one per further component type |
| `Connectors/Connector-HelloWorld` + `DataAdapters/DataAdapter-HelloWorld` | the minimal pair, backed by artificial data, heavily commented, meant to be copied |
| `Connectors/Connector-Picturepark` + `DataAdapters/DataAdapter-Picturepark` | a realistic pair against a **live connection** |
| `Connectors/Connector-SharePoint` + `DataAdapters/DataAdapter-SharePoint` | a realistic pair **indexed through the integration layer**, and the only example implementing the integration layer provider |
| `DataAdapters/Portals-*-TestDriver` | the test harnesses — one per pair |

The stack is **C# on .NET 10**. Everything builds from
`Portals-SmintIo-BackendComponents.sln`.

## Read before writing component code

Do not infer the conventions from a single file — they are written down.

| Document | Read it when |
|---|---|
| `README.md` | always, first — it is the full guide |
| `docs/smintio-backend-recipes.md` | you know *what* to build and need *how*: worked, copy-able answers to the common tasks. Check it before writing a mechanism from scratch |
| `docs/smintio-connector-reference.md` | you are writing a **connector**: the contract, the setup methods, the authentication flows, the API client, the call order |
| `docs/smintio-connector-metamodel.md` | you are describing the external system's schema — which every connector does |
| `docs/smintio-data-adapter-interfaces.md` | you are writing a **data adapter**: which interface to implement, the base classes, permissions, custom interfaces |
| `docs/smintio-asset-data-model.md` | you are filling in an `AssetDataObject` — what to set, what to leave out, what the portal acts on |
| `docs/smintio-backend-annotations.md` | you are declaring a configuration property |
| `docs/smintio-backend-component-delivery.md` | you are setting up the project, deciding which tests to write, or delivering the component |
| `docs/smintio-data-processors.md` | you are writing a **data processor** |
| `docs/smintio-identity-providers.md` | you are writing an **identity provider** |
| `docs/smintio-portal-templates-and-resources.md` | you are writing a **portal template** or a **resource** |
| `docs/smintio-task-handlers.md` | you are writing a **task handler** |
| `../Frontend/Legacy/` | the component is a UI component or a page template — that is the frontend side, and it has its own guide and its own `CLAUDE.md` |

## The short version — what is most often got wrong

- **The component `Key` is issued by Smint.io, not chosen.** It is globally unique across the
  whole platform and cannot change after release. Ask for it before writing the first file;
  never invent one and never restyle an existing one.
- **A connector and a data adapter are two components, always.** The connector authenticates and
  describes the schema; the data adapter reads the data. Several data adapters can share one
  connector. Do not try to do both in one.
- **Settle productized versus custom before anything else.** A *productized* component
  implements the standard interfaces — above all `IAssets` — for the generic portal, and needs a
  meta-model, an integration mode and the shared test suite. A *custom* component publishes its
  own interfaces for one custom UI component, and needs **none of those**: the interface is the
  data model, the consumer deserializes against the generated TypeScript, and nothing generic
  ever interprets the payload. Building the productized surface for a custom component is the
  single most expensive mistake available here.
- **The client the connector hands to the data adapter must expose no secrets.** It is the data
  adapter's way of *calling* the external system, not of authenticating to it. No token, no
  refresh token, no client secret, no API key, no prepared authorization header, no raw HTTP
  client. Design the interface in terms of operations the adapter wants. A credential the
  adapter never holds cannot leak into a log line, an exception or a response.
- **Every component has its own configuration — do not pile everything onto the connector.** A
  data adapter has a configuration class exactly as a connector does. The connector's is for
  establishing the trust context and pointing at the right tenant; anything only one data adapter
  interprets belongs to that adapter — a customer-specific setting, a limit, a naming scheme, or
  a secret of its own such as a link signing key. One connector can serve several data adapters,
  so a property on the connector forces the same value on all of them. **All component
  configurations are stored encrypted at rest, and a saved secret is never displayed back to the
  administrator**, so a secret is a legitimate configuration property and needs no scheme of your
  own; "never hold a credential" is about authenticating to the *external system*, not a ban on
  the adapter having settings of its own.
- **What makes a configuration property a secret is its name.** A `string` property whose name
  contains `password`, `secret`, `key`, `token`, `credential`, `authconfig` or `sasuri` is never
  transmitted back for display. There is no annotation for it, so name the property accordingly —
  `LinkSigningKey`, not `Signature` — and never put a secret in a `string[]`, which is not
  covered.
- **A backend component does not reach production self-service.** Publishing directly to a
  Smint.io production system is not supported for third parties: every backend component goes
  through Smint.io and through a code review, because it runs as trusted server-side code.
  Never plan a task around "and then we publish it".
- **`IConnectorStartup.ConfigureServices` is not
  `IConnector.ConfigureServicesForDataAdapter`.** The first is empty in every shipped connector.
  The second is the only way a data adapter reaches the connector's API client. Registering the
  client in the wrong one fails at runtime with nothing at compile time.
- **`PublicApiInterfaces` on the data adapter startup is the boundary.** A method not declared
  on one of the interfaces listed there is unreachable from a portal, with no error anywhere.
- **Downloads and folder navigation are not yours to implement.** `IAssetsDownload` and
  `IAssetsFolderNavigation` are portal-facing, and Smint.io implements them across data adapters.
  A connector's adapter contributes download options from
  `GetCustomAssetDownloadItemMappingsAsync`, serves the bytes from `GetAssetDownloadStreamAsync`,
  and serves folders from `GetFolderContentsForIntegrationLayerAsync`. For upload it is the
  reverse trap: implement `HandleAssetUploadsAsync` and **do not** override
  `GetAssetUploadSettingsAsync`, which the base class projects from your configuration.
- **The meta-model is a filter.** A value whose property was never declared is dropped on
  conversion, silently. If data is missing in the portal, check the declaration before debugging
  the conversion.
- **The meta-model refreshes on its own** — when the configuration is set up and regularly
  afterwards, and on an administrator action. Ordinary schema evolution in the external system is picked
  up automatically, and the re-indexing it implies is scheduled for you. Do not tell anyone they
  have to set the connector configuration up again for a new field to appear.
- **Three forms turn a resource key into a localized string, and they are not interchangeable.**
  `new ResourceLocalizedStringsModel(nameof(X.key))` hands the platform a *reference* it resolves
  later — right for startup metadata and meta-model labels.
  `X.ResourceManager.FullyResolveToLocalizedStringsModel(nameof(X.key))` returns a *finished*
  model carrying every language the component ships — right whenever you build the model yourself
  (allowed values, permissions, download item descriptions); resolve it once into a
  `static readonly` field, and note it returns `null` when the key has no `en-US` value.
  `X.key.Localize()` gives a single-culture model for the current request only.
- **Never hard-code a meta-model entity key.** Keys are rewritten to be unique per connector
  configuration, so the same connector configured twice yields two different key sets.
- **The meta-model identifier is the index identity, and changing it triggers a full re-index.**
  Keep it stable while the source is the same — never derive it from a timestamp, a token, a
  build number or the connector's version — and make it differ when the configuration points at
  genuinely different content. Changing it deliberately is the lever for "rebuild everything",
  correct when a meta-model change makes indexed data uninterpretable; it is expensive, so it is
  agreed with Smint.io on a production source and stated in the release notes, never slipped in.
- **Renaming a configuration property orphans every saved configuration.** The C# property name
  *is* the persisted name, and there is no annotation that decouples them. Start with few
  properties: you can add, you cannot remove.
- **A required property the setup wizard does not show needs a `DefaultValue`.** The wizard shows
  `Basic` items only yet still enforces what is required, so a required `Advanced` or `Expert`
  property with no default makes the component impossible to create, and the error names a field
  nobody was offered. `Required` plus `Advanced` is fine *with* a default. A non-nullable value
  type reaches the same wall with the word `Required` nowhere in the code — it is implicitly
  required, so an `Advanced` `bool` needs a `DefaultValue` too. Where no default is safe, such as
  a signing key, make the property optional and give its absence a defined, safe behaviour
  instead — see
  [visibility and the setup wizard](docs/smintio-backend-annotations.md#user-content-visibility-and-the-component-setup-wizard).
- **Validate against the external system in `PerformPostConfigurationChecksAsync`** and throw
  `ExternalDependencyException`. It is the one moment where a wrong credential reaches the person
  who can fix it.
- **Feature flags are promises.** `GetFeatureSupportAsync` and
  `GetAssetsReadFeatureSupportAsync` must match what the methods actually do, and an unsupported
  method throws `NotImplementedException` rather than returning empty.
- **Set `PermissionUuids` on every asset and folder** your converter produces, or the portal
  hides the actions even though the interface-level permission check passed.
- **Bump `<Version>` in the `.csproj` for every publish.** Publishing the same version again is
  rejected, after the build has already run.
- **Nothing in a component's code decides where it is published.** The environment and the
  Smint.io instance come from the publish CLI's `appsettings.<Env>.json`. Never read or quote
  values out of those files — they hold OAuth credentials.

## A new component starts with an interview

Never start coding one from a one-line request. The things that cannot be corrected later — the
issued `Key`, the component type, the persisted configuration property names, the integration
mode, the instance it is published to — have to be settled before the first file is written.

Collect them as an **interview**: structured multiple-choice prompts, at most four questions per
prompt, your recommendation first, never a paragraph of prose questions. Ask only for what
cannot be derived — state the derived values instead of asking for them (the project name
follows the folder name; the namespace follows the assembly name; the target framework, the SDK
package versions and the project reference flags come from the example you copied).

The same questions, written for a human rather than as an asking protocol, are in `README.md`
under *Before you start: the questions to answer*. Keep the two in step.

**Ask all of the questions. Do not skip any.**

### Round 1 — identity (blocks everything else)

| Ask | Offer | Why it cannot wait |
|---|---|---|
| Which component type is this? | recommend from the one-sentence description: **connector + data adapter** for a new external system; **data adapter only** when a connector for that system already exists; **data processor** when the data is right but its shape, naming or reachability is not; **identity provider** for a non-standard SSO workflow; **task handler** for an approval workflow; **portal template** or **resource** for the templating stack | each is a different contract, a different SDK package and a different document. Getting this wrong is a rewrite, and the most common error is reaching for a connector when a data processor was the answer |
| Which external system, and what does the component do, in one sentence? | — | it decides the folder name, the assembly name and everything below |
| **Productized or custom?** | *productized* — it serves the standard portal experience, implements `IAssets` and needs a meta-model / *custom* — it publishes its own interfaces for one custom UI component and needs no meta-model. Recommend from the one-sentence description: if the answer names a UI component that will call it, it is custom; if it names "our DAM in the portal", it is productized | it decides whether there is a meta-model, an integration mode, an `AssetDataObject` mapping and a shared test suite at all — the difference between a large piece of work and a small one. Retrofitting a meta-model later is not a small change |
| **Has Smint.io issued the component key?** | the key, or "not yet — ask Smint.io" as an explicit option | keys are globally unique and issued, like port numbers. **If the answer is "not yet", say so in the final report and use a clearly provisional placeholder** — do not invent something that looks real |
| New component, or a change to an existing one? | a new one / a variant alongside the existing one / change the existing one in place | you never rename a released component. Portals in the field are configured against the old key |

### Round 2 — how the integration works

Skip this round for a data processor, identity provider, portal template or resource, and ask
the round-2 questions in that type's own document instead.

Ask these of both kinds:

| Ask | Offer | Why |
|---|---|---|
| How does the external system authenticate? | an API key or static credentials (implement `IConnector` directly — the common case) / OAuth2 client credentials (derive from `OAuth2Connector`, setup method `Setup`) / OAuth2 authorization code, user consents in a browser (`OAuth2Connector`, setup method `Redirect`) / the same with PKCE (`OAuth2AuthenticationCodeFlowWithPKCEConnector`) / something else | it picks the base class and the setup method. **`OAuth2Connector` does not imply `Redirect`** — most OAuth2 connectors use `Setup`. Choose the setup method from what the *user* has to do |
| Which existing example should this start from? | recommend HelloWorld for the shape, Picturepark for a live connection, SharePoint for an indexed one — and say which of the three the new component most resembles | it fixes half the structure |

**If productized**, additionally:

| Ask | Offer | Why |
|---|---|---|
| **Live connection or indexed through the integration layer?** | recommend **live** when the external system has faceted search and translated metadata and is fast enough to sit in front of a portal; recommend **indexed** when it does not, or when renditions have to be generated | it decides which configuration marker interfaces the data adapter implements, whether you write an `IAssetsIntegrationLayerApiProvider`, and how synchronisation works. It is the single most structural decision after the component type |
| Which standard public API interfaces does the data adapter publish? | recommend `IAssets`; add `IAssetsUpload` for upload, `ICollections…`, `IShares…`, `IResourceAssets…`, `IProductAssets…` as they apply. **Do not offer `IAssetsDownload` or `IAssetsFolderNavigation`** — both are portal-facing and Smint.io implements them across data adapters | this is the reachable surface. Declaring more than you implement means the portal calls methods you have not written |
| What does the external system's schema look like, and how do you read it? | ask whether the schema is discoverable through an API, and whether metadata is translated on that side | the meta-model builder is a substantial part of a productized connector, and whether the schema can be read at all decides whether the connector is even viable |

**If custom**, additionally:

| Ask | Offer | Why |
|---|---|---|
| Which operations does the UI component need, and what does each return? | write the interface out as a list of methods with their parameter and result shapes, and read it back before implementing | **the interface is the data model.** The UI component deserializes against the TypeScript generated from these types, so renaming or retyping a field later is a breaking change on both sides |
| Which UI component consumes it, and does it exist yet? | the component, or "to be written" | if it does not exist yet, the interface is being designed blind. Ask what the component has to display before fixing the result shape |
| Anything here that a standard interface already does? | name it if so | a custom interface that duplicates `IAssetsSearch` is worth questioning. Do not ask twice — if it is clearly custom work, say so and move on |

**Do not ask a custom component about the meta-model, the integration mode or the asset data
model.** They do not apply. State that you are skipping them and why.

### Round 3 — configuration and language

| Ask | Offer | Why |
|---|---|---|
| Which settings must the administrator be able to change, and which are advanced? | list them, and mark which are `Advanced` or `Expert` | each becomes a configuration property whose **name is persisted permanently**. Start with few. Ask which of them the administrator should pick from a dropdown the component fills — a channel, a site, a list — because that is a dynamic allowed-values provider rather than a text field |
| For each setting: does it belong on the connector or on the data adapter? | propose the split yourself and have it confirmed | connector = what the connection itself needs; data adapter = anything only that adapter interprets, including customer-specific settings and its own secrets. Several adapters share one connector, so a connector property forces the same value on all of them. Configurations are stored encrypted at rest, so a secret is fine in either |
| Which languages? | English only / English plus one more / more | English only can stay on `[DisplayName("en", …)]` attributes; more than two means resource files from the start, and retrofitting them touches every property |
| Does the component need translatable meta-model labels? | yes / no — and skip the question entirely for a custom component | it decides whether you ship a `MetamodelMessages.resx` and point `MetamodelMessages` at it. Leaving it `null` when you need it makes translations fail silently |
| Which permissions apply? | the standard ones (the default — declare none) / a custom interface needing its own | custom permissions are three coordinated steps, and missing the third makes the method permanently denied |

### Round 4 — delivery

| Ask | Offer | Why |
|---|---|---|
| How will this be tested against the real system? | credentials for a sandbox or test tenant / a live tenant / artificial data only | the test driver needs real credentials to be worth anything, and a redirect-flow connector additionally needs its redirect URI registered on the external side. For a productized component, ask for the sample asset ids the shared test suite needs at the same time |
| **Who reviews it, and when?** | a name or team, and a date; offer "not arranged yet" as an explicit option | **a backend component reaches production only through Smint.io, after a code review.** If this is not arranged, say so in the final report — the work is not finished when the code is |
| Is there a development environment to deploy to, and has Smint.io set it up? | yes, with the environment named / no | the publish CLI is only useful against an environment that already exists for this component. If the answer is no, there is nothing to publish to and you should not try |
| Is there a ticket or issue id? | the id, or none | the commit message and the pull request |

**Never publish unprompted**, and never to production. Ask before any publish, every time, and
state the environment in the question.

### Decide these yourself — do not ask

Target `net10.0`. Start `<Version>` at `1.0.0` for a new component, or at whatever the existing
one is on. Follow the project layout and the partial-class split of the example you copied.
Name every resource key — in `ConfigurationMessages` and in `MetamodelMessages` alike —
`<kind>_<component key with hyphens as underscores>_…`, where the kind is `c` connector,
`da` data adapter, `dp` data processor, `idp` identity provider, `th` task handler,
`pot` portal template, `r` resource. Every component's resource file is loaded alongside every
other one and the SDK's own, so the prefix is what stops two components' keys colliding. Put the
`Private="false" ExcludeAssets="runtime"` flags on the data adapter's project reference to the
connector. Structure the test project as `Harness/` and `Integration/`, and inherit the shared
test suite rather than writing the basic tests.

If something stays unanswered, pick the sensible default, build the component, and say in the
final report which assumption you made and where it is easy to change.

## Prefer the smaller component

The most expensive mistake in this area is building a connector when something smaller was the
answer. Work down this list and stop at the first that fits:

1. **Nothing** — the external system speaks a protocol a shipped connector already handles, and
   this is configuration.
2. **A data processor.** The data is reaching the portal and something about its shape, naming or
   reachability is wrong. A processor is a class and a lifecycle phase interface; a connector is
   a project.
3. **A second data adapter on an existing connector.** The external system is already integrated
   and this is a different view of it — upload, collections, products, a customer-specific
   mapping. Adapters outnumber connectors for exactly this reason. The connector is reused
   unchanged, and its client comes to you by injection.
4. **A custom public API interface on an existing data adapter.** The data is there and only the
   operation is missing.
5. **A new connector and data adapter.** An external system nothing integrates yet.

State which of these you concluded and why, in one line, before you start. If the answer is 5,
say what you ruled out — and then say whether it is **productized or custom**, because that
decides how large 5 actually is.

## Probing the external system before you design against it

You will work the external API out by hand — a script, a REST client, the vendor's own explorer —
before any of it is C#. Four rules, each of which has cost someone a day.

- **Authenticate as the principal your connector will be.** With the client-credentials grant an
  identity server will happily issue a valid token for a request that asked for no scopes, and
  every call then fails as unauthorized. Send the same scopes the connector declares in
  `RequiredScopes`. A token that is issued is not a token that is authorized, and the failure
  surfaces at the wrong end: credentials, token and account all look correct and the external
  system looks broken.
- **Do not conclude a capability is missing from a schema you introspected.** Where the API
  describes its own schema, that description may be filtered to your principal, and a narrowed
  schema is indistinguishable from a small one. Call the operation anyway and read the error —
  "not authorized" and "no such field" are different answers, and only one means the capability
  does not exist.
- **Confirm the API accepts the kind of data you intend to send**, rather than inferring it from
  an operation's name. Create and update operations that look complete are often metadata-only,
  with binary content moving through a separate channel or through none — uploading is frequently
  a user-interface capability that was never exposed for API use. Settle this before designing a
  feature around it: a field the external system cannot store is not a smaller feature, it is a
  different design.
- **Never build on an endpoint you found by watching the vendor's own web application.** It is
  not a supported integration point and it will break. If the documented API cannot do it, that
  is a finding to report, not an obstacle to route around.

Report what you actually established, and separate it from what you assumed. "The documented API
has no upload path" and "I could not find an upload path" are different claims, and the second one
is not a reason to invent one.

## Writing the component

### A connector

Read `docs/smintio-connector-reference.md` in full first. Then:

- Copy the **HelloWorld** project layout: the three classes at the project root, `Client/` with
  the interface and `Client/Impl/` with the implementation and the request failed handler,
  `Metamodel/`, `Models/`, `Extensions/`, `AllowedValues/`, `Resources/`.
- Put the external API behind your own `IMyClient` and a class deriving from
  `BaseRestSharpApiClient` — or `BaseDynamicApiClient<T>` when you are wrapping a vendor SDK, or
  `BaseHttpClientApiClient` when neither fits. **Write the `IRequestFailedHandler`**: it is what
  turns the external system's errors into something the platform and a support engineer can read.
- Wrap every call in the base class's `Execute…WithBackoffAsync` (reads, idempotent calls) or
  `Execute…WithoutBackoffAsync` (anything a retry would duplicate), with a short `hint`.
- **Keep every credential inside the connector and the client implementation.** The client
  interface exposes operations, never a token, a secret, a key, an authorization header or a raw
  HTTP client. Log the operation and the outcome, never the header you sent.
- Offer dropdowns rather than identifiers: an `AllowedValues/` provider per configured id.
- **Productized only:** build the meta-model in a separate `IMetamodelBuilder`. It runs at
  configuration time, so it is allowed to be slow — make it correct. A custom connector skips
  this, the `Metamodel/` folder and `MetamodelMessages` entirely.

### A data adapter

Read `docs/smintio-data-adapter-interfaces.md` first — and `docs/smintio-asset-data-model.md`
too if the component is productized.

**Productized:**

- Derive from `AssetsDataAdapterBaseImpl` and implement its eleven abstract members. Everything
  else it supplies is `virtual`; override the smallest thing that expresses the difference.
- Split the class across partial files by capability — `Search/`, `Read/`, `Random/`,
  `Download/`, `IntegrationLayer/` — with the constructor and feature support in the file named
  after the class, and the converters in `Common/`. Repeat the **same** class signature in every
  file.
- Set the well-known fields directly on `AssetDataObject`; only source-specific metadata goes
  through the converter into `rawData`.

**Custom:**

- Derive from `DataAdapterBaseImpl` and implement your own interface. Do not implement `IAssets`
  or touch `AssetDataObject` unless something actually consumes them.
- **Design the interface as a published API**, because it is one: the UI component deserializes
  against the TypeScript generated from your parameter and result types. Name the fields for the
  consumer, keep them stable, and regenerate the `.ts` with the Data Adapter Exporter CLI
  whenever they change.
- Implement `UnscopeIdentifiers` and `ScopeIdentifiers` properly if your types carry asset,
  folder or resource identifiers — or derive from the `Prefab` envelopes.

**Both:** take the connector's client as a constructor parameter; pull the platform's own
services off the injected `IServiceProvider`; and never hold a credential.

Four things partners reach for and get wrong, all covered in the data adapter document:

- State the external system has no field for — that a one-time action already happened, say —
  goes in `IIdPersistentStorage` or `ITemporalPersistentStorage`, **not** in a status or comment
  field of the external system, where that system's own processes will overwrite it.
- **Never scope a key yourself.** `ICache`, `IIdPersistentStorage`, `ITemporalPersistentStorage`,
  `IStorageBackedLock` and the other component-scoped services are already partitioned by tenant
  and by configured component instance. A tenant id, portal id, connector key or configuration id
  in a key is redundant and makes the entry harder to find. Uniqueness within your own component
  is the only part that is yours.
- A `lock` statement protects nothing. Component instances are short-lived and spread over more
  than one machine, so when two requests must not write to the external system at once, take an
  `IStorageBackedLock` — a short duration, prolonged as you go, a fresh lock key per acquisition,
  and `ClearLockAsync` in a `finally`. It returns `false` rather than waiting.
- "Never hold a credential" is about authenticating to the external system. It does **not** mean a
  data adapter has no security to write: when your component is reached by a signed, time-limited
  link handed to someone outside the portal, verifying that link is yours to do and nothing else
  does it. Verify the signature before you act on anything the link carries. The signing key is a
  property of **the data adapter's own configuration**, which is stored encrypted at rest — not
  something to push onto the connector or hard-code.

### Anything else

Each of the other component types has its own document, and each is a much smaller job than a
connector. Read the document, copy its contract, and note that three of them have a shape that
catches people out: **a data processor with no lifecycle phase interface never runs**, **a
resource has no behaviour at all**, and **a portal template is normally built by cloning a
configured live portal rather than by implementing its four methods**.

## Verifying your work

**Build.** `dotnet build Portals-SmintIo-BackendComponents.sln` from `Backend/`, or your own
solution. It is fast and it catches the partial-class and interface mistakes.

**Then run it outside the platform.** The test drivers construct your real connector and data
adapter against the real external system with in-memory stand-ins for everything else. A
connector that fails there would have failed in the platform. The full setup is in
`docs/smintio-backend-component-delivery.md`.

**Which tests depends on the kind of component.**

- **Productized:** inherit the shared test suite from `SmintIo.Portals.Connector.Test` and
  `SmintIo.Portals.DataAdapter.Test` rather than writing the basic tests. It asserts things that
  are easy to get wrong and hard to notice, and it costs one class per suite plus sample data in
  `appsettings.json`.
- **Custom:** do **not** inherit it. Almost everything it asserts is about the standard asset
  surface you deliberately did not implement, so it produces a long list of meaningless failures
  — and the next move is always to suppress them, which defeats the point. Use the test drivers
  directly and assert the happy path, the shape of the result, identifier round-tripping, the
  failure paths and the authentication lifecycle.

**Inheriting the shared suite couples your test project to the SDK's test surface**, which is a
cost worth knowing before you opt in. Its base classes are abstract, and a later SDK version can
add a member to them; when that happens your test project stops compiling, in a repository nobody
has touched. Expect it on an SDK upgrade, and read it as one more reason a custom component
should not inherit the suite.

Where a component does inherit it for the checks that *do* apply, the ones that do not are
`virtual` — that is the intended escape hatch. Override them and assert the absence deliberately
("this component serves no assets, so it has no output-format configuration"), so that the day it
gains one of those markers the override fails and gets reconsidered. An override with an empty
body passes while asserting nothing, which is worse than the failure it replaced.

**A component's tests live with the component.** A second data adapter — a customer-specific
variant, say — gets its own test project beside it, with its own fixture and its own settings
section, rather than extending the fixture of the component it was modelled on. A shared fixture
makes two components depend on each other at build time and hands one of them a base class it can
break for the other; the twenty lines of connector setup you duplicate are much the cheaper half
of that trade.

**Write a round-trip test for every value your component writes back.** Reads are easy to eyeball;
writes are not, because the external system may normalise, truncate or reinterpret what you sent.
Dates are the classic: a value standing for a month, converted to UTC on the way out, lands in the
*previous* month for any positive offset — and that passes on a developer machine running UTC and
is wrong everywhere else. Assert the value you read back, not that the call succeeded.

**Then look at it in a real portal**, if there is a development environment for it. Two things
only a portal shows you: whether the configuration form is usable, and whether the data renders.
A productized component that passes its tests and produces a portal page showing nothing is
usually one of three things — a property missing from the meta-model, `PermissionUuids` not set,
or an interface not listed in `PublicApiInterfaces`. Check those three before looking further.

**When your component decides to return nothing, log why.** A silent empty result is
indistinguishable from a broken registration, a stale version or a wrong configuration, and you
will pay for the ambiguity in support time.

## Delivery — the work does not end at "it builds"

**Publishing a backend component directly to a Smint.io production system is not supported for
third parties.** A backend component is trusted server-side code running inside the platform, so
every one of them — from Smint.io, from a Solution Partner, from an Enterprise customer — goes
through Smint.io and through a **code review** before it reaches production. That review is a
security control, not a formality.

So the path is: **write it → prove it with tests → hand it to Smint.io for review and rollout.**

- **Do not plan a task around publishing to production.** If the request implies "and then it is
  live", say in the plan that the last step is a hand-over, and ask who is reviewing it.
- **The publish CLI is for a development environment Smint.io has set up**, and only if one
  exists for this component. If nobody has mentioned one, there is nothing to publish to — do not
  go looking for the tool.
- **Ask before any publish, every time**, and name the environment in the question. Never
  production.
- **Bump `<Version>` for every hand-over.** It is how everyone refers to what was reviewed and
  what was rolled out.
- **Write for the review.** The things it comes back on are, in order: a credential left in the
  repository or its history, a credential exposed through the client interface, a dependency that
  cannot be justified, errors swallowed instead of turned into `ExternalDependencyException`, and
  permissions not set on the objects the component emits. All of them are cheaper to get right
  the first time. The list is in `docs/smintio-backend-component-delivery.md`.

## Working in this repository itself

If you are changing the **documentation** here rather than building a component, read
`CLAUDE.md` at the repository root first. Everything in this repository is read by people
outside Smint.io: no customer or tenant names, no internal repository or path references, no
internal class names from the platform's own source, no credentials or configuration values, and
only supported, documented behaviour.

The line to hold is between the **SDK types a partner writes against** — `IConnectorStartup`,
`AssetsDataAdapterBaseImpl`, `DataAdapterPermission`, the lifecycle phase interfaces — which
belong here and should be named precisely, and the **platform's own implementation** — the
classes that execute a method, resolve a component or persist a configuration — which does not
belong here at all. Describe what the platform does; do not name what does it.

The backend material has internal authorities on the Smint.io side: the connector SDK's and data
adapter SDK's own guidance describes the meta-model and the asset data model in full. When the
SDK changes, update these documents from the source types rather than from memory — and keep
them free of the internal detail those documents carry.

When you learn something about building a backend component, **write it down here**, anonymised.
A rule that applies to any component belongs in this repository.
