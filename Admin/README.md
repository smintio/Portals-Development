Administrator-side development for Smint.io Portals
===================================================

Current version of this document is: 1.0.0 (as of 16th of September, 2026)

Not everything you build for Smint.io Portals is a component. A portal's behaviour is also shaped
by configuration that carries logic — fields an administrator defines for their tenant, and rules
written as scripts that run inside the platform. Nothing here is compiled, packaged or deployed:
it is authored in the Portals administration or through the Portals backend API, and it takes
effect immediately.

That work has a different rhythm from component development. There is no build to break, so a
mistake reaches a live portal directly. There is no type system, so a wrong identifier fails
silently rather than loudly. And because much of it decides what a user is allowed to see, the
cost of a mistake is not a broken page but a disclosure.

The documents here are written with that in mind. Each one leads with the failure modes.

## What lives here

| Topic | |
|---|---|
| [Custom forms](CustomForms/) | Administrator-defined fields on portal user groups, data adapter configurations, assets search page configurations and resources. The way per-tenant configuration that Smint.io cannot anticipate gets into the platform. |
| [Dynamic Content Routing](DynamicContentRouting/) | Per-user, per-request decisions about which assets a portal finds and which assets a user may open or download, derived from asset metadata and user group settings rather than from permissions assigned by hand. |
| [Guides for the backend MCP tools](Guides/) | How an agent that works through the backend MCP tools handles drafts and live changes, permissions, search filters, shared component configurations and resources. |

The two fit together. Custom forms supply the inputs — what this user is entitled to, what this
page is for — and Dynamic Content Routing scripts read them to decide what to show. If you are
starting a routing project, read [Custom forms](CustomForms/) first: the shape of the form
determines what the rule can say.

## How this relates to component development

| | Administrator-side | Component development |
|---|---|---|
| Written in | JSON and JavaScript | C#, TypeScript, Vue |
| Authored in | the Portals administration and backend API | your own repository |
| Delivered by | saving a configuration | building, packaging and publishing |
| Scope | one tenant | any tenant that installs the component |
| Takes effect | immediately | on the next deployment |

The boundary is not always obvious, and it is worth getting right. A rule that belongs to one
customer's content model — which market sees which brand — is administrator-side work, and
putting it in a component means recompiling to change a business rule. A capability that any
tenant would want — a new kind of integration, a new way of rendering a gallery — is a component,
and expressing it as a script means reimplementing it per tenant.

If you are building a component, start from [the repository root](../README.md) instead.

## Questions

Please do not hesitate to contact us at [support@smint.io](mailto:support@smint.io) if you run
into any issues.

Contributors
============

- Reinhard Holzner, Smint.io GmbH
