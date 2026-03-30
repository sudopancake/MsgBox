# MsgBox

MsgBox is a small **ASP.NET Core** web app that presents a **chat-style UI** for viewing and managing messages: threads, reactions, themes, file uploads, and bulk JSON import. The server stores data in **LiteDB** (`Data/msgbox.db` by default) and exposes REST APIs under `/api`. The interactive UI is a **React** app (compiled in the browser with **Babel standalone**) with **Bootstrap 5**. React, React DOM, and Babel are **vendored** under `wwwroot/lib/` so the page does not load those runtimes from a CDN.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run

From the project directory:

```bash
dotnet run
```

Open the URL shown in the console (typically `https://localhost:#####`).

## Configuration: `RunSeedDemoData`

In `appsettings.json`, under the **`Database`** section:

| Setting | Default | Purpose |
|--------|---------|---------|
| **`RunSeedDemoData`** | **`false`** | When **`true`**, the **`SeedDemoData`** migration is registered and can run on startup. It only inserts the demo people, chat, and messages if the **`people`** collection is **empty** (so existing databases are not duplicated). When **`false`**, that migration is **not** registered; you get an empty database until you add data (import, API, etc.). |

Example:

```json
"Database": {
  "RunSeedDemoData": false
}
```

Set it to **`true`** in `appsettings.Development.json`, environment-specific config, or user secrets if you want the built-in John/Jane sample thread on a **new** database.

Migrations that **do** run are recorded in the LiteDB collection **`schema_migrations`**.

## Project layout (high level)

- **`Program.cs`** — startup, DI, database init (filesystem, indexes, migrations)
- **`Controllers/`** — Web API
- **`Data/`** — LiteDB context, models, repositories, **`Migrations/`**
- **`Pages/`** — Razor host page and layout
- **`wwwroot/`** — static assets, React app (`js/msgbox-app.jsx`), vendored React/Babel (`lib/react/`, `lib/babel/`), CSS, uploads (at runtime)
