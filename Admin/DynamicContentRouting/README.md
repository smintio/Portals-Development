Dynamic Content Routing
=======================

Current version of this document is: 1.1.0 (as of 30th of September, 2026)

Dynamic Content Routing decides, per request and per user, which assets a Smint.io Portal shows
and which assets a user may open or download. It derives that decision from the asset's own
metadata and from settings carried on the user's portal user groups — so permissions follow the
content, across systems, without anyone assigning rights to a folder or an asset by hand.

It is implemented as a Smint.io data processor of the scripting type. An administrator attaches
it to one or more data adapter configurations and fills in two scripts. Nothing is compiled and
nothing is deployed.

1. [The problem it solves](#user-content-the-problem-it-solves)
1. [How it works](#user-content-how-it-works)
1. [The two scripts — and why you always write both](#user-content-the-two-scripts--and-why-you-always-write-both)
1. [Where the inputs come from](#user-content-where-the-inputs-come-from)
1. [Configuring it](#user-content-configuring-it)
1. [Designing a routing scheme](#user-content-designing-a-routing-scheme)
1. [Caching](#user-content-caching)
1. [Testing and troubleshooting](#user-content-testing-and-troubleshooting)
1. [Limits](#user-content-limits)
1. [Where to go next](#user-content-where-to-go-next)

## The problem it solves

Content management systems and digital asset management systems have permission models, and they
are the wrong shape for a portal.

**They are built for administrators, not audiences.** A DAM's permission scheme answers "who may
maintain this?" Its categories are teams, roles and workflow states. A portal has to answer a
different question — "what is this audience entitled to see?" — whose categories are markets,
brands, regions, distribution channels, embargo dates and contract terms. Expressing the second
in the vocabulary of the first means maintaining a second, parallel scheme inside a tool that was
not designed to hold it.

**They are per-system.** A portal routinely draws on a DAM, a PIM, a document store and a media
library at once. No single one of them can express "this partner may see these products from the
PIM and these images from the DAM and nothing from the document store", because none of them
knows about the others.

**They are assignment-based.** Most are variations on role-based access control lists: somebody
grants a group rights on a folder, a collection or an individual record. That scales with the
number of objects, and it decays — new assets land outside the folders that carry the grants, and
nobody notices until someone sees something they should not have.

Dynamic Content Routing inverts all three. The rule is written once, expressed in the metadata the
assets already carry, evaluated at request time, and it spans every data adapter the routing is
attached to. An asset that acquires the right metadata becomes visible to the right audience the
moment it is indexed, with no assignment step at all. A dimension you route on can be anything
the asset carries — a market taxonomy, a security classification, a brand, a go-live date, a
folder path, or the source system's own permission identifiers where those are worth reusing.

## How it works

Dynamic Content Routing hooks into the data adapter flows that find and read assets.

**On the way in, it rewrites the search.** Before the query is built, the routing script sets the
values of search fragments — the same fragments the portal's filter panel renders — so that the
query the data source receives is already narrowed to what this user may see. It usually hides
those fragments as well, so that the dimension being routed on does not appear in the filter
panel at all; but hiding is cosmetic, and the value is what narrows the search.

**On the way out, it validates the asset.** When an asset is read, listed or prepared for
download, the routing script receives the asset's data object and decides whether this user may
have it.

The result is that a routed portal looks, to its user, like a portal that simply contains less.
There is no visible permission model, no greyed-out content and no "access denied" on ordinary
navigation — the user sees a coherent, complete-looking library that happens to be theirs.

## The two scripts — and why you always write both

| | Runs on | Decides |
|---|---|---|
| **Prepare asset search** | `SearchAssetsAsync`, `GetFormItemDefinitionAllowedValuesAsync`, `GetFullTextSearchProposalsAsync` | what is **found** |
| **Validate asset access** | `GetAssetAsync`, `GetAssetsAsync`, `GetRandomAssetsAsync`, `GetAssetsDownloadItemMappingsAsync` | what may be **read or downloaded** |

**Never implement only the search script.** Narrowing a search hides assets from one route to
them; it does not protect them. Assets are reached without a search in many ordinary ways:

- a direct link to an asset detail page, which anyone who has ever had access can share;
- a collection, a share or a lightbox assembled earlier or by another user;
- related assets, variants and renditions surfaced from an asset the user *is* allowed to see;
- random or featured asset widgets on a landing page;
- a download request for an asset id;
- any caller that builds its own request rather than using the filter panel — including a caller
  that simply omits the fragment you removed from the panel.

Security by obscurity is not security. Every rule you express in the prepare script must have its
enforcement counterpart in the validate script, and the two must agree. When they disagree the
symptom is a portal that shows an asset in a result list and refuses it when clicked — which is
both a bad experience and a sign that the enforcing half is the one that is right.

The practical discipline: **write the validate script first.** It is the security boundary. Then
write the prepare script as the experience layer that stops the user from meeting that boundary.
Keep the two structurally parallel — same order, same variable names, same custom form reads — so
a reviewer can put them side by side and see that they say the same thing.

## Where the inputs come from

Nothing is hard-coded into a well-built routing scheme. The inputs are:

| Input | Scope | Typical use |
|---|---|---|
| Frontend user group custom form values | per user, merged across their groups | what this person is entitled to |
| Assets search page configuration custom form values | per page | what this page is for |
| Data adapter configuration custom form values | per data adapter | how this source is mapped |
| Portal and page configuration UUIDs | per request | rules that apply to one portal or page only |
| Connector and data adapter instance keys | per request | selecting the right metadata schema |
| Request language | per request | language-scoped content |
| The asset's own metadata | per asset | everything the rule is evaluated against |

The first three come from [custom forms](../CustomForms/README.md) — administrator-defined
fields attached to the standard Portals objects. Read that document before designing a routing
scheme; the shape of the form determines what the script can say.

The merging rule matters enough to repeat here: **a user's values are collected across all of
their portal user groups and de-duplicated**, so every user-scope read returns an array. Adding a
group can only widen what a user may see, which is what an administrator expects when they add
someone to a second group.

One consequence to design around: **the values on a user's groups are sent to that user's
browser** as part of the portal context. They are not secret. That is fine — the enforcement is
server-side and that is what protects the assets — but it means a custom form field is not a
place for anything confidential, and a routing scheme should not depend on a user not knowing
which market or classification they are assigned to.

## Configuring it

Setting up a routing scheme takes these steps, in this order:

1. **Create the custom form** through the backend API — see
   [Defining a form](../CustomForms/README.md#user-content-defining-a-form).
1. **Assign the form to the tenant**, also through the backend API — see
   [Attaching a form to the tenant](../CustomForms/README.md#user-content-attaching-a-form-to-the-tenant).
   Until then, the user group editor shows no custom form panel, and values sent through the API are
   dropped without an error.
1. **Fill in the values** on the portal user groups, and on pages or data adapters where the scheme
   uses them.
1. **Create the data processor configuration**, write both scripts, and attach it to the data adapter
   configurations it should govern.

Dynamic Content Routing is configured as a data processor. An administrator creates a data
processor configuration from it and attaches that configuration to the data adapter
configurations it should govern.

| Setting | |
|---|---|
| **Prepare asset search script** | the JavaScript that narrows the search |
| **Validate asset access script** | the JavaScript that enforces access |
| **Enable extended logging** | writes the full decision context to the Smint.io platform log on every denial. **For Smint.io support** — the log is not accessible to portal administrators, so turn this on only when support asks you to |

Two consequences of how it is attached:

- **One configuration can govern several data adapters**, which may sit on different connectors
  with different metadata schemas. A script that serves more than one has to branch on
  `getConnectorInstanceKey()` or `getDataAdapterInstanceKey()` and select the right identifiers.
- **Several data processors can be attached to the same data adapter**, running in priority
  order. Do not assume yours is the only one shaping the search.

Leaving either script empty disables that half. That is a supported configuration for a search
script you have not written yet — but a configuration with a search script and no access script
is the failure mode described above, not a stepping stone.

## Designing a routing scheme

**Deny by default.** A user whose groups carry no settings at all should see nothing, not
everything. The routing code runs before you know whether the administrator has finished
configuring the user groups, and "no restriction found, so no restriction applied" is how a
half-configured tenant leaks. Substitute a value that matches no asset instead of skipping the
filter.

**Route on stable keys.** Match taxonomy fields on their enum keys, never on display names.
Display names are localized and editable; a permission model that depends on them changes when
somebody fixes a typo.

**Let absence deny.** An asset that does not carry the field you route on should fail the test,
not pass it vacuously. Write the check so that a missing value is a denial.

**Prefer additive dimensions.** Because group values are unioned, a dimension where more values
means more access composes cleanly with group membership. A dimension where a value *removes*
access does not, and produces a scheme where adding someone to a group takes their content away.

**Keep the public baseline explicit.** Where some content is open to everyone, express that as a
value every user gets — pushed onto the list in the script — rather than as an exception branch.
The rule stays one shape, and the validate script mirrors it without a special case.

**Put the dimension on the user group, narrow it on the page.** A page-level value is not
available when an asset is read directly or downloaded, so a dimension that exists only at page
level cannot be enforced. Page values are for scoping a page within what the user may already
see.

**Name fields for the concept, not the mechanism.** A form item called `regions` or
`securityClassification` survives a change of data source; one called `filterValue2` does not.

## Caching

Dynamic Content Routing forces **user-scoped result caching** on every flow it touches. Results
are never shared between users, which is what makes per-user routing safe to cache at all.

The consequence for a script author: a routing decision must be a function of the user, the
request and the asset, and of nothing else that varies within a user's session. Comparing a
stored date against the current time is fine — an embargo that lifts is picked up on the next
cache miss. Making a decision that flips several times within a user's session is not, because
the user may see a cached answer from either side of the flip.

## Testing and troubleshooting

**`error()` is your tool.** A routing script has no console and no log you can read, but the
message you pass to `error()` is carried into the error the portal displays — so you can force any
value into view:

```javascript
var regions = getUserStringArrayCustomFormFieldValues("regions");

error(JSON.stringify(regions));
return;
```

That is almost always the whole diagnosis. When a script denies an asset you believe it should
allow, the answer is nearly always that the user's groups do not carry the value the script
expects, or carry it under a different id — and printing the value settles it in one round trip.

Two properties to respect, because this is an *error*, not logging:

- **It fails the request.** The search returns nothing, or the asset will not open, while the line
  is in place. It is a probe, not instrumentation.
- **Everyone sees it.** The message reaches whoever triggered the request, including ordinary
  portal visitors.

Add the probe, reproduce once, read the message, remove the line. Never leave one in a script real
users are hitting.

`debug()` exists but writes to the Smint.io platform log, which portal administrators cannot read.
It is of no use to you; reach for `error()` instead.

**Extended logging** is likewise for Smint.io support, not for you. With it enabled, every denial
writes the full decision context — the method, which script fired, the user's UUID and anonymity,
the custom form values in play at user, page and data adapter scope, the script's instance key and
configuration version, and the request input — to the platform log. Turn it on when Smint.io
support asks you to, and turn it off again afterwards: a portal doing its job denies constantly,
so it produces a great deal of logging for no benefit you can see.

If `error()` has not answered the question, that is the point to raise it with
[support@smint.io](mailto:support@smint.io).

The failure modes worth knowing on sight:

| Symptom | Usually means |
|---|---|
| Every asset is visible, routing appears to do nothing | the search fragment id is wrong, or written in the expanded form |
| Search is correctly narrowed, detail page refuses the asset | the two scripts disagree — most often the asset path is written in the shortened form |
| Everything is denied for everyone | an empty allow-list is being matched literally, or an enum is being compared against display names |
| The user group editor shows no custom form panel, and user values read as `null` | the form was created but never assigned to the tenant |
| A page that reads several assets shows nothing at all | one asset in the batch is denied, and a denial fails the whole call |
| Intermittent execution errors under load | the statement or time budget is being exceeded |

Each of these can be confirmed in one round trip by printing the value in question with
`error(JSON.stringify(…))` at the point the script reads it.

## Limits

The scripts run in a sandbox with hard limits — 512 statements, 500 ms, 512 KB, no recursion, no
network, no asynchronous work. See
[the script reference](docs/smintio-dcr-script-reference.md#user-content-engine-limits) for the
full table. These are generous for a routing rule and tight for anything else; if a scheme does
not fit, the answer is usually to move work into the metadata rather than into the script.

The validate script runs once per asset, and the first denial fails the whole call rather than
filtering that asset out. A rule intended to hide individual assets from a list therefore belongs
in the search script, with the validate script as the backstop.

## Where to go next

- [Script reference](docs/smintio-dcr-script-reference.md) — every function, what it returns, and
  what it does to the request.
- [Recipes: how do I …?](docs/smintio-dcr-recipes.md) — complete paired scripts for the rules
  that come up, and the mistakes each one invites.
- [Custom forms](../CustomForms/README.md) — how to define and fill in the fields the scripts
  read.
- [Smint.io Portals data processors](../../Backend/docs/smintio-data-processors.md) — the
  component type Dynamic Content Routing is built on, for readers who want to write their own.

## Questions

Please do not hesitate to contact us at [support@smint.io](mailto:support@smint.io) if you run
into any issues.

Contributors
============

- Reinhard Holzner, Smint.io GmbH
