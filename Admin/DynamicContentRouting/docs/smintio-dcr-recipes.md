Dynamic Content Routing recipes: how do I …?
============================================

Current version of this document is: 1.0.0 (as of 16th of September, 2026)

Task-shaped guidance for writing Dynamic Content Routing scripts. Each recipe gives both halves —
the prepare asset search script and the validate asset access script — because
[a rule expressed in only one of them is not enforced](../README.md#user-content-the-two-scripts--and-why-you-always-write-both).
For the contract behind any function used here, see
[the script reference](smintio-dcr-script-reference.md); for the fields the scripts read, see
[Custom forms](../../CustomForms/README.md).

The identifiers in these recipes are illustrative. Search fragment ids, entity keys and taxonomy
keys are specific to one connector configuration in one tenant — read yours from the portal
rather than adapting the ones below.

1. [How do I restrict assets by a metadata value the user carries?](#user-content-how-do-i-restrict-assets-by-a-metadata-value-the-user-carries)
1. [How do I deny everything when the user has no setting at all?](#user-content-how-do-i-deny-everything-when-the-user-has-no-setting-at-all)
1. [How do I let the user keep filtering, but only within what they are allowed to see?](#user-content-how-do-i-let-the-user-keep-filtering-but-only-within-what-they-are-allowed-to-see)
1. [How do I combine a page-level restriction with a user-level one?](#user-content-how-do-i-combine-a-page-level-restriction-with-a-user-level-one)
1. [How do I apply a different rule per portal or per page?](#user-content-how-do-i-apply-a-different-rule-per-portal-or-per-page)
1. [How do I serve several connectors or data adapters from one script?](#user-content-how-do-i-serve-several-connectors-or-data-adapters-from-one-script)
1. [How do I restrict by folder rather than by metadata?](#user-content-how-do-i-restrict-by-folder-rather-than-by-metadata)
1. [How do I hide assets until a go-live date, with an early access exception?](#user-content-how-do-i-hide-assets-until-a-go-live-date-with-an-early-access-exception)
1. [How do I give everyone a public baseline on top of their own entitlements?](#user-content-how-do-i-give-everyone-a-public-baseline-on-top-of-their-own-entitlements)
1. [How do I keep a restriction applied while the user browses folders?](#user-content-how-do-i-keep-a-restriction-applied-while-the-user-browses-folders)
1. [How do I reuse the source system's own permission identifiers?](#user-content-how-do-i-reuse-the-source-systems-own-permission-identifiers)
1. [How do I restrict by the language the portal is being viewed in?](#user-content-how-do-i-restrict-by-the-language-the-portal-is-being-viewed-in)
1. [How do I route a download to an approval page instead of refusing it?](#user-content-how-do-i-route-a-download-to-an-approval-page-instead-of-refusing-it)
1. [How do I apply a rule only to certain asset types?](#user-content-how-do-i-apply-a-rule-only-to-certain-asset-types)
1. [How do I work out why an asset is being denied?](#user-content-how-do-i-work-out-why-an-asset-is-being-denied)

## How do I restrict assets by a metadata value the user carries?

The base pattern, and the one every other recipe is a variation of. The user's portal user groups
carry a `regions` field; assets carry a `region` taxonomy. A user sees an asset when the two
overlap.

**Prepare asset search** — set the fragment to the user's regions, then hide it:

```javascript
var regions = getUserStringArrayCustomFormFieldValues("regions") ?? [];
regions = regions.filter((region) => !!region);

if (regions.length > 0) {
  setFormFieldValueStringArray("rawData.c101___CR.region", regions);
}

addNotAllowedSearchFragment("rawData.c101___CR.region");
```

**Validate asset access** — deny when the asset's regions do not overlap the user's:

```javascript
var dataObject = getDataObject();

var regions = getUserStringArrayCustomFormFieldValues("regions") ?? [];
regions = regions.filter((region) => !!region);

if (regions.length > 0) {
  var assetRegions = dataObject.getEnumKeyArrayValueByPath(["rawData", "c101___CloudinaryResource", "region"]);

  if (!assetRegions || assetRegions.filter((assetRegion) => regions.includes(assetRegion)).length === 0) {
    permissionDenied();
    return;
  }
}
```

Note the two things that differ between the halves and must not be copied across:

- the search fragment id uses the **shortened** entity key, `c101___CR`; the asset path uses the
  **expanded** one, `c101___CloudinaryResource`;
- the search takes a flat string, the asset read takes a path array.

**Mistakes this invites.** Calling `addNotAllowedSearchFragment` inside the `if` — the fragment
then reappears for exactly the users who have no restriction, advertising the dimension. Writing
the denial as `assetRegions.filter(…).length === 0` without the `!assetRegions` guard, which lets
an asset with no region through. And matching with `getStringArrayValueByPath` instead of
`getEnumKeyArrayValueByPath`, which compares against localized display names.

## How do I deny everything when the user has no setting at all?

The recipe above leaves an unconfigured user unrestricted. That is the wrong default: it means a
tenant that is half configured, or a user group somebody forgot to fill in, sees the whole
library.

Substitute a sentinel value that no asset can carry, rather than skipping the filter:

**Prepare asset search**

```javascript
var regions = getUserStringArrayCustomFormFieldValues("regions") ?? [];
regions = regions.filter((region) => !!region);

if (regions.length === 0) {
  regions = ["-"];
}

setFormFieldValueStringArray("rawData.c101___CR.region", regions);
addNotAllowedSearchFragment("rawData.c101___CR.region");
```

**Validate asset access**

```javascript
var dataObject = getDataObject();

var regions = getUserStringArrayCustomFormFieldValues("regions") ?? [];
regions = regions.filter((region) => !!region);

if (regions.length === 0) {
  regions = ["-"];
}

var assetRegions = dataObject.getEnumKeyArrayValueByPath(["rawData", "c101___CloudinaryResource", "region"]);

if (!assetRegions || assetRegions.filter((assetRegion) => regions.includes(assetRegion)).length === 0) {
  permissionDenied();
  return;
}
```

The sentinel has to be a value the taxonomy cannot produce. A single hyphen is conventional and
safe for key-based taxonomies; pick something else if your keys could legitimately be one.

Where a dimension is genuinely optional — a refinement rather than an entitlement — the
conditional form in the first recipe is right. The test is whether an administrator forgetting to
fill the field in should open the library or close it. If it should close it, use a sentinel.

**Mistakes this invites.** Applying the sentinel in the search script and the conditional form in
the validate script, so unconfigured users see an empty result list but can still open assets by
direct link.

## How do I let the user keep filtering, but only within what they are allowed to see?

Sometimes the dimension you route on is one the user genuinely wants to filter by — a product
type, a brand — and hiding it removes something useful. Bound the fragment instead of replacing
it.

**Prepare asset search**

```javascript
var productTypes = getUserStringArrayCustomFormFieldValues("productTypes") ?? [];
productTypes = productTypes.filter((productType) => !!productType);

const productTypeKey = "rawData.c101___CR.product_type";

if (productTypes.length === 0) {
  // nothing allowed at all
  setFormFieldValueStringArray(productTypeKey, ["-"]);
  addNotAllowedSearchFragment(productTypeKey);
} else if (productTypes.length === 1) {
  // only one option left, so the fragment has nothing to offer
  setFormFieldValueStringArray(productTypeKey, productTypes);
  addNotAllowedSearchFragment(productTypeKey);
} else {
  var existingSelection = getFormFieldValueStringArray(productTypeKey);
  var reducedSelection = existingSelection?.filter((selected) => productTypes.includes(selected));

  if (!reducedSelection || reducedSelection.length === 0) {
    // the user has selected nothing, or nothing they are entitled to
    setFormFieldValueStringArray(productTypeKey, productTypes);
    addLimitFormFieldValueStringArrayValues(productTypeKey, productTypes);
    addClearSelectionSearchFragment(productTypeKey);
  } else {
    // keep the part of their selection they are entitled to
    setFormFieldValueStringArray(productTypeKey, reducedSelection);
    addLimitFormFieldValueStringArrayValues(productTypeKey, reducedSelection);
  }
}
```

**Validate asset access** — unchanged in shape from the base recipe. The bounding is an experience
concern; the entitlement is still the full user list:

```javascript
var dataObject = getDataObject();

var productTypes = getUserStringArrayCustomFormFieldValues("productTypes") ?? [];
productTypes = productTypes.filter((productType) => !!productType);

if (productTypes.length === 0) {
  productTypes = ["-"];
}

var assetProductTypes = dataObject.getEnumKeyArrayValueByPath(["rawData", "c101___CloudinaryResource", "product_type"]);

if (!assetProductTypes || assetProductTypes.filter((assetProductType) => productTypes.includes(assetProductType)).length === 0) {
  permissionDenied();
  return;
}
```

What the three presentation calls do: `addLimitFormFieldValueStringArrayValues` cuts the
fragment's option list and current selection down to the allowed set;
`addClearSelectionSearchFragment` stops the panel reporting a filter as user-modified when the
modification was yours; `addNotAllowedSearchFragment` removes it entirely.

**Mistakes this invites.** Declaring `existingSelection` with `let` in both branches of a longer
script — strict mode rejects the redeclaration. Relying on `addLimitFormFieldValueStringArrayValues`
alone without also setting the value: limiting the options does not narrow the query. And listing
a large allowed set, which disables paging through the fragment's values.

## How do I combine a page-level restriction with a user-level one?

A page is scoped to a set of distribution channels; a user is entitled to a set. What the page
shows is the intersection.

**Prepare asset search**

```javascript
const pageChannels = getPageStringArrayCustomFormFieldValues("distributionChannels") ?? [];
const userChannels = getUserStringArrayCustomFormFieldValues("distributionChannels") ?? [];

let channels = pageChannels.filter((pageChannel) => userChannels.indexOf(pageChannel) > -1);

if (channels.length === 0) {
  channels = ["-"];
}

setFormFieldValueStringArray("rawData.c101___CR.channel", channels);
addNotAllowedSearchFragment("rawData.c101___CR.channel");
```

**Validate asset access** — the page scope is **not available here**, so the enforcement is on the
user dimension alone:

```javascript
var dataObject = getDataObject();

let userChannels = getUserStringArrayCustomFormFieldValues("distributionChannels") ?? [];

if (userChannels.length === 0) {
  userChannels = ["-"];
}

var assetChannel = dataObject.getEnumKeyValueByPath(["rawData", "c101___CloudinaryResource", "channel"]);

if (!assetChannel || userChannels.indexOf(assetChannel) === -1) {
  permissionDenied();
  return;
}
```

This asymmetry is deliberate and correct. A direct asset read or a download does not come from a
page configuration, so there is no page value to enforce against. The page value narrows *within*
what the user may see; the user value is the entitlement. Put the dimension that has to be
enforced on the user group, always.

**Mistakes this invites.** Putting the entitlement only on the page — the portal then looks
restricted and is not. Reading the page value in the validate script and denying when it is
absent, which denies every direct link and download.

## How do I apply a different rule per portal or per page?

One data adapter often feeds several portals, or a portal has a page with its own rule. Branch on
the identifier and return early when the rule does not apply.

**Prepare asset search**

```javascript
const portalUuid = getPortalUuid();

if (portalUuid !== 1234) {
  return;
}

const categoryKey = "rawData.c101___CR.category";

setFormFieldValueStringArray(categoryKey, ["a1b2c3", "d4e5f6"]);
addNotAllowedSearchFragment(categoryKey);
```

**Validate asset access**

```javascript
const portalUuid = getPortalUuid();

if (portalUuid !== 1234) {
  return;
}

var dataObject = getDataObject();

var assetCategories = dataObject.getEnumKeyArrayValueByPath(["rawData", "c101___CloudinaryResource", "category"]);

const allowedCategories = ["a1b2c3", "d4e5f6"];

if (!assetCategories || assetCategories.filter((assetCategory) => allowedCategories.includes(assetCategory)).length === 0) {
  permissionDenied();
  return;
}
```

`getPageConfigurationUuid()` works the same way for a page-specific rule.

Prefer a custom form value to a hard-coded UUID wherever you can. A UUID in a script is invisible
to the administrator who later rebuilds that page, and the rule silently stops applying — or
worse, silently starts applying to something else. Hard-coding is acceptable for a rule that is
genuinely about one portal and will be revisited when that portal is; it is not a pattern to
build a scheme on.

**Mistakes this invites.** Guarding the prepare script on the portal but not the validate script,
so the rule is enforced on portals it was never meant for. Comparing the UUID as a string —
`getPortalUuid()` returns a number.

## How do I serve several connectors or data adapters from one script?

One Dynamic Content Routing configuration can be attached to several data adapter configurations
sitting on different connectors, each with its own entity key. Select the identifiers, then run
one rule.

**Prepare asset search**

```javascript
let channels = getUserStringArrayCustomFormFieldValues("distributionChannels") ?? [];

if (channels.length === 0) {
  channels = ["-"];
}

const connectorInstanceKey = getConnectorInstanceKey();

let entity;
switch (connectorInstanceKey) {
  case "c:101": entity = "c101___LayoutFile"; break;
  case "c:102": entity = "c102___DocumentFile"; break;
  case "c:103": entity = "c103___MediaFile"; break;
  default: entity = "c104___ImageFile"; break;
}

setFormFieldValueStringArray(`rawData.${entity}.channel`, channels);
addNotAllowedSearchFragment(`rawData.${entity}.channel`);
```

**Validate asset access**

```javascript
var dataObject = getDataObject();

let channels = getUserStringArrayCustomFormFieldValues("distributionChannels") ?? [];

if (channels.length === 0) {
  channels = ["-"];
}

const connectorInstanceKey = getConnectorInstanceKey();

let entity;
switch (connectorInstanceKey) {
  case "c:101": entity = "c101___LayoutFile"; break;
  case "c:102": entity = "c102___DocumentFile"; break;
  case "c:103": entity = "c103___MediaFile"; break;
  default: entity = "c104___ImageFile"; break;
}

var assetChannel = dataObject.getEnumKeyValueByPath(["rawData", entity, "channel"]);

if (!assetChannel || channels.indexOf(assetChannel) === -1) {
  permissionDenied();
  return;
}
```

The same technique with `getDataAdapterInstanceKey()` handles the case where two data adapters on
the *same* connector nest the field differently — one exposes it on the asset, another under a
related product — and the path, not just the entity key, has to change.

Note that a `default:` branch which picks a real entity is a deliberate choice: it means a data
adapter you did not anticipate is routed as if it were the last one. Where that would be wrong,
make the default deny instead.

**Mistakes this invites.** Letting the two scripts drift apart — the switch has to be identical in
both, and it is the part most likely to be updated in one place only. Forgetting that entity keys
are shortened in the search id and expanded in the asset path, which turns one switch into two
different switches.

## How do I restrict by folder rather than by metadata?

Where the source system's folder structure already encodes the audience, route on folder
membership. The fragment is not under `rawData` — folder membership is a first-class property.

**Prepare asset search**

```javascript
const folderIds = [];

const courseLevels = getUserStringCustomFormFieldValues("courseLevel") ?? [];

let hasCourseLevel = false;

for (const courseLevel of courseLevels) {
  if (courseLevel !== "none") {
    folderIds.push(`Training Library/Courses/${courseLevel}`);

    hasCourseLevel = true;
  }
}

if (!hasCourseLevel) {
  folderIds.push("Training Library/Open Access");
}

const showArchive = getUserBooleanCustomFormFieldValues("showArchivedCourses")?.includes(true) ?? false;

if (showArchive) {
  folderIds.push("Training Library/Archive");
}

setFormFieldValueStringArray("parentFolderPaths.folderIds", folderIds);
addNotAllowedSearchFragment("parentFolderPaths.folderIds");
```

**Validate asset access** — build the same list, then test the asset's folder membership against
it:

```javascript
const folderIds = [];

const courseLevels = getUserStringCustomFormFieldValues("courseLevel") ?? [];

let hasCourseLevel = false;

for (const courseLevel of courseLevels) {
  if (courseLevel !== "none") {
    folderIds.push(`Training Library/Courses/${courseLevel}`);

    hasCourseLevel = true;
  }
}

if (!hasCourseLevel) {
  folderIds.push("Training Library/Open Access");
}

const showArchive = getUserBooleanCustomFormFieldValues("showArchivedCourses")?.includes(true) ?? false;

if (showArchive) {
  folderIds.push("Training Library/Archive");
}

var dataObject = getDataObject();

var assetFolderIds = dataObject.getStringArrayValueByPath(["parentFolderPaths", "folderIds"]);

if (!assetFolderIds || !assetFolderIds.some((folderId) => folderIds.includes(folderId))) {
  permissionDenied();
  return;
}
```

Folder paths are strings, not enum keys, so `getStringArrayValueByPath` is correct here.

Folder routing is the most brittle dimension available: the rule breaks the moment somebody
renames a folder in the source system, and it breaks silently in the permissive direction if a
new folder appears outside the tree you named. Prefer metadata when the source system carries it.
Where folders are all you have, keep the list of paths in a custom form field so that a rename is
an administrator's edit rather than a script change.

**Mistakes this invites.** Building the folder list twice and letting the two copies drift.
Consuming most of the statement budget in the loop — keep the list of folder paths short, or
move it into a custom form value.

## How do I hide assets until a go-live date, with an early access exception?

Assets carry a publication date; most users should not see them before it; a few should.

**Prepare asset search**

```javascript
let earlyAccess = getUserBooleanCustomFormFieldValues("earlyAccess")?.includes(true) ?? false;

if (!earlyAccess) {
  setFormFieldValueDateTime("rawData.c101___CR.go_live_date.max", new Date());
}

addNotAllowedSearchFragment("rawData.c101___CR.go_live_date");
addNotAllowedSearchFragment("rawData.c101___CR.go_live_date.min");
addNotAllowedSearchFragment("rawData.c101___CR.go_live_date.max");
```

**Validate asset access**

```javascript
var dataObject = getDataObject();

let earlyAccess = getUserBooleanCustomFormFieldValues("earlyAccess")?.includes(true) ?? false;

if (!earlyAccess) {
  if (!dataObject.getDateTimeNowOrInPastByPath(["rawData", "c101___CloudinaryResource", "go_live_date"], false)) {
    permissionDenied();
    return;
  }
}
```

A date range is rendered as a group with two fragments, `.min` and `.max`, so hiding it means
hiding all three ids — the group and both ends. Setting only `.max` leaves the range open at the
bottom, which is what "everything published so far" means.

`getDateTimeNowOrInPastByPath(path, false)` says "published, and an asset with no publication date
is not published". That second argument is the whole point of the predicate: it forces you to
decide what an absent date means instead of letting a `null` comparison decide for you.

**Mistakes this invites.** Hiding only the bare id and leaving `.min` and `.max` in the panel,
where the user can reopen the range. Writing the comparison as `assetDate > new Date()` without
handling a missing date, which lets undated assets through. Assuming the embargo lifts the instant
the date passes — results are cached per user, so it lifts on the next cache miss.

## How do I give everyone a public baseline on top of their own entitlements?

Where some content is open to all comers, express it as a value every user gets rather than as a
branch that skips the filter.

**Prepare asset search**

```javascript
let accessControls = getUserStringArrayCustomFormFieldValues("accessControl") ?? [];
accessControls.push("public");

setFormFieldValueStringArray("rawData.c101___CR.access_control", accessControls);
addNotAllowedSearchFragment("rawData.c101___CR.access_control");
```

**Validate asset access**

```javascript
var dataObject = getDataObject();

let accessControls = getUserStringArrayCustomFormFieldValues("accessControl") ?? [];
accessControls.push("public");

var assetAccessControls = dataObject.getStringArrayValueByPath(["rawData", "c101___CloudinaryResource", "access_control"]);

if (!assetAccessControls || assetAccessControls.filter((assetAccessControl) => accessControls.includes(assetAccessControl)).length === 0) {
  permissionDenied();
  return;
}
```

Because the public value is pushed onto the same list, there is no second code path and no
"unrestricted" branch to get wrong. A user with no entitlements sees exactly the public content,
which is almost always what "public baseline" should mean — and an asset with no access control
value at all is denied, not published by accident.

**Mistakes this invites.** Writing it as `if (accessControls.length === 0) { return; }` instead,
which grants the whole library to unconfigured users.

## How do I keep a restriction applied while the user browses folders?

When a portal offers folder navigation, the platform counts the assets behind each folder so that
empty folders can be hidden, and it deliberately ignores the user's own metadata filters when
counting — otherwise every folder would look empty the moment a filter was set.

Your restriction is not the user's filter. A folder whose entire contents your routing has
removed should read as empty and disappear. Pass the `"DCR"` hint to say so:

**Prepare asset search**

```javascript
setFormFieldValueString("rawData.c101___CR.is_public", "yes", "DCR");
addNotAllowedSearchFragment("rawData.c101___CR.is_public");
```

**Validate asset access**

```javascript
var dataObject = getDataObject();

var isPublic = dataObject.getStringValueByPath(["rawData", "c101___CloudinaryResource", "is_public"]);

if (isPublic !== "yes") {
  permissionDenied();
  return;
}
```

Use the hint on every value your routing sets in a portal that offers folder navigation. Without
it the folder tree advertises folders the user cannot open anything in — a small leak of
structure, and a confusing experience.

**Mistakes this invites.** Setting the hint on some of the routing values and not on others: the
folder counts then ignore the values without it. `"Filter"` is a different hint, read currently by
the Picturepark data adapter only; it does not replace `"DCR"`.

## How do I reuse the source system's own permission identifiers?

Sometimes the source system's permission model is worth keeping — a DAM's permission sets map
cleanly onto audiences, and duplicating them would be worse than reading them. Route on the
identifiers rather than reimplementing the scheme.

**Prepare asset search**

```javascript
let permissionIds = getUserStringArrayCustomFormFieldValues("sourcePermissionIds") ?? [];

if (permissionIds.length === 0) {
  permissionIds = ["-"];
}

setFormFieldValueStringArray("permissionSetIds", permissionIds);
addNotAllowedSearchFragment("permissionSetIds");
```

**Validate asset access**

```javascript
let permissionIds = getUserStringArrayCustomFormFieldValues("sourcePermissionIds") ?? [];

if (permissionIds.length === 0) {
  permissionIds = ["-"];
}

var dataObject = getDataObject();

var assetPermissionIds = dataObject.getStringArrayValueByPath(["externalSecurity", "externalSecurityGroupIds"]);

if (!assetPermissionIds || assetPermissionIds.filter((assetPermissionId) => permissionIds.includes(assetPermissionId)).length === 0) {
  permissionDenied();
  return;
}
```

This is the one case where routing and the source system's scheme are the same scheme, so the
usual objection — two models to maintain — does not apply. It does mean the portal inherits the
source system's granularity, which is why it works for permission sets and not for per-folder
grants.

**Mistakes this invites.** Assuming every connector exposes external security identifiers; most
do not, and the path returns `null`, which with the guard above denies everything. Check the
asset shape before committing to this dimension.

## How do I restrict by the language the portal is being viewed in?

Route on the request's language where the content is genuinely language-specific — a price list,
a legal document, a market-specific campaign.

**Prepare asset search**

```javascript
let language = getTwoLetterISOLanguageName();

if (language === "de") {
  setFormFieldValueStringArray("rawData.c101___CR.content_language", ["de"]);
} else {
  setFormFieldValueStringArray("rawData.c101___CR.content_language", ["en"]);
}

addNotAllowedSearchFragment("rawData.c101___CR.content_language");
```

**Validate asset access**

```javascript
var dataObject = getDataObject();

let language = getTwoLetterISOLanguageName();

const allowedLanguage = language === "de" ? "de" : "en";

var assetLanguage = dataObject.getEnumKeyValueByPath(["rawData", "c101___CloudinaryResource", "content_language"]);

if (assetLanguage !== allowedLanguage) {
  permissionDenied();
  return;
}
```

Think carefully before enforcing this one in the validate script. Language is a **presentation**
dimension, not an entitlement: a user who switches the portal to English and follows a link to a
German asset gets a refusal rather than the asset. Where language is about relevance rather than
permission, narrow the search and leave the validate script alone — that is the rare, principled
exception to implementing both halves, and it should be a conscious decision recorded in a
comment, not an omission.

**Mistakes this invites.** Treating the language code as an entitlement and then fielding support
tickets about broken links. Falling back to a hard-coded language for every locale you did not
anticipate, which silently hides content from a market you later add.

## How do I route a download to an approval page instead of refusing it?

Where the answer is "not yet" rather than "no", send the request to a page that collects an
approval or a justification.

**Validate asset access** — there is no prepare half; this is a download-flow rule:

```javascript
var methodName = getMethodName();

if (methodName !== "GetAssetsDownloadItemMappingsAsync") {
  return;
}

const dataObject = getDataObject();

const approvalRequired = dataObject.getBooleanValueByPath(["rawData", "c101___SharepointFile", "ApprovalRequired"]) ?? false;

if (approvalRequired) {
  setRequestPageConfigurationUuid(5678);
}
```

`setRequestPageConfigurationUuid` is available **only** in the download flow — the guard on
`getMethodName()` is not optional. Unlike `permissionDenied()` it does not end the script, and the
request proceeds; the first uuid set while the platform iterates the assets is the one used.

To distinguish "the portal is rendering a download dialog" from "this user just pressed download",
test `getIsInitiatingAssetDownloadForItemId(itemId)` or
`getIsInitiatingAssetDownloadForOneOfItemIds(itemIds)`. A rule that should only fire on the real
download — demanding approval, recording a justification — tests for the initiating case; a rule
about which renditions may be offered at all does not.

**Mistakes this invites.** Calling `setRequestPageConfigurationUuid` without the method guard,
which fails the request in every other flow. Using an approval page *instead of* an access rule:
this recipe adds a step to a download the user is otherwise entitled to make, it is not a
substitute for `permissionDenied()`.

## How do I apply a rule only to certain asset types?

Where one data adapter serves several kinds of object — products, editorial, certificates — a
security classification may live on a different entity for each, and may not apply to all of them.

**Prepare asset search**

```javascript
var restrictedTypes = ["certificate", "editorial", "billOfMaterials"];

var assetTypes = getFormFieldValueStringArray("rawData.c101___CatalogGeneral.asset_type");

var applyRestriction = !assetTypes ||
  assetTypes.length === 0 ||
  assetTypes.some((assetType) => restrictedTypes.indexOf(assetType) > -1);

if (applyRestriction) {
  var classifications = getUserStringArrayCustomFormFieldValues("securityClassifications") ?? [];

  if (classifications.length === 0) {
    classifications = ["notRestricted"];
  }

  if (assetTypes && assetTypes.indexOf("editorial") > -1) {
    setFormFieldValueStringArray("rawData.c101___CatalogEditorial.security_classification", classifications);
  } else {
    setFormFieldValueStringArray("rawData.c101___CatalogProduct.security_classification", classifications);
  }
}

addNotAllowedSearchFragment("rawData.c101___CatalogEditorial.security_classification");
addNotAllowedSearchFragment("rawData.c101___CatalogProduct.security_classification");
```

**Validate asset access** — the asset's own type is known here, so the branch is simpler and
exact:

```javascript
var dataObject = getDataObject();

const assetType = dataObject.getEnumKeyValueByPath(["rawData", "c101___CatalogGeneral", "asset_type"]);

const restrictedTypes = ["certificate", "editorial", "billOfMaterials"];

if (restrictedTypes.indexOf(assetType) === -1) {
  return;
}

var classifications = getUserStringArrayCustomFormFieldValues("securityClassifications") ?? [];

if (classifications.length === 0) {
  classifications = ["notRestricted"];
}

const entity = assetType === "editorial" ? "c101___CatalogEditorial" : "c101___CatalogProduct";

var assetClassifications = dataObject.getEnumKeyArrayValueByPath(["rawData", entity, "security_classification"]);

if (!assetClassifications || assetClassifications.filter((assetClassification) => classifications.includes(assetClassification)).length === 0) {
  permissionDenied();
  return;
}
```

The asymmetry here is inherent. The prepare script is guessing at asset types from what the user
has filtered on, because the assets have not been found yet; the validate script knows. Write the
prepare script to apply the restriction **whenever it might be relevant** — including when the
type filter is empty — and let the validate script be exact. Erring toward applying it in the
search costs a few results; erring the other way leaks.

Note that both classification fragments are hidden unconditionally, outside the branch. A fragment
that appears only for some users is itself a disclosure.

**Mistakes this invites.** Making the prepare script as exact as the validate script and
concluding the restriction does not apply, when the user simply had not filtered by type yet.
Hiding only the fragment the branch happened to set.

## How do I work out why an asset is being denied?

Print the value you are unsure about with `error()`. The message it carries is shown in the error
the portal displays, which is the only channel a script has back to you:

```javascript
var regions = getUserStringArrayCustomFormFieldValues("regions");

error(JSON.stringify(regions));
return;
```

Nine times in ten that settles it: the user's groups do not carry the value the script expects, or
carry it under a different id, or the id in the script does not match the one in the form.

The same probe answers "which branch did it take?" without stringifying anything:

```javascript
error("reached the editorial branch");
return;
```

And it works in either script. In the validate script, put it just above the `permissionDenied()`
you are investigating and print both sides of the comparison:

```javascript
var assetRegions = dataObject.getEnumKeyArrayValueByPath(["rawData", "c101___CloudinaryResource", "region"]);

error(JSON.stringify({ user: regions, asset: assetRegions }));
return;
```

That one line distinguishes all three of the usual causes at once — the user has no values, the
asset has none because the path is wrong, or both are present and simply do not overlap.

**Remove the probe when you are done.** `error()` fails the request: while the line is there the
search returns nothing or the asset will not open, and the message is shown to whoever triggered
the request, including ordinary portal visitors. It is a probe, not logging.

`debug()` looks like the tool for this and is not — it writes to the Smint.io platform log, which
portal administrators cannot read. The same goes for the **extended logging** setting on the
routing configuration: it records the full decision context on every denial, but into that same
log. Enable it when Smint.io support asks you to, and turn it off again afterwards. If `error()`
has not answered the question, that is the point to write to
[support@smint.io](mailto:support@smint.io).

Two symptoms with specific causes worth checking first:

- **Routing appears to do nothing at all.** The search fragment id is wrong. A setter with an
  unknown id adds a value nothing reads, and fails silently. Check it against the portal's filter
  panel, and check you used the shortened entity key.
- **Search is narrowed correctly but the detail page refuses the asset.** The two scripts
  disagree. Check the asset path uses the expanded entity key, and that an enum is being read
  with `getEnumKeyValueByPath` rather than `getStringValueByPath`.

## Questions

Please do not hesitate to contact us at [support@smint.io](mailto:support@smint.io) if you run
into any issues.

Contributors
============

- Reinhard Holzner, Smint.io GmbH
