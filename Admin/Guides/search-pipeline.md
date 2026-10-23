# From a source attribute to a portal search filter

An attribute of a source system becomes a filter in the portal search only after two steps, it is indexed in the data adapter configuration, and the search page allows it as a search fragment.

## The chain

| Step | What it holds | Backend MCP tools |
|---|---|---|
| 1. Connector configuration | The connection to the source system | `connector_configuration_get`, `connector_configuration_update` |
| 2. Data adapter configuration | The data source, where attributes are indexed for search | `data_adapter_configuration_get`, `data_adapter_configuration_update`, `data_adapter_configuration_create` |
| 3. Search index | Filled from the indexed attributes | no tool of its own |
| 4. Page configuration | The search page with its facet based search form | `page_configuration_get`, `page_edit_add`, `page_edit_save_draft`, `page_edit_save_live` |

Use `form_schema_get` to read the fields of a data adapter or of a component, and read the help text of each field first.

## Steps 2 and 3, index the attribute

- A data adapter configuration belongs to a connector configuration. Find it with `connector_data_adapter_configurations_list` or `data_adapter_configurations_list`.
- The list of indexed attributes is the field `SmintIoIntegrationLayerSearchIndexMetadataAttributeList` of the data adapter configuration. Check the field ids of your data adapter with `form_schema_get` with kind `data_adapter`.
- Each entry is a metadata attribute, see the table below. Only an attribute with `index_type` `fragment` or `default` can become a filter. With `full_text` it is found by the full text search only.
- A data source has no setting called search fragments. Search fragments belong to the search page.

## Step 4, allow the search fragment on the search page

The facet based search form of the search page has two fields.

- `formItemAllowList` holds the search fragments the user may search for.
- `formItemDenyList` holds search fragments the user may not search for.

An empty `formItemAllowList` allows every search fragment. A fragment in `formItemDenyList` is hidden unless `formItemAllowList` names it.

`form_allowed_values_search` on either field, with the `portalUuid`, lists the search fragments of the portal with the label the admin UI shows. `form_schema_get` names the fragments a field holds in `selectedValues`.

The search page reads one data source. The attribute must be indexed in that data source, not in another one.

## If the filter is missing

1. Read the search page with `page_configuration_get` and check which data source it reads.
2. Check that this data adapter configuration indexes the attribute, with `data_adapter_configuration_get`.
3. Check `formItemAllowList` and `formItemDenyList` of the facet based search form.
4. Change the form with `page_edit_add`, then save with `page_edit_save_draft` or `page_edit_save_live`.

## Options of an indexed attribute

| Property | Allowed values or meaning |
|---|---|
| `json_path_selector` | The JSON path selector of the attribute |
| `labels` | The localized strings |
| `display_type` | `default` shows a date without time and a decimal number with 2 digits. `date_short`, `date_medium`, `date_long` show a date, `date_time_short`, `date_time_medium`, `date_time_long` a date and time, and the three lengths look the same. `number_no_digits` shows a number without decimals. On a date attribute, `date_min` offers a "From" field in the search filter, `date_max` an "Until" field, `date_min_max` both. `chip_always` and `chip_never` have no effect, leave them out |
| `column_count_desktop`, `column_count_mobile` | The width of the attribute in the metadata view of the asset detail page, in columns of a 12 column grid, 1 to 12. Mobile falls back to desktop, and 12 is the default |
| `index_type` | `fragment` indexes the value as a search fragment, so the search page can offer it as a filter. `default` is treated as `fragment`. `full_text` indexes the value for full text search only, it does not become a filter. `none` does not index the value |
| `exclude_from_global_full_text_index` | Removes the content of the field completely from the global full text index |
| `include_to_autocompletion_index` | Includes the content of the field in the autocompletion index |
| `include_values`, `exclude_values` | The values to include, the values to exclude |
| `include_empty_values` | Includes empty values in the field selection |
| `number_of_values_to_display` | The number of filter values shown before the rest is offered on request, 15 by default |
| `default_sort` | The sort direction when this attribute sorts the results. `desc` sorts descending. `default` and `asc` sort ascending |
| `values_sort` | The order of the filter values. `count_desc` and `default` put the values with most assets first, `count_asc` those with fewest. `natural_asc` and `natural_desc` sort by name. Date range filters keep their range order |
| `is_default_sort_metadata_attribute` | Marks the default sort attribute |
| `is_query_default_sort_metadata_attribute` | Marks the default sort attribute when a query string is given |
| `split_values` | Splits values by common delimiters |
| `clean_space_equivalents` | Replaces space equivalents by spaces |
| `search_substrings` | Searches substrings, not only edge-ngrams |

## After a change

A change to a data adapter configuration can start an index run. The server then raises the version of the data adapter configuration by itself, so read the version again after a version conflict. An index run takes a while. Tell the person that the filter shows after it, and if it is still missing later, go through the steps of "If the filter is missing".

## Read more

- [Data source setup](https://documentation.smint.io/en/other-topics-data-source/)
- [Connector setup](https://documentation.smint.io/en/other-topics-connector/)
- [Frontend data adapter reference](https://github.com/smintio/Portals-Development/blob/main/Frontend/Legacy/docs/smintio-data-adapter-reference.md)
- [Asset data model](https://github.com/smintio/Portals-Development/blob/main/Backend/docs/smintio-asset-data-model.md)
