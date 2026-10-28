# Set up SharePoint and OneDrive

The connectors `sharepoint` and `onedrive` read files from Microsoft SharePoint and OneDrive through the Microsoft Graph API. They share one form, and the person signs in at Microsoft before the connector can read anything.

## Find the connector

Both connectors are advanced, so `capabilities_search` with kind `connector` lists them only with `includeAdvanced` true. Read the fields with `form_schema_get` with kind `connector` and the connector key. Their setup method is `redirect`, so `connector_configuration_create` also needs a `redirectUrl`.

## Standard mode or high security mode

The field High security mode decides who owns the Azure app registration that the sign in uses.

| | Standard mode | High security mode |
|---|---|---|
| Azure app registration | The one of Smint.io, nothing to register | The customer's own, in their Azure tenant |
| Sign in at | Any Microsoft tenant | Only the tenant of the Azure tenant ID |
| Fields to fill | Your SharePoint URL | Your SharePoint URL, Azure tenant ID, Azure client ID, Azure client secret |
| Site, drive, list, folders | Picked from lists after the sign in | The site ID is typed in, drive, list and folders are picked after the sign in |
| Permissions asked at sign in | Read all sites, search all sites | Read all sites only |
| Who should use it | Most customers | Experts, when the customer's IT requires its own app |

Leave high security mode off unless the person says that their IT requires it. Do not guess the Azure values, ask the person or their IT administrator. `connector_configuration_setup_task_create` emails an IT administrator who can do the setup, name the recipient and wait for a clear yes first.

## What the customer's IT prepares for high security mode

- An app registration in their Azure tenant, with a client secret.
- The delegated permissions `Sites.Read.All` of Microsoft Graph and `AllSites.Read` of SharePoint, and `offline_access`, so the connector can renew its sign in.
- The redirect address of Smint.io as a web redirect URI of the app. It is the `redirect_uri` parameter of the sign in address that `connector_configuration_create` returns in `signInUrl`, or that `connector_configuration_redirect_url_get` returns.
- Their Azure tenant ID, client ID and client secret, which go into the form. The secret shows a mask when it is read back, sending the mask back keeps the stored secret.

## Order of work

1. Create the connector configuration with `connector_configuration_create`, with Your SharePoint URL, the URL of the SharePoint instance as the browser shows it after login. Path and query are removed.
2. Open the `signInUrl` for the person exactly as it is. The person signs in at Microsoft and agrees to the permissions.
3. Finish with `connector_configuration_setup` when the sign in did not finish by itself. The setup tests the connection by reading the sites. When that fails, the error says that the SharePoint endpoint test failed. Check the URL, the permissions and, in high security mode, the Azure values.
4. Pick the site, drive, list and folders. In standard mode use `form_allowed_values_search` on the site field, then on the drive, list and folder fields, which show only once a site is set. In high security mode type the site ID, then pick the rest the same way. Save them with `connector_configuration_update`, with the version of the last answer.
5. The setup creates the standard data adapter `assets`. Find it with `connector_data_adapter_configurations_list` and ask the person whether to enable it on the current portal with `portal_data_adapters_set`.

## What the fields mean

| Field | Meaning |
|---|---|
| SharePoint site | The site whose files the connector reads |
| SharePoint drive | The document library of the site whose folder structure is read |
| SharePoint list | The list whose column definitions become the metadata of the files |
| SharePoint folders to sync | The folders to read. At least one is needed, the connector reads nothing while it is empty, and the root folder reads the whole drive |

The site, drive, list and folder fields are advanced, so `form_schema_get` lists them only for an existing connector configuration.

## Read more

- [SharePoint connector](https://github.com/smintio/Portals-Development/blob/main/Backend/Connectors/Connector-SharePoint/README.md)
- [From a source attribute to a portal search filter](search-pipeline.md)
