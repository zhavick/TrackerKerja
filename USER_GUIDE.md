# Work Tracker Pro (TrackerKerja) — User Guide

> **Version**: 3.1 (Docker & Enterprise Cloud Edition)  
> **Platform**: Web Application (ASP.NET Core 8.0 MVC / REST API / Docker Container)  
> **Repository**: [https://github.com/zhavick/TrackerKerja.git](https://github.com/zhavick/TrackerKerja.git)  
> **Last Updated**: August 31, 2026

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Getting Started & Deployment](#2-getting-started--deployment)
   - [2.1 System Requirements](#21-system-requirements)
   - [2.2 Running with Docker & Docker Compose (Recommended)](#22-running-with-docker--docker-compose-recommended)
   - [2.3 Running with .NET 8.0 SDK / Standalone Binary](#23-running-with-net-80-sdk--standalone-binary)
   - [2.4 GitHub Repository & Synchronization](#24-github-repository--synchronization)
   - [2.5 Login & Default Credentials](#25-login--default-credentials)
   - [2.6 User Interface Overview](#26-user-interface-overview)
3. [Themes & Appearance (16 Themes)](#3-themes--appearance)
4. [Dashboard & Quick Metrics](#4-dashboard--quick-metrics)
5. [Task Management](#5-task-management)
   - [5.1 Task List View & Advanced Search](#51-task-list-view--advanced-search)
   - [5.2 Creating a Task](#52-creating-a-task)
   - [5.3 Editing a Task & Permission Hierarchy](#53-editing-a-task--permission-hierarchy)
   - [5.4 Task Progress (0–100%) & Status Auto-Sync](#54-task-progress-0100--status-auto-sync)
   - [5.5 Parent / Child Task (Sub-tasks)](#55-parent--child-task-sub-tasks)
   - [5.6 Obstacles & Technical Solutions Logging](#56-obstacles--technical-solutions-logging)
   - [5.7 Bulk Actions (Delete & Filtered Export)](#57-bulk-actions-delete--filtered-export)
   - [5.8 Exporting Tasks to Excel with Period Filter](#58-exporting-tasks-to-excel-with-period-filter)
6. [Interactive Kanban Board](#6-interactive-kanban-board)
   - [6.1 Drag & Drop Workflow](#61-drag--drop-workflow)
   - [6.2 Mobile Segmented Column Switcher](#62-mobile-segmented-column-switcher)
7. [Projects & Milestones](#7-projects--milestones)
8. [Timesheet & Work Sessions](#8-timesheet--work-sessions)
   - [8.1 Recording Work Time (Live Timer & Manual Entry)](#81-recording-work-time-live-timer--manual-entry)
   - [8.2 Concurrent Multi-Timer per User](#82-concurrent-multi-timer-per-user)
   - [8.3 Exporting Personal Timesheet Report (.xlsx)](#83-exporting-personal-timesheet-report-xlsx)
9. [Notes & Documentation (Quill.js Rich-Text)](#9-notes--documentation-quilljs-rich-text)
   - [9.1 Creating & Formatting Notes](#91-creating--formatting-notes)
   - [9.2 Multi-File Attachments in User Directory](#92-multi-file-attachments-in-user-directory)
   - [9.3 Note Pinning & Standalone vs Task Notes](#93-note-pinning--standalone-vs-task-notes)
10. [Calendar & Timeline](#10-calendar--timeline)
11. [Reports & Analytics (Gantt Chart & Workload)](#11-reports--analytics-gantt-chart--workload)
12. [Members & Team Management](#12-members--team-management)
    - [12.1 Member Cards & Contribution Metrics](#121-member-cards--contribution-metrics)
    - [12.2 Admin Functions & Account Management](#122-admin-functions--account-management)
    - [12.3 Admin Instant Password Reset](#123-admin-instant-password-reset)
13. [Import & Export Excel Enterprise](#13-import--export-excel-enterprise)
    - [13.1 Standard 9-Column Import with Bulk PIC Reassignment](#131-standard-9-column-import-with-bulk-pic-reassignment)
    - [13.2 ARMS Enterprise 21-Column Export & Import](#132-arms-enterprise-21-column-export--import)
14. [JSON Tools (Developer Utilities)](#14-json-tools-developer-utilities)
15. [Audit Trail & Activity Logging](#15-audit-trail--activity-logging)
16. [Master Data Management](#16-master-data-management)
17. [System Configuration & Database Maintenance](#17-system-configuration--database-maintenance)
18. [RESTful API, Swagger UI & Postman](#18-restful-api-swagger-ui--postman)
19. [Role-Based Access Control (RBAC) & Privileges](#19-role-based-access-control-rbac--privileges)
20. [Mobile & Responsive UI Experience](#20-mobile--responsive-ui-experience)
21. [User Account & Profile Settings](#21-user-account--profile-settings)
22. [Tips & Best Practices](#22-tips--best-practices)
23. [Troubleshooting & FAQ](#23-troubleshooting--faq)
24. [Appendix A - Data Model Quick Reference](#appendix-a--data-model-quick-reference)
25. [Appendix B - ARMS Export Format (21 Columns)](#appendix-b--arms-export-format-21-columns)
26. [Appendix C - Export Period Filter Parameters](#appendix-c--export-period-filter-parameters)

---

## 1. Introduction

**Work Tracker Pro** (TrackerKerja) is an enterprise work tracking, timesheet recording, technical documentation, and team analytics platform built on **ASP.NET Core 8.0 MVC & Web API** with **SQLite EF Core**, **ClosedXML spreadsheet engine**, **Tailwind CSS**, and **Docker containerization**.

### Key Feature Matrix

| Feature | Capability & Implementation |
|---|---|
| **Task Management** | Full hierarchy (parent-child), obstacles/solutions log, 0-100% slider, priority badges |
| **Kanban Board** | Touch-friendly drag-and-drop powered by SortableJS, mobile column tab switcher |
| **Timesheet** | Real-time timers, **concurrent active multi-timer per user**, manual session entry |
| **Personal Timesheet Excel** | Multi-sheet ClosedXML export with employee metadata, sessions, and `=SUM()` formulas |
| **Documentation & Notes** | Quill.js rich text editor, multi-file attachments in `wwwroot/uploads/notes/{username}/` |
| **Team Management** | Member cards, performance charts, and **Admin Direct Password Reset** |
| **Excel Interoperability** | Standard 9-col template + ARMS 21-col enterprise export/import with period filter |
| **Reports & Analytics** | Interactive Chart.js charts, project distribution, team workload, and Gantt timeline |
| **Calendar** | FullCalendar integration with date range, assignee, and project filtering |
| **Developer Tools** | JSON formatter, minifier, validator, history snippets, and test payloads |
| **Audit Trail** | Global action filter logging controller actions, durations, status codes, and user IP |
| **Master Data** | Admin-managed SDLC Waterfall Milestones, Priorities, Statuses, and Categories |
| **REST API & Swagger** | 70+ OpenAPI/Swagger-documented endpoints with Postman Collection & Environment |
| **Themes & Styling** | 16 instant themes (10 light + 6 dark OLED/matrix) via dynamic CSS custom tokens |
| **Docker Support** | Multi-stage Dockerfile, Docker Compose, volume persistence (`./db_data`, `./uploads`) |

---

## 2. Getting Started & Deployment

### 2.1 System Requirements

| Requirement | Docker Environment | Native .NET Environment |
|---|---|---|
| **Engine** | Docker Desktop 20+ / Docker Engine + Compose v2 | .NET 8.0 SDK or ASP.NET Core Runtime |
| **Memory** | Minimum 1 GB RAM (2 GB recommended) | Minimum 512 MB RAM |
| **Storage** | 500 MB free disk space | 200 MB free disk space |
| **Browser** | Chrome 110+, Firefox 110+, Edge 110+, Safari 16+ | Chrome 110+, Firefox 110+, Edge 110+, Safari 16+ |
| **OS** | Windows 10/11, macOS, Ubuntu/Debian Linux | Windows, macOS, Linux |

---

### 2.2 Running with Docker & Docker Compose (Recommended)

Docker is the quickest and cleanest way to run TrackerKerja in Production or local testing:

```bash
# 1. Start the container in background
docker compose up -d --build

# 2. Check container status
docker compose ps

# 3. View live logs
docker compose logs -f trackerkerja

# 4. Stop the container
docker compose down
```

#### Windows PowerShell Helper Script:
```powershell
# Copy local database & uploads to persistent Docker volumes
.\docker-run.ps1 init-data

# Build and start container
.\docker-run.ps1 up

# View live streaming logs
.\docker-run.ps1 logs

# Restart container
.\docker-run.ps1 restart

# Stop container
.\docker-run.ps1 down
```

> **Data Persistence**: Database is stored in `./db_data/trackerkerja.db` and uploaded files in `./uploads/`. Data will NOT be lost when containers are recreated or updated.

---

### 2.3 Running with .NET 8.0 SDK / Standalone Binary

#### Option A: Local Development
```bash
dotnet run --urls=http://localhost:5000
```

#### Option B: Standalone Release Build
```bash
dotnet publish -c Release -o ./publish
cd publish
dotnet TrackerKerja.dll --urls=http://localhost:5000
```

Open your browser and navigate to: **`http://localhost:5000`**

---

### 2.4 GitHub Repository & Synchronization

Official Repository: 👉 **[https://github.com/zhavick/TrackerKerja.git](https://github.com/zhavick/TrackerKerja.git)**

To push updates to GitHub using a Personal Access Token (PAT):
```powershell
# Run the automated push assistant
.\git-push.ps1
```

---

### 2.5 Login & Default Credentials

On the login screen (`/Account/Login`):
1. Enter your registered **Email Address**
2. Enter your **Password**
3. Optionally check **Remember Me** for a 7-day persistent session
4. Click **Masuk (Login)**

#### Pre-seeded Default Accounts:

| Email | Default Password | Role | Job Title |
| :--- | :--- | :--- | :--- |
| `admin@trackerkerja.com` | `Admin123!` | Administrator | System Administrator |
| `glenn.hakim@elistec.com` | `Password123!` | User | Project Lead |
| `heni.rahayu@elistec.com` | `Password123!` | User | QA & Product Specialist |
| `haviz.indra@elistec.com` | `Password123!` | User | Frontend Developer |
| `Iqbal.ali@elistec.com` | `Password123!` | User | Backend Developer |
| `mohammad.danang@elistec.com` | `Password123!` | User | DevOps Engineer |
| `syafix.said@elistec.com` | `Password123!` | User | System Analyst *(Elevated Edit Permissions)* |
| `nanda.putri@elistec.com` | `Password123!` | User | Technical Writer *(Elevated Edit Permissions)* |
| `athallah.bariq@elistec.com` | `Password123!` | User | Fullstack Developer |

---

### 2.6 User Interface Overview

- **Desktop Sidebar Navigation**: Fixed left-side navigation with collapsible sections and active module badges.
- **Top Header Bar**: Search bar, Notification bell with unread indicator, Instant Theme Switcher dropdown, and User Profile Menu.
- **Floating Mobile Bottom Bar**: 5 key touch buttons (*Home*, *Tasks*, *+ Quick Add*, *Projects*, *Menu Drawer*).
- **Off-Canvas Navigation Drawer**: Full responsive menu on mobile with backdrop blur.

---

## 3. Themes & Appearance

Work Tracker Pro provides **16 distinct theme palettes** (10 Light & 6 Dark modes) powered by native CSS design tokens.

### Dark Modes (6 Themes)
- **Midnight OLED** (`#6366F1` + `#38BDF8`): Deep pure black for battery efficiency and high-end OLED displays.
- **Cyberpunk Synthwave** (`#F43F5E` + `#06B6D4`): Neon fuchsia and electric cyan on obsidian dark backdrop.
- **Emerald Matrix** (`#10B981` + `#34D399`): Dark slate background with terminal hacker green highlights.
- **Dracula Eclipse** (`#A855F7` + `#EC4899`): Midnight purple with vibrant pastel pink accents.
- **Abyssal Ocean** (`#38BDF8` + `#3B82F6`): Deep navy blue ocean with luminous sapphire highlights.
- **Solar Ember** (`#F97316` + `#F59E0B`): Charcoal dark with radiant flame orange & warm gold.

### Light Modes (10 Themes)
- **Indigo Nebula** (Default): Indigo violet with clean modern aesthetics.
- **Emerald Forest**: Calming natural greens for high-focus working environments.
- **Ocean Azure**: Fresh corporate blue palette.
- **Sunset Crimson**: Warm crimson and orange tones.
- **Cyberpunk Neon**: High contrast neon pink/cyan.
- **Royal Amethyst**: Regal purple and soft violet.
- **Amber Gold**: Classic golden warmth.
- **Slate Minimalist**: Ultra clean monochrome greyscale.
- **Nordic Teal**: Scandinavian teal and soft slate.
- **Midnight Titanium**: Titanium metallic silver accents.

---

## 4. Dashboard & Quick Metrics

The Dashboard (`/Home/Index` or `/`) provides real-time visibility into operations:

1. **Top Metric Cards**: Total Tasks, In Progress, Completed (Done), and Overdue Tasks with trend badges.
2. **Project Distribution Progress Bars**: Percentage completion per active project.
3. **Task Activity Chart (7 Days)**: Daily breakdown of tasks created vs completed.
4. **Recent Work Items**: Quick links to active tasks with PIC avatar, milestone badge, and priority pill.

---

## 5. Task Management

### 5.1 Task List View & Advanced Search
Navigate to **Tasks** (`/Task`).
- Filter by **Status** (*Todo, InProgress, Review, Done, Overdue*).
- Filter by **Priority** (*Low, Medium, High, Critical*).
- Filter by **Project**, **PIC Assignee**, and **Milestone SDLC**.
- Full-text search across Title, Description, Obstacles, and Solutions.

### 5.2 Creating a Task
Click **+ Add Task** (`/Task/Create`) and complete the fields:
- **Title** (Required): Clear, descriptive task name.
- **Project & Category**: Organization and work classification.
- **Assignee (PIC)**: Assign to team member.
- **Parent Task**: Optional parent task to establish sub-task hierarchy.
- **Priority & Status**: Priority level and initial Kanban state.
- **Progress Slider (0–100%)**: Visual slider with quick 25% step presets.
- **Milestone SDLC**: Choose from Waterfall phases (*Requirement Analysis*, *System Design*, *Implementation*, *Testing & QA*, *Deployment*, *Maintenance*).
- **Obstacle & Solution**: Document technical blockers and fixes.

### 5.3 Editing a Task & Permission Hierarchy
- **Administrators**: Can edit and delete any task in the system.
- **System Analysts & Technical Writers**: Elevated permission to edit all tasks and start timers on any task.
- **Team Members (Users)**: Can edit tasks assigned to themselves.

### 5.4 Task Progress (0–100%) & Status Auto-Sync
- Setting progress to **100%** automatically transitions the task status to **Done**.
- Marking status as **Done** automatically sets the progress slider to **100%**.
- Selecting progress between **1% and 99%** transitions status from *Todo* to *InProgress*.

### 5.5 Parent / Child Task (Sub-tasks)
- Link granular technical sub-tasks under a high-level user story / parent feature.
- Sub-tasks display indented under their parent with a visual link icon.

### 5.6 Obstacles & Technical Solutions Logging
- **Obstacle**: Record root cause, error message, or operational impediment.
- **Solution**: Record code fix, architecture change, or troubleshooting step.
- Persisted in database and included in ARMS Excel exports for sprint retrospectives.

### 5.7 Bulk Actions (Delete & Filtered Export)
- Check multiple checkboxes in the task list.
- Click **Delete Selected** to bulk remove tasks (permissions respected).
- Click **Export Selected** to download an Excel sheet with only the chosen items.

### 5.8 Exporting Tasks to Excel with Period Filter
Export tasks by clicking **Export Excel** with customizable options:
- **Period Filter**: *Today*, *Yesterday*, *Last 7 Days*, *Last 30 Days*, *This Month*, *Last Month*, or *Custom Date Range*.
- **Format Selection**: Standard (9 Columns) or ARMS Enterprise (21 Columns).
- **Project & Priority Filter**: Isolate specific project scopes.

---

## 6. Interactive Kanban Board

Navigate to **Kanban** (`/Task/Kanban`).

### 6.1 Drag & Drop Workflow
- Drag cards smoothly between columns: **Todo** ➡️ **InProgress** ➡️ **Review** ➡️ **Done**.
- Status updates persist instantly to SQLite via AJAX without page reloads.

### 6.2 Mobile Segmented Column Switcher
On smartphones, instead of squishing columns horizontally, a segmented tab switcher (`📋 Todo`, `🔄 In Progress`, `🔍 Review`, `✅ Done`) lets you switch columns with one thumb tap.

---

## 7. Projects & Milestones

Navigate to **Projects** (`/Project`).
- Create and organize multi-month project deliverables with deadlines.
- Track overall aggregate completion percentage calculated from child tasks.
- Associate Waterfall SDLC milestones with individual task items.

---

## 8. Timesheet & Work Sessions

Navigate to **Timesheet** (`/Timesheet`).

### 8.1 Recording Work Time (Live Timer & Manual Entry)
- **Live Timer**: Click the **Play (Start)** button on any assigned task card to start tracking time.
- **Stop Timer**: Click the **Square (Stop)** button to finalize the session, compute duration, and save to SQLite.
- **Manual Session Entry**: Add completed past sessions with custom start time, end time, and session notes.

### 8.2 Concurrent Multi-Timer per User
- Users can run timers on **multiple tasks simultaneously** (e.g. running a long test build on Task A while actively writing code on Task B).
- The REST API endpoint `GET /api/timesheets/active-timers` synchronizes all currently running timers for the logged-in user in real-time.

### 8.3 Exporting Personal Timesheet Report (.xlsx)
Click **Export Timesheet Personal** (`/Timesheet/ExportPersonalExcel`):
- **Sheet 1 ("Timesheet Personal")**: Employee Name, Job Title, Period, detailed table of dates, tasks, durations in hours, and automated `=SUM(...)` formulas.
- **Sheet 2 ("Rekap per Proyek")**: Summary breakdown showing total hours and percentage time contribution per project.
- **Data Privacy**: Non-admin users are strictly restricted to downloading only their own timesheet sessions.

---

## 9. Notes & Documentation (Quill.js Rich-Text)

Navigate to **Notes** (`/Note`).

### 9.1 Creating & Formatting Notes
- Full WYSIWYG rich text editor with headings, bold, italics, code blocks, lists, blockquotes, and tables.
- Choose note categories: *Meeting*, *Technical*, *Architecture*, *UAT*, *Task Note*, or *General*.

### 9.2 Multi-File Attachments in User Directory
- Attach multiple images (PNG, JPG, SVG), PDFs, or documents to any note.
- Files are safely stored and isolated in user-specific paths: `wwwroot/uploads/notes/{username}/`.
- Download or preview attachments with 1 click.

### 9.3 Note Pinning & Standalone vs Task Notes
- **Pinned Notes**: Toggle pin to keep critical reference notes at the top of the dashboard.
- **Linked Notes**: Associate notes directly with a specific task ID for context.

---

## 10. Calendar & Timeline

Navigate to **Calendar** (`/Calendar`).
- Interactive monthly, weekly, and daily timeline view powered by FullCalendar.
- Filter calendar events by project, assignee, or priority.
- Click any task event to open the task details modal directly.

---

## 11. Reports & Analytics (Gantt Chart & Workload)

Navigate to **Reports** (`/Report`).
- **Team Workload Breakdown**: Bar chart showing hours logged and tasks assigned per team member.
- **Status & Priority Distribution**: Doughnut charts displaying workload health.
- **Gantt Chart**: Visual timeline showing project deadlines, start dates, and milestones.

---

## 12. Members & Team Management

Navigate to **Members** (`/Member`).

### 12.1 Member Cards & Contribution Metrics
- View all registered team members, their job titles, avatar badges, and email addresses.
- View total assigned tasks, completed tasks, and total logged timesheet hours.

### 12.2 Admin Functions & Account Management
- Only **Administrators** can create new user accounts, edit roles, or lock/unlock accounts.

### 12.3 Admin Instant Password Reset
- Administrators can directly reset any member's password without needing old credentials or email confirmation.
- Accessible via Web UI button on member detail/edit pages or via REST API:
  `POST /api/members/{id}/reset-password` with `{ "newPassword": "NewPassword123!" }`.

---

## 13. Import & Export Excel Enterprise

Navigate to **Import / Export** (`/Import`).

### 13.1 Standard 9-Column Import with Bulk PIC Reassignment
- Download official Excel template (`/Import/DownloadTemplate`).
- Upload `.xlsx` file to inspect the **Interactive Preview Table**.
- Use **Assign All PIC** to bulk assign all imported tasks to a specific team member before confirming.

### 13.2 ARMS Enterprise 21-Column Export & Import
- Compatible with enterprise banking & enterprise management ARMS specifications.
- Maps 21 columns including SDLC Milestones, Parent IDs, Obstacles, Solutions, and logged hours.

---

## 14. JSON Tools (Developer Utilities)

Navigate to **JSON Tools** (`/JsonTools`).
- **Format / Pretty-Print**: Beautify JSON payloads with 2-space indentation.
- **Minify**: Compress JSON for production payload delivery.
- **Validate**: Instant syntax validation with clear line error highlighting.
- **History Snippets**: Save frequently used JSON payloads into SQLite for quick retrieval.

---

## 15. Audit Trail & Activity Logging

Navigate to **Audit Trail** (`/AuditTrail`).
- Powered by `AuditLogActionFilter` registered globally across all controllers.
- Logs User ID, Email, Controller, Action Name, HTTP Method, URL Path, Status Code, Duration (ms), and Timestamp.
- Filter logs by date range, user, or status code; export audit logs to CSV.

---

## 16. Master Data Management

Navigate to **Master Data** (`/MasterData`) *(Admin Only)*.
- **Priorities**: Customize priority names, colors, icons, and order.
- **Statuses**: Configure workflow statuses and specify which status represents the terminal *Done* state.
- **Milestones**: Manage Waterfall SDLC Milestones (*Requirement*, *Design*, *Implementation*, *Testing*, *Deployment*, *Maintenance*).
- **Categories**: Create and manage work categories.

---

## 17. System Configuration & Database Maintenance

Navigate to **Configuration** (`/Configuration`) *(Admin Only)*.
- **Global Base URL**: Set global URL for API responses, webhooks, and Swagger.
- **Database Capacity & Shrink (VACUUM)**: Reclaim SQLite storage space and defragment database tables.
- **Storage Statistics**: View total uploaded attachment size and counts.

---

## 18. RESTful API, Swagger UI & Postman

Work Tracker Pro includes a complete **OpenAPI 3.0 REST API** accessible at `http://localhost:5000/swagger`.

### API Module Overview (70+ Endpoints)

| API Controller | Route Prefix | Main Features |
| :--- | :--- | :--- |
| **AuthApiController** | `/api/auth` | Login, Logout, Profile, Change Password, Refresh |
| **TasksApiController** | `/api/tasks` | Full CRUD, Status, Kanban feed, Bulk Delete, Sessions |
| **ProjectsApiController** | `/api/projects` | CRUD, Task list per project, Summary metrics |
| **NotesApiController** | `/api/notes` | CRUD, Pin toggle, Multi-file uploads, Categories |
| **TimesheetsApiController** | `/api/timesheets` | CRUD, Start/Stop timer, **Active timers** (`/active-timers`) |
| **MembersApiController** | `/api/members` | List, Details, **Admin Password Reset** (`/{id}/reset-password`) |
| **CalendarApiController** | `/api/calendar` | Event feed with project/assignee filters |
| **ReportsApiController** | `/api/reports` | Dashboard summary, Chart data, Workload, Gantt |
| **MasterDataApiController** | `/api/master-data` | Priorities, Statuses, Milestones, Categories |
| **ConfigurationApiController**| `/api/configuration`| System settings, Base URL, DB Shrink |
| **NotificationsApiController**| `/api/notifications`| User alerts, Mark read, Mark all read |

### Standard JSON Response Structure
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { },
  "errors": null,
  "timestamp": "2026-08-31T09:00:00Z"
}
```

### Postman Files
- `TrackerKerja_Postman_Collection.json`
- `TrackerKerja_Postman_Environment.json`

---

## 19. Role-Based Access Control (RBAC) & Privileges

| Action / Module | Admin | System Analyst | Technical Writer | Regular User |
| :--- | :---: | :---: | :---: | :---: |
| **Dashboard & Kanban** | ✅ | ✅ | ✅ | ✅ |
| **Create Tasks** | ✅ | ✅ | ✅ | ✅ |
| **Edit Own Tasks** | ✅ | ✅ | ✅ | ✅ |
| **Edit Others' Tasks** | ✅ | ✅ | ✅ | ❌ |
| **Start Timer on Others' Tasks** | ✅ | ✅ | ✅ | ❌ |
| **Run Multi-Timer Concurrently**| ✅ | ✅ | ✅ | ✅ |
| **Export Personal Timesheet** | ✅ | ✅ | ✅ | ✅ |
| **Delete Own Tasks** | ✅ | ✅ | ✅ | ✅ |
| **Delete Others' Tasks** | ✅ | ❌ | ❌ | ❌ |
| **Export Tasks (Standard/ARMS)**| ✅ | ✅ | ✅ | ✅ |
| **Create / Edit Projects** | ✅ | ❌ | ❌ | ❌ |
| **Manage Members** | ✅ | ❌ | ❌ | ❌ |
| **Admin Password Reset** | ✅ | ❌ | ❌ | ❌ |
| **Master Data & Config** | ✅ | ❌ | ❌ | ❌ |
| **Audit Trail Logs** | ✅ | ❌ | ❌ | ❌ |

---

## 20. Mobile & Responsive UI Experience

- **Off-Canvas Drawer**: Tap the top hamburger or bottom Menu button to open the full navigation drawer.
- **Glassmorphic Bottom Bar**: Floating bottom navigation bar on mobile with quick action buttons.
- **Safe Area Inset Support**: Designed with CSS environment variables (`env(safe-area-inset-bottom)`) for modern notch and bezel-less displays.

---

## 21. User Account & Profile Settings

Navigate to **Profile** (`/Account/Profile`):
- Update Full Name, Job Title, Phone Number, and Avatar Color.
- Upload a custom Profile Picture (stored in `wwwroot/uploads/avatars/`).
- Change password with confirmation.

---

## 22. Tips & Best Practices

1. **Use Select2 Searchable Dropdowns**: Type keywords to quickly select Projects, Assignees, or Parent Tasks.
2. **Use Obstacle & Solution Fields**: Document technical hurdles and how they were solved for team retrospectives.
3. **Regular SQLite Shrink**: Run **Configuration > Shrink Database** monthly to keep database file size compact.
4. **Multi-Timer Usage**: Run independent timers across concurrent tasks without worrying about session collisions.
5. **Periodic Timesheet Exports**: Export personal timesheet Excel reports every Friday for billing and reporting.

---

## 23. Troubleshooting & FAQ

| Problem | Cause & Solution |
| :--- | :--- |
| **Docker Port 5000 Already in Use** | Change port mapping in `docker-compose.yml` to `"5050:5000"`, then run `docker compose up -d`. |
| **File Upload Fails** | Ensure the upload file is under 10MB and permissions on `./uploads/` directory allow write access. |
| **Access Denied (403)** | Your user role or job title lacks permission for this action. Contact an Administrator. |
| **Swagger UI Page Blank** | Ensure `TrackerKerja.xml` XML documentation file is present alongside the build binary. |
| **Forgot Password** | Ask an Administrator to use the **Reset Password** button in the Members module. |
| **Container Database Reset** | Ensure your Docker Compose volume is mounted to `./db_data:/app/data` to persist data. |

---

## Appendix A – Data Model Quick Reference

- **WorkTask**: `Id`, `Title`, `Description`, `ProjectId`, `CategoryId`, `AssignedToUserId`, `ParentTaskId`, `Priority`, `Status`, `Progress`, `Obstacle`, `Solution`, `StartDate`, `DueDate`, `Milestone`, `Tags`
- **Project**: `Id`, `Name`, `Description`, `Color`, `Status`, `Deadline`, `CreatedAt`
- **WorkSession**: `Id`, `TaskId`, `UserId`, `StartTime`, `EndTime`, `Duration`, `Notes`
- **WorkNote**: `Id`, `Title`, `ContentHtml`, `Category`, `Color`, `IsPinned`, `AuthorUserId`, `TaskId`, `CreatedAt`, `UpdatedAt`
- **NoteAttachment**: `Id`, `NoteId`, `FileName`, `FilePath`, `FileSize`, `ContentType`, `FileExtension`, `UploadedAt`, `UploadedByUserId`
- **AppUser**: `Id`, `FullName`, `Email`, `JobTitle`, `AvatarColor`, `ProfilePictureUrl`, `CreatedAt`
- **AuditLog**: `Id`, `UserId`, `UserEmail`, `ControllerName`, `ActionName`, `HttpMethod`, `Path`, `StatusCode`, `DurationMs`, `Timestamp`
- **MasterMilestone**: `Id`, `Name`, `Phase`, `Color`, `Icon`, `OrderIndex`, `Description`, `IsDefault`
- **MasterPriority**: `Id`, `Name`, `Color`, `Icon`, `OrderIndex`, `Description`, `IsDefault`
- **MasterStatus**: `Id`, `Name`, `Color`, `IsDoneState`, `OrderIndex`, `Description`, `IsDefault`

---

## Appendix B – ARMS Export Format (21 Columns)

1. `No` | 2. `Task ID` | 3. `Parent Task ID` | 4. `Module / Sub-Module` | 5. `Task Name` | 6. `PIC (Name)` | 7. `PIC (Email)` | 8. `Category` | 9. `Priority` | 10. `Status` | 11. `Progress (%)` | 12. `Milestone SDLC` | 13. `Obstacle / Kendala` | 14. `Solution / Solusi` | 15. `Tags` | 16. `Project Name` | 17. `Start Date` | 18. `End Date (Due Date)` | 19. `Total Sessions` | 20. `Total Duration (Hours)` | 21. `Created At`

---

## Appendix C – Export Period Filter Parameters

| `period` Value | Date Range Resolved |
| :--- | :--- |
| `today` | Current calendar day from 00:00 to 23:59 |
| `yesterday` | Previous full calendar day |
| `last7days` | Last 7 calendar days from today |
| `last30days` | Last 30 calendar days from today |
| `this_month` | From the 1st of current month to current date |
| `last_month` | Full previous calendar month |
| *(custom)* | Provide `startDate=YYYY-MM-DD` and `endDate=YYYY-MM-DD` |

---

*Work Tracker Pro User Guide — Version 3.1 | August 31, 2026 | TrackerKerja Engineering Team*
