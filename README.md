# Notes — a To-Do board

A Microsoft To-Do style task manager where tasks are sticky notes on a three-column
board. Cards can be dragged between columns and reordered within one, the board stays in
step across every window you have open, and sign-in goes through Google OAuth 2.0.

- **Backend** — REST API on ASP.NET Core 9 with EF Core (code first) against MS SQL Server
- **Frontend** — Angular 21 with Bootstrap 5, Angular CDK drag-and-drop, and SignalR

---

## Architecture

Four layers, one project each, with dependencies pointing inwards:

```
ToDoApp.Server        Controllers, SignalR hub, auth pipeline, DI composition root
      |                       (depends on Services, Interfaces, DataAccess)
      v
ToDoApp.Services      Business logic: reordering, paging, sign-in, validation
      |                       (depends on Interfaces only)
      v
ToDoApp.Interfaces    Contracts: entities, DTOs, repository and service interfaces
      ^                       (depends on nothing)
      |
ToDoApp.DataAccess    EF Core DbContext, entity configuration, repositories, migrations
                              (depends on Interfaces only)
```

`ToDoApp.Interfaces` is the only project the other three share, and it references nothing
itself. That is what keeps the graph acyclic while still letting `DataAccess` implement
interfaces that `Services` consumes: both sides depend on the contract, neither on the other.

Two consequences worth knowing:

- **The service layer never sees EF Core or HTTP.** It talks to `IRepository`/`IUnitOfWork`
  and to `ICurrentUser` and `IBoardNotifier`, whose implementations live in the API layer.
- **Realtime is an outbound port.** `IBoardNotifier` is declared in `Interfaces`, called from
  `Services`, and implemented over SignalR in `Server`. Swapping the transport touches one class.

---

## Getting started

### Prerequisites

- .NET SDK 9.0
- Node.js 20+ and npm
- SQL Server — the default connection string uses **LocalDB**
  (`(localdb)\MSSQLLocalDB`), which ships with Visual Studio

### Run it

From the solution root:

```bash
dotnet run --project ToDoApp.Server --launch-profile https
```

```bash
cd ToDoApp.Client && npm install && npm start
```

Then open <http://localhost:4200>.

**One-time setup: the JWT signing key.** It is deliberately not in `appsettings.json`, so
it can never be committed. Store it in user secrets once per machine (any random string of
32+ characters):

```bash
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project ToDoApp.Server
```

On Windows without `openssl`, paste any 32+ character string. Without this the API refuses
to start and prints exactly this instruction.

In Visual Studio, set **ToDoApp.Server** as the startup project and press F5 for the API;
run the client from a terminal with `npm start`.

The API listens on `https://localhost:7255` and `http://localhost:5090`. The Angular dev
server proxies `/api` and `/hubs` to the HTTP endpoint (see `ToDoApp.Client/proxy.conf.json`),
so the browser only ever talks to `localhost:4200` and there is no CORS or dev-certificate
step to get through.

**The database creates itself.** In Development the API applies any pending migrations at
startup, so the first run creates the `ToDoApp` database. To do it by hand instead:

```bash
dotnet ef database update --project ToDoApp.DataAccess --startup-project ToDoApp.Server
```

Swagger UI, with an **Authorize** button for pasting an access token, is at
<https://localhost:7255/swagger>.

### Signing in

Out of the box no Google client id is configured, so the sign-in screen offers a
**Development-only local sign-in** instead: enter any email address and you get a board of
your own. Coming back to the same address reopens the same board. This endpoint is refused
outside the Development environment, and is never advertised by `/api/auth/config` in any
other environment.

To turn on the real thing:

1. In the [Google Cloud console](https://console.cloud.google.com/apis/credentials), create
   an **OAuth 2.0 Client ID** of type *Web application*.
2. Add `http://localhost:4200` to **Authorised JavaScript origins**. (Google Identity
   Services returns the ID token to the page, so no redirect URI is needed.)
3. Put the client id in configuration — user secrets keep it out of source control:

   ```bash
   dotnet user-secrets set "Authentication:Google:ClientId" "<your-client-id>" --project ToDoApp.Server
   ```

4. Restart the API. The sign-in screen picks it up from `/api/auth/config` and renders
   Google's button.

Once a client id is set you can turn the local shortcut off with
`"Authentication:Google:AllowDevLogin": false` in `appsettings.Development.json`.

**How the flow works.** Google Identity Services signs the user in inside the browser and
hands the page an ID token. The page posts that to `POST /api/auth/google`, where
`Google.Apis.Auth` verifies its signature, issuer, expiry and audience against the
configured client id. On a first sign-in the account is created (with three starter
categories); on every sign-in the profile is refreshed from Google. The API then issues its
own short-lived JWT, which is what every later request and the SignalR connection carry.
Signing out discards that token and drops the hub connection.

### Configuration

| Key | Meaning |
| --- | --- |
| `ConnectionStrings:ToDoDatabase` | MS SQL connection string. Defaults to LocalDB. |
| `Jwt:SigningKey` | HMAC-SHA256 key for the access tokens. **At least 32 characters.** |
| `Jwt:Issuer` / `Jwt:Audience` | Token issuer and audience. |
| `Jwt:AccessTokenLifetimeMinutes` | Access token lifetime. Defaults to 480. |
| `Authentication:Google:ClientId` | OAuth 2.0 client id. Empty disables Google sign-in. |
| `Authentication:Google:AllowDevLogin` | Enables the local sign-in shortcut. Development only. |
| `Cors:AllowedOrigins` | Origins allowed to call the API directly, bypassing the proxy. |

> `appsettings.json` stays in source control on purpose: everything in it is non-secret
> configuration the app needs to run. The one secret, `Jwt:SigningKey`, is blank there and
> comes from user secrets in development or an environment variable (`Jwt__SigningKey`) /
> secret store elsewhere. Treat `Authentication:Google:ClientId` the same way — it is not a
> secret, but it is yours, and it does not belong in a shared repository.

---

## API

Everything except the three auth endpoints marked below requires
`Authorization: Bearer <token>`. Mutating requests may also send `X-Connection-Id` with the
caller's SignalR connection id, which excludes that window from the broadcast it triggers.

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/auth/config` | *Anonymous.* Which sign-in methods are available. |
| `POST` | `/api/auth/google` | *Anonymous.* Exchange a Google ID token for an access token. |
| `POST` | `/api/auth/dev-login` | *Anonymous, Development only.* Local sign-in. |
| `GET` | `/api/auth/me` | The signed-in profile. |
| `POST` | `/api/auth/logout` | End the session. |
| `GET` | `/api/tasks` | Paged, searchable, filterable list. `search`, `categoryId`, `state`, `page`, `pageSize`. |
| `GET` | `/api/tasks/board` | All three columns at once, paged independently. |
| `GET` | `/api/tasks/{id}` | One task. |
| `POST` | `/api/tasks` | Create. |
| `PUT` | `/api/tasks/{id}` | Update. |
| `POST` | `/api/tasks/{id}/move` | Apply a drop: `targetState` + `targetIndex`. |
| `DELETE` | `/api/tasks/{id}` | Delete. |
| `GET` | `/api/categories` | Categories with task counts. |
| `POST` | `/api/categories` | Create. |
| `PUT` | `/api/categories/{id}` | Rename or recolour. |
| `DELETE` | `/api/categories/{id}` | Delete; its tasks survive, uncategorised. |

Failures come back as RFC 7807 problem documents: `404` for a missing task, `409` for a
duplicate category name, `400` for validation, `401` for a rejected sign-in.

**Hub:** `/hubs/board`, method `boardChanged`. Since WebSockets cannot carry an
`Authorization` header, the hub route also accepts the token as an `access_token` query
parameter.

---

## How the interesting parts work

### Drag-and-drop ordering

Each task carries an integer `Position` that is kept **dense** — 0, 1, 2, … — within its
user and column. A drop sends the target column plus the index the card landed at, and the
server lifts the card out, inserts it at that index, and renumbers the affected columns
inside a transaction. Dense positions are what let the client address a drop target by
index at all, and they keep an out-of-range index meaningful: it just clamps to the end.

The client sends an **absolute** index, not one relative to the visible page: it adds the
current page offset before sending. Dragging a card to the top of page 2 puts it at
position 6 of the column, not position 0.

### Pagination

Each of the three columns pages independently, and the search text and category filter
apply to all of them. `GET /api/tasks/board` takes `toDoPage`, `doingPage`, `donePage` and a
shared `pageSize`, so one request refreshes the whole board.

If a column's last page empties out — you deleted its final card, or dragged it away, or
narrowed a filter — the client notices that its page number is now past the end and steps
back rather than showing an empty column.

### Synchronising across windows

Every window opens a SignalR connection and joins a group named after the account. After a
successful write, the server broadcasts to that group what changed — created, updated,
moved, deleted — but deliberately **not** the new board contents, and skips the window that
made the change (that one already applied it locally, so it never flickers).

Sending the change rather than the state is the point: every window has its own search
text, category filter and per-column page, so there is no single "new board" to send. Each
listener re-queries with its own view state instead. Two windows on different pages of the
same column both stay correct.

If the hub connection drops, the indicator in the top bar turns from **Live** to
**Offline**. The board keeps working; it just stops updating on its own.

### Categories

Categories belong to a user and their names are unique per user, not globally. Deleting one
keeps its tasks and clears their category — which is also why the foreign key is mapped
`ClientSetNull` rather than `SetNull`: a database-level `ON DELETE SET NULL` there would
give SQL Server two cascade paths from `Users` (directly to `Tasks`, and again via
`Categories`) and it refuses to create the schema. The service detaches the tasks itself,
in one statement, inside the same transaction as the delete.

---

## Project layout

```
ToDoApp.Interfaces/
  Entities/          User, Category, TaskItem, TaskState
  Dtos/              Requests, responses, PagedResult<T>, TaskQuery, BoardQuery, BoardChange
  Repositories/      IRepository<T>, ITaskRepository, ICategoryRepository, IUserRepository, IUnitOfWork
  Services/          ITaskService, ICategoryService, IAuthService, ITokenService,
                     IGoogleTokenValidator, IBoardNotifier, ICurrentUser
  Exceptions/        NotFoundException, ConflictException, BusinessRuleException, ...

ToDoApp.DataAccess/
  ToDoDbContext.cs
  Configurations/    Fluent entity configuration
  Repositories/      EF Core implementations + UnitOfWork
  Migrations/
  DependencyInjection.cs    AddDataAccess()

ToDoApp.Services/
  TaskService.cs     Board queries, CRUD, and the reorder/relocate logic
  CategoryService.cs
  Auth/              AuthService, JwtTokenService, GoogleTokenValidator
  Mapping/           Entity -> DTO projections
  Options/           JwtOptions, GoogleAuthOptions
  DependencyInjection.cs    AddBusinessServices()

ToDoApp.Server/
  Controllers/       TasksController, CategoriesController, AuthController
  Hubs/BoardHub.cs
  Infrastructure/    CurrentUser, SignalRBoardNotifier, ExceptionHandlingMiddleware
  Program.cs

ToDoApp.Client/src/app/
  core/              models, services (auth, task, category, realtime), interceptor, guards
  features/login/    Sign-in screen
  features/board/    BoardStore + board, column, card, task dialog, category dialog
  shared/            Paginator
```

The Angular app is standalone-components and **zoneless**, so all view state is held in
signals. `BoardStore` is provided by `BoardComponent` and owns the whole board: filters,
per-column pages, the loaded cards, and every mutation. The toolbar, the columns and the
dialogs all read and write that one instance.

## Working with migrations

```bash
dotnet ef migrations add <Name> --project ToDoApp.DataAccess --startup-project ToDoApp.Server
dotnet ef database update       --project ToDoApp.DataAccess --startup-project ToDoApp.Server
```

In the Visual Studio Package Manager Console, set *Default project* to **ToDoApp.DataAccess**
and the startup project to **ToDoApp.Server**.
