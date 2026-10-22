# Permissions model

How the backend MCP tools decide who may do what, and how an agent finds out why a call was refused.

## Two kinds of users

| Kind | Also called | Belongs to | Decides |
|---|---|---|---|
| Backend user | admin user | backend user groups | what the user may change in the admin area |
| Frontend user | portal visitor | frontend user groups | what the visitor sees in a portal |

The backend MCP tools act for a backend user. They never act for a visitor.

## Backend users and backend user groups

A backend user gets permission flags through the backend user groups the user belongs to. A flag is either global or tied to a portal. Examples are `create_portal` (global) and `update_page_configuration` (portal).

- A tool needs the flags the `permissions_list` tool names for it. A call without them is refused.
- A backend user can only work in a portal the user belongs to. Without membership the call is refused, unless the user holds `read_portals`.
- A few tools need no flag, only any backend user. `documents_list`, `feedback_submit`, `current_user_get` and `permissions_list` are examples.
- `permissions_list` also returns presets that mirror the roles of the admin UI, administrator, portal administrator and portal editor. Send their flags to `backend_user_group_update` to build a group.

### Special users

- **System administrators group.** A system backend user group holds every backend user group, frontend user group, connector, data adapter, data processor and identity provider permission. A user must be a member of a system group to change its members, which the flag `is_system_backend_user_group_member` describes. Only a developer admin can rename a system group or change its description. A system group must keep at least one member.
- **Developer admin.** A developer admin bypasses every flag and portal membership check.

## Frontend users and frontend user groups

A frontend user group is attached to one or more portals. Its settings decide what a visitor of that portal sees, for example the page and data source permissions. The flags that manage them include `read_frontend_user_groups`, `update_frontend_user_group` and `add_portal_frontend_user_group`.

## Why was my call refused

A refusal is permanent. It is not a temporary failure, so do not retry the call. The error says so, and the server instructions repeat it.

1. Find the tool in `permissions_list` and note the flag it needs.
2. Call `current_user_get` and read `backend_user_permissions`. The flags there are the union of the global flags and those of the portals of the user, so a flag can be true for one portal only. Pass a portal UUID to see the portal permissions.
3. If the flag is missing, ask a portal administrator to add it to a backend user group of the user. Do not try other tools to get around it.

## Tools that manage groups and members

| Area | Tools |
|---|---|
| Backend user groups | `backend_user_groups_list`, `backend_user_group_get`, `backend_user_group_update`, `backend_user_group_members_get`, `backend_user_group_members_set`, `backend_user_group_portals_list`, `backend_user_groups_export` |
| Backend users | `backend_users_list`, `backend_user_get`, `backend_user_backend_user_groups_list` |
| Frontend user groups | `frontend_user_groups_list`, `frontend_user_group_get`, `frontend_user_group_create`, `frontend_user_group_update`, `frontend_user_group_delete`, `frontend_user_group_members_get`, `frontend_user_group_members_set`, `frontend_user_group_portals_list` |
| Frontend users | `frontend_users_list`, `frontend_user_frontend_user_groups_list` |
| Group to portal | `portal_user_groups_set` (frontend), `portal_backend_user_groups_set` (backend), `portal_frontend_user_groups_list`, `portal_backend_user_groups_list` |
| Visitor permissions | `frontend_user_group_scenario_permissions_get` and `_set`, `frontend_user_group_data_adapter_permissions_get` and `_set` |

`frontend_user_group_update` needs the current version of the group. Read it with `frontend_user_group_get` first, a stale version is refused.

## Invites send a real email

`backend_user_invite` and `frontend_user_invite` can send an email to a third party. Use them only when the user names the email address and the group and gives a clear yes.

## Read more

- [User management introduction](https://documentation.smint.io/en/user-management-introduction/)
- [Admin users](https://documentation.smint.io/en/user-management-admin-users/)
- [Admin user permissions](https://documentation.smint.io/en/user-management-admin-users-permissions/)
- [Portal user permissions](https://documentation.smint.io/en/user-management-permissions/)
