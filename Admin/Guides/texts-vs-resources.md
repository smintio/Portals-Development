# Texts versus resources

A text in a portal either belongs to one component configuration, where a draft and a preview protect it, or is a resource that many pages share, where every change is live at once.

## The two places a text can live

| | Text in a component configuration | Resource |
|---|---|---|
| What it is | A text field of a UI component | A string, text, image, video, menu item or other resource, referred to by its id |
| Who sees it | The one component | Every page and component that uses the resource |
| How you change it | Page edits with `page_edit_add` | `resource_create`, `resource_update`, `resource_delete` |
| Draft and preview | Yes, `page_edit_save_draft` gives a preview before `page_edit_save_live` | No draft step |
| Effect of a change | Live when you save live | Saved and live at once |

Only a text embedded directly in a component is part of the page edits, so only that text can be checked in a preview. A resource that a component uses by its id has no draft. `resource_create` and `resource_update` change the live portal on every page that uses the resource.

## How to tell which one a field holds

Read the field with `form_schema_get`. A resource field gives its `selectedValues`, each with a name and a description. Those values are the resources the field points at. A plain localized text field has no selected resource, it holds the text itself. An asset field also lists its assets in `selectedValues`, with their scoped ids and names. Those are assets of a data source, not resources.

A resource also has a type key such as `string`, `text` or `image`. Use `resources_list` with `filterByResourceType` to find resources of one type. Standard resources are left out unless you pass `excludeStandard` false.

## When to use which

- Use a text in the component when the text belongs to one page. This is the safe choice, because you can review it in a draft first.
- Use a resource when several components must show the same text, image or video, or when a component asks for one.
- Resources that are reused across a portal, such as the text of the login button, change rarely. Treat a change to them as a portal wide change.
- To change a shared text on one page only, do not edit the resource. Put a text into the component instead.

## Find the pages that use a resource

No tool lists the pages that use a resource. To name the pages, you can read the component configurations of the portal and compare their `selectedValues` with the resource. Use `component_configurations_list`, `page_configuration_component_configurations_list` and `form_schema_get`. If you cannot be sure you found every page, say so to the person.

## Before you change a resource

Follow these steps for `resource_update`, and for `resource_create` when a component already refers to the resource.

1. Read the resource with `resource_get` and note its `version`.
2. Tell the person that the change is live at once on every page and component that uses the resource.
3. Name the pages you found, and say when the list may be incomplete.
4. Show the exact change.
5. Wait for a clear yes.
6. Call `resource_update` with the `version`, so a change made by someone else is refused instead of lost.

`resource_delete` is destructive. Before you call it, tell the person which resource is deleted, by name and uuid, and wait for a clear yes.

## Cultures of a text

- A localized text always needs an `x-default` entry. Other cultures alone are refused.
- A text value replaces all cultures of that text, so send every culture you want to keep. `page_edit_add` and `page_edit_get` note a culture that a text leaves out.

## Read more

- [Portal templates and resources](https://github.com/smintio/Portals-Development/blob/main/Backend/docs/smintio-portal-templates-and-resources.md)
- [Custom forms](https://documentation.smint.io/en/custom-components-custom-forms/)
- [UI components](https://documentation.smint.io/en/ui-components/)
- [Create and edit pages](https://documentation.smint.io/en/editing-pages-create-&-edit-pages/)
