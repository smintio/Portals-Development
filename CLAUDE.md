> **Read the workspace root config first.** This repo is one of ~46 that build together.
> `.claude/CLAUDE.md` in the Claude Code config repo checked out at the **workspace root**
> (the folder containing `HCore/` and `Smint.io/`) carries two things you need: the
> **workspace map** — what each repo is, how they depend on each other, and the OpenAPI
> code-generation workflow — and the **working agreements** that govern how changes are
> made here. Read it before making cross-repo changes.
>
> Note: cross-repo dependencies are **relative `ProjectReference` paths**, so every
> repo must be cloned side by side in the standard layout or this repo will not build.

# Portals-Development

**This repository is public.** It is published at `github.com/smintio/Portals-Development` and is
the front door for Smint.io Solution Partners and Enterprise-plan customers developing solutions
for Smint.io Portals. That governs everything written here — see "Writing for this repo" below before editing
a single file. It also governs this file: keep it free of internal repository names and paths.

Four things live here:

| | |
|---|---|
| **Reference implementations** (~158 `.cs`) | `Backend/` — paired connectors and data adapters: `Connector-HelloWorld` + `DataAdapter-HelloWorld` as the minimal skeleton, plus `Picturepark` (live connection) and `SharePoint` (indexed through the integration layer) as realistic ones, each with a test driver. `Frontend/Legacy/Example/ui-example-hello-world-1` is the UI component starter, and `Frontend/Legacy/config/` holds the shared rollup and TypeScript config it extends. |
| **Frontend reference sources** (`Frontend/Legacy/Reference/`) | **not in this repository.** The sources behind the components Smint.io ships — 64 UI components, 34 page templates, the shared `portals-components` library and the gallery view — are made available to Smint.io Certified Partners on request, and copied into this directory by the partner. The directory ships with a README and a `.gitignore` that hides anything copied in, so the sources can never be committed here by accident. Never describe them as part of this repository, and never link into `Reference/<folder>` from another document — refer to a package by name instead, because the path does not exist for most readers. |
| **Partner-facing documentation** (~1.1 MB of Markdown) | `Overview/`, `Backend/README.md` + `Backend/docs/` (eleven documents, including the recipe cookbook), `Frontend/Legacy/README.md` + `Frontend/Legacy/docs/` (ten documents, including the recipe cookbook and four generated catalogues), and a README per tool under `Tools/`. |
| **Agent guidance** | `Frontend/Legacy/CLAUDE.md` and `Backend/CLAUDE.md` carry the partner-facing working rules for building a component of each kind — the four-round interview, what is most often got wrong, verification and publishing. They are the public counterparts of the UI component library's and the backend SDKs' own CLAUDE.md files. |

`Tools/` ships three tools as release zips: `Portals-SDK-PublishComponent-CLI` (publish and register
a component), `Legacy/Portals-DevServer` (serve local frontend component builds to a development portal),
and `Portals-DataAdapter-SDK-DataAdapterExporter-CLI` (generate TypeScript definitions from a data
adapter assembly — **win-x64 only**, unlike the other two).

- Consumes `SmintIo.Portals.ConnectorSDK`, `SmintIo.Portals.DataAdapterSDK`,
  `SmintIo.Portals.DataAdapterSDK.TestDriver`, `SmintIo.Portals.SDK.Core`, and the
  `Connector.Test` and `DataAdapter.Test` packages. All examples are net10.0.

Start from `HelloWorld` for the shape of a component, then read `Picturepark` or `SharePoint` for
how a real source system is handled. The components Smint.io ships in production are maintained
separately and are not part of this repository.

Because this is documentation-by-example, keep it building: an example that no longer compiles
against the current SDK is worse than no example at all. The examples are also *copied* by partners
as their starting point, so a bug in one propagates — treat example code with the care of published
source, not of a sketch.

## Writing for this repo

Everything here is read by people outside Smint.io. Before committing anything:

- **No customer or tenant names**, no internal repository or path references, no internal class
  names from the platform's own source, and no captured payloads from a real portal.
- **No credentials or configuration values** — in particular nothing out of an
  `appsettings.<Env>.json`, which hold OAuth secrets and npm authorization headers.
- **Document only what is supported.** Internal or undocumented parameters and behaviours do not
  belong in a partner-facing document even when they work.
- Explain a mechanism in terms of the SDK types a partner writes against, not in terms of how the
  platform implements it. "Smint.io prefixes entity keys per connector configuration, so never
  hard-code one" is right; naming the literal prefix and the class that applies it is not.

Conventions every partner-facing document here follows — match them. The `CLAUDE.md` files are
agent guidance rather than partner documents, and deliberately carry no version line, no table of
contents and no `Contributors` block.

- Setext-style title (`=====` underline), then a `Current version of this document is: X.Y.Z (as of
  <date>)` line, and a `Contributors` block at the end. **Bump the version line when you change a
  document**, and make sure there is exactly one of it.
- A numbered link list as the table of contents, with `#user-content-`-prefixed anchors for
  in-document links (GitHub's rendering of README files inside a directory listing needs the
  prefix).
- Relative links, resolved from the file's own directory — not from the directory the document
  talks about. **Check every link and anchor after editing**; both are easy to get wrong and
  nothing fails loudly when they are. A heading with a stray double space silently changes its
  own anchor.

## This repository is the home of the frontend component documentation

`Frontend/Legacy/` — the README, `docs/`, `CLAUDE.md` and `Example/` — is the single description
of how a Smint.io Portals frontend component is built, for Smint.io and for partners alike. It
has to stand on its own: most readers do not have the reference sources, so a document that only
works if you can open a component's source is not finished.

The internal UI component repositories keep only what is specific to them: their monorepo layout
and lerna globs, their dev server mapping prefixes, their tenants and customers, and the worked
examples behind the anonymised lessons told here. Their own CLAUDE.md files delegate everything
else to this repository.

So: **when you learn something about building a frontend component, write it down here**, and
add a pointer on the internal side only if the internal specifics change it. A rule that applies
to any component belongs in this repository, anonymised. Never move a customer name, a tenant
name, a portal URL or an internal path in the other direction.

Where it goes: a **contract** — what exists, what it is called, what it returns — belongs in the
reference document that owns that area. A **task** — how you actually do something, end to end —
belongs in `docs/smintio-frontend-recipes.md`, as a `## How do I …?` recipe with a complete
snippet and the mistakes it invites. Do not explain the same mechanism in both; the reference
links to the recipe, and the recipe links back.

Four documents under `Frontend/Legacy/docs/` are **generated**, not written:
`smintio-data-adapter-reference.md` and `smintio-page-type-contracts.md` from the SDK source and
the page templates, and `smintio-ui-components.md` and `smintio-page-templates.md` from the
component sources — none of which are in this repository. Regenerate them here, from those
sources, rather than copying a second copy in. **Never hand-edit one**: the next regeneration
discards the edit, so anything wrong in them — a stale name, a customer name in a display name —
has to be fixed in the component metadata upstream.

## This repository is also the home of the backend component documentation

`Backend/` — the README, `docs/`, `CLAUDE.md` and the example projects — is the single
description of how a Smint.io Portals backend component is built, for Smint.io and for partners
alike, and it covers all seven backend component types: connectors, data adapters, data
processors, task handlers, portal templates, resources and identity providers. The same rule
applies as on the frontend side: **when you learn something about building one, write it down
here**, anonymised, and add a pointer on the internal side only if the internal specifics change
it. The same split holds: contracts in the reference document that owns the area, tasks as
`## How do I …?` recipes in `docs/smintio-backend-recipes.md`.

The backend material has internal authorities too: the connector SDK's and data adapter SDK's own
CLAUDE.md files describe the metamodel and the asset data model in full, and the SDK core types
under `Models/Metamodel/Data` are the last word on what an asset actually carries.
`Backend/docs/smintio-asset-data-model.md` and `smintio-connector-metamodel.md` are the
partner-facing rendering of that material. When the SDK changes, update these from the source
types rather than from memory — and keep them free of the internal detail the internal documents
carry (wire encodings, the execute routes, the platform's own resolver classes).

The line to hold for every backend document: the **SDK types a partner writes against** —
`IConnectorStartup`, `AssetsDataAdapterBaseImpl`, `DataAdapterPermission`, the data processor
lifecycle phase interfaces — belong here and should be named precisely. The **platform's own
implementation** — the classes that execute a method, resolve a component, load an assembly or
persist a configuration — does not belong here at all. Describe what the platform does; never
name what does it. The same goes for the shipped components' own class names, which live in an
internal repository partners cannot read: describe a shipped data processor by what it does, not
by its type name.
