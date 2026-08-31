# Work Tracker Pro (TrackerKerja) — User Guide

> **Version**: 3.1 (Advanced Export, Multi-Timer & Admin Controls Edition)  
> **Platform**: Web Application (ASP.NET Core 8.0)  
> **Last Updated**: August 27, 2026

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Getting Started](#2-getting-started)
   - [2.1 System Requirements](#21-system-requirements)
   - [2.2 Starting the Application](#22-starting-the-application)
   - [2.3 Login](#23-login)
   - [2.4 User Interface Overview](#24-user-interface-overview)
3. [Themes & Appearance](#3-themes--appearance)
4. [Dashboard](#4-dashboard)
5. [Task Management](#5-task-management)
   - [5.1 Task List View](#51-task-list-view)
   - [5.2 Creating a Task](#52-creating-a-task)
   - [5.3 Editing a Task](#53-editing-a-task)
   - [5.4 Task Progress & Status](#54-task-progress--status)
   - [5.5 Parent / Child Task (Sub-tasks)](#55-parent--child-task-sub-tasks)
   - [5.6 Bulk Actions (Delete & Export)](#56-bulk-actions-delete--export)
   - [5.7 Exporting Tasks to Excel (with Period Filter)](#57-exporting-tasks-to-excel-with-period-filter)
6. [Kanban Board](#6-kanban-board)
7. [Projects](#7-projects)
8. [Timesheet & Work Sessions](#8-timesheet--work-sessions)
   - [8.1 Recording Work Time](#81-recording-work-time)
   - [8.2 Exporting Personal Timesheet Report](#82-exporting-personal-timesheet-report)
   - [8.3 Multi-User Concurrent Timers](#83-multi-user-concurrent-timers)
9. [Notes & Documentation](#9-notes--documentation)
   - [9.1 Creating a Note](#91-creating-a-note)
   - [9.2 File Attachments](#92-file-attachments)
   - [9.3 Pinning Notes](#93-pinning-notes)
10. [Calendar](#10-calendar)
11. [Reports & Analytics](#11-reports--analytics)
12. [Members & Team Management](#12-members--team-management)
    - [12.1 Member Cards & Detail View](#121-member-cards--detail-view)
    - [12.2 Admin Functions](#122-admin-functions)
    - [12.3 Admin Password Reset](#123-admin-password-reset)
13. [Import & Export Excel](#13-import--export-excel)
    - [13.1 Importing Tasks from Excel](#131-importing-tasks-from-excel)
    - [13.2 Exporting Tasks to Excel](#132-exporting-tasks-to-excel)
14. [JSON Tools (Developer Utilities)](#14-json-tools-developer-utilities)
15. [Audit Trail](#15-audit-trail)
16. [Master Data Management](#16-master-data-management)
17. [System Configuration](#17-system-configuration)
18. [REST API & Swagger Documentation](#18-rest-api--swagger-documentation)
19. [Role-Based Access Control](#19-role-based-access-control)
20. [Mobile & Responsive Usage](#20-mobile--responsive-usage)
21. [User Account & Profile](#21-user-account--profile)
22. [Tips & Best Practices](#22-tips--best-practices)
23. [Troubleshooting](#23-troubleshooting)
24. [Appendix A - Data Model](#appendix-a--data-model-quick-reference)
25. [Appendix B - ARMS Export Format](#appendix-b--arms-export-format-21-columns)
26. [Appendix C - Export Period Filter Parameters](#appendix-c--export-period-filter-parameters)

---

## 1. Introduction

**Work Tracker Pro** (also known as **TrackerKerja**) is a comprehensive web-based work management platform designed for software engineering teams and project managers. It integrates task tracking, time recording, documentation, team analytics, and data exchange into a single unified application.

### Key Features at a Glance

| Feature | Description |
|---|---|
| Task Management | Create, edit, track tasks with full hierarchy (parent/child) support |
| Kanban Board | Visual drag-and-drop board with real-time status updates |
| Timesheet | Live timer + manual session recording; **concurrent multi-timer** per user |
| Notes | Rich-text documentation with multi-file attachments |
| Projects | Organize tasks by project with dynamic progress tracking |
| Reports | Charts, team workload analysis, Gantt chart |
| Calendar | Task timeline view with date range and project filters |
| Members | Team directory with individual contribution metrics; **admin password reset** |
| Import/Export | Excel import/export (Standard & ARMS 21-col) with **period & project filter** |
| JSON Tools | Format, minify, validate, and save JSON snippets |
| Audit Trail | Automatic activity logging for all system actions |
| Master Data | Admin-managed categories, priorities, statuses, milestones |
| REST API | Full OpenAPI/Swagger-documented REST interface (70+ endpoints) |
| 16 Themes | 10 light + 6 dark mode themes, switchable instantly |
| Responsive | Full mobile and tablet support with touch navigation |

---

## 2. Getting Started

### 2.1 System Requirements

| Requirement | Details |
|---|---|
| **Runtime** | .NET 8.0 SDK or Runtime |
| **Database** | SQLite (bundled – no installation required) |
| **Browser** | Chrome 110+, Firefox 110+, Edge 110+, Safari 16+ |
| **Screen** | Minimum 320px width (optimized for 1280px+) |
| **OS** | Windows 10/11, Linux, macOS |

### 2.2 Starting the Application

Run the application from command line:

```cmd
# Using the compiled executable (Windows)
TrackerKerja.exe --urls=http://localhost:5000

# Using .NET Runtime CLI
dotnet TrackerKerja.dll --urls=http://localhost:5000
```

Then open your browser and navigate to: **`http://localhost:5000`**

> **Tip**: To run on a network-accessible address, use `--urls=http://0.0.0.0:5000`

### 2.3 Login

![Login Page](C:\Users\WAHANA 24\.gemini\antigravity-ide\brain\7c763f71-399d-48c9-b9d2-f02976dda91e\00_login_1787812448510.jpg)

On the login screen:
1. Enter your registered **Email Address**
2. Enter your **Password** (click the eye icon to toggle visibility)
3. Optionally check **Remember Me** for a 7-day persistent session
4. Click **Masuk (Login)**

> **Note**: If you are a new user, ask your **Administrator** to create your account via the Members module.

### 2.4 User Interface Overview

After login, you will see the main application layout:

- **Left Sidebar** (Desktop): Fixed navigation with all module links
- **Topbar**: Application title, notification bell, theme switcher, and user profile dropdown
- **Main Content Area**: Module-specific content
- **Bottom Navigation Bar** (Mobile): Quick-access buttons for Dashboard, Tasks, Add Task, Projects, and Menu

---

## 3. Themes & Appearance

Work Tracker Pro offers **16 premium visual themes** — 10 light modes and 6 dark modes — switchable instantly from the topbar.

### Dark Modes (6 Themes)
| Theme | Primary Color | Description |
|---|---|---|
| **Midnight OLED** | #6366F1 + #38BDF8 | Deep black with Neon Indigo & Cyan |
| **Cyberpunk Synthwave** | #F43F5E + #06B6D4 | Obsidian with Fuchsia Neon & Electric Cyan |
| **Emerald Matrix** | #10B981 + #34D399 | Dark emerald with Hacker Matrix Green |
| **Dracula Eclipse** | #A855F7 + #EC4899 | Purple night with Pastel Pink |
| **Abyssal Ocean** | #38BDF8 + #3B82F6 | Deep ocean with Sapphire Blue |
| **Solar Ember** | #F97316 + #F59E0B | Lava charcoal with Flame Orange & Gold |

### Light Modes (10 Themes)
| Theme | Primary Color | Description |
|---|---|---|
| **Indigo Nebula** (Default) | #6366F1 + #8B5CF6 | Modern tech aesthetic |
| **Emerald Forest** | #10B981 + #0D9488 | Harmonious and cool |
| **Ocean Azure** | #0284C7 + #2563EB | Fresh and professional |
| **Sunset Crimson** | #F43F5E + #EA580C | Warm and energetic |
| **Cyberpunk Neon** | #D946EF + #06B6D4 | High contrast neon |
| **Royal Amethyst** | #9333EA + #6366F1 | Luxurious and premium |
| **Amber Gold** | #F59E0B + #EA580C | Classic gold |
| **Slate Minimalist** | #475569 + #334155 | Clean monochrome |
| **Nordic Teal** | #0D9488 + #0284C7 | Fresh teal |
| **Midnight Titanium** | #6366F1 + #38BDF8 | Bright titanium |

**How to switch themes**: Click the theme icon in the topbar, then select your preferred theme. The change applies instantly without page reload.

---

## 4. Dashboard

![Dashboard](C:\Users\WAHANA 24\.gemini\antigravity-ide\brain\7c763f71-399d-48c9-b9d2-f02976dda91e\01_dashboard_1787812427475.jpg)

The Dashboard (`/`) is your central control panel. It displays:

### Summary Cards
- **Total Tasks** – Total number of tasks in the system
- **In Progress** – Tasks currently being worked on
- **Completed** – Tasks marked as Done
- **Overdue** – Tasks past their due date

### Project Progress Bars
Visual progress bars per project showing aggregate task completion percentage.

### Task Activity Chart
A bar chart of task activity over the past 7 days (creation and update activity by day).

### Recent Tasks
The latest tasks ordered by update time, allowing quick navigation to active work.

---

## 5. Task Management

### 5.1 Task List View

![Task List](C:\Users\WAHANA 24\.gemini\antigravity-ide\brain\7c763f71-399d-48c9-b9d2-f02976dda91e\03_task_list_1787812474548.jpg)

Navigate to **Tasks** in the sidebar (`/Task`).

The task list supports:
- **Search** – Filter by title or description keywords
- **Filter by Status** – Todo, InProgress, Done, Overdue
- **Filter by Priority** – Low, Medium, High, Critical
- **Filter by Project** – Dropdown with Select2 search
- **Filter by Assignee** – Dropdown with Select2 search
- **Filter by Milestone** – SDLC phase filter

Each row displays:

| Column | Description |
|---|---|
| **Task Code** | Auto-generated code (e.g., TSK-0001) |
| **Title** | Task name |
| **Project** | Assigned project |
| **Assignee** | Avatar and name of the person responsible |
| **Priority** | Badge: Critical / High / Medium / Low |
| **Status** | Badge: Todo / In Progress / Done / Overdue |
| **Progress** | Visual progress bar (0-100%) |
| **Due Date** | Deadline date |
| **Actions** | Edit and Delete buttons |

### 5.2 Creating a Task

1. Click the **+ Add Task** button (top right)
2. Fill in the task form:

| Field | Required | Description |
|---|---|---|
| **Title** | Yes | Clear, descriptive task name |
| **Description** | Optional | Detailed description of the task |
| **Project** | Optional | Select from dropdown (Select2 searchable) |
| **Category** | Optional | Type of work (Backend, Frontend, etc.) |
| **Assignee (PIC)** | Optional | Person in charge (Select2 searchable) |
| **Parent Task** | Optional | Select parent for sub-tasks (Select2 searchable) |
| **Priority** | Yes | Low / Medium / High / Critical |
| **Status** | Yes | Todo / InProgress / Done / Overdue |
| **Progress** | Optional | 0-100% (slider or preset buttons) |
| **Milestone** | Optional | SDLC phase (Requirement Analysis, System Design, Implementation, Testing & QA, Deployment, Maintenance) |
| **Start Date** | Optional | Planned start date |
| **Due Date** | Optional | Deadline |
| **Obstacle** | Optional | Document blockers or issues |
| **Solution** | Optional | Document solutions found |
| **Tags** | Optional | Comma-separated tags |

3. Click **Save Task**

> **Tip**: Use the **Obstacle** and **Solution** fields to keep a running log of technical issues and their resolutions — excellent for knowledge management and retrospectives.

### 5.3 Editing a Task

1. Click the **Edit** button on any task row
2. Modify any field
3. Click **Update Task**

> **Access Control**: Only the task's assignee, System Analysts, Technical Writers, and Administrators can edit tasks. Regular users cannot edit tasks assigned to others.

### 5.4 Task Progress & Status

Tasks have synchronized progress and status:

- **Setting Status to `Done`** → Progress automatically locks to **100%**
- **Setting Progress to `100%`** → Status automatically changes to **Done**
- Use the **quick preset buttons** for fast progress updates: `0%`, `25%`, `50%`, `75%`, `100%`
- Or drag the **interactive slider** to set a custom percentage

### 5.5 Parent / Child Task (Sub-tasks)

Work Tracker Pro supports hierarchical task structures:

- When creating a task, set the **Parent Task** field (Select2 searchable dropdown)
- Child tasks are linked to their parent with a displayed parent code
- Each task shows:
  - **Task Code**: `TSK-0001`
  - **Parent Code**: Same as itself if it's a root task, or the parent's code if it's a sub-task
- A parent task shows "All Children Done" when all sub-tasks are completed

### 5.6 Bulk Actions (Delete & Export)

1. Check the **checkboxes** on the left of multiple task rows
2. A **Bulk Action Toolbar** appears at the top with these options:
   - **Delete Selected** - Removes the selected tasks (SweetAlert2 confirmation required)
   - **Export Selected (Standard)** - Opens the Export Modal to download selected rows as Standard Excel
   - **Export Selected (ARMS)** - Opens the Export Modal to download selected rows in ARMS 21-column format
3. The export modal lets you refine by **period** before downloading

> **Admin only**: The **Clear All Tasks** button removes all tasks from the system permanently.

---

### 5.7 Exporting Tasks to Excel (with Period Filter)

Work Tracker Pro supports exporting tasks directly from the **Task List** with flexible filtering:

#### How to Export
1. (Optional) Check rows you want — or leave unchecked to export **all filtered results**
2. Click **Export** in the Bulk Action Bar and choose the format:
   - **Standard Excel (.xlsx)** - 9-column import-compatible format
   - **ARMS Excel (.xlsx)** - Enterprise 21-column format
3. The **Export Modal** opens with filter options:

| Filter | Options |
|---|---|
| **Period Preset** | Today / Yesterday / Last 7 Days / Last 30 Days / This Month / Last Month |
| **Custom Date Range** | Select start and end dates manually |
| **Project** | Filter by a specific project |
| **Status** | All / Todo / InProgress / Done / Overdue |
| **Priority** | All / Low / Medium / High / Critical |
| **Assignee** | All or a specific team member |

4. Click **Download Excel** - the file is generated and downloaded immediately

> **Note**: If specific rows were checked before opening the export modal, only those tasks are exported regardless of the period filter. Leave all rows unchecked to apply the period/project filter to all matching tasks.

---

## 6. Kanban Board

![Kanban Board](C:\Users\WAHANA 24\.gemini\antigravity-ide\brain\7c763f71-399d-48c9-b9d2-f02976dda91e\04_kanban_1787812489942.jpg)

Navigate to **Tasks → Kanban View** (`/Task/Kanban`).

### Desktop Kanban
Three columns are displayed side by side:
- **To Do** (Gray) – Tasks waiting to start
- **In Progress** (Blue) – Tasks actively being worked on
- **Done** (Green) – Completed tasks

**Drag and drop** any task card to a new column to update its status instantly. The backend is updated via AJAX with a success notification.

Each card shows:
- Task title
- Priority badge
- Assignee avatar
- Due date
- Mini progress bar

### Mobile Kanban
On mobile devices, the Kanban uses a **Segmented Pill Tab Switcher**. Tap the `Todo`, `In Progress`, or `Done` tabs to switch columns. Each tab shows a live badge with the task count.

---

## 7. Projects

Navigate to **Projects** (`/Project`).

Projects organize tasks into logical groups. Each project has:
- **Name** and **Description**
- **Brand Color** (custom hex color for visual identity)
- **Status**: Active / On Hold / Completed
- **Deadline**: Project due date
- **Progress**: Automatically calculated from the average progress of all tasks in the project

### Creating a Project (Admin Only)
1. Click **+ New Project**
2. Fill in Name, Description, Color, Status, Deadline
3. Click **Save**

> **Note**: Only **Administrators** can create, edit, or delete projects.

---

## 8. Timesheet & Work Sessions

![Timesheet](C:\Users\WAHANA 24\.gemini\antigravity-ide\brain\7c763f71-399d-48c9-b9d2-f02976dda91e\06_timesheet_1787812518887.jpg)

Navigate to **Timesheet** (`/Timesheet`).

### Summary Cards
At the top, three summary cards show your work duration:
- **Today** – Total hours logged today
- **This Week** – Total hours logged this week
- **This Month** – Total hours logged this month

### 8.1 Recording Work Time

**Method 1 - Live Timer**
1. Click **Start Timer** button
2. Select the task you are working on (Select2 searchable)
3. Add a session note (optional)
4. Click **Start**
5. Work on your task
6. When finished, click **Stop Timer**
7. The session is saved automatically

> **Multi-Task Tip**: You can start **multiple timers simultaneously** on different tasks. Each running timer appears as a separate card in the sidebar with a live elapsed counter.

**Method 2 - Manual Entry**
1. Click **+ Add Session**
2. Select the task
3. Enter Start Time and End Time
4. Add session notes
5. Click **Save**

### Timesheet Table
Each session row shows: Date, Task Code, Task Title, Project, Start Time, End Time, Duration (HH:MM:SS), Notes, and Edit/Delete actions.

### 8.3 Multi-User Concurrent Timers

Work Tracker Pro fully supports **concurrent timers** scoped per-user:

- **User isolation**: Each user's timer sessions are tracked independently. User A's running timers never interfere with User B — even if tracking the same task.
- **Multiple tasks at once**: A single user can run timers on 2 or more different tasks simultaneously. Each appears as a separate live card in the **sidebar timer panel**.
- **Sidebar live display**: While any timer is running, the left sidebar shows a dedicated **Active Timers** section. Each card displays:
  - Task code and name
  - Live elapsed time counter (ticking every second)
  - A **Stop** button to end that specific session
- **API endpoint**: `GET /api/timesheets/active-timers` returns all currently running sessions for the authenticated user.

> **Note**: If you close the browser while a timer is running, the session remains open. Go to the Timesheet page and manually edit the session to set an end time.

### 8.2 Exporting Personal Timesheet Report

1. Click **Download Excel Report** (or access via Reports page)
2. A modal opens — select:
   - **Period Preset**: This Week / Last Week / This Month / Last Month / Custom Range
   - **Project Filter** (optional)
3. Click **Download .xlsx**

The Excel file has **two sheets**:
- **Sheet 1 – Personal Timesheet**: Header banner, metadata info box (name, email, job title, period, total sessions, total hours), detailed session table with auto-SUM formula
- **Sheet 2 – Summary by Project**: Distribution table showing project name, session count, hours, and percentage allocation

> **Privacy**: The timesheet report is strictly isolated to your own data. You cannot view or export other users' sessions.

---

## 9. Notes & Documentation

![Notes](C:\Users\WAHANA 24\.gemini\antigravity-ide\brain\7c763f71-399d-48c9-b9d2-f02976dda91e\07_notes_1787812533963.jpg)

Navigate to **Notes** (`/Note`).

The Notes module is a rich documentation workspace for meeting minutes, technical notes, and work documentation.

### 9.1 Creating a Note

1. Click **+ Add Note**
2. Fill in:
   - **Title** – Note title
   - **Category** – Free-form label (e.g., Notula, Technical, Meeting)
   - **Color** – Card accent color for visual organization
   - **Related Task** (optional) – Link note to a specific task
   - **Content** – Rich text editor (Quill.js) with bold, italic, lists, code blocks, headings
3. Attach files if needed
4. Click **Save Note**

### 9.2 File Attachments

Notes support multi-file attachments:
1. Click **Choose Files** or drag files into the upload zone
2. A live preview shows: file icon, filename, size, and a remove button per file
3. Any file type is supported; **maximum 10MB per file**
4. Files are stored in: `wwwroot/uploads/notes/{your_username}/`

On the Note Detail view, attachments are displayed with:
- Thumbnail preview (for images)
- File metadata (name, size, uploader, upload date)
- **Download** and **Delete** buttons per attachment

### 9.3 Pinning Notes

- Click the **Pin** icon on any note card to pin it to the top of the list
- Pinned notes appear with a highlighted pin icon
- Click again to unpin

---

## 10. Calendar

Navigate to **Calendar** (`/Calendar`).

The Calendar provides a timeline view of all tasks using FullCalendar.js:
- Tasks are shown as colored event blocks on their scheduled dates
- Click any event to see task details
- Filter by **Project** or **Assignee**
- Switch between **Month**, **Week**, and **Day** views
- Navigate with Previous and Next buttons

Task events are color-coded by project or status for quick visual scanning.

---

## 11. Reports & Analytics

![Reports](C:\Users\WAHANA 24\.gemini\antigravity-ide\brain\7c763f71-399d-48c9-b9d2-f02976dda91e\08_reports_1787812557674.jpg)

Navigate to **Reports** (`/Report`).

### Available Reports

| Report | Description |
|---|---|
| **Personal Timesheet Excel** | Download your personal work session report in .xlsx format |
| **Task Completion Trend** | Line chart showing tasks completed over time |
| **Team Workload Analysis** | Stacked bar chart: tasks per team member by status |
| **Project Distribution** | Donut/pie chart of task distribution by project |
| **Gantt Chart** | Timeline view of tasks by planned start/end dates |

### Filter Options
- **Period**: This Month, Last Month, Last Quarter, Custom date range
- **Project**: Filter all charts by a specific project

---

## 12. Members & Team Management

Navigate to **Members** (`/Member`).

The Members module provides a team directory with individual performance insights.

### Member Card View
Each member card shows:
- Profile picture or initials avatar
- Full name, job title, email
- Role badge (Admin / User)
- Task counts: Total Assigned / Completed / In Progress
- Total logged work hours

### Individual Member Detail
Clicking on a member shows their full profile with:
- **Personal Stats**: Tasks by status, total hours
- **Contribution Analysis**: Task completion rate and activity chart
- **Project Breakdown**: Which projects and how many tasks per project

### 12.1 Member Cards & Detail View
Each member card shows: profile picture or initials avatar, full name, job title, email, role badge (Admin / User), task counts (Total Assigned / Completed / In Progress), and total logged work hours.

Clicking on a member shows their full profile with **Personal Stats**, **Contribution Analysis**, and **Project Breakdown**.

### 12.2 Admin Functions (Administrators Only)
- **Add Member**: Create a new user account
- **Edit Member**: Update name, job title, role, avatar
- **Lock / Unlock Account**: Toggle account access without deleting it
- **Delete Member**: Remove a user from the system (confirmation required)

### 12.3 Admin Password Reset

Administrators can reset any member's password **directly** without requiring the member to use a "Forgot Password" flow:

#### Via Member List (`/Member`)
1. Locate the member card
2. Click the **Reset Password** button (key icon) on the card
3. In the SweetAlert2 dialog, enter the **New Password** (minimum 6 characters)
4. Click **Reset Password** to confirm
5. A success notification confirms the action

#### Via Member Detail (`/Member/Details/{id}`)
1. On the member profile page, find the **Reset Password** section
2. Enter a new password in the input field (with show/hide toggle)
3. Click **Reset Password**

> **Security**: Only users with the `Administrator` role can perform this action. The reset uses ASP.NET Identity's secure token-based flow. The action is logged to the Audit Trail automatically.

---

## 13. Import & Export Excel

![Import/Export](C:\Users\WAHANA 24\.gemini\antigravity-ide\brain\7c763f71-399d-48c9-b9d2-f02976dda91e\11_import_1787812574073.jpg)

Navigate to **Import/Export** (`/Import`).

### 13.1 Importing Tasks from Excel

> **Requirement**: Administrator role only

**Step 1 – Download Template**
Click **Download Template (.xlsx)** to get the standard import template with the correct column headers.

**Expected columns for Standard Format (9 columns)**:

| Column | Description |
|---|---|
| Task Name | Title of the task |
| Category | Task category (e.g., Backend, Frontend) |
| Project | Project name |
| Assignee | Name or email of the person in charge |
| Priority | Low / Medium / High / Critical |
| Status | Todo / InProgress / Done |
| Progress | 0-100 |
| Start Date | YYYY-MM-DD |
| Due Date | YYYY-MM-DD |

**Step 2 – Upload File**
1. Click the upload zone or drag your `.xlsx` file into it
2. The system automatically detects the format (Standard 9-column or ARMS 21-column)
3. A progress bar shows parsing status

**Step 3 – Preview & Assign PIC**
The preview page shows each parsed row with validation:
- **Green rows** – Valid and ready to import
- **Red rows** – Errors (missing required fields, invalid dates, etc.)
- Each row has a PIC dropdown to assign or re-assign the person in charge
- Use **Bulk Assign All** to set the same PIC for all rows

**Step 4 – Confirm Import**
Click **Confirm Import Tasks** — the system will:
1. Auto-create Project and Category records if they don't exist
2. Insert all valid task records with assigned PICs
3. Log the import event to the Audit Trail
4. Redirect to the Task list with a success notification

### 13.2 Exporting Tasks to Excel

Tasks can be exported from both the **Import/Export** page and directly from the **Task List** page.

| Format | Description | Columns |
|---|---|---|
| **Standard Format (.xlsx)** | Compatible with the import template | 9 columns |
| **ARMS Format (.xlsx)** | Enterprise format for cross-system interoperability | 21 columns |

#### Export from Task List (with Period & Filter)

See [Section 5.7](#57-exporting-tasks-to-excel-with-period-filter) for the full flow. Filters available:
- **Period**: Today / Yesterday / Last 7 Days / Last 30 Days / This Month / Last Month / Custom Range
- **Project**, **Status**, **Priority**, **Assignee**

#### Export from Import/Export Page
1. Navigate to **Import/Export** (`/Import`)
2. Click **Export Standard Excel** or **Export ARMS Excel**
3. The file downloads immediately (exports all tasks)

---

## 14. JSON Tools (Developer Utilities)

Navigate to **JSON Tools** (`/JsonTools`).

A handy utility for developers to work with JSON data directly in the browser:

| Tool | Description |
|---|---|
| **Format / Pretty Print** | Indent and beautify raw JSON with configurable spacing |
| **Minify** | Compress JSON by removing all whitespace |
| **Validate** | Check JSON syntax validity; shows line number and error message if invalid |
| **Save Snippet** | Save named JSON snippets to the database for later reference |
| **History** | Browse, view, and delete previously saved JSON snippets |

**How to use**:
1. Paste or type JSON content into the editor
2. Click the desired action button
3. The result appears in the output area
4. Optionally give it a name and click **Save** to store it

---

## 15. Audit Trail

![Audit Trail](C:\Users\WAHANA 24\.gemini\antigravity-ide\brain\7c763f71-399d-48c9-b9d2-f02976dda91e\12_audit_trail_1787812621478.jpg)

Navigate to **Audit Trail** (`/AuditTrail`) — **Administrator only**.

All system activities are automatically logged by the **Global Action Filter** without any manual action required.

### Activity Trend Chart
A multi-series line chart shows activity over the past 14 days grouped by action type:
- Blue: GET / View
- Green: Create / Add
- Orange: Edit / Update
- Red: Delete
- Purple: Login / Logout

### Log Table
Each audit log entry contains:

| Field | Description |
|---|---|
| **Timestamp** | Exact date and time of the action |
| **User Email** | Who performed the action |
| **HTTP Method** | GET / POST / PUT / DELETE |
| **Controller** | Which module (TaskController, etc.) |
| **Action** | Specific method name |
| **Path** | Request URL path |
| **Status Code** | HTTP response status (200, 201, 403, etc.) |
| **Duration (ms)** | Response time in milliseconds |

### Filters
- **Date Range** – Pick a start and end date
- **Action Type** – All / Create / Edit / Delete / Login / Logout
- **User Email** – Search by email address

### Export
Click **Export CSV** to download the filtered audit log as a CSV file.

---

## 16. Master Data Management

Navigate to **Master Data** (`/MasterData`) — **Administrator only**.

The Master Data module manages system reference data used across all modules:

### Task Categories
Custom task categories (e.g., Backend, Frontend, API, Database, DevOps, Testing):
- **Name** and **Color** (hex)
- **Description**
- Inline add/edit/delete with confirmation

### Task Priorities
Manage the priority options:
- **Name** (e.g., Low, Medium, High, Critical)
- **Color** (hex — used for badge coloring)
- **Icon** (FontAwesome class name)
- **Order Index** (display order)
- **Is Default** flag

### Task Statuses
Manage workflow statuses:
- **Name** (e.g., Todo, InProgress, Done, Overdue)
- **Color** (hex)
- **Is Done State** (marks this status as a "completed" state)
- **Order Index**
- **Is Default** flag

### SDLC Milestones
Manage Waterfall SDLC phases: Requirement Analysis, System Design, Implementation, Testing & QA, Deployment, Maintenance. Custom milestones can be added, renamed, or reordered.

---

## 17. System Configuration

Navigate to **Configuration** (`/Configuration`) — **Administrator only**.

### Global Base URL
Set the publicly accessible URL of the application (used for API documentation and links):
1. Enter the full URL (e.g., `http://myserver:5000`)
2. Click **Save**

### Database Management

| Action | Description |
|---|---|
| **Database Capacity** | View file size, record counts per table, and storage statistics |
| **Shrink Database** | Run SQLite VACUUM to reclaim unused storage space |
| **Reset Database (Transactional)** | Clear sessions, audit logs, JSON history, and import logs |
| **Reset Database (Factory)** | DANGER: Wipe all data and reset to clean state (irreversible) |

All destructive operations require confirmation via a SweetAlert2 dialog.

### API Documentation Summary
View the complete summary of all REST API endpoints grouped by module, with a link to the interactive Swagger UI.

---

## 18. REST API & Swagger Documentation

![Swagger UI](C:\Users\WAHANA 24\.gemini\antigravity-ide\brain\7c763f71-399d-48c9-b9d2-f02976dda91e\15_swagger_1787812606947.jpg)

Work Tracker Pro includes a complete **OpenAPI 3.0 REST API** accessible at `/swagger`.

### Available API Modules (70+ Endpoints)

| Module | Base Route | Key Endpoints |
|---|---|---|
| **Auth** | `/api/auth` | Login, Logout, Profile (/me), Change Password, Update Profile |
| **Tasks** | `/api/tasks` | CRUD, Status update, Kanban feed, Bulk delete, Sessions CRUD |
| **Projects** | `/api/projects` | CRUD, Task list per project, Summary metrics |
| **Notes** | `/api/notes` | CRUD, Pin toggle, Categories list, File attachments |
| **Timesheets** | `/api/timesheets` | CRUD, Start/Stop timer, **Active timers** (`/active-timers`), Summary, Excel/CSV export |
| **Members** | `/api/members` | CRUD, Toggle lock, Contributions summary, **Admin password reset** (`/{id}/reset-password`) |
| **MasterData** | `/api/master-data` | CRUD for Categories, Priorities, Statuses, Milestones |
| **Calendar** | `/api/calendar` | Events feed with date/project/assignee filters |
| **Import/Export** | `/api/import` | Template download, Preview, Execute import, ARMS export |
| **JsonTools** | `/api/json-tools` | Format, Minify, Validate, Save, History CRUD |
| **Notifications** | `/api/notifications` | Get alerts, Mark read, Mark all read |
| **Dashboard** | `/api/dashboard` | Summary metrics, Manual sync trigger |
| **Reports** | `/api/reports` | Dashboard, Chart data, Team workload, Gantt |
| **AuditTrail** | `/api/audit-trail` | List, Detail, Stats, Export CSV, Clear |
| **Configuration** | `/api/configuration` | Base URL, DB capacity, Shrink, Reset, API summary |

#### New Endpoints (v3.1)

| Endpoint | Method | Description | Access |
|---|---|---|---|
| `/api/timesheets/active-timers` | GET | Returns all running timer sessions for the authenticated user | Any logged-in user |
| `/api/members/{id}/reset-password` | POST | Admin resets a member's password; body: `{ "newPassword": "..." }` | Admin only |
| `/Task/ExportArmsExcel` | GET | Download ARMS 21-col Excel with filter params | Any logged-in user |
| `/Task/ExportStandardExcel` | GET | Download Standard 9-col Excel with filter params | Any logged-in user |

### Standard API Response Format
All endpoints return a consistent JSON structure:

```json
{
  "success": true,
  "message": "Operation successful message",
  "data": { },
  "errors": null,
  "timestamp": "2026-08-27T13:00:00"
}
```

### Using Swagger UI
1. Navigate to `http://localhost:5000/swagger`
2. Click on any endpoint group to expand it
3. Click on a specific endpoint to see its parameters and response schema
4. Click **Try it out** then **Execute** to test the API directly

---

## 19. Role-Based Access Control

Work Tracker Pro implements a two-tier access control system: **Roles** (Admin vs User) and **Job Titles** (System Analyst and Technical Writer receive elevated permissions).

| Feature | Admin | System Analyst | Technical Writer | Regular User |
|---|:---:|:---:|:---:|:---:|
| Login & Profile Management | Yes | Yes | Yes | Yes |
| View Dashboard & Kanban | Yes | Yes | Yes | Yes |
| Create Tasks | Yes | Yes | Yes | Yes |
| Edit Own Tasks | Yes | Yes | Yes | Yes |
| Edit Other Users' Tasks | Yes | Yes | Yes | No |
| Start Timer on Others' Tasks | Yes | Yes | Yes | No |
| **Run Multiple Concurrent Timers** | Yes | Yes | Yes | Yes |
| Delete Own Tasks | Yes | Yes | Yes | Yes |
| Delete Others' Tasks | Yes | No | No | No |
| Bulk Delete (own only for non-admin) | All | Own only | Own only | Own only |
| **Export Tasks (Standard & ARMS, Period Filter)** | Yes | Yes | Yes | Yes |
| Clear All Tasks | Yes | No | No | No |
| Create / Edit Projects | Yes | No | No | No |
| View & Record Timesheets | Yes | Yes | Yes | Yes |
| Reset All Timesheets | Yes | No | No | No |
| Notes & File Uploads | Yes | Yes | Yes | Yes |
| Import Tasks from Excel | Yes | No | No | No |
| Manage Members | Yes | No | No | No |
| **Reset Member Password (Admin Only)** | Yes | No | No | No |
| Audit Trail & Master Data | Yes | No | No | No |
| System Configuration | Yes | No | No | No |

> **Special Privilege**: Users with Job Title "System Analyst" or "Technical Writer" (case-insensitive) automatically receive elevated edit permissions.

---

## 20. Mobile & Responsive Usage

Work Tracker Pro is fully optimized for mobile and tablet devices (screens below 1024px wide).

### Bottom Navigation Bar (5 quick-access buttons)
1. Home (Dashboard)
2. Tasks (Task List)
3. Quick Add (floating + button to create a task instantly)
4. Projects (Project List)
5. Menu (opens the full sidebar drawer)

### Off-Canvas Sidebar Drawer
Tap the Menu button to open the full navigation drawer with all module links.

### Mobile-Specific Features
- Touch-friendly sliders for task progress
- Responsive single-column form layouts on mobile
- Mobile Kanban Tab Switcher instead of 3-column layout
- Safe area insets for iPhones with notches and Android gesture navigation
- Viewport-aware dropdowns that never overflow off-screen on 320px devices

---

## 21. User Account & Profile

Navigate to **Profile** from the user dropdown in the topbar.

### Updating Your Profile
1. Click your **avatar / name** in the top right corner
2. Select **Profile Settings**
3. Update: Full Name, Job Title, Phone Number, Avatar Color, Profile Picture
4. Click **Save Profile**

### Changing Password
1. Go to Profile Settings
2. Enter your **current password**
3. Enter and **confirm** the new password
4. Click **Update Password**

---

## 22. Tips & Best Practices

| Tip | Description |
|---|---|
| **Use Select2 dropdowns** | All dropdowns (Project, Assignee, Parent Task) support search — just start typing |
| **Quick progress presets** | Use the 0%, 25%, 50%, 75%, 100% buttons to update progress in one click |
| **Status-Progress sync** | Marking Done auto-sets 100%; setting 100% auto-marks Done |
| **Pin important notes** | Pinned notes always appear first in the list |
| **Bulk assign PIC on import** | Use "Assign All" in import preview to set one PIC for all rows at once |
| **VACUUM the database regularly** | Go to Configuration > Shrink Database to keep the SQLite file compact |
| **Check Audit Trail for issues** | If something unexpected happened, check the Audit Trail to trace the exact action |
| **JSON Tools for API testing** | Use Format/Validate tools to quickly verify and debug API response payloads |
| **Use Obstacle/Solution fields** | Document blockers and their solutions on tasks for knowledge retention |

---

## 23. Troubleshooting

| Issue | Solution |
|---|---|
| **Cannot login** | Verify your email and password. Contact your admin to check if your account is locked. |
| **Page returns 403 Forbidden** | You don't have the required role for this action. Contact your administrator. |
| **Task edit button not working** | The task is assigned to another user. You need Admin/SA/TW privileges to edit it. |
| **File upload fails** | Check that the file is under 10MB. Ensure the uploads directory has write permissions on the server. |
| **Kanban drag-and-drop not working** | Check that JavaScript is enabled in your browser. Try refreshing the page (Ctrl+R). |
| **Swagger shows no endpoints** | Ensure `TrackerKerja.xml` is in the same directory as `TrackerKerja.dll` when running the app. |
| **Import preview shows all red rows** | Your Excel file may have incorrect column headers. Download and use the official template. |
| **Database size growing too large** | Run Configuration > Shrink Database to perform SQLite VACUUM and reclaim space. |
| **Timer won't stop** | If the browser was closed during a timer session, go to Timesheet and manually edit the session to add an end time. |
| **Themes not applying** | Clear your browser cache (Ctrl+Shift+Del) and reload. Theme preference is stored in a session cookie. |

---

## Appendix A – Data Model Quick Reference

| Entity | Key Fields |
|---|---|
| **WorkTask** | Id, Title, Description, ProjectId, CategoryId, AssignedToUserId, ParentTaskId, Priority, Status, Progress, Obstacle, Solution, StartDate, DueDate, Milestone, Tags |
| **Project** | Id, Name, Description, Color, Status, Deadline |
| **WorkSession** | Id, TaskId, **UserId** (timer owner for isolation), StartTime, EndTime, Duration (seconds), Notes |
| **WorkNote** | Id, Title, ContentHtml, Category, Color, IsPinned, AuthorUserId, TaskId |
| **NoteAttachment** | Id, NoteId, FileName, FilePath, FileSize, ContentType, UploadedByUserId |
| **AppUser** | Id, FullName, Email, JobTitle, AvatarColor, ProfilePictureUrl, CreatedAt |
| **AuditLog** | Id, UserId, UserEmail, ControllerName, ActionName, HttpMethod, Path, StatusCode, DurationMs, Timestamp |
| **MasterPriority** | Id, Name, Color, Icon, OrderIndex, IsDefault |
| **MasterStatus** | Id, Name, Color, IsDoneState, OrderIndex, IsDefault |
| **MasterMilestone** | Id, Name, Description, OrderIndex |

---

## Appendix B – ARMS Export Format (21 Columns)

When using **Export ARMS Format**, the Excel file contains these 21 columns compatible with enterprise project management systems:

| # | Column Name | Description |
|---|---|---|
| 1 | No | Row number |
| 2 | Task ID | System task code (TSK-XXXX) |
| 3 | Parent Task ID | Parent task code (if sub-task) |
| 4 | Module / Sub-Module | Task title |
| 5 | Task Name | Task title (duplicate for ARMS compat.) |
| 6 | PIC (Name) | Assignee full name |
| 7 | PIC (Email) | Assignee email address |
| 8 | Category | Task category |
| 9 | Priority | Priority level |
| 10 | Status | Current status |
| 11 | Progress (%) | Progress value 0-100 |
| 12 | Milestone SDLC | SDLC phase |
| 13 | Obstacle / Kendala | Blocker notes |
| 14 | Solution / Solusi | Resolution notes |
| 15 | Tags | Comma-separated tags |
| 16 | Project Name | Project the task belongs to |
| 17 | Start Date | Planned start date |
| 18 | End Date (Due Date) | Deadline date |
| 19 | Total Sessions | Number of work sessions logged |
| 20 | Total Duration (Hours) | Total time spent (decimal hours) |
| 21 | Created At | Date/time the task was created |

---

## Appendix C - Export Period Filter Parameters

When calling the export endpoints directly (e.g., for scripting or API integration), use these query parameter values for the `period` field:

| `period` Value | Date Range Resolved |
|---|---|
| `today` | From 00:00 today to now |
| `yesterday` | Full previous calendar day |
| `last7days` | Last 7 calendar days |
| `last30days` | Last 30 calendar days |
| `this_month` | From 1st of the current month to now |
| `last_month` | Full previous calendar month |
| *(omit `period`)* | Use `startDate` and `endDate` query params for a custom range |

**Example URLs:**
```
GET /Task/ExportArmsExcel?period=this_month&projectId=3&priority=High
GET /Task/ExportStandardExcel?selectedIds=12,15,20
GET /Task/ExportArmsExcel?startDate=2026-08-01&endDate=2026-08-27
GET /api/timesheets/active-timers
POST /api/members/{id}/reset-password  {"newPassword": "NewPass123!"}
```

---

*Work Tracker Pro User Guide - Version 3.1 | August 27, 2026 | TrackerKerja Development Team*
