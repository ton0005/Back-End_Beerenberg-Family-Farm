# FarmManagement API Contract

This document provides a concise API contract for the Shifts endpoints so frontend developers and testers can exercise and verify the backend behavior.

Base URL: https://{host}/api/shifts

Authentication: JWT bearer token required for all endpoints. The `Admin` role is required where noted.

Date/time formats:
- Dates: `YYYY-MM-DD` (ISO 8601 date)
- Times (time of day): `HH:mm:ss` (e.g. `08:30:00`)
- Date-times: full ISO 8601 with timezone (use UTC where possible), e.g. `2025-10-02T09:00:00Z`

---

Quick start & common flows (frontend & tester friendly)
----------------------------------------------------

This section explains the common user flows and the exact endpoints the front-end and testers should call. Think of these as recipes you can copy-paste.

1) Authentication / Role quick reference
  - All endpoints require a Bearer JWT in the Authorization header unless noted otherwise (e.g. `POST /timeentries/clock` allows anonymous kiosk entries).
  - Admin-only endpoints require the token to include the `Admin` role claim; front-end should show admin-only UI controls only when the user has that role.

2) Clock-in / Clock-out (typical staff flow)
  - Endpoint: POST /api/timeentries/clock
  - Auth: Authenticated staff (or Admin to clock for any staff). Kiosk-mode allows anonymous but still requires `staffNumber` in the body.
  - Request body: minimal `TimeEntryDto` fields are accepted. The server will populate `createdAt`, `modifiedBy` and other metadata.
  - Example (staff clock-in):
    ```bash
    curl -X POST "https://api.example.com/api/timeentries/clock" \
      -H "Authorization: Bearer <JWT>" \
      -H "Content-Type: application/json" \
      -d '{ "staffNumber": "S12345", "stationId": 1, "entryTypeId": 1 }'
    ```
  - Result: 200 OK with created `TimeEntryDto`.

3) Admin manual edit of a time entry (audit recorded)
  - Endpoint: PUT /api/timeentries/{entryId}/manual
  - Auth: Admin only
  - Required: `modifiedReason` in body. Server sets `modifiedBy` to the current admin's staffNumber (auditing depends on this).
  - Example:
    ```bash
    curl -X PUT "https://api.example.com/api/timeentries/123/manual" \
      -H "Authorization: Bearer <ADMIN_JWT>" \
      -H "Content-Type: application/json" \
      -d '{ "entryTimestamp": "2025-10-08T09:00:00Z", "modifiedReason": "Fix clock - corrected to approved time" }'
    ```
  - Result: 200 OK with updated `TimeEntryDto`. An `AuditLog` is created with `ActionType = ManualEdit` and a `changesJson` describing the edits.

4) Front-end: show list of entries and lazy-load audits on toggle (recommended pattern)
  - Use `GET /api/timeentries/query` to retrieve the list (paged). Display entries in a table/list.
  - For Admin users, add a small "Show audits" toggle per entry.
  - When the user toggles open, call: `GET /api/timeentries/{entryId}/audits?fullChanges=false` (default `fullChanges=false` returns a preview of `changesJson`).
  - If the user wants to view the full audit payload, call the same endpoint with `fullChanges=true`.

5) Admin: fetch audits for multiple entries at once
  - If your UI expands many rows at once or prefetches, use the batch audit endpoint:
    - GET /api/audit?tableName=TimeEntries&recordIds=123,456
    - This returns a `PagedResult<AuditDto>` where `items` contains audits for the requested records.

6) Exception create & resolve flow
  - Staff creates an exception: POST /api/timeentries/exceptions (authenticated staff or Admin)
  - Admin resolves: POST /api/timeentries/exceptions/{id}/resolve (Admin only). This creates an AuditLog with `ActionType = Resolve`.

7) Testing tips (quick)
  - Use a real Admin JWT when testing Admin flows. If you don't have one, the test harness should provide a token generator.
  - Use small `pageSize` for initial tests (10–20) to keep responses fast.
  - For audit visibility tests: create a manual edit, then call `GET /api/timeentries/{entryId}/audits` to verify the audit appears with the expected `performedBy` and `changesJson`.

---

---

## Endpoints

### 1) GET /types
- Description: Get shift type lookup (Admin only)
- Method: GET
- Auth: Admin
- Query: none
- Response: 200 OK
  - Body: array of ShiftTypeDto

Example ShiftTypeDto:
{
  "shiftTypeId": 1,
  "name": "Morning",
  "defaultStartTime": "08:00:00",
  "defaultEndTime": "12:00:00",
  "description": "Morning shift"
}

---

### 2) GET /
- Description: List shifts with paging and filters (Admin only)
- Method: GET
- Auth: Admin
- Query parameters:
  - page (int, default 1)
  - pageSize (int, default 20)
  - startDate (YYYY-MM-DD)
  - endDate (YYYY-MM-DD)
  - shiftTypeId (int)
  - search (string)
  - sortBy (string)
  - sortDesc (bool)
  - onlyPublished (bool)
- Response: 200 OK
  - Body: PagedResult<ShiftDto>

Example PagedResult wrapper:
{
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "items": [ /* array of ShiftDto */ ]
}

Example ShiftDto:
{
  "shiftId": 123,
  "shiftTypeId": 1,
  "date": "2025-10-02",
  "startTime": "08:00:00",
  "endTime": "12:00:00",
  "break": 30,
  "note": "Barn cleanup",
  "isPublished": true
}

---

### 3) POST /
- Description: Create a new shift (Admin only)
- Method: POST
- Auth: Admin
- Body: ShiftCreateRequest (JSON)
- Responses:
  - 201 Created: returns created ShiftDto and Location header pointing to `/api/shifts/{id}`
  - 400 Bad Request: validation or template errors

Example (template-based):
POST /api/shifts
Authorization: Bearer <ADMIN_JWT>

Body:
{
  "shiftTypeName": "Morning",
  "date": "2025-10-10",
  "note": "Barn cleanup",
  "isPublished": true
}

Example (custom):
{
  "shiftTypeName": "Custom",
  "date": "2025-10-10",
  "startTime": "14:00:00",
  "endTime": "18:30:00",
  "note": "Special harvest",
  "isPublished": false
}

Response 201 Location: /api/shifts/456
Response Body: ShiftDto (see above)

---

### 4) GET /{id}
- Description: Get a shift by id.
  - Admin: can view any shift and receives assignments in the `assignments` property.
  - Non-admin authenticated user: only allowed if the user has an assignment for this shift.
- Method: GET
- Auth: Authenticated (Admin or User)
- Response:
  - 200 OK: ShiftView (Admin includes Assignments) or ShiftDto for normal users
  - 403 Forbidden: if user not allowed to view
  - 404 Not Found: if shift id doesn't exist

Example ShiftView (admin):
{
  "shiftId": 123,
  "shiftTypeId": 1,
  "date": "2025-10-02",
  "startTime": "08:00:00",
  "endTime": "12:00:00",
  "break": 30,
  "note": "Barn cleanup",
  "isPublished": true,
  "assignments": [
    {
      "shiftAssignmentId": 1001,
      "shiftId": 123,
      "staffNumber": "S12345",
      "firstName": "Alice",
      "lastName": "Farmer",
      "roleName": "Worker",
      "status": "Assigned",
      "assignedAt": "2025-09-25T09:00:00Z",
      "completedAt": null
    }
  ]
}

---

### 5) POST /{id}/assign
- Description: Assign one or more staff members to an existing shift (Admin only).
- Method: POST
- Auth: Admin
- Body: ShiftAssignRequest (supports single `staffNumber` or `staffNumbers` array)
- Response: 200 OK with array of per-staff results:
  - Ok with assignmentId on success
  - NotFound if staff doesn't exist
  - Conflict if overlap prevents assignment
  - Error with message on exception

Example request (single):
{
  "staffNumber": "S12345",
  "status": "Assigned",
  "assignedAt": "2025-09-25T09:00:00Z"
}

Example request (multiple):
{
  "staffNumbers": ["S12345","S67890"],
  "status": "Assigned",
  "assignedAt": "2025-09-25T09:00:00Z"
}

Example response:
[
  { "staffNumber": "S12345", "status": "Ok", "assignmentId": 1001 },
  { "staffNumber": "S67890", "status": "Conflict", "message": "Assignment overlaps..." }
]

---

### 6) GET /assignments/{staffNumber}
- Description: Admin-only listing of assignments for a staff number (includes resolved shift details)
- Method: GET
- Auth: Admin
- Response: 200 OK array of ShiftAssignmentView

Example ShiftAssignmentView:
{
  "shiftAssignmentId": 1001,
  "shiftId": 123,
  "staffNumber": "S12345",
  "firstName": "Alice",
  "lastName": "Farmer",
  "roleName": "Worker",
  "shiftTypeName": "Morning",
  "date": "2025-10-02",
  "startTime": "08:00:00",
  "endTime": "12:00:00",
  "status": "Assigned",
  "assignedAt": "2025-09-25T09:00:00Z",
  "completedAt": null
}

---

### 7) PUT /{id}
- Description: Update an existing shift (Admin only). If `staffNumbers` provided, assignments are reconciled.
- Method: PUT
- Auth: Admin
- Body: ShiftUpdateRequest (same shape as create but optional staffNumbers)
- Response:
  - 200 OK: updated ShiftDto (may include assignments if provided)
  - 400 Bad Request: validation/template errors
  - 404 Not Found: if shift doesn't exist

Example body:
{
  "shiftTypeName": "FullDay",
  "date": "2025-10-02",
  "note": "Changed to fullday",
  "isPublished": true,
  "staffNumbers": ["S12345","S67890"]
}

---

### 8) DELETE /{id}
- Description: Delete a shift (Admin only)
- Method: DELETE
- Auth: Admin
- Response:
  - 200 OK: { message: "Deleted" }
  - 404 Not Found

---

### 9) DELETE /assignments/{assignmentId}
- Description: (NEW) Delete a single assignment by its id (Admin only). This removes the assignment so the staff is no longer assigned to that shift.
- Method: DELETE
- Auth: Admin
- Route: /api/shifts/assignments/{assignmentId}
- Response:
  - 200 OK: { message: "Assignment deleted" }
  - 404 Not Found: { message: "Assignment not found" }

Example:
DELETE /api/shifts/assignments/1001
Authorization: Bearer <ADMIN_JWT>

Response 200:
{ "message": "Assignment deleted" }

---

---

## TimeEntries API

Base URL: https://{host}/api/timeentries

Auth: JWT required. Most endpoints require the caller to be the staff (matching StaffNumber claim) or an Admin. Certain actions (manual edits, bypassing shift validation, resolve exceptions, query) are Admin-only.

Date/time formats follow the top-level definitions.

Common DTO: TimeEntryDto
{
  "entryId": 0,
  "staffNumber": "S12345",
  "stationId": 1,
  "shiftAssignmentId": 1001,        // optional
  "bypassShiftValidation": false,   // optional; Admin-only when true
  "bypassReason": null,             // optional; if provided when bypassing, it's recorded in audit
  "entryTypeId": 1,
  "entryTimestamp": "2025-10-08T08:00:00Z",
  "breakReason": null,
  "geoLocation": null,
  "isManual": false,
  "status": "Open",
  "createdAt": "2025-10-08T08:00:00Z",
  "modifiedAt": null,
  "modifiedBy": null,               // set by server: current user's staffNumber (Admin for manual edits)
  "modifiedReason": null            // required for manual edits; optional elsewhere
}

### Seeded lookup values (EntryType and ExceptionType)

These values are seeded into the database on startup (see `src/.../Seed`). Frontend can rely on these stable names to map user actions to entryTypeId/typeName.

EntryType (seeded names):
- 1: CLOCK_IN
- 2: CLOCK_OUT
- 3: BREAK_START
- 4: BREAK_END

ExceptionType (seeded names):
- 1: MISSING_CLOCK_IN — Staff forgot to clock in
- 2: MISSING_CLOCK_OUT — Staff forgot to clock out
- 3: ADJUST_REQUEST — Staff requests adjustment to an existing entry
- 4: INCORRECT_STATION — Entry recorded at incorrect station
- 5: OTHER — Other / miscellaneous exception

Example lookup responses:

GET /api/entrytypes
Response 200:
[
  { "entryTypeId": 1, "typeName": "CLOCK_IN" },
  { "entryTypeId": 2, "typeName": "CLOCK_OUT" },
  { "entryTypeId": 3, "typeName": "BREAK_START" },
  { "entryTypeId": 4, "typeName": "BREAK_END" }
]

GET /api/exceptiontypes
Response 200:
[
  { "typeId": 1, "typeName": "MISSING_CLOCK_IN", "description": "Staff forgot to clock in" },
  { "typeId": 2, "typeName": "MISSING_CLOCK_OUT", "description": "Staff forgot to clock out" },
  { "typeId": 3, "typeName": "ADJUST_REQUEST", "description": "Staff requests adjustment to an existing entry" },
  { "typeId": 4, "typeName": "INCORRECT_STATION", "description": "Entry recorded at incorrect station" },
  { "typeId": 5, "typeName": "OTHER", "description": "Other / miscellaneous exception" }
]

Example request/response for creating an exception (frontend-friendly):

POST /api/timeentries/exceptions
Request body:
{
  "staffNumber": "S12345",
  "exceptionDate": "2025-10-08",
  "typeId": 1,
  "description": "Forgot to clock in this morning"
}

Response 200:
{
  "exceptionId": 201,
  "staffNumber": "S12345",
  "exceptionDate": "2025-10-08",
  "typeId": 1,
  "description": "Forgot to clock in this morning",
  "status": "Open",
  "resolutionNotes": null,
  "resolvedBy": null,
  "createdAt": "2025-10-08T10:00:00Z",
  "resolvedAt": null
}

Example request/response for creating a time entry (clock-in):

POST /api/timeentries/clock
Request body (minimal):
{
  "staffNumber": "S12345",
  "stationId": 1,
  "entryTypeId": 1
}

Response 200 (created TimeEntryDto):
{
  "entryId": 1001,
  "staffNumber": "S12345",
  "stationId": 1,
  "shiftAssignmentId": null,
  "bypassShiftValidation": false,
  "entryTypeId": 1,
  "entryTimestamp": "2025-10-08T08:00:00Z",
  "breakReason": null,
  "geoLocation": null,
  "isManual": false,
  "status": "Open",
  "createdAt": "2025-10-08T08:00:00Z",
  "modifiedAt": null,
  "modifiedBy": null,
  "modifiedReason": null
}


### POST /clock
- Description: Create a clock entry (clock-in / clock-out). Frontend should supply EntryTypeId for specific events (e.g., ClockIn, ClockOut, BreakStart, BreakEnd).
- Method: POST
- Auth: Authenticated (staff may clock only for themselves; Admin may clock for any staff)
- Body: TimeEntryDto (staffNumber required)
- Special: If `bypassShiftValidation == true` the server will skip the requirement that the staff has a shift assignment for the entry date. Only Admins may set bypass; controller will reject non-admin attempts.
- Responses:
  - 200 OK: TimeEntryDto (created)
  - 400 Bad Request: missing staffNumber or invalid payload
  - 403 Forbidden: staff trying to act for another staff or non-admin trying to bypass

Example:
```bash
curl -X POST "https://api.example.com/api/timeentries/clock" \
  -H "Authorization: Bearer <JWT>" \
  -H "Content-Type: application/json" \
  -d '{ "staffNumber": "S12345", "stationId": 1, "entryTypeId": 1 }'
```

If Admin needs to bypass shift validation:
```bash
curl -X POST "https://api.example.com/api/timeentries/clock" \
  -H "Authorization: Bearer <ADMIN_JWT>" \
  -H "Content-Type: application/json" \
  -d '{ "staffNumber": "S12345", "stationId": 1, "entryTypeId": 1, "bypassShiftValidation": true, "bypassReason": "No published shift - emergency coverage" }'
```

When bypass is used, the system creates an AuditLog record with ActionType `BypassShiftValidation` and recorded `performedBy` set to the caller; if `bypassReason` is provided, it is included in the audit metadata.

### POST /start-break
- Alias for creating a Break start entry. Same auth rules as /clock.
- Body: TimeEntryDto (frontend should set entryTypeId to the configured Break_Start id)
- Responses: same as /clock

### POST /end-break
- Alias for creating a Break end entry. Same auth rules and behavior as /clock.

### PUT /{entryId}/manual
- Description: Admin-only manual edit of an existing time entry (adjust timestamp, geo, reason etc.).
- Method: PUT
- Auth: Admin
- Route: /api/timeentries/{entryId}/manual
- Body: TimeEntryDto
- Responses:
  - 200 OK: updated TimeEntryDto
  - 400 Bad Request: if `modifiedReason` is missing/empty, or the server cannot resolve the current admin's staff number for auditing
  - 404 Not Found: if entry not found

Example:
```bash
curl -X PUT "https://api.example.com/api/timeentries/123/manual" \
  -H "Authorization: Bearer <ADMIN_JWT>" \
  -H "Content-Type: application/json" \
  -d '{ "entryTimestamp": "2025-10-08T09:00:00Z", "modifiedReason": "Fix clock - corrected to approved time" }'
```

Notes:
- `modifiedReason` is REQUIRED for manual edits.
- `modifiedBy` is ignored if provided by clients; the server populates it with the current admin's staffNumber.
- If `modifiedAt` is not supplied, the server sets it to the current UTC time.

### POST /exceptions
- Description: Staff may create an exception (e.g., forgot to clock). Admins may create for any staff.
- Method: POST
- Auth: Authenticated
- Body: ExceptionDto (see below)
- Response: 200 OK: created ExceptionDto

ExceptionDto shape (example):
{
  "exceptionId": 0,
  "staffNumber": "S12345",
  "exceptionDate": "2025-10-08",
  "typeId": 2,
  "description": "Forgot to clock in",
  "status": "Open",
  "resolutionNotes": null,
  "resolvedBy": null,
  "createdAt": "2025-10-08T10:00:00Z",
  "resolvedAt": null
}

### GET /staff/{staffNumber}/exceptions/{date}
- Description: Get exceptions for a staff on a specific date. Staff can view only their own records; admin can view any.
- Method: GET
- Auth: Authenticated
- Response: 200 OK: array of ExceptionDto

### POST /exceptions/{id}/resolve
- Description: Admin-only endpoint to mark an exception resolved. Creates an AuditLog entry for the resolution.
- Method: POST
- Auth: Admin
- Route: /api/timeentries/exceptions/{id}/resolve
- Body: { "resolvedBy": "admin1", "resolutionNotes": "Approved" }
- Responses:
  - 200 OK: resolved ExceptionDto
  - 404 Not Found: if exception id not found

### GET /staff/{staffNumber}
- Description: Admin or the staff themself may retrieve all time entries for a staff.
- Method: GET
- Auth: Authenticated
- Response: 200 OK: array of TimeEntryDto

### GET /staff/{staffNumber}/today
- Description: Get today's entries for a staff (based on UTC date). Staff or Admin may call.
- Method: GET
- Auth: Authenticated
- Response: 200 OK: array of TimeEntryDto

### GET /staff/{staffNumber}/sessions
- Description: Summarize a staff's session(s) by day in the given date or date range. A session contains the first `clockIn`, optional first `breakStart`/`breakEnd`, and first `clockOut` for each day.
- Method: GET
- Auth: Authenticated (Staff can only view their own sessions; Admin can view any staff)
- Query parameters:
  - date (YYYY-MM-DD) — single day; if omitted, you can provide a range via `startDate`/`endDate`
  - startDate (YYYY-MM-DD) — start of date range (inclusive)
  - endDate (YYYY-MM-DD) — end of date range (inclusive)
  - If no date params are provided, defaults to today (UTC)
- Response: 200 OK: array of StaffSessionDto (one or more objects per date in the requested window when multiple sessions exist)

Notes:
- `breaks` lists all break intervals within a session. The legacy `breakStart`/`breakEnd` fields mirror the first break interval for backward compatibility.
- `totalBreakMinutes` sums closed break intervals only (open-ended breaks are excluded until ended).
- `workedMinutes` is `(clockOut - clockIn) - totalBreakMinutes`; if `clockOut` is missing (session in progress), `workedMinutes` is null.

Example response item (with a break):
{
  "staffNumber": "S12345",
  "firstName": "Alice",
  "lastName": "Farmer",
  "date": "2025-10-12",
  "clockIn": "2025-10-12T08:00:00Z",
  "breakStart": "2025-10-12T12:00:00Z",
  "breakEnd": "2025-10-12T12:30:00Z",
  "clockOut": "2025-10-12T17:00:00Z",
  "breaks": [
    { "start": "2025-10-12T12:00:00Z", "end": "2025-10-12T12:30:00Z" }
  ],
  "totalBreakMinutes": 30,
  "workedMinutes": 450
}

Example response item (no break):
{
  "staffNumber": "S67890",
  "firstName": "Bob",
  "lastName": "Harvester",
  "date": "2025-10-12",
  "clockIn": "2025-10-12T09:00:00Z",
  "breakStart": null,
  "breakEnd": null,
  "clockOut": "2025-10-12T15:00:00Z",
  "breaks": [],
  "totalBreakMinutes": 0,
  "workedMinutes": 360
}

Example response item (in-progress: no clockOut, open break):
{
  "staffNumber": "S24680",
  "firstName": "Charlie",
  "lastName": "Picker",
  "date": "2025-10-12",
  "clockIn": "2025-10-12T10:00:00Z",
  "breakStart": "2025-10-12T12:15:00Z",
  "breakEnd": null,
  "clockOut": null,
  "breaks": [
    { "start": "2025-10-12T12:15:00Z", "end": null }
  ],
  "totalBreakMinutes": 0,
  "workedMinutes": null
}

### GET /query
- Description: Admin-only query across time entries with optional filters. Supports server-side paging to avoid large responses.
- Method: GET
- Auth: Admin
- Query parameters:
  - staffNumber (optional)
  - entryTypeId (optional)
  - start (optional) ISO 8601 datetime
  - end (optional) ISO 8601 datetime
  - page (int, default 1)
  - pageSize (int, default 20)
- Response: 200 OK: PagedResult<TimeEntryDto> (matching filters)

### GET /sessions
- Description: Admin-only endpoint to list session summaries for all staff over a date or date range. Returns a paged result.
- Method: GET
- Auth: Admin
- Query parameters:
  - date (YYYY-MM-DD) — single day; if omitted, provide `startDate`/`endDate`
  - startDate (YYYY-MM-DD)
  - endDate (YYYY-MM-DD)
  - page (int, default 1)
  - pageSize (int, default 50)
- Response: 200 OK — PagedResult<StaffSessionDto>

Example response (paged):
{
  "page": 1,
  "pageSize": 50,
  "totalCount": 123,
  "items": [
    {
      "staffNumber": "S12345",
      "firstName": "Alice",
      "lastName": "Farmer",
      "date": "2025-10-12",
      "clockIn": "2025-10-12T08:00:00Z",
      "breakStart": "2025-10-12T12:00:00Z",
      "breakEnd": "2025-10-12T12:30:00Z",
      "clockOut": "2025-10-12T17:00:00Z",
      "breaks": [
        { "start": "2025-10-12T12:00:00Z", "end": "2025-10-12T12:30:00Z" },
        { "start": "2025-10-12T14:30:00Z", "end": "2025-10-12T14:40:00Z" }
      ],
      "totalBreakMinutes": 40,
      "workedMinutes": 440
    }
  ]
}

### PUT /staff/{staffNumber}/sessions/{date}/manual
- Description: Admin-only. Edit an entire session (clock in/out + multiple breaks) for a staff member on a date in a single, atomic operation. Creates, updates, and deletes underlying time entries as needed.
- Method: PUT
- Auth: Admin
- Route: /api/timeentries/staff/{staffNumber}/sessions/{date}/manual
- Path parameters:
  - staffNumber (string)
  - date (YYYY-MM-DD)
- Request body (ManualSessionEditRequest):
{
  "clockIn": "2025-10-11T07:55:00Z",
  "clockOut": "2025-10-11T16:10:00Z",
  "breaks": [
    { "start": "2025-10-11T12:05:00Z", "end": "2025-10-11T12:35:00Z" },
    { "start": "2025-10-11T14:50:00Z", "end": "2025-10-11T14:58:00Z" }
  ],
  "stationId": 1,
  "shiftAssignmentId": 123,
  "isManual": true,
  "bypassShiftValidation": false,
  "bypassReason": null,
  "modifiedReason": "Admin corrected shift and breaks per supervisor note",
  "status": "Open"
}

- Response: 200 OK — StaffSessionDto for the date (includes breaks, totalBreakMinutes, workedMinutes). If `clockOut` or last break end are missing, derived fields are computed accordingly (open intervals excluded from totalBreakMinutes).

Notes:
- stationId is required if the operation needs to create new entries and cannot infer stationId from existing entries.
- Breaks must not overlap and must fall within [clockIn, clockOut] when both are provided.
- Each edit is audited as a single ManualSessionEdit with details of created/updated/deleted entries.

### Audit behavior
- When Admin sets `bypassShiftValidation=true` the system records an AuditLog with ActionType `BypassShiftValidation` and details including whether a ShiftAssignmentId was provided and the `performedBy` (the admin staff number).
- When an exception is resolved the system records an AuditLog with ActionType `Resolve` on the ExceptionLogs table.
- When an Admin performs a manual edit (`PUT /timeentries/{entryId}/manual`), the system records an AuditLog with ActionType `ManualEdit` on the TimeEntries table, including a structured list of changed fields and metadata: `TimeEntryId`, `StaffNumber`, `ShiftAssignmentId`, `EditedAt`, `EditedBy` (admin staffNumber), and the `Reason` (modifiedReason).

### Assignment completion behaviour (automatic)
- When a staff clocks out (a `TimeEntry` created with the `CLOCK_OUT` EntryType) and the `TimeEntry` includes a `shiftAssignmentId`, the system will:
  - set the corresponding `ShiftAssignment.CompletedAt` to the `entryTimestamp` value from the clock-out TimeEntry,
  - set the assignment `status` to `Completed`.
- If a `ShiftAssignment.CompletedAt` value is later cleared (set to null) and the assignment's current status is `Completed`, the system will revert the assignment `status` back to `Assigned`. The repository logic will not override an explicit `Removed` status.

### NOTE: query-with-audits removed

The `GET /query-with-audits` endpoint has been removed to avoid returning large, duplicated payloads.
Preferred approaches for retrieving audits are:

- Per-entry audits (recommended for the UI toggle):
  - GET /api/timeentries/{entryId}/audits
  - Auth: Admin
  - Response: 200 OK — array of AuditDto

- Batch audit fetch (useful when expanding many entries at once):
  - GET /api/audit?tableName=TimeEntries&recordIds=123,456
  - Auth: Admin
  - Response: 200 OK — PagedResult<AuditDto>

Both approaches avoid returning unnecessarily large combined payloads and give the frontend control over when to fetch audit details.

## Audit API (new)

Expose audit logs directly for Admin users. This endpoint is useful for audit screens, debugging, and correlating requests.

Base URL: https://{host}/api/audit

Auth: JWT required. Role: Admin only.

Query parameters (all optional except paging defaults):
- tableName (string) — filter by table name, e.g. `TimeEntries` or `ExceptionLogs`
- recordIds (CSV) — comma-separated ints, e.g. `recordIds=123,456` (optional)
- correlationId (string) — match the request correlation id header
- actionType (string) — e.g. `ManualEdit`, `BypassShiftValidation`, `Resolve`
- performedBy (string) — admin staffNumber who performed the action
- from (ISO 8601 datetime) — start of performedAt range (inclusive)
- to (ISO 8601 datetime) — end of performedAt range (inclusive)
- page (int, default 1)
- pageSize (int, default 20)
- fullChanges (bool, default false) — when true, return the full `changesJson`; when false, response contains a preview (first 200 chars)

Response: 200 OK — PagedResult<AuditDto>

AuditDto (returned fields):
{
  "auditId": 10,
  "tableName": "TimeEntries",
  "recordId": 123,
  "actionType": "BypassShiftValidation",
  "changesJson": "{...}",        // preview unless fullChanges=true
  "performedBy": "admin1",
  "performedAt": "2025-10-08T09:00:00Z"
}

Example: fetch audits for multiple time entries (preview changes)

```bash
curl -X GET "https://api.example.com/api/audit?tableName=TimeEntries&recordIds=123,456&page=1&pageSize=50" \
  -H "Authorization: Bearer <ADMIN_JWT>" \
  -H "Accept: application/json"
```

Example response (paged):

{
  "page": 1,
  "pageSize": 50,
  "totalCount": 3,
  "items": [
    {
      "auditId": 101,
      "tableName": "TimeEntries",
      "recordId": 123,
      "actionType": "ManualEdit",
      "changesJson": "{\"modifiedBy\":\"admin1\",\"modifiedReason\":\"Fix time\"}",
      "performedBy": "admin1",
      "performedAt": "2025-10-08T09:00:00Z"
    }
  ]
}

Front-end friendly JavaScript example (fetch with fullChanges):

```javascript
const url = new URL('/api/audit', window.location.origin);
url.searchParams.set('tableName', 'TimeEntries');
url.searchParams.set('recordIds', '123,456');
url.searchParams.set('page', '1');
url.searchParams.set('pageSize', '50');
url.searchParams.set('fullChanges', 'true');

const res = await fetch(url.toString(), {
  headers: { 'Authorization': `Bearer ${adminJwt}`, 'Accept': 'application/json' }
});
if (!res.ok) throw new Error(await res.text());
const data = await res.json();
// data.items -> array of AuditDto
```

Notes:
- `fullChanges=true` should be used sparingly (may return large payloads). Default is preview to keep list performance good.
- The endpoint returns `changesJson` as a string. If you need it parsed to JSON on the client, run `JSON.parse(audit.changesJson)` after checking that the string is present.
- Use `correlationId` to correlate UI actions with server logs when debugging multi-request flows.


## Common DTOs (example shapes)

ShiftDto
{
  "shiftId": 123,
  "shiftTypeId": 1,
  "date": "2025-10-02",
  "startTime": "08:00:00",
  "endTime": "12:00:00",
  "break": 30,
  "note": "Barn cleanup",
  "isPublished": true
}

ShiftAssignmentDto / ShiftAssignmentView
{
  "shiftAssignmentId": 1001,
  "shiftId": 123,
  "staffId": 55,
  "staffNumber": "S12345",
  "firstName": "Alice",
  "lastName": "Farmer",
  "roleName": "Worker",
  "status": "Assigned",
  "assignedAt": "2025-09-25T09:00:00Z",
  "completedAt": null
}

PagedResult<T>
{
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "items": [ /* T */ ]
}

StaffSessionDto
{
  "staffNumber": "S12345",
  "firstName": "Alice",
  "lastName": "Farmer",
  "date": "2025-10-12",
  "clockIn": "2025-10-12T08:00:00Z",
  "breakStart": "2025-10-12T12:00:00Z",
  "breakEnd": "2025-10-12T12:30:00Z",
  "clockOut": "2025-10-12T17:00:00Z",
  "breaks": [
    { "start": "2025-10-12T12:00:00Z", "end": "2025-10-12T12:30:00Z" },
    { "start": "2025-10-12T14:30:00Z", "end": "2025-10-12T14:40:00Z" }
  ],
  "totalBreakMinutes": 40,
  "workedMinutes": 440
}

---

## Curl examples

Note: Replace `https://api.example.com` and `<ADMIN_JWT>` with your host and token.

1) Delete assignment

```bash
curl -X DELETE "https://api.example.com/api/shifts/assignments/1001" \
  -H "Authorization: Bearer <ADMIN_JWT>" \
  -H "Accept: application/json"
```

2) Assign staff to shift (single):

```bash
curl -X POST "https://api.example.com/api/shifts/123/assign" \
  -H "Authorization: Bearer <ADMIN_JWT>" \
  -H "Content-Type: application/json" \
  -d '{ "staffNumber": "S12345", "status": "Assigned", "assignedAt": "2025-09-25T09:00:00Z" }'
```

3) Create shift (custom):

```bash
curl -X POST "https://api.example.com/api/shifts" \
  -H "Authorization: Bearer <ADMIN_JWT>" \
  -H "Content-Type: application/json" \
  -d '{ "shiftTypeName": "Custom", "date": "2025-10-10", "startTime": "14:00:00", "endTime": "18:30:00", "note": "Special harvest", "isPublished": false }'
```

---

## Testing tips
- Use an Admin JWT for Admin endpoints. If you don't have a token, create one via your auth tooling or test harness.
- Ensure times are in proper format and `assignedAt` datetimes use UTC.
- When assigning multiple staff, the response array contains per-staff result objects — inspect for `status: "Ok"` and `assignmentId`.
- Deleting an assignment is immediate; clients may want to refresh assignment lists after a delete.

---

## Payroll API

Base URL: https://{host}/api/payroll

Auth: JWT bearer token required. All payroll endpoints require `Admin` role (controller is authorized with [Authorize(Roles = "Admin")]).

Date formats: follow top-level definitions. Pay calendar dates use `YYYY-MM-DD`.

Summary: The payroll endpoints manage pay calendars, generate payroll runs, view payroll history, and query configured pay rates. Key endpoints below include sample requests and expected responses.

---

Quick start — how to use the Payroll feature
------------------------------------------------

Follow these steps as an Admin to create a pay period, generate payroll and review results. This is the minimal flow front-end teams and testers should implement or automate.

1) Create a pay calendar (define the pay period)
  - Endpoint: POST /api/payroll/calendar
  - Body: { "startPeriodDate": "YYYY-MM-DD", "payDate": "YYYY-MM-DD" }
  - Note: The system sets EndPeriodDate = StartPeriodDate + 13 days for fortnightly calendars. PayDate must be after EndPeriodDate.

2) Wait for the pay period to complete (EndPeriodDate has passed)
  - The payroll generator requires the pay period to be completed before a payroll run can be finalised. You may still create a draft run (the API will reject if rules are violated).

3) Generate (or regenerate) payroll for the calendar
  - Endpoint: POST /api/payroll/create
  - Body: { "payCalendarId": <id> }
  - Result: returns a PayrollRunDto containing totals and LineItems for each staff member.
  - If a payroll already exists for the period, calling POST /create will recalculate the run.

4) Review the payroll run and resolve issues
  - Endpoint: GET /api/payroll/run/{payrollRunId}
  - Check LineItems for missing rates, unexpected hours, or anomalies.
  - Common fixes: add/adjust pay rates (GET/PUT via rates endpoints if available in admin UI), fix staff contract data, correct time entries.

5) View payroll history or calendar-scoped runs
  - All runs: GET /api/payroll/history
  - Calendar runs: GET /api/payroll/history/calendar/{calendarId}

6) Verify pay rates before generating payroll
  - Endpoint: GET /api/payroll/rates
  - Ensure contract types and effective rates exist for the period; missing or inactive rates can cause line item issues.

Quick example (happy-path):
- POST /api/payroll/calendar -> returns payCalendarId = 1
- POST /api/payroll/create { "payCalendarId": 1 } -> returns payrollRunId = 101
- GET /api/payroll/run/101 -> review line items and totals

Tips & common errors
- Auth: all payroll endpoints require Admin role and a valid Bearer JWT.
- Date format: use `YYYY-MM-DD` for calendar dates.
- 400 Bad Request: returned when pay period not completed, calendar overlaps, pay date invalid, or calendar not found during create.
- Regenerate: calling POST /create again recalculates the run for the same calendar.


### 1) POST /calendar
- Description: Create a new pay calendar (fortnightly). End period date is auto-generated as StartPeriodDate + 13 days. PayDate must be after EndPeriodDate.
- Method: POST
- Auth: Admin
- Body: CreatePayCalendarRequest
- Responses:
  - 200 OK: returns created PayCalendarDto
  - 400 Bad Request: validation error (overlap, invalid dates, pay date before end period)

Sample request:
POST /api/payroll/calendar
Authorization: Bearer <ADMIN_JWT>

Body:
{
  "startPeriodDate": "2025-10-20",
  "payDate": "2025-11-05"
}

Sample successful response 200 (PayCalendarDto):
{
  "payCalendarId": 1,
  "startPeriodDate": "2025-10-20",
  "endPeriodDate": "2025-11-02",
  "payDate": "2025-11-05",
  "payFrequency": "Fortnightly",
  "status": "Active",
  "isPayrollGenerated": false,
  "createdAt": "2025-10-20T00:00:00Z",
  "createdBy": "admin_user"
}

---

### 2) GET /calendar
- Description: Get all pay calendars ordered by start date (newest first).
- Method: GET
- Auth: Admin
- Query: none
- Response: 200 OK - array of PayCalendarListResponse

Sample request:
GET /api/payroll/calendar
Authorization: Bearer <ADMIN_JWT>

Sample response 200:
[
  {
    "payCalendarId": 2,
    "startPeriodDate": "2025-11-03",
    "endPeriodDate": "2025-11-16",
    "payDate": "2025-11-19",
    "payFrequency": "Fortnightly",
    "status": "Active",
    "isPayrollGenerated": false
  },
  {
    "payCalendarId": 1,
    "startPeriodDate": "2025-10-20",
    "endPeriodDate": "2025-11-02",
    "payDate": "2025-11-05",
    "payFrequency": "Fortnightly",
    "status": "Active",
    "isPayrollGenerated": true
  }
]

---

### 3) GET /calendar/{id}
- Description: Get a specific pay calendar by id.
- Method: GET
- Auth: Admin
- Response:
  - 200 OK: PayCalendarDto
  - 404 Not Found: if not exists

Sample response 200:
{
  "payCalendarId": 1,
  "startPeriodDate": "2025-10-20",
  "endPeriodDate": "2025-11-02",
  "payDate": "2025-11-05",
  "payFrequency": "Fortnightly",
  "status": "Active",
  "isPayrollGenerated": true,
  "createdAt": "2025-10-20T00:00:00Z",
  "createdBy": "admin_user"
}

---

### 4) POST /create
- Description: Create (or regenerate) a payroll run for the specified pay calendar. If a payroll already exists for the period it will be recalculated.
- Method: POST
- Auth: Admin
- Body: CreatePayrollRequest
- Responses:
  - 200 OK: PayrollRunDto (detailed payroll with line items)
  - 400 Bad Request: pay period not completed or invalid calendar

Sample request:
POST /api/payroll/create
Authorization: Bearer <ADMIN_JWT>

Body:
{
  "payCalendarId": 1
}

Sample response 200 (PayrollRunDto):
{
  "payrollRunId": 101,
  "payCalendarId": 1,
  "totalLabourCost": 2543.75,
  "totalWorkHours": 312.5,
  "staffCount": 12,
  "status": "Draft",
  "runNumber": 1,
  "createdAt": "2025-11-03T12:00:00Z",
  "createdBy": "admin_user",
  "approvedAt": null,
  "approvedBy": null,
  "startPeriodDate": "2025-10-20",
  "endPeriodDate": "2025-11-02",
  "payDate": "2025-11-05",
  "lineItems": [ /* array of PayrollLineItemDto */ ]
}

Note: `lineItems` details are returned as `PayrollLineItemDto` objects (see application DTOs) and include per-staff hours, rates and line totals.

---

### 5) GET /run/{id}
- Description: Get detailed payroll run by id (includes line items).
- Method: GET
- Auth: Admin
- Responses:
  - 200 OK: PayrollRunDto
  - 404 Not Found: if not exists

Sample response 200: same shape as the POST /create response above.

---

### 6) GET /history
- Description: Get all payroll runs (summary list).
- Method: GET
- Auth: Admin
- Response: 200 OK - array of PayrollSummaryDto. If none exist, returns message with empty data array.

Sample response 200:
[
  {
    "payrollRunId": 101,
    "payCalendarId": 1,
    "payCalendarPeriod": "2025-10-20 - 2025-11-02",
    "totalLabourCost": 2543.75,
    "totalWorkHours": 312.5,
    "staffCount": 12,
    "status": "Draft",
    "createdAt": "2025-11-03T12:00:00Z"
  }
]

---

### 7) GET /history/calendar/{calendarId}
- Description: Get payroll runs for a specific pay calendar id (summary list).
- Method: GET
- Auth: Admin
- Response: 200 OK - array of PayrollSummaryDto

Sample response 200: same as /history but scoped to the calendarId.

---

### 8) GET /rates
- Description: Get all configured pay rates (current and historical).
- Method: GET
- Auth: Admin
- Response: 200 OK - array of PayRateDto

Sample response 200:
[
  {
    "payRateId": 1,
    "contractType": "FullTime",
    "rateType": "Regular",
    "hourlyRate": 25.00,
    "effectiveFrom": "2025-01-01",
    "effectiveTo": null,
    "isActive": true,
    "description": "Standard full-time regular rate"
  },
  {
    "payRateId": 2,
    "contractType": "Casual",
    "rateType": "Regular",
    "hourlyRate": 28.50,
    "effectiveFrom": "2025-06-01",
    "effectiveTo": null,
    "isActive": true,
    "description": "Casual regular hourly"
  }
]

---

DTO examples (from application DTOs):

PayCalendarDto
{
  "payCalendarId": 1,
  "startPeriodDate": "2025-10-20",
  "endPeriodDate": "2025-11-02",
  "payDate": "2025-11-05",
  "payFrequency": "Fortnightly",
  "status": "Active",
  "isPayrollGenerated": false,
  "createdAt": "2025-10-20T00:00:00Z",
  "createdBy": "admin_user"
}

CreatePayCalendarRequest
{
  "startPeriodDate": "2025-10-20",
  "payDate": "2025-11-05"
}

CreatePayrollRequest
{
  "payCalendarId": 1
}

PayrollRunDto (summary fields shown)
{
  "payrollRunId": 101,
  "payCalendarId": 1,
  "totalLabourCost": 2543.75,
  "totalWorkHours": 312.5,
  "staffCount": 12,
  "status": "Draft",
  "runNumber": 1,
  "createdAt": "2025-11-03T12:00:00Z",
  "createdBy": "admin_user",
  "startPeriodDate": "2025-10-20",
  "endPeriodDate": "2025-11-02",
  "payDate": "2025-11-05",
  "lineItems": [ /* PayrollLineItemDto objects */ ]
}

PayRateDto
{
  "payRateId": 1,
  "contractType": "FullTime",
  "rateType": "Regular",
  "hourlyRate": 25.00,
  "effectiveFrom": "2025-01-01",
  "effectiveTo": null,
  "isActive": true,
  "description": "Standard full-time regular rate"
}

---

