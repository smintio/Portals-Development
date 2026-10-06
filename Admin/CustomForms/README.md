Custom forms
============

Current version of this document is: 1.1.0 (as of 30th of September, 2026)

A custom form adds administrator-defined fields to a standard Smint.io Portals object or flow — a
portal user group, a data adapter configuration, an assets search page configuration or a resource,
or the registration, request and upload forms a portal presents to its users. The form describes the
fields once; each object, user or request then carries its own values for them.

Custom forms are how configuration that Smint.io cannot anticipate gets into the platform. Their
largest consumer is [Dynamic Content Routing](../DynamicContentRouting/README.md), whose scripts
read them to decide what a user may see, but they stand on their own: anything that needs a
per-user-group, per-page or per-data-adapter setting uses them.

**There is no administrative user interface for defining custom forms yet.** A form is created and
maintained through the Smint.io Portals backend API. Once it exists, the fields it defines appear
in the Portals administration for the objects it is attached to, and administrators fill in values
there as they would for any other setting.

1. [The shape of a custom form](#user-content-the-shape-of-a-custom-form)
1. [Attachment points](#user-content-attachment-points)
1. [Defining a form](#user-content-defining-a-form)
1. [Form groups](#user-content-form-groups)
1. [Form items](#user-content-form-items)
1. [Data types](#user-content-data-types)
1. [Allowed values](#user-content-allowed-values)
1. [Conditional visibility](#user-content-conditional-visibility)
1. [A complete example](#user-content-a-complete-example)
1. [Managing a form through the API](#user-content-managing-a-form-through-the-api)
1. [Attaching a form to the tenant](#user-content-attaching-a-form-to-the-tenant)
1. [Filling in values](#user-content-filling-in-values)
1. [How values reach a script](#user-content-how-values-reach-a-script)
1. [Things that are easy to get wrong](#user-content-things-that-are-easy-to-get-wrong)

## The shape of a custom form

Three levels:

```
CustomForm
└── custom_form_groups: FormGroup[]          one or more, each with an id and a name
    └── form_items: FormItem[]               one or more, each with an id, a name and a data type
```

A **form** is the unit that is created, versioned and attached. A **group** is a visual and
logical section — administrators see one panel per group. An **item** is a single field.

Ids are the contract. A script or a component reads a value by its item id, so the id is the name
the rest of the system knows the field by. Names and descriptions are localized strings for
humans; ids are not localized and should not change once anything reads them.

## Attachment points

A custom form is attached in one of two ways:

- **To a specific configuration**, where the form is one setting of that configuration and is
  selected in the Portals administration.
- **Tenant-wide**, to every object of one kind at once — all portal user groups, all assets search
  page configurations, and so on. These assignments are tenant settings and currently have no
  administrative user interface.

### Attached to a specific configuration

Some configurations have a **Custom form** setting of their own. It is selected in the Portals
administration from the tenant's forms, like any other setting, and applies only where that
configuration is used — one login system, one request form component, one upload data adapter.

| Configuration | Setting | The form is used for |
|---|---|---|
| Login system of type Smint.io | `CustomFormId` | the registration form. Values are stored on the portal user, shown on the user in the Portals administration, and included in download reports. With **Custom form can be changed in self service**, users can edit their values in their account |
| Request download form component (`ui-generic-request-download-form-1`) | `requestRequestDownloadCustomFormId` | the form of a download request, for the approval process and simple form task handlers. Values are stored with the task and with the resulting download event, and included in download reports |
| Request access, request permission and generic request form components (`ui-generic-request-access-form-1`, `ui-generic-request-permission-form-1`, `ui-generic-request-generic-form-1`) | `requestRequest…CustomFormId` | the form of the request the component submits |
| Asset upload data adapters | `CustomFormId` | the form used when uploading |

These settings are shown only in the **Expert** view of the login system and the request form
components, and in the **Advanced** view of an upload data adapter. Switch the configuration editor
to that view if the setting is not visible.

### Attached tenant-wide

For the four object types below, the form is assigned at **tenant level**, one form per attachment
point — see [Attaching a form to the tenant](#user-content-attaching-a-form-to-the-tenant):

| Attachment point | Values are filled in on | Read by a routing script with |
|---|---|---|
| Frontend user group | each portal user group | `getUser…CustomFormFieldValues(id)` |
| Data adapter configuration | each data adapter configuration | `getDataAdapter…CustomFormFieldValue(s)(id)` |
| Assets search page configuration | each assets search page configuration | `getPage…CustomFormFieldValue(s)(id)` |
| Resource | each resource, per resource type | — |

This is the most important structural fact about tenant-wide forms: **one definition per tenant
per attachment point**. Every portal user group in the tenant shares one form and fills in its own
values. You cannot give two user groups different fields — only different values for the same
fields.

Design accordingly. A form that tries to serve several unrelated purposes grows fields that are
meaningless for most groups. Use [conditional visibility](#user-content-conditional-visibility) to
keep such a form readable, or model the distinction as a value rather than as a field.

The resource attachment point is per resource *type*, so a tenant may have several resource forms
— one for each type of resource it uses.

## Defining a form

The form definition is a JSON document posted to the backend API. The minimum that is accepted:

```json
{
  "name": [{ "culture": "x-default", "value": "Portal user group settings" }],
  "custom_form_groups": [
    {
      "id": "contentRouting",
      "name": [{ "culture": "x-default", "value": "Content routing" }],
      "form_items": [
        {
          "id": "regions",
          "name": [{ "culture": "x-default", "value": "Regions" }],
          "data_type": "string_array",
          "visibility": "basic"
        }
      ]
    }
  ]
}
```

A form must have at least one group; a group must have at least one item; an item must have an
`id`, a `data_type` and a `visibility`. Omitting any of these is rejected with a specific error
rather than silently defaulted.

## Form groups

| Field | |
|---|---|
| `id` | required; the group's identifier |
| `name` | localized; the panel heading administrators see |
| `description` | localized; explanatory text under the heading |
| `is_default_group` | marks the group that holds items with no section of their own |
| `form_items` | required; at least one |

The default group is reconciled between the flag and the id: a group whose id is the reserved
default group name `__default_form_group__` is treated as the default group whether or not the
flag is set, and a group with the flag set is given that id. Set one or the other, not both, and
do not use that id for an ordinary group.

Use groups to separate concerns an administrator thinks about separately — content routing
settings, branding settings, integration settings. Administrators read the group headings to find
the field they want; a single group with twenty items is a worse form than four groups with five.

## Form items

| Field | |
|---|---|
| `id` | required; the identifier a script or component reads |
| `name` | localized; the field label |
| `description` | localized; the help text |
| `data_type` | required; see below |
| `visibility` | required; `basic`, `advanced`, `expert` or `hidden` |
| `is_required` | defaults to `false` |
| `sort_order` | position within the group; defaults to `-1` |
| `column_count_desktop` | 1–12; how wide the field renders on desktop |
| `column_count_mobile` | 1–12; the same on mobile |
| `line_feed_before` | force a line break before the field |
| `line_feed_after` | force a line break after it |
| `default_<type>_value(s)` | the value the field starts with |
| `allowed_values` | a fixed list of selectable values |
| `visible_ifs` | conditions under which the field is shown |

`visibility` controls how much digging an administrator has to do to find the field. `basic` is
always shown; `advanced` and `expert` are behind progressive disclosure; `hidden` is not offered
in the administration at all, which is how a value that is set programmatically is kept out of
the way.

Names are limited in length, with a longer allowance for boolean items — a checkbox label is a
sentence, a text field label is a phrase.

## Data types

Ten data types are accepted in a custom form:

| `data_type` | Value field on a form field value | Notes |
|---|---|---|
| `string` | `string_value` | |
| `string_array` | `string_array_values` | multi-select when `allowed_values` is given |
| `localized_strings_model` | `localized_strings_value` | one value per culture |
| `int32` | `int32_value` | |
| `int32_array` | `int32_array_values` | |
| `int64` | `int64_value` | |
| `int64_array` | `int64_array_values` | |
| `decimal` | `decimal_value` | |
| `boolean` | `boolean_value` | |
| `date_time` | `date_time_value` | RFC 3339 |

The API's data type enumeration lists further values — asset and folder references, page and data
adapter instance keys, metadata attribute selectors and the like. **Those exist for component
configuration forms and are rejected in a custom form.** Attempting to use one fails with a
missing-data-type error.

Per-type constraints that are honoured:

| Data type | Also accepts |
|---|---|
| `string`, `string_array` | `string_min_length`, `string_max_length`, `string_is_email` |
| `localized_strings_model` | `string_min_length`, `string_max_length`, `localized_strings_values_is_rich_text` |

The other string flags in the API schema — JSON, URI, colour, rich text, CSS and so on — describe
component configuration fields and are not carried over to custom form items.

**There is no `decimal_array` and no `date_time_array`.** A dimension that needs several decimals
or several dates has to be modelled as a `string_array` with parsing in the consumer, or split
into several items.

**`localized_strings_model` cannot be read from a Dynamic Content Routing script.** There is no
accessor for it. Use it for a field a component renders, never for one a routing rule matches on
— and note that localized text is the wrong shape for a rule anyway, because the value changes
with the culture.

## Allowed values

Giving an item an `allowed_values` list turns a free-text field into a selection. Each entry
carries the value in the slot matching the item's **scalar** type, plus a localized name and
description:

```json
{
  "id": "regions",
  "name": [{ "culture": "x-default", "value": "Regions" }],
  "data_type": "string_array",
  "visibility": "basic",
  "allowed_values": [
    {
      "string_value": "emea",
      "name": [
        { "culture": "x-default", "value": "Europe, Middle East and Africa" },
        { "culture": "de", "value": "Europa, Naher Osten und Afrika" }
      ]
    },
    {
      "string_value": "amer",
      "name": [{ "culture": "x-default", "value": "Americas" }]
    }
  ]
}
```

Note that a `string_array` item's allowed values carry `string_value`, not `string_array_values` —
the list describes the individual options, and the array is what the administrator selects from
them. The same holds for `int32_array` and `int64_array`.

An allowed value with no value in the expected slot is rejected. An allowed value list on a data
type that cannot carry one is rejected too.

**Always use allowed values for a field a routing script will match on.** The value is the thing
the script compares; free text invites typos that fail silently and take an afternoon to find.
Where the values are taxonomy keys from a source system, the key goes in `string_value` and the
readable label goes in `name` — that way administrators pick "Europe, Middle East and Africa" and
the script receives `emea`.

## Conditional visibility

`visible_ifs` shows an item only when another item in the same form has a particular value:

```json
"visible_ifs": [
  {
    "property": "restrictByRegion",
    "operator": "equal",
    "boolean_value": true
  }
]
```

| Field | |
|---|---|
| `property` | the id of another item in the **same group** |
| `operator` | `equal`, `not_equal`, `greater_than`, `greater_than_or_equal`, `less_than`, `less_than_or_equal`, `one_of` |
| value | supplied in the slot matching the **target item's** data type |

Rules the API enforces: the property must name an item that exists in the same form group, and it
may not be the item's own id. For every operator except `one_of`, a value must be supplied in the target
item's own type slot — a condition on a `boolean` item needs `boolean_value`, one on a
`string_array` item needs `string_array_values`.

Several conditions on one item all have to hold.

Conditional visibility is what keeps a shared tenant-level form readable. A portal user group form
that serves three routing dimensions can show each one's fields only when that dimension is
switched on, rather than presenting all of them to every administrator.

It is **presentation only**. A hidden field still holds whatever value it had, and a script still
reads it. Do not use it as a way of turning a setting off — clear the value instead.

## A complete example

A portal user group form for a routing scheme with three dimensions: regions, a security
classification, and an early-access flag.

```json
{
  "name": [
    { "culture": "x-default", "value": "Portal user group settings" },
    { "culture": "de", "value": "Portal-Benutzergruppen-Einstellungen" }
  ],
  "description": [
    { "culture": "x-default", "value": "Content routing settings applied to the members of this group." }
  ],
  "custom_form_groups": [
    {
      "id": "contentRouting",
      "name": [{ "culture": "x-default", "value": "Content routing" }],
      "description": [
        { "culture": "x-default", "value": "Determines which assets members of this group can see and download." }
      ],
      "form_items": [
        {
          "id": "regions",
          "name": [{ "culture": "x-default", "value": "Regions" }],
          "description": [
            { "culture": "x-default", "value": "Members see assets released for any of these regions. Leave empty to grant no regions." }
          ],
          "data_type": "string_array",
          "visibility": "basic",
          "sort_order": 10,
          "column_count_desktop": 6,
          "column_count_mobile": 12,
          "allowed_values": [
            { "string_value": "emea", "name": [{ "culture": "x-default", "value": "Europe, Middle East and Africa" }] },
            { "string_value": "amer", "name": [{ "culture": "x-default", "value": "Americas" }] },
            { "string_value": "apac", "name": [{ "culture": "x-default", "value": "Asia Pacific" }] }
          ]
        },
        {
          "id": "securityClassification",
          "name": [{ "culture": "x-default", "value": "Highest security classification" }],
          "data_type": "string",
          "visibility": "basic",
          "is_required": true,
          "sort_order": 20,
          "column_count_desktop": 6,
          "column_count_mobile": 12,
          "default_string_value": "notRestricted",
          "allowed_values": [
            { "string_value": "notRestricted", "name": [{ "culture": "x-default", "value": "Not restricted" }] },
            { "string_value": "internal", "name": [{ "culture": "x-default", "value": "Internal" }] },
            { "string_value": "confidential", "name": [{ "culture": "x-default", "value": "Confidential" }] }
          ]
        },
        {
          "id": "earlyAccess",
          "name": [
            { "culture": "x-default", "value": "Members may see assets before their go-live date" }
          ],
          "data_type": "boolean",
          "visibility": "advanced",
          "sort_order": 30,
          "line_feed_before": true,
          "default_boolean_value": false
        }
      ]
    }
  ]
}
```

The corresponding routing script reads `regions`, `securityClassification` and `earlyAccess` —
and nothing else. Keep the form and the script in step: a field nothing reads is a field an
administrator will fill in and expect to matter.

## Managing a form through the API

| Operation | Endpoint |
|---|---|
| List forms | `GET /customForms` |
| Create a form | `POST /customForms` |
| Read a form | `GET /customForms/{customFormUuid}` |
| Update a form | `PUT /customForms/{customFormUuid}?version=<version>` |
| Delete a form | `DELETE /customForms/{customFormUuid}?version=<version>` |
| Read a form's groups | `GET /customForm/{customFormUuid}/formGroups` |

Creating a form returns its UUID. Creating it does not attach it to anything: until it is selected
in a configuration or assigned in the tenant settings, the form does nothing. See
[Attachment points](#user-content-attachment-points).

`version` is optimistic locking. Pass the version you read; if the form has changed since, the
request is rejected rather than overwriting somebody else's edit. Forms are versioned, and values
recorded against an older version keep working — a form's history is retained so that a value set
under version 3 can still be interpreted after the form moves to version 4.

`is_standard` marks forms provided by Smint.io. Leave those alone.

A form that is no longer wanted can be marked `disabled` rather than deleted, which stops it being
offered without discarding the values already recorded against it.

## Attaching a form to the tenant

This applies to the [tenant-wide attachment points](#user-content-attached-tenant-wide) only. A
form selected in a specific configuration needs no tenant setting.

A tenant-wide form takes effect only once its UUID is assigned in the tenant settings. Each
attachment point has one field on the tenant:

| Attachment point | Tenant field |
|---|---|
| Frontend user group | `frontend_user_group_custom_form_uuid` |
| Data adapter configuration | `data_adapter_configuration_custom_form_uuid` |
| Assets search page configuration | `assets_search_page_configuration_custom_form_uuid` |
| Resource | `resource_custom_form_uuids` — an array of `{ "resource_type", "custom_form_uuid" }` |

Unlike a configuration's **Custom form** setting, a tenant-wide assignment currently has no
administrative user interface. It is made by updating the tenant through the backend API:

```
PUT /tenants/{tenantUuid}?version=<version>
```

```json
{
  "frontend_user_group_custom_form_uuid": "<customFormUuid>"
}
```

Only the fields you send change. Sending `null` removes the assignment. The form must already
exist in the same tenant — an unknown UUID is rejected. The caller must be a backend user in the
tenant's system backend user group.

**Until the form is assigned, it is invisible, and values sent for it are lost.** For a frontend
user group, specifically:

- the user group editor in the Portals administration shows no custom form panel — the panel is
  rendered only when the backend returns the form's groups with the user group, and it does that
  only for an assigned form;
- `custom_form_field_values` sent when creating or updating a user group are ignored without an
  error — the request succeeds and nothing is stored;
- every `getUser…CustomFormFieldValues` accessor in a routing script returns `null`.

So the order is always: create the form, assign it to the tenant, then fill in values. Values
provisioned before the assignment have to be sent again.

## Filling in values

Values live on the object, as `custom_form_field_values` — an array in which each entry names the
item's `id`, repeats its `data_type`, and carries the value in the matching slot:

```json
"custom_form_field_values": [
  {
    "id": "regions",
    "data_type": "string_array",
    "string_array_values": ["emea", "apac"]
  },
  {
    "id": "securityClassification",
    "data_type": "string",
    "string_value": "internal"
  },
  {
    "id": "earlyAccess",
    "data_type": "boolean",
    "boolean_value": true
  }
]
```

For a tenant-wide form, administrators normally do this through the Portals administration, where
the form renders as a panel on the object. A form attached to a registration, request or upload
configuration is filled in by the portal user in that flow. The API shape matters when values are provisioned programmatically — from an
identity provider's claims, or from a nightly synchronisation with the system of record for
entitlements.

## How values reach a script

For the full accessor list see
[the script reference](../DynamicContentRouting/docs/smintio-dcr-script-reference.md#user-content-reading-custom-form-values).
Three properties of the plumbing belong here, because they constrain how a form should be
designed.

**User values are merged across the user's groups.** A user in three portal user groups gets the
union of what those three groups carry for each item, de-duplicated. Every user-scope read
therefore returns an **array**, even for a `string` or `boolean` item — a `string` item read from
a user in two groups returns both groups' strings.

This makes group membership additive, which is what administrators expect: adding somebody to a
second group can only widen what they see. Design fields so that more values means more access. A
field whose semantics are "the maximum this user may see" does not compose — two groups produce
two maxima and the script has to decide which wins, which is a rule nobody will remember.

**Data adapter and page values are not merged.** One configuration, one set of values, read as a
single scalar or a single array.

**User group values are visible to the user.** The values on a user's portal user groups are sent
to that user's browser as part of the portal context. They are not secret. The enforcement that
protects content is server-side and unaffected, but it means a custom form field is not a place
for a credential, an internal note, or anything whose disclosure would matter.

## Things that are easy to get wrong

- **Creating a tenant-wide form but not assigning it.** A form for portal user groups, pages, data
  adapters or resources does nothing until its UUID is set on the tenant. No panel appears in the
  administration, and values sent through the API are dropped without an error.
- **Expecting per-group fields.** One form per tenant per attachment point. Groups differ in their
  values, never in their fields.
- **Reading a user value as a scalar.** It is always an array, merged across the user's groups,
  and it is `null` when there are no values at all.
- **Using a data type the API rejects.** Only the ten listed above are accepted in a custom form;
  the rest of the enumeration belongs to component configuration.
- **Putting the array type on an allowed value.** An allowed value for a `string_array` item
  carries `string_value`.
- **Free text where a routing script will match.** Use `allowed_values`; a typo in a free-text
  field fails silently.
- **Matching on labels instead of keys.** The value is what a script compares — put the stable key
  in the value slot and the readable text in `name`.
- **Renaming an id.** The id is the contract. Renaming it silently stops every script and
  component that reads it; add a new item and migrate instead.
- **Treating `visible_ifs` as a switch.** Hiding a field does not clear it, and a script still
  reads what is there.
- **Storing anything confidential in a user group field.** It reaches the user's browser.
- **Updating without `version`.** Optimistic locking is what stops two administrators overwriting
  each other.

## Questions

Please do not hesitate to contact us at [support@smint.io](mailto:support@smint.io) if you run
into any issues.

Contributors
============

- Reinhard Holzner, Smint.io GmbH
