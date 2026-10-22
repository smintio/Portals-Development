Description
===========

Current version of this document is: 1.5.0 (as of 16th of September, 2026)

This repository is the guide to **development for Smint.io Portals** — examples, reference
documentation and tools.

Development here falls into two areas, and they work quite differently:

- **Custom components** — connectors, data adapters, UI components, page templates and the other
  component types. Written in C#, TypeScript and Vue, built in your own repository, packaged and
  published, and installable by any tenant.
- **Administrator-side coding** — configuration that carries logic, such as Dynamic Content
  Routing rules and the custom forms that feed them. Written in JavaScript and JSON, authored in
  the Portals administration or through the Portals backend API, scoped to one tenant, and live
  the moment it is saved.

Most portal projects need some of both. A component adds a capability any tenant might want; an
administrator-side rule expresses what one customer's content model means.

Custom components
=================

- [What are Smint.io Portals components?](Overview/)

- [Developing frontend components](Frontend/Legacy/)

	- Page templates
	- UI components
	- [Recipes: how do I …?](Frontend/Legacy/docs/smintio-frontend-recipes.md)

- [Developing backend components](Backend/)

	- Connectors
	- Data adapters
	- Data processors
	- Task handlers
	- Portal templates
	- Resources
	- Identity providers
	- [Recipes: how do I …?](Backend/docs/smintio-backend-recipes.md)

This is the tools you need for developing Smint.io Portals components:

- The [Data Adapter Exporter CLI tool](Tools/Portals-DataAdapter-SDK-DataAdapterExporter-CLI/Release/) is used to generate TypeScript public API interface definitions from backend component assemblies
- The [Portals-SDK-PublishComponent-CLI tool](Tools/Portals-SDK-PublishComponent-CLI/Release/) is used to compile, package and deploy components
- The [Portals-DevServer](Tools/Legacy/Portals-DevServer/Release/) is used for local distribution of frontend components

Please note that future tools may be added or existing ones may be updated based on user feedback.

Administrator-side coding
=========================

- [Administrator-side development](Admin/) — what it is, and where the boundary with component
  development runs

	- [Custom forms](Admin/CustomForms/) — administrator-defined fields on portal user groups, data adapter configurations, assets search page configurations and resources
	- [Dynamic Content Routing](Admin/DynamicContentRouting/) — per-user, per-request decisions about which assets a portal finds and which assets a user may open or download

		- [Script reference](Admin/DynamicContentRouting/docs/smintio-dcr-script-reference.md)
		- [Recipes: how do I …?](Admin/DynamicContentRouting/docs/smintio-dcr-recipes.md)

	- [Guides for the backend MCP tools](Admin/Guides/), how an agent that works through the backend MCP tools handles drafts, permissions, search filters, shared components and resources

Nothing in this area is compiled or deployed, which also means nothing catches a mistake before a
live portal does. Each of these documents leads with the failure modes for that reason.

Contributors
============

- Reinhard Holzner, Smint.io GmbH
- Yosif Velev, Smint.io GmbH
