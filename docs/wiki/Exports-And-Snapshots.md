# Exports And Snapshots

## Export CSV

Use `File > Export CSV...` to export:

- The current search results
- The current song
- The full library, depending on the current view

CSV export lets you:

- Choose which fields to include
- Include numbered Track Notes separately from Song Notes
- Include a numbered list of Disabled Tracks
- Save your preferred export field set for later use
- Include newer sections such as:
  - Groups
  - Mixer Main
  - Mixer Inserts
  - Mixer Sends

Selected mixer data is written to a companion `-mixer-details.csv` file so the main catalog remains one row per song.

## Export Excel Workbook

Use `File > Export Excel Workbook...` to create one `.xlsx` file containing:

- A `Catalog` worksheet with one row per song
- A normalized `Mixer Details` worksheet with one row per mixer item
- Links from song names in `Catalog` to their first matching row in `Mixer Details`

Both worksheets include frozen headers and column filters.

## Save Snapshot

Use `File > Save Snapshot...` or the `Snapshot` button above the detail tabs.

When SongLens View contains more than one song, choose:

- The selected song, which defaults to Detail View
- Every song in the current folder or search result, which defaults to List View

For a multi-song snapshot:

- List View captures the visible SongLens View columns, their display order, the current sort order, and every visible row.
- Detail View creates the full report for every song, with a clear separator between songs.

List View goes directly to the save dialog. Detail View opens Snapshot Preview first so you can choose the report sections and review the result.

Detailed snapshots use your current Track View Options by default. Clear `Use Current Track View Options` to choose snapshot-only settings for:

- With Events
- Hide Disabled
- Hide Folders

These overrides affect only the snapshot output. They do not change the Track View Options dropdown or your saved preferences.

Snapshots can be saved as:

- Text
- JSON

Snapshot Preview lets you review the result before saving.

Snapshots can include sections such as:

- Summary
- Attributes
- Tracks
- Groups
- Mixer
- Notes

Tab visibility is separate from snapshot selection. Hiding a tab does not automatically remove that section from snapshots.
