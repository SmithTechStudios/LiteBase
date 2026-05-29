# Row Edit Modal — Design

## Overview

Add in-place row editing to the LiteBase UI via a modal dialog. Rows are identified by SQLite's built-in `rowid`. Changes are persisted via a new `PUT` endpoint.

---

## Backend

### 1. Update `GET /litebase/table/{tableName}`

Change `SELECT * FROM {table}` to `SELECT rowid, * FROM {table}` so every row response includes `rowid`.

### 2. New `PUT /litebase/table/{tableName}/{rowid}`

- Reads a JSON body of `{ columnName: value, ... }`
- Excludes `rowid` from the SET clause
- Builds: `UPDATE {table} SET col1=@col1, col2=@col2 ... WHERE rowid=@rowid`
- Executes via `connection.ExecuteAsync` (Dapper)
- Returns `200 OK` on success, `400` if body is empty/invalid

Table name gets the same `.ToLower()` treatment as existing endpoints.

---

## Frontend

### Data flow

1. `GET /litebase/table/{tableName}` now returns `rowid` on each row
2. Row click → open Dialog with form pre-populated from row data
3. User edits → Save → `PUT /litebase/table/{tableName}/{rowid}` with edited values (rowid excluded from body)
4. On success → `refetchTableData()` → modal closes

### Modal form

- PrimeVue `Dialog` component (already available, no new deps)
- One `InputText` per column, excluding `rowid`
- Column type drives input: `INTEGER`/`REAL` → `type="number"`, `TEXT` → `type="text"`, `BLOB` → disabled
- Save button shows loading state while request is in flight (prevents double-submit)
- Error message shown inline if PUT fails

### DataTable changes

- Add a click handler on each row to open the edit modal
- `rowid` column hidden from display (excluded from `selectedTableColumns` rendering)

---

## Files changed

| File | Change |
|------|--------|
| `LiteBase/LiteBase.cs` | Add `PUT` route handler; update `SELECT` to include `rowid` |
| `LiteBase/ui/src/App.vue` | Add edit modal, row click handler, PUT axios call |
