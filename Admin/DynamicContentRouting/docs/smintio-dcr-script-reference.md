Dynamic Content Routing script reference
========================================

Current version of this document is: 1.0.0 (as of 16th of September, 2026)

This is the complete contract for the two scripts a Dynamic Content Routing configuration
carries: what functions exist, what they return, and what they do to the request. For what
Dynamic Content Routing is and when to reach for it, read
[the topic introduction](../README.md) first. For task-shaped guidance — *how do I actually
express this rule?* — read [the recipes](smintio-dcr-recipes.md).

Two conventions run through everything below. **The prepare asset search script narrows what is
found; the validate asset access script decides what may be read.** A function that exists in
one does not exist in the other, and a rule expressed in only one of them is not enforced.

1. [The execution model](#user-content-the-execution-model)
1. [Engine limits](#user-content-engine-limits)
1. [Which functions exist where](#user-content-which-functions-exist-where)
1. [Ending a script: the four outcomes](#user-content-ending-a-script-the-four-outcomes)
1. [Request context](#user-content-request-context)
1. [Reading custom form values](#user-content-reading-custom-form-values)
1. [Shaping the search — prepare asset search only](#user-content-shaping-the-search--prepare-asset-search-only)
1. [Search fragment identifiers](#user-content-search-fragment-identifiers)
1. [Reading the asset — validate asset access only](#user-content-reading-the-asset--validate-asset-access-only)
1. [The data object accessor](#user-content-the-data-object-accessor)
1. [Downloads and request pages](#user-content-downloads-and-request-pages)
1. [Things that are easy to get wrong](#user-content-things-that-are-easy-to-get-wrong)

## The execution model

Each script is a **body**, not a function declaration. Smint.io wraps the text you supply in a
function and calls it, so:

- a bare `return;` ends the script cleanly and allows the request to continue;
- falling off the end of the script does the same;
- `return` may be used at the top level, and is the idiomatic way to leave early.

Scripts run in **strict mode**. Undeclared assignment is an error, and `let` or `const` may not
be redeclared in the same scope — a long script that reuses a name like `existingSelection`
should declare it with `var`, or use distinct names.

A script is parsed once per configuration version and the parsed form is cached, so editing the
script re-parses it and a syntax error surfaces when the configuration is saved.

Both scripts run **synchronously and per request**. There is no `await`, no timer, no network
access, no `require`, no file system, and no way to reach the platform other than through the
functions documented here. Anything you need has to come from a custom form value, from the
request context, or from the asset itself.

## Engine limits

The scripts run inside a constrained sandbox. These are hard limits: exceeding one aborts the
script with an execution error, which fails the request rather than silently allowing it.

| Limit | Value |
|---|---|
| Statements executed | 512 |
| Wall-clock time | 500 ms |
| Memory | 512 KB |
| Recursion depth | 0 — a function may not call itself, directly or indirectly |
| Array size | 65 536 elements |
| Regular expression evaluation | 5 ms |

The statement budget is the one that bites in practice. A loop counts each iteration's
statements, so a `for` over a hundred values doing three things per value is already most of the
budget. Prefer array methods over hand-written loops, build the small list you need rather than
iterating the large one, and do not walk a result set — the validate script is called once per
asset already.

Writing to the host is disabled, so you cannot mutate anything you are handed except through the
setter functions below.

## Which functions exist where

| Group | Prepare asset search | Validate asset access |
|---|---|---|
| Request context | ✔ | ✔ |
| Custom form values | ✔ | ✔ |
| `debug`, `error`, `permissionDenied` | ✔ | ✔ |
| `redirectToLogin` | – | ✔ |
| Form field value get/set, search fragment control | ✔ | – |
| `getDataObject` | – | ✔ |
| Download item interrogation, `setRequestPageConfigurationUuid` | – | ✔ (download flow only) |

Calling a function that does not exist in the script you are in is a reference error, which ends
the request with an execution error. Keep the two scripts structurally parallel but do not copy
calls across.

Where each script runs:

| Data adapter method | Prepare asset search | Validate asset access |
|---|---|---|
| `SearchAssetsAsync` | ✔ | – |
| `GetFormItemDefinitionAllowedValuesAsync` | ✔ | – |
| `GetFullTextSearchProposalsAsync` | ✔ | – |
| `GetAssetAsync` | – | ✔ |
| `GetAssetsAsync` | – | ✔ |
| `GetRandomAssetsAsync` | – | ✔ |
| `GetAssetsDownloadItemMappingsAsync` | – | ✔ |

`getMethodName()` returns exactly these names, and is how a script branches on the flow it is
serving.

The validate script is invoked **once per asset**. For `GetAssetsAsync`, random assets and
download item mappings, the platform iterates the result and calls your script for each asset in
turn; **the first denial fails the whole call**, it does not filter the offending asset out of
the set. Design for that: a page that reads twenty assets in one call shows nothing at all if one
of them is denied, so a rule that is meant to hide individual assets belongs in the search
script, with the access script mirroring it as the enforcement backstop.

## Ending a script: the four outcomes

| Function | Effect |
|---|---|
| *(fall through, or `return;`)* | Continue. The request proceeds with whatever the script changed. |
| `permissionDenied()` | The request fails with an access-denied result. |
| `redirectToLogin()` | The request fails with a redirect-to-login result. Validate script only. |
| `error(message)` | The request fails with an execution error, and `message` is carried into the error the portal displays. |

**These functions record the outcome; they do not stop the script.** Execution continues at the
next statement, and a later call overwrites the outcome that an earlier one recorded. Always
return immediately:

```javascript
if (!allowed) {
  permissionDenied();
  return;
}
```

`redirectToLogin()` is for the case where the asset is not public but the user is anonymous, and
signing in would plausibly grant access — it sends the visitor to the login flow instead of
showing a refusal. Reach for it only when the distinction is real; for an authenticated user who
simply lacks the permission, `permissionDenied()` is correct.

### `error()` is how you see inside a running script

`error(message)` is the only channel a script has back to the person looking at the portal, and
it is therefore the practical way to debug one. The message you pass is carried into the error the
portal displays, so a value you cannot otherwise inspect can be forced into view:

```javascript
var regions = getUserStringArrayCustomFormFieldValues("regions");

error(JSON.stringify(regions));
return;
```

`JSON.stringify` is available, and is what makes this useful for arrays and objects. A plain
string works too — `error("reached the editorial branch"); return;` answers a question about which
path a script took.

Two things follow from the fact that this is an *error*:

- **It fails the request.** This is a deliberate probe, not logging. The search returns nothing, or
  the asset fails to open, for as long as the line is in place.
- **Everyone sees it.** The message reaches whoever triggered the request, including ordinary
  portal visitors. A diagnostic left in a live script both breaks the portal and shows your
  internals to its users.

So: add the probe, reproduce once, read the message, remove the line. Never leave one in a script
that real users are hitting.

### `debug()` writes where you cannot read

`debug(value)` writes to the Smint.io platform log. **Portal administrators have no access to
those logs**, so unless you are Smint.io support this call produces nothing you can see — it just
costs a statement out of the budget. Use `error()` instead.

## Request context

| Function | Returns |
|---|---|
| `getMethodName()` | the data adapter method being served, e.g. `"SearchAssetsAsync"` |
| `getTypeName()` | the short name of the public API interface being invoked |
| `getTypeFullName()` | its fully qualified name |
| `getUserUuid()` | the user's UUID, or `null` for an anonymous visitor |
| `getUserIsAnonymous()` | `true` for an anonymous visitor, `false` for a signed-in user, `null` if unknown |
| `getPortalUuid()` | the UUID of the portal the request belongs to |
| `getPageConfigurationUuid()` | the UUID of the page configuration the request originates from, where there is one |
| `getConnectorInstanceKey()` | the connector instance behind this data adapter, in the form `"c:<uuid>"` |
| `getDataAdapterInstanceKey()` | the data adapter instance, in the form `"da:<uuid>"` |
| `getTwoLetterISOLanguageName()` | the two-letter language code of the current request, e.g. `"de"` |

`getConnectorInstanceKey()` and `getDataAdapterInstanceKey()` matter because **one Dynamic
Content Routing configuration can be attached to several data adapter configurations**, which may
sit on different connectors with different metadata schemas. A script serving more than one has
to select the right identifiers for the instance it is running against — see the recipe on
[serving several connectors from one script](smintio-dcr-recipes.md#user-content-how-do-i-serve-several-connectors-or-data-adapters-from-one-script).

`getPortalUuid()` and `getPageConfigurationUuid()` let one script carry rules that apply to only
one portal or one page. Prefer a page-level custom form value where you can: a UUID hard-coded in
a script is invisible to the administrator who later moves or rebuilds that page.

## Reading custom form values

Everything a script knows about *this user* comes from custom form values. See
[Custom forms](../../CustomForms/README.md) for how the forms themselves are defined and filled
in. Three scopes are readable, and **they do not have the same return shape**.

### User scope — always an array, merged across groups

A user may belong to several frontend user groups, each with its own values for the same form.
Every user-scope accessor collects the value from **all** of the user's groups, discards
duplicates, and returns an array. A field an administrator thinks of as a single value therefore
arrives as an array with one element — or with several, when two of the user's groups disagree.

| Function | Reads a form item of type | Returns |
|---|---|---|
| `getUserStringCustomFormFieldValues(id)` | `string` | `string[]` |
| `getUserStringArrayCustomFormFieldValues(id)` | `string_array` | `string[]`, flattened across groups |
| `getUserInt32CustomFormFieldValues(id)` | `int32` | `int[]` |
| `getUserInt32ArrayCustomFormFieldValues(id)` | `int32_array` | `int[]`, flattened |
| `getUserInt64CustomFormFieldValues(id)` | `int64` | `long[]` |
| `getUserInt64ArrayCustomFormFieldValues(id)` | `int64_array` | `long[]`, flattened |
| `getUserDecimalCustomFormFieldValues(id)` | `decimal` | `decimal[]` |
| `getUserBooleanCustomFormFieldValues(id)` | `boolean` | `bool[]` |
| `getUserDateTimeCustomFormFieldValues(id)` | `date_time` | date array |

All of them return `null` — not an empty array — when no custom form is attached or the user has
no groups carrying values. Guard accordingly, and note that the arrays may contain empty strings
where a group has the item but left it blank:

```javascript
var regions = getUserStringArrayCustomFormFieldValues("regions") ?? [];
regions = regions.filter((region) => !!region);
```

The merge is a **union**, which is the right default for a permission model: being in a second
group can only ever add. A boolean flag is therefore true for the user if *any* of their groups
sets it:

```javascript
var earlyAccess = getUserBooleanCustomFormFieldValues("earlyAccess")?.includes(true) ?? false;
```

If you need "all groups must agree", you have to express that yourself — but think twice, because
it makes group membership take permissions away, which surprises administrators.

The accessor you call has to match the form item's declared type. Reading a `string_array` item
with `getUserStringCustomFormFieldValues` returns nothing at all, silently.

### Data adapter configuration scope — a single value

| Function | Reads a form item of type | Returns |
|---|---|---|
| `getDataAdapterStringCustomFormFieldValue(id)` | `string` | `string` or `null` |
| `getDataAdapterStringArrayCustomFormFieldValues(id)` | `string_array` | `string[]` or `null` |
| `getDataAdapterInt32CustomFormFieldValue(id)` | `int32` | `int` or `null` |
| `getDataAdapterInt32ArrayCustomFormFieldValues(id)` | `int32_array` | `int[]` or `null` |
| `getDataAdapterInt64CustomFormFieldValue(id)` | `int64` | `long` or `null` |
| `getDataAdapterInt64ArrayCustomFormFieldValues(id)` | `int64_array` | `long[]` or `null` |
| `getDataAdapterDecimalCustomFormFieldValue(id)` | `decimal` | `decimal` or `null` |
| `getDataAdapterBooleanCustomFormFieldValue(id)` | `boolean` | `bool` or `null` |
| `getDataAdapterDateTimeCustomFormFieldValue(id)` | `date_time` | date or `null` |

Note the singular `…Value` for scalars and the plural `…Values` for arrays. There is no merging
here — one data adapter configuration, one set of values.

### Assets search page configuration scope — a single value

The same set, with `getPage…` in place of `getDataAdapter…`:
`getPageStringCustomFormFieldValue`, `getPageStringArrayCustomFormFieldValues`,
`getPageInt32CustomFormFieldValue`, `getPageInt32ArrayCustomFormFieldValues`,
`getPageInt64CustomFormFieldValue`, `getPageInt64ArrayCustomFormFieldValues`,
`getPageDecimalCustomFormFieldValue`, `getPageBooleanCustomFormFieldValue`,
`getPageDateTimeCustomFormFieldValue`.

Page values express *what this page is for*; user values express *what this person may see*. The
useful rule is almost always their intersection — a page that is scoped to two brands, seen by a
user entitled to one of them, shows one. See
[the intersection recipe](smintio-dcr-recipes.md#user-content-how-do-i-combine-a-page-level-restriction-with-a-user-level-one).

Page values are **not available in every flow**: a direct asset read or a download does not
originate from a page configuration. A rule that exists only at page level is therefore not
enforceable in the validate script. Put the enforcing dimension on the user group and use the
page value to narrow further.

### Reserved shortcut accessors

Nine shortcuts exist for three reserved form item ids, `filterValue`, `filterValue2` and
`filterValue3`: `getUserFilterValues()`, `getUserFilterValues2()`, `getUserFilterValues3()`,
`getDataAdapterFilterValue()` and its numbered variants, and `getPageFilterValue()` and its
numbered variants. They are equivalent to calling the corresponding string accessor with that id.

Prefer the explicit accessor with a meaningful id. A form item called `regions` reads as what it
is in both the form and the script; `filterValue2` does not.

## Shaping the search — prepare asset search only

A search is described by a set of **form field values**, each identified by a search fragment id.
The portal's filter panel renders these, the user changes them, and the data adapter turns them
into a query. The prepare script runs before the query is built, so what it writes is what the
data source is asked for.

### Reading and writing values

| Function | Notes |
|---|---|
| `hasFormFieldValue(id)` | is any value present for this id? |
| `getFormFieldValueString(id)` / `setFormFieldValueString(id, value, hint)` | |
| `getFormFieldValueStringArray(id)` / `setFormFieldValueStringArray(id, values, hint)` | the common case |
| `getFormFieldValueInt(id)` / `setFormFieldValueInt(id, value, hint)` | |
| `getFormFieldValueIntArray(id)` / `setFormFieldValueIntArray(id, values, hint)` | |
| `getFormFieldValueDecimal(id)` / `setFormFieldValueDecimal(id, value, hint)` | |
| `getFormFieldValueDecimalArray(id)` / `setFormFieldValueDecimalArray(id, values, hint)` | |
| `getFormFieldValueBoolean(id)` / `setFormFieldValueBoolean(id, value, hint)` | |
| `getFormFieldValueDateTime(id)` / `setFormFieldValueDateTime(id, value, hint)` | takes and returns a `Date`, interpreted as UTC |

A setter **replaces** any value already present for that id — it does not merge with what the
user selected. When you want to narrow rather than replace, read first, intersect, and write
back; that is the shape of
[the limit-the-options recipe](smintio-dcr-recipes.md#user-content-how-do-i-let-the-user-keep-filtering-but-only-within-what-they-are-allowed-to-see).

### Several values: any of them, or all of them

On an Integration Layer data adapter, the values of one fragment combine the way that fragment's
metadata attribute is set in the data adapter configuration. By default an asset matches when it
has **any** of them (OR). With *Find assets that have all selected values* switched on, it must have
**all** of them (AND). The values a script writes follow the same setting as the user's; there is no
separate mode for scripts, with or without the `"DCR"` hint.

Routing almost always means "OR": a user allowed three regions should see assets of any of the
three. On a fragment set to "AND", the same script finds only assets tagged with all three
regions, usually none. Route on fragments left at the default, and check the setting of a
fragment before you rely on its values being a union. The setting exists only for fragments that
hold several values per asset (lists, split values, fields of a record list); other data adapters
run their own search and ignore it.

The getters are **type-checked**. `getFormFieldValueStringArray` returns `null` if the stored
value is a string rather than a string array, even though the id matches. Read with the accessor
that matches how the fragment is declared, not with the one that matches what you intend to
write.

`hint` is a third, optional argument passed through to the data adapter. Two values carry meaning:

| Hint | Meaning |
|---|---|
| `"DCR"` | Keeps the value applied while the user is **browsing folders**. |
| `"Filter"` | Currently read only by the Picturepark data adapter, which distinguishes a hard filter from an aggregation filter: marks this value as the hard filter. Leave it out elsewhere. |

The `"DCR"` hint deserves a sentence of its own. When a portal shows folder navigation, the
platform counts the assets behind each folder so that empty folders can be hidden, and it
deliberately ignores the user's metadata filters when doing so — otherwise every folder would
look empty as soon as a filter was set. Your restriction is not the user's filter, though: a
folder whose entire contents you have routed away *should* read as empty. Passing `"DCR"` is what
makes that happen.

Any other hint string is passed through and ignored by data adapters that do not recognise it.

### Controlling what the filter panel shows

| Function | Effect on the filter model returned to the portal |
|---|---|
| `addNotAllowedSearchFragment(id)` | removes that fragment from the panel entirely |
| `addClearSelectionSearchFragment(id)` | resets that fragment to its default and clears its modified marker |
| `addLimitFormFieldValueStringArrayValues(id, values)` | restricts the fragment's selectable options, and its current selection, to `values` |

These three change **presentation**; the value you set with a setter is what actually restricts
the query, and it applies whether or not the fragment is visible. They are not a security
mechanism on their own — a fragment removed from the panel can still be sent by a caller that
constructs its own request, which is exactly why the value has to be set as well as the fragment
hidden.

**`addNotAllowedSearchFragment`** is the partner to a setter. The pair "set the value, then hide
the fragment" is the basic Dynamic Content Routing move: the user cannot see the dimension you
are routing on and cannot widen it.

```javascript
setFormFieldValueStringArray(regionKey, allowedRegions);
addNotAllowedSearchFragment(regionKey);
```

**`addLimitFormFieldValueStringArrayValues`** is for the opposite case, where the dimension is
genuinely useful to the user and you only want to bound it. It intersects both the fragment's
allowed-value list and the user's current selection with the values you pass. It applies to
string-array fragments only; on any other type it does nothing. Because the option list is
rewritten, the fragment can no longer page through its values, so keep the allowed set small.

**`addClearSelectionSearchFragment`** exists for the moment when you have just overwritten the
user's selection wholesale — it stops the panel from reporting a filter as modified when the
modification is yours, not theirs. If clearing it leaves nothing else in its group modified, the
group is marked unmodified too.

A date range is rendered as a group holding two fragments, `<id>.min` and `<id>.max`. Hiding the
range means hiding all three ids — the group and both ends.

## Search fragment identifiers

Getting the id wrong is the single most common Dynamic Content Routing bug, and it fails
**quietly**: a setter with an unknown id adds a value nothing reads, so the search is not
narrowed and every asset is returned.

For a data source that Smint.io indexes, a metadata fragment is identified by its path through
the asset:

```
rawData.<connector instance>___<metamodel entity>.<property>
```

for example `rawData.c101___CR.access_control`. Properties that are not part of a connector's
raw data sit at the top level instead, such as `parentFolderPaths.folderIds` for folder
membership or `permissionSetIds` for a source system's own permission identifiers.

### The identifier is abbreviated in search, expanded in the asset

**Search fragment ids use a shortened form of the entity key. Paths into the asset data object
use the full one.** The two are not interchangeable, and this is where most paired scripts go
wrong — the prepare script works, the validate script silently reads `null`, and everything is
denied or everything is allowed depending on how the comparison is written.

| In a search fragment id | In an asset data object path |
|---|---|
| `___SF.` | `___SharepointFile.` |
| `___CR.` | `___CloudinaryResource.` |
| `___TC_` | `___ThronContent_` |

So the same field is `rawData.c101___CR.region` to the prepare script and
`["rawData", "c101___CloudinaryResource", "region"]` to the validate script. Entity keys
that are not in this table are identical in both.

### Finding the id

Read the id from the portal itself rather than deriving it. Open the search page the routing will
apply to, inspect the filter panel's form item definitions, and take the id verbatim. The
connector instance number is part of it, so an id is specific to one connector configuration and
does not survive being copied to another tenant or another environment.

## Reading the asset — validate asset access only

`getDataObject()` returns an accessor over the asset being read. It throws if there is no asset,
which cannot happen in the flows the validate script serves, so it needs no guard.

```javascript
var dataObject = getDataObject();
```

The accessor is a read-only view. Everything below returns `null` (or an empty result) rather
than throwing when the path does not exist — which means **an asset that is missing the field you
route on passes every "is it in my list?" test vacuously if you write the test the wrong way
round.** Write the test so that absence denies:

```javascript
var assetRegions = dataObject.getEnumKeyArrayValueByPath(["rawData", "c101___CloudinaryResource", "region"]);

if (!assetRegions || assetRegions.filter((region) => allowedRegions.includes(region)).length === 0) {
  permissionDenied();
  return;
}
```

## The data object accessor

Every getter comes in two forms: `…Value(propertyKey)` for a property on the object you hold, and
`…ValueByPath(path)` taking an array of path elements for a property nested inside it.

| Getter | Returns |
|---|---|
| `getId()` | the asset's id |
| `getMetamodelEntityKey()` | the entity key of the object |
| `getStringValue(key)` / `getStringValueByPath(path)` | a string, resolving localized text in the request culture |
| `getStringArrayValue(key)` / `getStringArrayValueByPath(path)` | a string array |
| `getIntValue(key)` / `getIntValueByPath(path)` | an integer or `null` |
| `getIntArrayValue(key)` / `getIntArrayValueByPath(path)` | an integer array |
| `getDecimalValue(key)` / `getDecimalValueByPath(path)` | a decimal or `null` |
| `getDecimalArrayValue(key)` / `getDecimalArrayValueByPath(path)` | a decimal array |
| `getBooleanValue(key)` / `getBooleanValueByPath(path)` | a boolean or `null` |
| `getDateTimeValue(key)` / `getDateTimeValueByPath(path)` | a date or `null` |
| `getDateTimeArrayValue(key)` / `getDateTimeArrayValueByPath(path)` | a date array |
| `getEnumKeyValue(key)` / `getEnumKeyValueByPath(path)` | the **key** of a taxonomy value |
| `getEnumKeyArrayValue(key)` / `getEnumKeyArrayValueByPath(path)` | the keys of a multi-valued taxonomy field |
| `getDataObjectValue(key)` / `getDataObjectValueByPath(path)` | a nested accessor |
| `getDataObjectArrayValue(key)` / `getDataObjectArrayValueByPath(path)` | an array of nested accessors |

Four date-time convenience predicates take the path and the answer to give when the path is not
found, so that the "field missing" case is an explicit decision rather than an accident:

| Predicate | True when |
|---|---|
| `getDateTimeInFutureByPath(path, pathNotFoundResult)` | the value lies strictly in the future |
| `getDateTimeNowOrInFutureByPath(path, pathNotFoundResult)` | now or later |
| `getDateTimeInPastByPath(path, pathNotFoundResult)` | strictly in the past |
| `getDateTimeNowOrInPastByPath(path, pathNotFoundResult)` | now or earlier |

For a go-live rule, `getDateTimeNowOrInPastByPath(path, false)` reads as "published, and an asset
with no publication date is not published" — which is the safe default.

### Enum keys versus display names

A metadata field backed by a taxonomy is an **enum**. Its stable identity is its key; its display
name is localized and may be edited by anyone with rights in the source system.

- `getEnumKeyValueByPath` returns the key — match on this.
- `getStringValueByPath` on the same field returns the localized display name — do not match on
  this.

Routing on display names produces a permission model that changes when someone renames a
taxonomy entry in another language. Custom form values that feed an enum comparison must
therefore hold keys too, which is why an allowed-value list on a custom form item carries the key
as its value and the readable label as its localized `name`.

### How a path is walked

- The first element `rawData` is special: the **second** element is matched against the metamodel
  entity key of the connector's raw data objects, and the walk continues inside the one that
  matches.
- Nested objects and enum objects in the middle of a path are traversed transparently.
- When a path element resolves to an **array** of objects, a scalar getter returns the value from
  the **first** element that yields one. If the distinction matters, take the array with
  `getDataObjectArrayValue…` and decide yourself.
- A scalar getter on a field that holds a single value returns it as a one-element array when you
  ask for the array form; the array getters are forgiving in that direction.

Paths are also where a script that serves several data adapters diverges, because the same
logical field may be nested differently in each. Branch on `getDataAdapterInstanceKey()` and
select the path, rather than trying to write one path that fits both.

## Downloads and request pages

In the download flow — `getMethodName() === "GetAssetsDownloadItemMappingsAsync"` — three
further functions are available.

| Function | Returns / does |
|---|---|
| `getIsInitiatingAssetDownloadForItemId(itemId)` | `true` if the request is actually starting a download of that item |
| `getIsInitiatingAssetDownloadForOneOfItemIds(itemIds)` | the same for any of several items |
| `setRequestPageConfigurationUuid(uuid)` | routes the request to a request page instead of proceeding |

The distinction the first two draw is between *listing what could be downloaded* — which the
portal does to render a download dialog — and *this user pressing this download button now*. A
rule that should only fire on the real download, such as demanding an approval step, tests for
the initiating case; a rule about which renditions may be offered at all does not.

`setRequestPageConfigurationUuid` hands the request to a page configuration — typically a form
that asks for approval or collects a justification — rather than refusing it. It is the humane
alternative to `permissionDenied()` where the answer is "not yet" rather than "no". Unlike the
outcome functions it does not end the script, and the request continues normally; the first uuid
set during the iteration over the assets wins.

**It exists only in the download flow.** Guard every call:

```javascript
if (getMethodName() !== "GetAssetsDownloadItemMappingsAsync") {
  return;
}
```

## Things that are easy to get wrong

- **Only narrowing the search.** A search fragment you hide is not a permission. Direct asset
  links, collections, related assets, random asset widgets and download requests all reach assets
  without going through a search. Every rule in the prepare script needs its mirror in the
  validate script. This is the one rule in this document that is not negotiable.
- **Forgetting to `return` after `permissionDenied()`.** The outcome is recorded but the script
  keeps running, and a later statement can overwrite it or fail.
- **Using the shortened entity key in an asset path, or the expanded one in a search fragment
  id.** Both fail silently in opposite directions.
- **Routing on a fragment set to "AND".** Your list of allowed values then means
  "has every one of them", not "has any of them", and the search usually comes back empty. See
  [several values](#user-content-several-values-any-of-them-or-all-of-them).
- **Matching on display names instead of enum keys.** Works today, breaks when someone renames a
  taxonomy value or a user switches language.
- **Treating a user-scope read as a scalar.** It is always an array, merged across the user's
  groups, and it may be `null`.
- **Allowing when the value is absent.** `!assetValues || assetValues.filter(…).length === 0` is
  the correct shape; testing only the overlap lets an asset with no value through.
- **Granting everything to a user with no settings.** An empty allow-list has to mean "nothing",
  not "no restriction". Substitute a sentinel that matches no asset — see
  [the deny-by-default recipe](smintio-dcr-recipes.md#user-content-how-do-i-deny-everything-when-the-user-has-no-setting-at-all).
- **Exceeding the statement budget with a loop.** 512 statements is not many. Build small arrays,
  avoid nested iteration, and remember the validate script already runs once per asset.
- **Hard-coding a portal, page or connector identifier.** It ties the script to one environment
  and breaks silently when the object is rebuilt. Prefer a custom form value.
- **Leaving an `error()` probe in a live script.** It fails every request that reaches it and
  shows your diagnostic to portal visitors. Add it, reproduce once, remove it.
- **Reaching for `debug()`.** It writes to a log only Smint.io support can read. `error()` is the
  channel you can actually see.

## Questions

Please do not hesitate to contact us at [support@smint.io](mailto:support@smint.io) if you run
into any issues.

Contributors
============

- Reinhard Holzner, Smint.io GmbH
