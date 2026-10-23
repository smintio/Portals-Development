Developing Smint.io Portals frontend components
===============================================

This README.md serves to clarify the general concept of Smint.io Portals frontend components, which is page templates and UI components.
Also it contains an overview of a lot of currently existing default Smint.io Portals frontend components that are delivered and maintained directly by us.

Finally, it will shed some light on how you can get started with developing your own custom Smint.io Portals frontend components.

You can build your own page templates or UI components at any time, based on our Smint.io Portals SDKs.
Access to them is restricted and is arranged with Smint.io — see
[getting access to the SDKs](../../Overview/README.md#user-content-getting-access-to-the-sdks).

1. [UI components](#user-content-ui-components)
1. [Page templates](#user-content-page-templates)
1. [Portal templates](#user-content-portal-templates)
1. [Data adapter public API interfaces](#user-content-data-adapter-public-api-interfaces)
1. [Recipes: how do I …? — worked answers to the common tasks](docs/smintio-frontend-recipes.md)
1. [Overview of Smint.io UI components](docs/smintio-ui-components.md)
1. [Overview of Smint.io page templates](docs/smintio-page-templates.md)
1. [Overview of Smint.io mixins](docs/smintio-mixins.md)
1. [Overview of Smint.io annotations](docs/smintio-annotations.md)
1. [Overview of Smint.io frontend component types](docs/smintio-frontend-component-types.md)
1. [Frontend reference: services, filters, property mixins, providers, CSS](docs/smintio-frontend-reference.md)
1. [Data adapter reference: every public API interface and model](docs/smintio-data-adapter-reference.md)
1. [Page type contracts: what a page hands to the components it hosts](docs/smintio-page-type-contracts.md)
1. [Building a page template: slots, contracts and the page skeleton](docs/smintio-building-page-templates.md)
1. [Reference sources: the components Smint.io ships, for Certified Partners](Reference/README.md)
1. [How to develop your own frontend component](#user-content-how-to-develop-your-own-frontend-component)
1. [Before you start: the questions to answer](#user-content-before-you-start-the-questions-to-answer)
1. [Getting started](#user-content-getting-started)
1. [When npm cannot authenticate](#user-content-when-npm-cannot-authenticate-azure-devops-feeds-on-windows)
1. [Anatomy of a component package](#user-content-anatomy-of-a-component-package)
1. [Things to do for Mac or Linux users](#user-content-things-to-do-for-mac-or-linux-users)
1. [The example frontend component](#user-content-the-example-frontend-component)
1. [Crafting a page template instead](#user-content-crafting-a-page-template-instead)
1. [Building a section component](#user-content-building-a-section-component)
1. [Build your custom frontend component](#user-content-build-your-custom-frontend-component)
1. [Publish your custom frontend component](#user-content-publish-your-custom-frontend-component)
1. [Local development](#user-content-local-development)
1. [Common problems](#user-content-common-problems)
1. [Checklist before you ship](#user-content-checklist-before-you-ship)
1. [How we built our own components](#user-content-how-we-built-our-own-components)
1. [Problems](#user-content-problems)

Current version of this document is: 1.14.0 (as of 23rd of October, 2026)

## UI components

The most atomic part of a Smint.io Portal is the *UI component* — a header bar, a text block, a search result display
and so on. [What are Smint.io Portals components?](../../Overview/README.md#user-content-ui-components) introduces the
concept; this section is about building one.

*Focus on simplicity*

To make the setup of a portal very easy and fast for a Smint.io Portals user, please avoid building very granular or
highly flexible UI components (like a single button, with a lot of options to customize).

It is desirable to build higher level UI components (like a complete metadata viewer) with less options, because any
excess granularity or flexibility will cause confusion and source of error. On top of that it will confuse users that
are not as tech savvy as we developers or designers are.

*Start simple, become more sophisticated over time*

So please build higher level UI components, and avoid a lot of options at the start. If needed, you can always introduce
additional options to your UI components and increase the flexibility of your UI component at any time later on.

*No nesting*

Please note, that components are high level elements. So, it is not possible to nest UI components. An UI component
may *NOT* contain another UI component.

*Note*: this applies to *UI components*. Of course you can nest non-Smint.io Portals UI components (aka
Vue.js components, see below) within your Smint.io Portals UI component as you like!

*Note*: there is a concept called *sections*. A section consists of the *section start* UI component (typically written
by you), and by the *section end* UI component (which is a generic component coming from us). Between the *section start*
and the *section end* UI component, users can add any number of other UI components. During rendering, the *section start*
UI component will get a reference to all the UI components within the section, and can then render the section accordingly.

The Smint.io Portals *smintio-ui-generic-section-start-table-1* UI component, for example, uses the section concept to implement a
very sophisticated and responsive table construct.

*Standalone*

UI components must *NOT* be dependent on the presence of any other UI component on the page, or on functionality
provided by the page itself. UI components should always be able to work standalone. UI components *MAY* communicate
with other UI components or with the page using props or events, but please keep this rule in mind.

*There is one exception*: the UI component can enforce through its settings to only be allowed to be added to a certain
page type (see below). E.g. a search page or an asset details page usually coordinates its UI components and provides
certain functionality. In this case, the UI component may rely on the page to behave as it is being specified by the
page type interface specification.

*Layout*

The most important layout rule for UI components is, that the UI component content always *MUST* fill its layout box.
The UI component is *NOT* responsible for padding and margin management outside of its own layout box.

This means that the UI component *MUST NOT HAVE* any white space around its content. This also means, that the
content *MUST NOT* exceed the UI component's layout box. This is to make sure that paddings and margins on the overall
page stay regular and look good (I am sure you know of situations where padding and margin management quickly became a
nightmare because of not following this rule).

*Settings*

Your Smint.io Portals UI component should only have settings that actually work. Please do *NEVER* add settings to
your Smint.io Portals UI component that have no effect!

*UI component ID and UI component type*

Each Smint.io Portals UI component has an ID that is being defined by the developer of the UI component. Please prefix
your UI component IDs by your partner ID (e.g. `smintio-`) to avoid ID clashes. On top of that it is assigned
to an UI component type. There is a predefined list of UI component types (see below) which is maintained by Smint.io.
If you need an additional UI component type, please get in touch.

*Instance creation*

UI component instances are being created either a) when the user creates a new portal from a portal template (see below)
or b) when the user creates a new page from a page template (see below) or c) when the user adds a new UI component to a
page.

*SDK*

Smint.io Portals UI components are developed against the *Smint.io Portals UI component SDK*, which you consume as the
npm package `@smintio/portals-component-sdk`, usually together with the shared component library
`@smintio/portals-components`. The SDK is based on *TypeScript*, *Vue.js 2* and *Vuetify 2*. Vue.js *props* can be made
available for editing UI component properties by the user through *annotations*.

The UI components can then be published to any NPM repository from where our system can consume the
components. You can locally develop Smint.io Portals UI components while having them run in our production system by
running our very simple *Portals-DevServer*.

## Page templates

The second level of structure on top of UI components is the *page template*, which defines the structure of a page by
declaring and rendering so-called *slots* — a header slot, a content slot, a footer slot and so on (do NOT confuse with
Vue.js slots, which is a completely different concept to Smint.io Portals slots). See
[what are Smint.io Portals components?](../../Overview/README.md#user-content-page-templates) for the concept.

Each slot can optionally restrict or enforce the *UI component types* that fit into that slot (e.g. the header slot is
only allowed to contain UI components of type "Header"). Also the *minimum or maximum number of UI components* that fit
into the slot can be optionally defined.

The page template will have the instantiated UI components for the slots injected by the Smint.io Portals runtime
environment upon render time and is responsible to render the UI components to the DOM.

*Layout*

Page templates are responsible for padding and margin management. This means that page templates *MUST* take care about
padding and margins between all of their UI components.

*Settings*

Your Smint.io Portals page template should only have settings that actually work. Please do *NEVER* add settings to
your Smint.io Portals page template that have no effect!

*Page template ID and page type*

Each Smint.io Portals page template has an ID that is being defined by the developer of the page template. Please prefix
your page template IDs by your partner ID (e.g. `smintio-`) to avoid ID clashes. On top of that it is
assigned to a page type. There is a predefined list of page types (see below) which is maintained by Smint.io. If you
need an additional page type, please get in touch.

*Instance creation*

Pages are being created from a page template either a) when the user creates a new portal from a portal template (see
below) or b) when the user creates a new page from a page template.

*SDK*

Smint.io page templates are developed against the very same npm SDK as UI components — `@smintio/portals-component-sdk`,
usually together with `@smintio/portals-components`. There is no separate npm SDK for page templates. The SDK is based
on *TypeScript*, *Vue.js 2* and *Vuetify 2*. Vue.js *props* can be made available for editing page template properties
by the user through *annotations*. The page templates can then be published to any NPM repository from where our system
can consume the page templates. You can locally develop Smint.io Portals page templates while having them run in our
production system by running our very simple *Smint.io Portals dev server*.

The differences of a page template to a UI component are small and are described under
[How to develop your own frontend component](#user-content-how-to-develop-your-own-frontend-component) below.

## Portal templates

To wrap the story up, the topmost level of the structure of a Smint.io Portals portal is introduced by the *portal
template*. A portal template combines and configures page templates for a complete portal experience. The portal
template itself does *NOT* render any output to the frontend itself, it is just being used when instantiating a new
Smint.io Portal from the portal template.

*Portal template ID and portal type*

Each Smint.io Portals portal template has an ID that is being defined by the developer of the portal template. Please
prefix your portal template IDs by your partner ID (e.g. `smintio-`) to avoid ID clashes. On top of that it
is assigned to a portal type. There is a predefined list of portal types (see below) which is maintained by Smint.io. If
you need an additional portal type, please get in touch.

*SDK*

Smint.io portal templates can be developed using the *Smint.io Portals portal template SDK*
(`SmintIo.Portals.PortalTemplateSDK`). Unlike UI components and page templates, this SDK is based on *C#* (.NET).
Template properties can be made available for editing by the user through *annotations*. Portal templates need to be
submitted to Smint.io for approval and inclusion to our portal template library.

## Data adapter public API interfaces

Each Smint.io Portals UI component can tie itself to public API interfaces published by Smint.io Portals data adapters.

You can do this by requesting a data adapter public API interface through the configuration of the Smint.io Portals
UI component:

```javascript
import type {
    IAssetsSearch,
} from "@smintio/portals-component-sdk";

...

@DisplayName("en", "Data source for auto completion", true)
@DisplayName("de", "Daten-Quelle für die Auto-Vervollständigung")
@Description("en", "The data source to query the search bar auto completion suggestions from.", true)
@Description("de", "Die Daten-Quelle, aus der die Vorschläge für die Auto-Vervollständigung für die Such-Eingabeleiste geladen werden.")
@Implements("IAssetsSearch")
@ComponentProperty({ name: "searchBarAutoCompletion" })
@FormGroup("s-search-bar")
public readonly searchBarAutoCompletion!: IAssetsSearch;
```

Once the Smint.io Portals UI component is instantiated, you can easily call methods of that public API interface.

```javascript
this.searchBarAutoCompletion.getFullTextSearchProposalsAsync({ searchQueryString: this.searchQuery })
	.catch((e) => {
		...
	})
	.then((searchProposals?: IGetFullTextSearchProposalsResult) => {
		...
	})
	.finally(() => {
		...
	});
```

In the above example, the `IAssetsSearch` is a standard public API interface provided by Smint.io Portals.
Every standard interface, every parameter and result model, and every enumeration is listed in the
[data adapter reference](docs/smintio-data-adapter-reference.md) — including `IAssetDataObject`, the shape of an
asset, which is what most components end up rendering.

Two things to keep in mind when calling a data adapter public API interface:

- The property can be `undefined`, because the portal editor may not have configured a data source for it. Guard for it.
- A data adapter implements the methods of the interfaces it declares, and one that does not support a capability
  does not declare its interface — so it cannot be selected for that purpose. An *optional* capability within a
  declared interface, such as search proposals, can still be unsupported at runtime: catch the failure, switch that
  feature off for the session, and carry on rendering.

*The great thing is*: if you develop your own Smint.io Portals data adapter, you can easily publish your own custom
public API interfaces as well. This enables you to easily develop any custom functionality required using the Smint.io
Portals component framework and runtime.

However, for use of your custom public API interfaces in a Smint.io Portals UI component you'll need its Typescript
public API interface definition.

Use the [Smint.io Portals Data Adapter Exporter CLI tool](../../Tools/Portals-DataAdapter-SDK-DataAdapterExporter-CLI/Release/)
to generate the Typescript public API interface definition directly from your Smint.io Portals data adapter assembly.

You can then simply use that TypeScript public API interface definition file
in your Smint.io Portals UI component.

*All the wiring from frontend to backend is done for you, without any further work involved.*

Learn more about how to do that [here](../../Backend/README.md#user-content-custom-public-api-interfaces).

## How to develop your own frontend component

### Before you start: the questions to answer

A custom frontend component is quick to write and expensive to change once it is out. The
component `key`, its `type`, the persisted names of its configuration properties and the tenant
it is published for are all effectively permanent as soon as a portal has been configured
against it.

So please settle the following before you create the first file. If you are building the
component for a customer, for a colleague or for a project team, ask them all of it in one go —
it is a short conversation, and it saves a rewrite.

*Identity and placement*

| Question | What it decides | If it is wrong |
|---|---|---|
| Who is the component for, and what does it do — in one sentence? | the directory name, and with it the package name and the component `key` | the `key` is what portals store in their page configuration. It cannot be changed after release |
| Is this a new component, or a variation of one you already have? | whether you start a fresh `-1`, or add a `-2` package next to the existing one | never renumber a released component. Portals in the field are configured against the old name |
| Which frontend component type does it have? | which page template slots will accept it, and which page contract it can rely on — see [the frontend component types](docs/smintio-frontend-component-types.md) | a type that does not exist cannot be invented on your side. If none of them fits, get in touch at [support@smint.io](mailto:support@smint.io) |

*Where it sits, and where its content comes from*

| Question | What it decides |
|---|---|
| Which portal types is it for, and which page does it go on? | whether you restrict it with `allowedPortalTypes`, and which contract applies — [what a page hands to the components it hosts](docs/smintio-page-type-contracts.md) |
| Where does the content come from: the component's own configuration, a Smint.io data adapter, or a system of your own? | whether you work against the [data adapter public API interfaces](docs/smintio-data-adapter-reference.md), or declare your own interface |
| Which existing component is the closest starting point — and is yours *that component plus something*, or something new? | how much you have to write, and whether you copy it or extend it. `ui-example-hello-world-1` is the minimal skeleton; the [overview of Smint.io UI components](docs/smintio-ui-components.md) shows what already exists. An addition to an existing component should [extend it](#user-content-is-your-component-an-existing-one-plus-something-extend-it-do-not-copy-it) — a copy is a fork, and forks stop receiving later improvements to the component they came from |
| Which settings must the portal user be able to change, and which of them are advanced? | your configuration properties. Start with few — you can add options later, you cannot take them away. Text, link, colour and layout settings already exist as [mixins](docs/smintio-mixins.md), so only ask for what those do not cover |

*Content and language*

| Question | What it decides |
|---|---|
| Which languages does the component have to offer? | the `@DisplayName` and `@Description` annotations, and whether user-editable text is a localized string rather than a plain one |
| Is anything pre-filled or shipped with the component — default values, string resources, images, an icon for the page editor? | the resource definition and the contents of your `resources` folder. A component that arrives blank in the page editor looks broken |
| What should it look like: a design, a screenshot, an existing page to match, or your own judgement? | there is no other source for this, and it is the question most often left unasked |

*Delivery*

| Question | What it decides |
|---|---|
| Which tenant is the component published for? | who can actually use it. Nothing in the component's source decides this — see [Which tenant your component is published for](#user-content-which-tenant-your-component-is-published-for). Agree it up front, because it is the one thing that cannot be checked by looking at the code |
| Which environment: development, staging or production? | which `appsettings` file the publish uses, and therefore which `smint-io-pc` script you run. Each environment has its own tenant URL, so confirm the tenant per environment |

If the component should be available more widely than a single tenant, that is something we
arrange on our side — please get in touch at [support@smint.io](mailto:support@smint.io) rather
than trying to configure it in the component.

### Getting started

Please note that [access to the SDKs is restricted](../../Overview/README.md#user-content-getting-access-to-the-sdks).

Steps to follow:

1. Navigate to a folder where the components should be physically present. We will call it the root folder
2. Copy **both** [`Example/ui-example-hello-world-1`](Example/ui-example-hello-world-1) and the shared [`config`](config) directory out of this repository, keeping the same two-level distance between them

	```
	<root folder>/
	├── config/                           <- copied from Frontend/Legacy/config
	└── components/
	    └── ui-example-hello-world-1/     <- copied from Frontend/Legacy/Example
	```

	- The component's `rollup.config.js` and `tsconfig.json` are small stubs that resolve `../../config/…`, so the depth matters. Put the component one directory below the root folder, not directly in it
	- If you get `Could not resolve '../../config/rollup/rollup-config.ts'` on your first build, this is why

3. Rename the `ui-example-hello-world-1` directory to your desired frontend component name

	- Start the directory name with `ui-` for UI components, and `page-` for page templates
	- End the directory name with `-1` so that later on, if desired, you can potentially create different variations of the frontend component

4. Edit the `package.json` file in the new directory, and change `name`, `description`, `version`, `author` and `publishConfig` of your new frontend component

	- Prefix the package `name` with your partner ID e.g. `@smintio/`
	- Then simply add the directory name from the previous step as the package name
	- Point `publishConfig.registry` at **the registry you publish to**. The example carries the Smint.io SDK feed, which is read-only for you — leaving it there makes `npm publish` fail, or publish to the wrong place. See [where your own components are published](#user-content-where-your-own-components-are-published)

5. Make sure you use node version 12.22.10 (use [NVM](https://github.com/nvm-sh/nvm) if you use different node versions)
6. Make sure that your component .npmrc file contains the proper reference to our NPM SDK repo:

```
@smintio:registry=https://smintio.pkgs.visualstudio.com/_packaging/Portals-Components-Public/npm/registry/
always-auth=true
```

7. Authorize your NPM for use of our `Portals-Components-Public` NPM SDK repo. You will find more info by accessing the [Azure DevOps location of our NPM SDK repo](https://smintio.visualstudio.com/SmintIo-UIComponents/_artifacts/feed/Portals-Components-Public) and clicking `Connect to feed` -> `npm` -> select `Windows` or `Other` for instructions
8. Add a second registry line to the same `.npmrc`, for the registry **your own components** are published to. Which one that is depends on who you are — see [where your own components are published](#user-content-where-your-own-components-are-published). If Smint.io hosts a partner feed for you, it is the one below (replace `partner-id` by your partner ID); otherwise point the line at your own registry and skip step 9:

```
@[partner-id]:registry=https://smintio.pkgs.visualstudio.com/_packaging/Portals-Components-Partners-[partner-id]/npm/registry/
always-auth=true
```

9. If Smint.io hosts the feed for you, authorize your NPM for use of your partner NPM repo. You will find more info by accessing the Azure DevOps location of your partner repo (https://smintio.visualstudio.com/SmintIo-UIComponents/_artifacts/feed/Portals-Components-Partners-[partner-id]) and by then clicking `Connect to feed` -> `npm` -> select `Windows` or `Other` for instructions)

10. Run `npm i` at the first time, or when you update dependencies

	- If there is any authorization issues you are running into, you will have done something wrong in step 6-9. Please revisit your settings. If the feed is hosted on Azure DevOps and you are on Windows, the token may simply have expired — see [when npm cannot authenticate](#user-content-when-npm-cannot-authenticate-azure-devops-feeds-on-windows)
	- If you absolutely cannot manage to get going, please get in touch at [support@smint.io](mailto:support@smint.io)
	
11. Please adjust `src/PortalsUiComponent.vue` accordingly

#### When npm cannot authenticate (Azure DevOps feeds on Windows)

The credentials an Azure DevOps npm feed issues are **tokens that expire**, so a setup that
worked last month fails today with a `401` on `npm i` or on `npm publish` — and nothing about
your `.npmrc` has changed. You do not have to walk through *Connect to feed* again.

On Windows, `vsts-npm-auth` refreshes the token in place. Run it in the directory that holds the
`.npmrc`:

```console
vsts-npm-auth -config .npmrc -force
```

It reads the registries named in that `.npmrc`, acquires a fresh token for each, and writes it
into your user-level `.npmrc` — leaving the component's own file untouched. `-force` refreshes
even when the existing token has not visibly expired, which is what you want when you are
diagnosing an authorization failure.

Install it once with `npm install -g vsts-npm-auth` if it is not already on your machine.

Notes:

- **Windows only.** On macOS and Linux, follow the *Connect to feed* → `Other` instructions for
  the feed, which produce a base64 personal access token you paste into your user-level `.npmrc`.
  Those tokens expire too, so the same symptom has the same cause.
- **Run it in the component directory**, not in the root folder — the `.npmrc` that names your
  registries is the component's.
- It applies to **any** Azure DevOps-hosted feed, so it covers both the Smint.io SDK feed and a
  partner or customer feed hosted there. For a registry hosted elsewhere, use whatever that
  registry's own authentication flow is.

#### Where your own components are published

The two feeds above are not the same thing, and only one of them is ours to give you:

| Feed | What it is | Who has one |
|---|---|---|
| `Portals-Components-Public` | the **SDK packages** you build against — `@smintio/portals-component-sdk`, `@smintio/portals-components` and their siblings. Read-only | everyone with SDK access |
| `Portals-Components-Partners-<partner-id>` | where **your own** built components are published, and where a Smint.io system installs them from | Smint.io itself, and the Solution Partners Smint.io works with |

**Smint.io hosts a partner feed for Smint.io and for its Solution Partners only.** If you are
neither — you are building components for **one customer of your own**, or you run your own
**VPC deployment** of Smint.io Portals — then you host the npm repository for your components
yourself. Any npm registry that supports scoped packages and authentication will do: Azure
Artifacts, GitHub Packages, JFrog Artifactory, Nexus, a private npm organization.

Nothing in a component changes because of it. You still consume the SDK from
`Portals-Components-Public`, and the only difference is the second `@scope:registry` line in
`.npmrc` and the `publishConfig` in `package.json`, both of which point at your registry instead
of a Smint.io one:

```
@smintio:registry=https://smintio.pkgs.visualstudio.com/_packaging/Portals-Components-Public/npm/registry/
@[your-scope]:registry=https://your-registry.example.com/npm/registry/
always-auth=true
```

Two things to settle with Smint.io when you host your own:

- **The Smint.io system has to be able to read it.** Registering a component points the platform
  at a package, so the registry has to be reachable from the environment you publish to, with
  credentials it can use. Arrange that before the first publish, not after.
- **Your scope is part of every package name**, and a package name — like a component key — is
  not something you change afterwards. Pick the scope once, with the deployment it will live in
  in mind.

### Anatomy of a component package

Every frontend component is a self-contained npm package with the same small set of files:

```
ui-my-thing-1/
├── .npmrc                       # the @smintio scope -> our SDK feed, your scope -> your registry
├── package.json                 # name, version, scripts, dependencies
├── tsconfig.json                # extends ../../config/tsconfig.json
├── rollup.config.js             # stub that loads ../../config/rollup/rollup-config.ts
├── licenses.json                # third-party license roll-up (generate it; the example has none)
├── resources/definition.ts      # resource and default settings definition (input)
├── portals-ui-component.json    # GENERATED from definition.ts — do not hand-edit
└── src/PortalsUiComponent.vue   # the component itself — the filename is fixed
```

Three things about that layout are not obvious:

- **The entry filename is fixed.** `src/PortalsUiComponent.vue` for a UI component and
  `src/PortalsPage.vue` for a page template — the shared rollup config hard-codes them. Ordinary
  Vue sub-components go in `src/components/` and are imported normally; they are invisible to the
  page editor, which is exactly right.
- **`portals-ui-component.json` and `portals-page-template.json` are build output.** They are
  generated from `resources/definition.ts` by `npm run build:resources`. Commit them, never edit
  them, and remember that `npm run watch` does not regenerate them.
- **The shared config in [`config/`](config) is shared on purpose.** Your `rollup.config.js` and
  `tsconfig.json` are small stubs that extend it, so every component builds identically. Do
  not fork it per component.

The build produces a **UMD bundle** in `lib/` with the SDK, the Smint.io Portals runtime, Vue,
Vuetify and `@smintio/portals-components` marked *external*. Those are provided by the portal
application at runtime rather than bundled, which is why the versions in your `package.json` have
to stay aligned with the ones the SDK expects — leave the dependency versions of the example
component alone unless you have a reason not to.

`licenses.json` is a roll-up of the licences in the package's production dependency tree. Nothing
in the build reads it and it is not published, but please generate one for your own component
rather than shipping the one you copied, which describes someone else's dependencies. The example
component in this repository deliberately ships without one, so there is nothing stale to inherit;
generate yours once the dependency tree is settled.

### Things to do for Mac or Linux users

The `package.json` scripts you find in the example component are tuned at Windows users.

For Mac or Linux users, you have to change your `package.json`. Please change the `smint-io-pc` script parts as follows (remove .exe extension of the executable file, fix the environment variable reference from `%SMINT_IO_SDK_HOME%` to `$SMINT_IO_SDK_HOME`, and fix the path separator):

```
"smint-io-pc:development": "npm publish && npm info --json | $SMINT_IO_SDK_HOME/SmintIo.Portals.SDK.PublishComponent.CLI -env development",
"smint-io-pc": "npm publish && npm info --json | $SMINT_IO_SDK_HOME/SmintIo.Portals.SDK.PublishComponent.CLI -env staging",
"smint-io-pc:production": "npm publish && npm info --json | $SMINT_IO_SDK_HOME/SmintIo.Portals.SDK.PublishComponent.CLI -env production"
```

### The example frontend component

The [example Vue.js component itself](Example/ui-example-hello-world-1/src/PortalsUiComponent.vue) is pretty basic.
It can display colored static text once added to a page.

The source follows established Vue.js structure practices by containing a template, typescript, and css sections.

`PortalsUiComponent` is annotated with custom attributes which contribute to the component description in way meaningful to Smint.io.

```javascript
    @PortalsUiComponent({
        type: "ui-type-text",
        key: "smintio-ui-example-hello-world-1",
        displayName: {
            [DefaultCulture]: "Hello world",
            de: "Hallo Welt",
        },
        description: {
            [DefaultCulture]: "This component displays a message with an optional color.",
            de: "Diese Komponente zeigt einen Text mit optionaler Farbe an.",
        },
    })
```

The full list of supported Smint.io Portals frontend component types can be seen [here](docs/smintio-frontend-component-types.md).

The `PortalsUiComponent` implementation exports fully localized properties that are interpreted by the Smint.io pages as `FormGroup`.

```javascript
    @DisplayName("en", "Component text", true)
    @DisplayName("de", "Komponententext")
    @Description("en", "The component text.", true)
    @Description("de", "Der Komponententext.")
    @Implements("ILocalizedStringsModel")
    @ComponentProperty({ name: "componentText" })
    @DynamicAllowedValuesProvider(
        PortalsGlobalServices.PortalsContext.toString(),
        "StringResourceAllowedValuesProvider"
    )
    @FormGroup("hw-text")
    public readonly componentText!: ILocalizedStringsModel;

    @DisplayName("en", "Color", true)
    @DisplayName("de", "Farbe")
    @Description("en", "The component text color.", true)
    @Description("de", "Die Textfarbe der Komponente.")
    @ComponentProperty({ name: "componentColor" })
    @IsColor()
    @FormGroup("hw-color")
    public readonly componentColor!: string;
```

Additional attributes such as `DynamicAllowedValuesProvider` or `IsColor` control how the component configuration can look.
In this case, an example would be a text input field or a color picker.

The full list of supported Smint.io Portals annotations can be found [here](docs/smintio-annotations.md).

`ILocalizedStringsModel` is a custom object type defined by Smint.io that can return the correct text value of a component according to the selected language by the user.

By default, [package.json](Example/ui-example-hello-world-1/package.json) is used by the npm CLI (and others) to identify the component and how to handle its relevant dependencies.

#### Is your component an existing one plus something? Extend it, do not copy it

Starting from the closest existing component is good advice, and it splits into two very different moves.
If your component is genuinely new, copy a similar one and change it. But if it is **an existing component
plus an addition** — the standard header with extra menu items, the standard search result with one more
badge — then take the published component as a dependency and extend it. A copy is a fork: it is large, and
it silently opts out of every later fix and feature of the component you copied from.

Add the component you are extending to your `dependencies`, then mix it in:

```javascript
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
import SmintIoPortalsUiComponentImplementation from "@smintio/ui-generic-header-1";

@PortalsUiComponent({
    type: "ui-type-header",
    key: "smintio-ui-<yours>-1",
    // ...
})
export default class PortalsUiComponentImplementation extends Mixins(
    SmintIoPortalsUiComponentImplementation,
    Mixins<AssetsReferenceMixin, RoutingMixin>(AssetsReferenceMixin, RoutingMixin)
) {
```

Your component then has the base's markup, its sub components, its props and all of its configuration
properties, and you declare only your own additions and override only what differs.

Five things worth knowing before you do it:

- **Leave out the `<template>` block entirely** when you are not changing the markup. Vue's mixin merge then
  keeps the base's compiled render function. A `.vue` file containing only a `<script>` block is valid.
- **Override the smallest thing that expresses your change** — usually one computed property. `super` is not
  available (vue-class-component moves getters into the component options), so if you need the base's result
  first, call the base's own helper methods to reproduce it, then add to it.
- **The base ships type definitions.** Do not re-declare inherited members to satisfy TypeScript: declaring an
  inherited *getter* as a property fails the build with `TS2610: ... is defined as an accessor ... but is
  overridden here as an instance property`.
- **Lifecycle hooks merge.** Your `created()` runs in addition to the base's, not instead of it.
- **The base's annotations come along**, including its display names and descriptions in every language it
  ships. Your own language choices apply only to the properties you declare yourself.

#### Do not hand write settings the shared props mixins already give you

Text, link, colour and layout settings are not yours to invent. `@smintio/portals-components`
ships them as `S...Props` mixins — `STextProps`, `SLinkButtonProps`, `SUiComponentColorsProps`,
`SBottomGapProps`, `SHtmlProps` and others — and mixing one in gives your component the same settings, the same
wording and the same form group as every Smint.io component. A portal editor then finds your component's
settings exactly where they expect them. Declaring your own equivalents produces a component that looks subtly
foreign in the editor, and the property names you pin are permanent.

**Not every `S...Props` mixin carries properties.** Roughly half of them — `STextsProps` (plural),
`SBackgroundProps`, `SImageProps`, `SEffectsProps`, `SLinkProps` and the rest of that group — declare *only a
form group*, so that several components can put their own properties into one consistently named tab. Mixing
one of those in and expecting settings to appear gives you an empty tab. The
[frontend reference](docs/smintio-frontend-reference.md) splits the two lists; check which kind you are reaching
for before you mix it in.

**Include the layout ones even when your component does not read them.** `SBottomGapProps` is the one most
easily forgotten: it declares a single `bottomGap` property — the editor's *Gap bottom* — and your template
never touches it. The **page slot** reads the configured value and puts the matching
`s-mb-space-between-components-*` class on the component's wrapper. So there is nothing to apply and nothing to
test in your own markup; mixing the class in *is* the entire implementation:

```javascript
export default class PortalsUiComponentImplementation extends Mixins(
    STextProps,
    SBottomGapProps
) {
```

A component without it simply has no spacing setting, and the omission only surfaces when an editor goes
looking for the gap control that every other component on the page has. Check the
[mixin reference](docs/smintio-frontend-reference.md) when you start a component, not after.

#### Is your text a resource, or just a text field?

Answer this before anything else, because a resource is the more involved of the two and you
often do not need one. A property typed `ILocalizedStringsModel` is **not** automatically a
resource. What makes it one is a provider:

| Your property has | What the portal editor sees | Right when |
|---|---|---|
| no `DynamicAllowedValuesProvider` | a plain localized text field they type into | the text is used only by your component — it is local to it |
| `StringResourceAllowedValuesProvider` or `TextResourceAllowedValuesProvider` | a dropdown of the portal's resources | the text should be **reusable across several components** and maintained in one place |

Both are ordinary `ILocalizedStringsModel` properties and both are fully localized — only the
provider differs. A heading that belongs to one component instance is a plain field. Wording
that has to read identically wherever it appears is a resource.

The rest of this section is about the resource case.

#### Shipping your own string resources

A resource backed text property can be given a **string resource id** as its default, so the
component is not blank the moment it is dropped onto a page. The example does this in
[resources/definition.ts](Example/ui-example-hello-world-1/resources/definition.ts), pointing
`componentText` at the resource id `component_text`:

```javascript
uiComponentBuilder.setFormFieldValues({
    values: [
        {
            id: "componentText",
            dataType: "resource_id",
            resourceIdValue: "component_text",
        },
    ],
} as IFormFieldValuesModel);
```

The id has to resolve to a resource that actually exists, and there are two sources:

- **resources that already exist in the portal**, maintained on the Smint.io side. You can
  reference these but you cannot add to them.
- **resources your component ships itself.** This is the route for any text of your own, and
  it is easy to miss — your `resources` folder looks empty because only *file* resources
  (images, videos, audio, documents) live there as files.

Declare one with `addEmbeddedResource`, inside `defineResources` and **before** the
`loadFileResources` call:

```javascript
import type { IResourceDefinitionBuilder } from "@smintio/portals-resource-builder-cli";
import { IEmbeddedResourceModel, IFormFieldValuesModel } from "@smintio/portals-resource-builder-cli";

// ...
await uiComponentBuilder.defineResources(async (resourceBuilder) => {
    resourceBuilder.addEmbeddedResource({
        id: "text_hello_world_greeting",
        resource: "string",
        name: {
            "x-default": "Greeting text",          // the label shown in the page editor
        },
        formFieldValues: {
            values: [
                {
                    id: "Text",                     // always "Text" for a string resource
                    dataType: "localized_strings_model",
                    localizedStringsModelValue: {
                        "x-default": "Hello world", // the text itself
                        de: "Hallo Welt",           // one key per further language
                    },
                },
            ],
        } as IFormFieldValuesModel,
        localizedResourceAssets: undefined,         // required, and always undefined here
    } as IEmbeddedResourceModel);

    await resourceBuilder.loadFileResources(async (fileResources) => {
        await fileResources.verifyLoadedResources(async (resource) => {
            return resource;
        });
    });
});
```

You can then reference it from the property itself. The provider is what makes this a resource
reference — without it the property is just a text field and the id will not resolve to
anything the editor can pick:

```javascript
    @Implements("ILocalizedStringsModel")
    @ComponentProperty({ name: "componentText" })
    @DynamicAllowedValuesProvider(
        PortalsGlobalServices.PortalsContext.toString(),
        "StringResourceAllowedValuesProvider"
    )
    @DefaultValue("text_hello_world_greeting")
    public readonly componentText!: ILocalizedStringsModel;
```

Things worth knowing before you write one:

| | |
|---|---|
| `IEmbeddedResourceModel` and `IFormFieldValuesModel` | import them as values, **not** with `import type` — the `as` casts need them at runtime |
| `resource` | `string` for a **plain text** — a label, a button caption, a heading. `text` for a **rich text** — long form body copy carrying markup, the kind you pair with `IsRichText` and render with `v-html`. `image`, `video`, `audio` and `document` are file based and come from your `resources` folder instead |
| matching the provider | the dropdown an editor sees comes from the **property**, not from the resource. `StringResourceAllowedValuesProvider` lists string resources, `TextResourceAllowedValuesProvider` lists rich text ones. If they do not match, the resource you shipped will not appear in the list |
| `name` versus the value | `name` is the label an editor sees when picking a resource from a list. The text itself is the `formFieldValues` entry with `id: "Text"` |
| id naming | prefix with `text_` and keep it unique to your component, for example `text_<yourcompany>_<what>` |
| languages | `"x-default"` is the fallback, then one key per further language |
| `DefaultValue` versus `setFormFieldValues` | see the table below — the difference is whether the value is written to the database, and therefore whether you can change it later |

#### Two ways to point a property at a resource, and only one is reversible

`DefaultValue` and `setFormFieldValues` both give a property a resource id to start from, but
they are not interchangeable:

| | `DefaultValue("<id>")` on the property | `setFormFieldValues` in `resources/definition.ts` |
|---|---|---|
| What it is | the property's declared default. **Nothing is written to the database** | a **real database write**, applied when the property is instantiated |
| Changing it later | **works** — publish a new version with a different id and portals follow it | **does not reach existing instances** — the value is already persisted against each one |
| Use it for | the normal case | a value that genuinely has to be materialised into the instance's saved configuration |

Prefer `DefaultValue` unless you specifically need the persisted write. A `setFormFieldValues`
entry is effectively permanent for every component instance created while it was in place.

Adding or changing a resource is an **annotation level change** — see the table under
[What the dev server covers, and what still needs publishing](#user-content-what-the-dev-server-covers-and-what-still-needs-publishing).
It will not show up in the page editor until you publish your component.

Page templates ship string resources in exactly the same way, through
`buildPageTemplateResourceDefinition`.

#### Before you point a property at a resource the portal already has

A portal carries resources of its own, and their ids are tempting to reuse — they are already
there, already translated, and they cost you nothing to ship. The catch is that **the id tells
you nothing about the wording**, which was written for whichever component introduced it. A
generic browse button resource may well read *"Browse all related assets"*, which is fine on the
component it was written for and wrong next to your own button.

So: reach for an existing portal resource only when the text genuinely has to read identically
across components — and when you do, look at its value on a live portal first. Otherwise ship
your own resource, as described above. It is a few lines in `resources/definition.ts` and it is
yours to change.

#### Linking to a search page from your component

A button that opens a pre-filtered search is a common requirement — "show me all assets in this
category". It is tempting to assemble the URL by hand from an id and a query parameter. Do not:
the filter is data, it already exists, and hand-built URLs go stale the moment the data side
changes.

**Take the search that is already attached to the asset.** An `IAssetDataObject` carries its
related searches in `relatedAssets`, one entry per relationship type, each with a complete
search spec — filters, data adapter instance, query string. `AssetsReferenceMixin` hands it to
you:

```typescript
const assetsReference = this.getAssetReferenceByRelationshipType(asset, "<relationship type>");

if (!assetsReference || assetsReference.type !== AssetsReferenceType.SearchAssetsSpec) {
    return undefined;       // no such relationship - render no button at all
}

const query = this.searchAssetsSpecQuery(assetsReference.searchAssetsSpecModel);

return this.generateRouterLocation(this.searchPage, { query });
```

**A search page restores a search from the query string** in a fixed shape: one parameter per
filter, named after the search field, plus the same name prefixed with `dt-` carrying the
field's data type, and the free text as `query`:

```typescript
for (const formFieldValueModel of searchAssetsSpecModel.currentFilters.values) {
    const id = formFieldValueModel.id;

    switch (formFieldValueModel.dataType) {
        case ValueType.String:
            if (formFieldValueModel.stringValue === undefined) {
                continue;
            }

            query[id] = formFieldValueModel.stringValue;
            break;

        // ... one case per ValueType you support: StringArray, Int32, Int64, Decimal,
        // Boolean, DateTime. Anything else cannot be expressed in a query string

        default:
            continue;
    }

    query[`dt-${id}`] = formFieldValueModel.dataType;
}

if (searchAssetsSpecModel.queryString) {
    query.query = searchAssetsSpecModel.queryString;
}
```

Take the data type from the spec, never from a sample URL you copied out of a browser — the two
can disagree, and the spec is the one the search page agrees with.

#### A page reference resolves to a named route, so it has no path

`generateRouterLocation(page, { query })` gives you a router location that looks like this:

```javascript
{ name: "pgt:34987", query: { … }, hash: "" }        // note: no path, and no fullPath
```

Bind that object to a `:to` and everything works. But some renderings want a URL **string** rather than a
location object — menu items are the common case — and there is nothing to build that string from. Do not
assemble it out of `location.path`; it is `undefined`, and the result is a link that quietly does not exist.
Ask the router:

```javascript
const url = this.$router.resolve(location).href;
```

This failure mode is worth recognising because it is silent: no URL means no link, and if your component then
skips items that have nothing to link to, it renders **nothing at all** with no error in the console. Which
leads to a general rule — **when your component decides to render nothing, log why**. An empty component is
otherwise indistinguishable from a wrong configuration, a stale bundle or a component that never loaded, and
you will spend that debugging time in a browser rather than in your editor.

#### Rendering nothing still leaves a gap — emit `component:hide`

An empty template is not the same as being absent. The page has already laid out a slot element for
your component — a column, its content gaps, possibly a background band — so a component that renders
nothing leaves a hole where it sits.

To disappear completely, emit `component:hide` and pass your component instance:

```javascript
this.$emit("component:hide", this);
```

The page then drops your component from the slot altogether, and lays out nothing for it. On a
`ui-type-section` component the whole section goes with it — everything placed inside it and its
matching section end, nested sections included; that is exactly how a conditional section works.

Two rules come with it. **Emit only when the answer is final**, because there is no counterpart event
and a component hidden while its data was still loading stays hidden for that page render. And **guard
your template as well**, because the page acts on its next render pass — an unguarded template renders
once before it vanishes.

See [the recipes](docs/smintio-frontend-recipes.md#user-content-how-do-i-hide-my-component-completely-leaving-no-gap-behind)
for a worked example, and [the frontend reference](docs/smintio-frontend-reference.md#events-a-component-can-emit)
for the contract.

#### Many links to the same search page: use `exact`

If your component renders a list and every entry links to the same search page, differing only
in the query, add `exact` to the link or button:

```html
<v-btn :to="searchLocationFor(item)" exact outlined small color="primary">…</v-btn>
```

Vue-router's default active matching compares the path and **ignores the query string**. Without
`exact`, following one entry's link marks every entry's link as the current route, and the whole
list takes on the active styling at once — which reads as a rendering bug. `exact` makes the
comparison include the query, so only the link you actually followed is active. It looks like a
prop you could drop; it is not, so leave a comment next to it.

## Crafting a page template instead

> The full guide — the page skeleton, the slot options, the layout props on the slot renderers,
> dialog pages, the page template resource builder and a checklist — is in
> [Building a page template](docs/smintio-building-page-templates.md). This section is the
> summary.

A page template is the same kind of npm package, built with the same SDK. Start from the same
`ui-example-hello-world-1` directory and change the following:

| | UI component | Page template |
|---|---|---|
| Directory name prefix | `ui-` | `page-` |
| Source file | `src/PortalsUiComponent.vue` | `src/PortalsPage.vue` |
| Class decorator | `@PortalsUiComponent` | `@PortalsPageTemplateComponent` |
| `type` | a UI component type | a page type |
| Shared rollup config imported by your `rollup.config.js` | `../../config/rollup/rollup-config.ts` | `../../config/rollup/rollup-config-page.ts` |
| `main` in `package.json` | `./lib/portals-ui-component.umd.min.js` | `./lib/portals-page.umd.js` |
| Resource builder call in `resources/definition.ts` | `buildUIComponentResourceDefinition` | `buildPageTemplateResourceDefinition` |
| Generated resource file | `portals-ui-component.json` | `portals-page-template.json` |

The shared rollup config for page templates is already part of this repository, so the only
change to your `rollup.config.js` is the import:

```javascript
import rollupConfig from "../../config/rollup/rollup-config-page.ts";
```

Instead of configuration properties alone, a page template declares its *slots*:

```javascript
import type { IUIComponentInfo } from "@smintio/portals-component-sdk";
import { UIComponentSlot } from "@smintio/portals-component-sdk";

@UIComponentSlot({
    slotId: "header",
    minimumItems: 0,
    maximumItems: 1,
    allowedUiComponentTypes: ["ui-type-header"],
})
public headerSlot: IUIComponentInfo[] = [];

@UIComponentSlot({ slotId: "content", minimumItems: 1 })
public contentSlot: IUIComponentInfo[] = [];
```

and renders them with the slot components from `@smintio/portals-components`. **Those components have to be
registered before you can use their tags**, and the easiest way is to mix in `SPageMixin`, which registers
`SHeaderSlot`, `SFooterSlot`, `SGenericSlot` and `SGenericMultiSlot` for you (and brings `SCssProps` along):

```javascript
import { SPageMixin } from "@smintio/portals-components";

export default class PortalsPage extends Mixins(SPageMixin) {
```

`SSideSlot`, `SSideMenuSlot` and `SBannerSlot` are *not* covered by it — import those and list them under
`components` in your `@PortalsPageTemplateComponent` metadata when you use them. Without the registration the
tag renders nothing and Vue warns about an unknown custom element.

```vue
<s-header-slot :ui-slot="headerSlot" />

<v-main>
    <s-generic-slot
        :ui-slot="contentSlot"
        :content-gap="contentGap"
        :apply-top-content-gap="true"
        :apply-bottom-content-gap="true"
    />
</v-main>
```

`@UIComponentSlot` does not create an ordinary data property — it replaces the field with a computed property that
reads the slot's components from the page context, so declare it with an `= []` initializer and never assign to it.

Remember that the page template — not the UI component — owns all padding and margins between the components it hosts.

To hand data and event handlers to the components in a slot, use `ui-slot-data`, a Vue render data object:

```vue
<s-generic-slot
    :ui-slot="searchResultSlot"
    :ui-slot-data="{
        props: { currentSearch, results, isSearching },
        on: { 'next-search-page': onNextSearchPage },
    }"
/>
```

This is how a page fulfils the contract that the UI components of its page type expect. **The
binding block *is* the contract** — there is no separate declaration of it anywhere. If you are
writing a page template for a page type that already exists, copy the prop and event names
exactly from [the page type contracts](docs/smintio-page-type-contracts.md), or the components
built against that page type will silently receive nothing.

## Building a section component

A *section* is the one sanctioned way for a UI component to wrap other UI components. The
portal editor adds your *section start* component, then any number of components, and finally
the generic *section end* component `smintio-ui-generic-section-end-1`. The runtime collects
everything in between and hands it to your component.

Declare your component with `type: "ui-type-section"`, and receive the collected components in
a plain Vue prop named `subUiComponentInfos`:

```javascript
import type { IUIComponentInfo } from "@smintio/portals-component-sdk";
import { Prop } from "vue-property-decorator";
import { SGenericSlot } from "@smintio/portals-components";

@PortalsUiComponent({
    type: "ui-type-section",
    key: "smintio-ui-example-section-start-1",
    displayName: { [DefaultCulture]: "Start example section" },
    components: {
        SGenericSlot,
    },
    inheritAttrs: false,
})
export default class PortalsUiComponentImplementation extends Mixins(SHtmlProps) {
    @Prop({ default: [] })
    public readonly subUiComponentInfos!: IUIComponentInfo[];
}
```

Then render them wherever your layout wants them, with the same `s-generic-slot` renderer a
page template uses. Pass `$attrs` and `$listeners` through, so that the components inside your
section still receive whatever the page handed to the section as a whole:

```vue
<s-generic-slot
    :ui-slot="subUiComponentInfos"
    :ui-slot-data="{
        attrs: $attrs,
        on: $listeners,
    }"
/>
```

Sections may be nested — the runtime tracks the nesting level and matches each section start
to its own section end. Because your component decides where the collected components go, you
can build quite sophisticated constructs this way: our own
`smintio-ui-generic-section-start-table-1` distributes them over the cells of a responsive
table, and there are further variants for expansion panels, tab panels and conditional
display.

Please remember that a section start component still follows all the usual UI component rules
— in particular, it must fill its layout box and add no white space of its own.

### Build your custom frontend component

Please note that [access to the SDKs is restricted](../../Overview/README.md#user-content-getting-access-to-the-sdks).

The npm feed setup — the node version, the two `.npmrc` files and the two feed authorizations — is
the same one you did once in
[getting started](#user-content-getting-started); it is not repeated here.

1. In the component folder open a command prompt or terminal window, on node 12.22.10
2. Run `npm i` the first time, and whenever you change dependencies

	- Any authorization failure here means something is wrong in the feed setup above. Revisit it
	- If you absolutely cannot manage to get going, please get in touch at [support@smint.io](mailto:support@smint.io)
	
3. Run `npm run build` or `npm run watch` to build your frontend component

`npm run build` runs two steps: `build:dist` (rollup, producing the bundle under `lib/`) and `build:resources`
(the `portals-resource-builder-cli`, producing `portals-ui-component.json` or `portals-page-template.json` from
`resources/definition.ts`).

`npm run watch` runs **rollup only**. It does not regenerate the resource definition file. After changing
`resources/definition.ts`, run `npm run build:resources` or a full `npm run build`.

Never edit `portals-ui-component.json` or `portals-page-template.json` by hand — they are build output and are
overwritten on the next build.

#### If you also have a newer node installed

`npm run build` runs the shims in `node_modules/.bin`, and those pick whichever `node` comes
first on your `PATH` — not necessarily the version you selected with NVM. On a machine that
also has a modern node, rollup then fails with:

```
Error loading `tslib` helper library.
[!] Error: Package subpath './package.json' is not defined by "exports" in .../tslib/package.json
```

The fix is to run the two build tools with the node 12 binary directly, from your component
folder:

```
<path-to-node-12>/node.exe node_modules/rollup/dist/bin/rollup -c --environment BUILD:production
<path-to-node-12>/node.exe node_modules/@smintio/portals-resource-builder-cli/bin/resource-builder-cli.js
```

Those two commands are exactly what `build:dist` and `build:resources` do.

### Publish your custom frontend component

Publishing a frontend component is **self-service**: you publish the package and register it, for whichever environment
you are set up for, and there is no Smint.io review step in between. That is specific to frontend components — a
*backend* component runs as trusted server-side code inside the Smint.io platform and reaches production only through
Smint.io, after a code review. See
[getting a component into a Smint.io Portals system](../../Overview/README.md#user-content-getting-a-component-into-a-smintio-portals-system).

Self-service assumes you have an npm registry to publish into. Smint.io hosts one for itself and for its Solution
Partners; for a customer-specific project or your own VPC deployment you host it yourself — see
[where your own components are published](#user-content-where-your-own-components-are-published).

1. In the component folder open a command prompt or terminal window
1. Run `npm run build` to ensure the latest version will be published
1. Run `npm run smint-io-pc` to publish to the Smint.io `Staging` environment

	- If the component already exists in the npm registry the component can be deployed by running `npm info --json | %SMINT_IO_SDK_HOME%\\SmintIo.Portals.SDK.PublishComponent.CLI.exe -env staging` (for Windows users) or `npm info --json | $SMINT_IO_SDK_HOME/SmintIo.Portals.SDK.PublishComponent.CLI -env staging` (for Mac or Linux users)

1. If you run into any issues in running the command, it is likely one of the following causes:

	- You did not properly provide the SMINT_IO_SDK_HOME environment variable
	- You did not adjust the package.json scripts to handle Mac or Linux - note the default scripts are tuned for Windows users
	- You did not use the proper tool builds for your operating system
	- There is authorization issues with your NPM setup (see previous chapter for more instructions). On Windows with an Azure DevOps feed, run `vsts-npm-auth -config .npmrc -force` in the component directory to refresh an expired token — see [when npm cannot authenticate](#user-content-when-npm-cannot-authenticate-azure-devops-feeds-on-windows)
	- If you absolutely cannot manage to get going, please get in touch at [support@smint.io](mailto:support@smint.io)
	
1. A browser window will open for you to authenticate to allow the component to be published
    - If successful, the window will close after a few seconds
    - The message *You can now return to the application.* will also indicate success
1. Go back to the command prompt or terminal window and validate that component is published using the correct version
1. `Done` will indicate that the publication was successful

Running `npm run smint-io-pc` will package the component in the defined npm registry and then trigger a REST API request to register the package with Smint.io.

Please note that calling the command repeatedly with the same package version will result in an error.

With each code change, the version number must be increased in the `package.json` file.

#### A new version must fit the configurations already saved

Administrators have saved configurations for your component, in every tenant that uses it, and a new version reads
them. The registration step checks the new version against all of them and **rejects it** if a saved value no
longer fits:

- **A configurable property keeps its type.** A property saved as a string cannot become a number, and a property
  that was removed cannot come back later with another type under the same name.
- **A value that is in use stays allowed.** Removing a value from an `AllowedValues` list is rejected while a saved
  configuration still uses it. Values nobody uses may go.

The rejection names the property, the type or the value, and how many saved configurations are affected — never
which tenant they belong to. Because `npm publish` has already run by then, **the rejected version number is
taken**: fix the component and publish it with the next version.

Adding a property is safe — give it a default, because existing configurations have no value for it — and so is
removing one, whose saved value is then no longer read. There is no way to rename a property or to migrate a saved
value: a rename is a removal plus an addition, and the value is lost.

#### The two steps fail independently — and the second one needs your `.npmrc`

`npm run smint-io-pc*` is `npm publish` **and then** the registration call, and they can fail apart. If the
publish succeeded and only the registration failed, do **not** run the whole script again: that version number
is already taken and `npm publish` alone will fail. Rerun just the registration, which is the piped command
shown above.

The failure to expect on a **brand new component** looks like this:

```
+ @your-scope/ui-your-component-1@1.0.0
npm ERR! code E404
npm ERR! 404 '@your-scope/ui-your-component-1' is not in the npm registry.
info: Missing component name
```

The publish worked; the registration did not, because the registration step pipes `npm info --json` into the
CLI and `npm info` resolves the registry from the **component folder's `.npmrc`**, not from `publishConfig` in
`package.json`. A new component folder without an `.npmrc` therefore asks the public npm registry, which has
never heard of your package, and the CLI receives nothing to register. Make sure the folder has the `.npmrc`
described in *Getting started*, then rerun the registration on its own. The same applies when you
[host the registry yourself](#user-content-where-your-own-components-are-published): it is the folder's
`.npmrc` that has to name it.

#### Which tenant your component is published for

Nothing in your component's source decides this, and there is nothing you need to add to it.
The tenant comes from the configuration of the *Portals-SDK-PublishComponent-CLI tool*.

Next to the CLI executable sits an `appsettings.json` as a template, plus one file per
environment — `appsettings.Development.json`, `appsettings.Staging.json` and
`appsettings.Production.json`. The `-env` argument in the `smint-io-pc` scripts chooses which
one is used. Each file holds:

| Setting | What it does |
|---|---|
| `SmintIo.ApiUrl` | the **tenanted** Smint.io API URL the component is registered with — this is what decides which tenant receives it |
| `SmintIo.Auth.Authority`, `ClientId`, `ClientSecret` | the OAuth client the publish authenticates with |
| `SmintIo.AuthorizationHeader` | the authorization header for your npm repository, where one is required |
| `RedirectUrl` | the local callback the authentication returns to — this is the browser window that opens during the publish and closes itself again |

**Your component is published for your own tenant only.** It becomes available in the page
editor of that tenant's portals, and nowhere else. This is exactly what you want for a
component you built for one customer or one portal: there is no additional step needed to keep
it private, and no setting in the component to get wrong.

If a component of yours should become available more widely, please get in touch at
[support@smint.io](mailto:support@smint.io) — that is something we arrange on our side.

> Please treat these settings files as secrets. They contain an OAuth client secret and your
> npm repository authorization header. Do not commit them, and do not paste their contents into
> a support request — tell us what you see, not what is in the file.

More information about the *Portals-SDK-PublishComponent-CLI tool* can be found [here](../../Tools/Portals-SDK-PublishComponent-CLI/Release/).

### Local development

This is the part that makes developing Smint.io Portals frontend components pleasant: the dev
server reroutes the requests that load your component's JavaScript to the files in your own
working folder. Your local build then runs inside a **real portal**, against real data.

**Set this up before you write the component, not after.** Scaffold the package, build it once,
publish it once so the portal knows it exists, add the mapping, start the dev server, and confirm
the portal actually reaches it — all while the component still does nothing. Then write it, with
`npm run watch` running, and every change is a rebuild and a reload.

It is the same work either way, and doing it first buys two things. You find out immediately
whether you can verify at all — if *Development mode* is off or no developer-cleared login exists,
you learn it while the component is empty rather than after a day's work. And once the loop has
been proven with an empty component, a page that breaks afterwards broke because of what you just
wrote, which removes most of the guesswork from the rest of the job.

#### What the dev server covers, and what still needs publishing

Please read this before you start, it saves a lot of confusion:

| What you changed | What is needed |
|---|---|
| The template, the script body, the styles — your component's **HTML and JavaScript** | Rebuild and reload the browser. The dev server serves your local files. **No publishing.** |
| **Annotations** — a new or renamed configuration property, a changed `DisplayName`, `Description`, `DefaultValue`, `AllowedValues`, `VisibleIf` or `FormGroup`, the component metadata, or a page template's slots | **Publish your component.** The configuration form and the component metadata live on the Smint.io server, not in your bundle. Until you publish, the portal editor will not offer the new setting, nor show a changed label. |
| A **brand new component** | **Publish your component**, and add it to the dev server's `ComponentMappings` so the dev server can find your local build at all. |

So: publish once when you create the component and whenever you change its settings, then
iterate freely on markup and behaviour without publishing. When you are finished, publish once
more, so that what is registered with Smint.io matches your final result.

The flip side is worth stating plainly: while the rerouting is active, **a working page proves
your working folder works, not that anything is published**. If you need to know which version
the portal is actually registered against, read it out of the request that loads your component
— the version is part of the URL:

```
https://development-host.smint.io:8443/components/js/js/uic-<type>-<key>-1.2.3.js
```

(The doubled `js/js/` is not a typo — the portal appends `js/` to a base path that already ends
in one.)

That same request answers a question that otherwise costs a lot of guessing: if it is not there
at all, your component is not on the page you are looking at — check the page rather than the
component.

#### Two switches decide whether your local build is asked for at all

The rerouting is a decision of the **portal**, not of the dev server. The portal only offers a
local URL for a component's JavaScript when **both** of these are true:

1. **The portal has development mode enabled** — *Basic settings > Development mode* in the
   portal's settings. Without it, the portal always uses the published component and never
   looks at your machine.
1. **A user is logged in whom Smint.io has cleared as a developer.** The clearance belongs to
   the logged-in frontend user, not to the portal, so an anonymous visitor never gets it — not
   even on a development portal. Please ask us to clear the login you develop with.

Both are things we set up for you, so please mention them when you get in touch.

If either one is missing, the symptom is easy to misread: the portal renders perfectly
normally, and the dev server's request log stays **empty**, because your local listener is
never contacted. So read the log this way — requests that arrive but are served from the
published package mean a wrong or missing mapping, whereas **no requests at all** mean one of
these two switches, and changing the mapping will not help.

1. Get in touch with [support@smint.io](mailto:support@smint.io) so that we can set up a development portal for you,
   enable *Basic settings > Development mode* on it, and clear your login as a developer —
   see [the two switches above](#user-content-two-switches-decide-whether-your-local-build-is-asked-for-at-all).
   Stay logged in with that user while you develop
1. Download the [Portals-DevServer](../../Tools/Legacy/Portals-DevServer/Release/) for your platform —
   Windows, Linux, macOS Intel and macOS Apple Silicon builds are provided, and each one is
   self-contained, so no .NET runtime has to be installed
1. Trust the `rootCA.pem` shipped with the dev server, so that your browser accepts its local HTTPS listener
1. In the dev server's `appsettings.json`, set `RootDirectory` to the folder that contains your component
   folders. It is optional — leave it out and every mapping below has to carry a full path instead
1. In the same file, add one entry under `ComponentMappings` for each component you want to
   develop locally, mapping the component id to its built bundle, relative to `RootDirectory`. The key is
   matched as a **substring** of your component's npm package name, so it does not have to be the full id —
   `ui-example-hello-world` matches `ui-example-hello-world-1-1.0.0.tgz`:

```
"ComponentMappings": {
    "ui-example-hello-world-1": "ui-example-hello-world-1/lib/portals-ui-component.umd.min.js",
    "page-example-hello-world-1": "page-example-hello-world-1/lib/portals-page.umd.js"
}
```

   The path must match the `main` entry of your component's `package.json`. Please note that
   the dev server reads `appsettings.json` when it starts, so restart it after you add a mapping.

1. Start the dev server — `SmintIo.Portals.DevServer.exe` on Windows, `./SmintIo.Portals.DevServer` on Mac or Linux.
   It listens on `development-host.smint.io`, HTTPS on port **8443** and HTTP on port 8000, and logs every
   request it serves. The portal always asks for the HTTPS one, so 8443 is the port that matters — note that
   the dev server's own startup banner prints `https://…:8000`, which is wrong and can be ignored
1. Build and publish your component once, so that Smint.io knows it and the portal editor can offer it
1. Run `npm run watch` for continuous building of your frontend component
1. Turn off your browser's cache
1. Navigate to your development portal, add your frontend component to a page
1. Change some code in your frontend component, and refresh the browser page. Enjoy! :)
1. Whenever you change an annotation, build and publish again before you continue
1. When you are finished, build and publish the final result

If your component does not show up, the dev server's request log is the first place to look:
it will show whether the request was rerouted to your file or served from the published
package. The usual causes are a mapping that does not match, a bundle that has not been built
yet, or a dev server that was not restarted after the mapping was added.

If the log shows **no requests at all**, the portal never asked for your local build: either
*Basic settings > Development mode* is off for that portal, or the user you are logged in with
has not been cleared as a developer by us (see above).

If instead your component shows up but a *setting* you added is missing from the configuration
form, that is the publishing boundary described above — build and publish, and it will appear.

### Common problems

| What you see | What it usually means |
|---|---|
| `Some properties lack type definition: [...]` when the component loads | one of your configuration properties has no `Is...` or `Implements` annotation |
| A property you added does not appear in the configuration form | it has no `FormGroup`, or its `VisibleIf` condition is never true |
| Settings that portals had already saved are suddenly empty | the TypeScript property was renamed without keeping the same `ComponentProperty` name |
| `portals-ui-component.json` or `portals-page-template.json` is out of date | `npm run watch` does not run the resource builder — run `npm run build:resources` |
| Your local change does not show up in the portal | the dev server mapping is missing or points at the wrong bundle path, the dev server was not restarted after the mapping was added, or the browser cache is on |
| Your local change does not show up **and the dev server log stays empty** | the portal never asked for your local build: *Basic settings > Development mode* is off for that portal, or the logged-in user has not been cleared as a developer by Smint.io — anonymous visitors never are |
| A setting you added is not in the configuration form, or your new component is not offered in the editor | the component has not been published since you changed its annotations — the configuration form lives on the Smint.io server, not in your bundle |
| Publishing fails right away | the `version` in `package.json` was not increased, or `SMINT_IO_SDK_HOME` is not set |
| `npm i` or `npm publish` fails with a `401`, and nothing in your setup changed | the feed token expired. On Windows with an Azure DevOps feed, run `vsts-npm-auth -config .npmrc -force` in the component directory — see [when npm cannot authenticate](#user-content-when-npm-cannot-authenticate-azure-devops-feeds-on-windows) |
| `npm publish` succeeded but the component was not registered | publishing is two steps and they fail independently. **Do not raise the version and publish again** — that version is already on the feed. Rerun only the registration: `npm info --json \| %SMINT_IO_SDK_HOME%\SmintIo.Portals.SDK.PublishComponent.CLI.exe -env <environment>` |
| Registration fails with a permissions error seconds after a successful `npm publish` | the feed may not have made the new version visible yet. Retry the registration step once before treating it as a permissions problem |
| Every link in your list highlights after you follow one of them | they all resolve to the same route and differ only in the query — add `exact` to the link, see [Many links to the same search page](#user-content-many-links-to-the-same-search-page-use-exact) |
| A filtered search link opens the search page but finds nothing | check the filter *value*, not your code: compare the value your link sends with the values the search page offers in its own facet list for that field |
| Your component does not appear in the page editor of the portal you expected | it was published against a different `SmintIo.ApiUrl` — the tenant comes from the CLI's `appsettings.<Env>.json`, not from the component |
| Unexpected rollup or babel errors when building | the wrong node version — see the build chapter above |
| Two identical `CSS class` fields in the configuration form | `SCssProps` and `SHtmlProps` were mixed in together; use only one of them |
| Your component renders **nothing at all**, with no error in the console | it decided to render nothing. A link that could not be built, a lookup that returned empty, a guard that skipped every item — log the reason at the point where your component gives up, then read the console |
| A link built from a page reference goes nowhere | a page reference resolves to a *named* route with no `path`; use `this.$router.resolve(location).href` when you need a URL string, see [A page reference resolves to a named route](#user-content-a-page-reference-resolves-to-a-named-route-so-it-has-no-path) |
| `TS2610: ... is defined as an accessor ... but is overridden here as an instance property` | you extended another component and re-declared one of its members. The base ships its own type definitions — use the member, do not declare it again |
| `E404 ... is not in the npm registry` followed by `info: Missing component name` | the registration step read the wrong registry: your component folder has no `.npmrc`. Add it, then rerun only the registration |
| A page shows your latest change although you never published | that is the dev server serving your working folder. It says nothing about what is registered — read the version out of the component's script URL |

## Checklist before you ship

- [ ] `key` is your partner id plus the directory name, and unchanged from the released version
- [ ] `type` is a real component type from [the list](docs/smintio-frontend-component-types.md)
- [ ] `displayName` and `description` are present, with one language marked as the default culture
- [ ] every configuration property has a type annotation (`Is...` or `Implements`) and a pinned
      `ComponentProperty` name
- [ ] every configuration property actually does something — no dead settings
- [ ] the shared `S...Props` mixins are used instead of re-declared text, link, colour and layout
      properties, and `SBottomGapProps` (or the asset detail equivalent) is among them
- [ ] `SCssProps` **or** `SHtmlProps`, never both
- [ ] the component fills its layout box: no outer margin, no overflow, no nested Smint.io UI
      component
- [ ] `npm run build` clean — both `build:dist` and `build:resources` — and `npm run lint` clean
- [ ] `portals-ui-component.json` / `portals-page-template.json` committed as regenerated, not
      hand-edited
- [ ] `version` bumped in `package.json`
- [ ] `licenses.json` generated for your component rather than inherited from the one you
      copied
- [ ] the `.npmrc` is present in the component directory, or the registration half of the publish
      will fail
- [ ] `publishConfig.registry` in `package.json` names **your** registry, not the Smint.io SDK feed
      the example carries
- [ ] the dev server mapping added, the dev server restarted, and the component verified in a
      real portal with *Development mode* on and a developer-cleared login
- [ ] published against the intended tenant, that is, the `SmintIo.ApiUrl` of the right
      `appsettings.<Env>.json`

## How we built our own components

**For Smint.io Certified Partners**, the full sources of the components we build and ship are
available on request: 64 UI components, 34 page templates, the shared
`@smintio/portals-components` library and the gallery view library. Read them to see how we
solved something before you solve it yourself, and copy from them rather than starting from
scratch.

They are not part of this repository. Once we have granted you access to the reference
repository, copy its contents into [Frontend/Legacy/Reference](Reference/) — that directory
carries [a README](Reference/README.md) explaining the layout, which package to start from for a
given kind of component, and how to bootstrap and build the tree. Please get in touch at
[support@smint.io](mailto:support@smint.io) to request access.

Without the sources you are not missing the essentials. Everything you need to build a component
is in this guide and in [docs](docs), and two generated overviews describe every component we
ship — its key, its type, its configuration properties and the mixins it uses:

- [the UI component list](docs/smintio-ui-components.md)
- [the page template list](docs/smintio-page-templates.md), including the slots each page
  template declares and the UI component types they accept

If your own component is an existing Smint.io component *plus something*, you do not need the
sources at all: take the published package as a dependency and
[extend it](#user-content-is-your-component-an-existing-one-plus-something-extend-it-do-not-copy-it).

If you have any questions about the code, on how to build those components, or whatever else,
please do not hesitate to get in touch at [support@smint.io](mailto:support@smint.io)!

## Problems

Please do not hesitate to contact us at [support@smint.io](mailto:support@smint.io) if you run into any issues.

Contributors
============

- Reinhard Holzner, Smint.io GmbH
- Yosif Velev, Smint.io GmbH