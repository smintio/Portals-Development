# Shared component configurations

A component configuration can sit on several pages, so changing its values changes every page that uses it.

## The concept

A page configuration holds slots, and each slot holds component configurations. One component configuration can be placed on more than one page configuration. It is then shared.

Each entry of `component_configurations_list` carries `page_configuration_count`, the number of pages that use the component configuration. A count above one means the component configuration is shared.

## What happens when you change a shared component

A `values` operation of `page_edit_add` changes the component configuration itself. When the component configuration is used on more than one page, the change shows on all of them.

`page_edit_add` keeps such a change. It does not refuse it. The result carries a note that names the number of pages and points to the copy route described below.

## What an agent must do first

1. Call `component_configurations_list` with the `portalUuid` and `includeAdvanced` true, so that an advanced component configuration is not missed.
2. Read `page_configuration_count` of each component configuration that is to change.
3. If the count is more than one, tell the person that the change shows on all of these pages.
4. Ask whether the change is for all pages or for this page only, and wait for the answer.
5. Only then collect the change with `page_edit_add`.

## Change all pages

Send a `values` operation with the `componentConfigurationUuid` and the `values`. Read the note in the result and check that it matches what the person approved.

## Change this page only

Add a copy and remove the shared component configuration in the same `page_edit_add` call.

- The `add` operation takes `slotId`, a `name` and `copyComponentConfigurationUuid`. The copy has its own values, so the other pages stay as they are.
- A `remove` operation with the `componentConfigurationUuid` of the shared one takes it off this page.
- The source must be a saved component of this page. Read the uuids with `page_configuration_get`.
- A copy whose source an earlier operation removes is refused, so send the copy before the remove.
- An operation takes either `componentConfigurationUuid` or `copyComponentConfigurationUuid`, not both.

### What the copy takes over

| Taken over | Left out |
|---|---|
| The component type and the component | Fields that point to shared text resources |
| The values of the source, except the left out fields | |
| The description, unless the operation sends its own | |
| Texts typed into the component itself | |

A value that the same operation sends for a field replaces the copied one. The result carries a note that gives the number of copied values and names the left out fields. If the copy needs them, set them with a `values` operation.

## Removing and deleting

Removing a component from a page does not delete its component configuration. After `page_edit_save_live`, the result names the removed components and notes that they keep their component configurations.

`component_configuration_delete` deletes a component configuration for good. It is refused while any page still uses it, so take it off those pages first with a `remove` in `page_edit_add` and `page_edit_save_live`. Read the version from `component_configurations_list`, and read it again after a component was taken off a page and put back. Tell the person what is deleted, by name and uuid, and wait for a clear yes.

## Find the components that show the assets of a data source

`component_configurations_list` has no filter by data source. List the asset overviews with the component type `ui-type-assets-preview`, then read each with `form_schema_get`. Its `selectedValues` of an asset field name the assets the field holds, with their scoped ids, which start with the data source.

## Read more

- [Frontend UI components reference](https://github.com/smintio/Portals-Development/blob/main/Frontend/Legacy/docs/smintio-ui-components.md)
- [Frontend page templates reference](https://github.com/smintio/Portals-Development/blob/main/Frontend/Legacy/docs/smintio-page-templates.md)
- [Portal templates and resources](https://github.com/smintio/Portals-Development/blob/main/Backend/docs/smintio-portal-templates-and-resources.md)
