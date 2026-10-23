# Drafts and live

Page edits can be saved as a draft for review before they go live, but most other changes made through the backend MCP tools are live at once.

## The concept

A portal change is either a draft or live.

- A draft is a copy of the page change that only the PreviewUrl shows. The live page stays as it is.
- Live means visitors see the change as soon as the call returns.

Page edits are the only way to collect many changes to one page and review them as a draft. Everything else saves live, unless the tool takes a `previewUuid`.

## What is a draft and what is live

| Change | Tool | Result |
|---|---|---|
| Collect changes to the components of a page | `page_edit_add` | Kept as page edits, the page does not change |
| Read the collected changes | `page_edit_get` | Nothing changes, each component shows in `PendingValues` the values the save writes |
| Drop the collected changes | `page_edit_discard` | The page edits are gone for every session of the person, the page and a saved draft stay as they are |
| Save the collected changes for review | `page_edit_save_draft` | Draft, the live page is unchanged, returns a PreviewUrl |
| Save the collected changes | `page_edit_save_live` | Live, then the page edits are cleared |
| Change page settings | `page_settings_update` | Live, or a draft when you pass `previewUuid` |
| Create a page | `page_configuration_create` | Live, a page created with `disabled` true is hidden |
| Create or change a resource | `resource_create`, `resource_update` | Live at once, no draft |

Of the tools in this table, only `page_settings_update` takes a `previewUuid`. Its draft starts from the live page and holds only that one change.

## Resources have no draft

A resource is a shared item such as a text, an image or a video that a component uses by its id. `resource_create` and `resource_update` change the live portal on every page that uses the resource. A text typed into a component is part of the page edits, so it can be reviewed as a draft first. To change a text on one page only, use a text in the component.

## Known limits

- Page edits expire 12 hours after the last page edit call.
- Page edits belong to one person and one page, and every session of that person shares them. After a discard in another session, `page_edit_get` and both saves name the time of the discard.
- An empty value in a `values` operation clears the field at the save. An `add` leaves empty values out.
- A slot that falls below its minimum count is noted by `page_edit_add` and refused at the save, so add a component back first.
- `page_edit_save_draft` refuses page edits with more than one new component and keeps them. Discard the extra adds, or save live after the person agrees. A live save of several new components works.
- A page created or set to disabled is hidden and cannot be previewed, so its PreviewUrl does not open it. Make it visible with `page_settings_update` and `disabled` false, then save the draft again.
- Both saves need the current page `version`. A changed page is refused and the answer names the new version.
- The PreviewUrl holds a sign in token of the portal. Show it only to the person who reviews.
- A portal with no preview address returns no PreviewUrl, so the person previews the draft in the admin UI.
- A removed component keeps its component configuration after a live save, and the save names it.

## Live portals and maintenance mode

There is no publish step. A portal that is marked as a live portal, which `portal_get` shows as `isLivePortal` true, can only be edited while it is in maintenance mode. On such a portal the saves of page edits, `page_settings_update`, `resource_create` and `resource_update` all fail with a message to turn maintenance mode on first. This includes a draft save. Turn it on with `portal_settings_update` and `isInMaintenanceMode` true, and tell the person first, because the portal cannot be used while maintenance mode is on. Other portals need no maintenance mode.

## Safe order of work

1. Call `page_edit_get`, because page edits of an earlier session may exist. Its `PendingValues` show what the save writes.
2. Collect the changes with `page_edit_add`.
3. Call `page_edit_save_draft` with the current `version`.
4. Show the PreviewUrl to the person.
5. Wait for a clear yes.
6. Call `page_edit_save_live` with the same `version`.

To stop, call `page_edit_discard` after the person agrees to lose the page edits. Before any resource change, show the change and wait for a yes, because it is live at once.

## Read more

- [Create and edit pages](https://documentation.smint.io/en/editing-pages-create-&-edit-pages/)
- [Portal templates and resources](https://github.com/smintio/Portals-Development/blob/main/Backend/docs/smintio-portal-templates-and-resources.md)
