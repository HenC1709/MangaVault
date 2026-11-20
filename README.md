## Proyecto desarrollado originalmente por mí y posteriormente refactorizado con asistencia de IA

# 📚 MangaVault

Sistema de gestión personal de **mangas y novelas ligeras** construido en **C# (.NET 10 / C# 14)** siguiendo **Clean Architecture** en una solución multi-proyecto.

---

## 📖 Documentación del proyecto

- [Visión WPF](doc/01-vision-wpf.md) — arquitectura MVVM, pantallas y publicación sin IDE.
- [Visión SQL](doc/01-vision-sql.md) — modelo relacional en SQLite y migración desde JSON.
- [Errores detectados](doc/02-errores.md) — auditoría técnica (29/29 ítems resueltos ✅).
- [Plan de 5 MVP](doc/03-mvp-5.md) — hoja de ruta paso a paso hasta el ejecutable independiente.
- [Progreso actual](doc/progress.md) — resumen detallado de lo construido y siguientes pasos.

---

## 🏗️ Arquitectura

La solución (`MangaVault.slnx`) separa las responsabilidades en 7 proyectos donde las capas internas no dependen de las externas:

```
Core                  ← Dominio puro (sin dependencias de proyecto)
Application           ← Casos de uso, DTOs, mappers y validadores (→ Core)
Infrastructure        ← Persistencia SQLite/JSON, PBKDF2 y logging (→ Core)
Presentation.Cli      ← Menús de consola y Composition Root (→ Core, Application, Infrastructure)
MangaVault.Wpf        ← Aplicación de escritorio WPF con MVVM y DI (→ Core, Application, Infrastructure)
Tests                 ← Suite de 29 pruebas unitarias y de integración SQLite xUnit (→ Core, Application, Infrastructure)
Tests.Wpf             ← Suite de 12 pruebas de ViewModels WPF + SQLite xUnit (→ Core, Application, Infrastructure, MangaVault.Wpf)
```

---

## 📁 Estructura actual

```
MangaVault/
├── Core/
│   ├── MangaVault.Core.csproj
│   ├── Constants/         (ValidationRules, ErrorMessages)
│   ├── Entities/          (User, Manga, Genre, MangaEntry, Character, Collection, Session)
│   ├── Enums/             (Role, MangaStatus, ReadingStatus, MediaType)
│   ├── Exceptions/        (DuplicateUserException, UserNotFoundException, DuplicateMangaException, MangaNotFoundException, DuplicateFavoriteException)
│   ├── Interfaces/        (IUserRepository, IMangaRepository, IFavoriteRepository, IGenreRepository, IPasswordHasher)
│   └── ValueObjects/      (Email, RatingValue)
├── Application/
│   ├── MangaVault.Application.csproj
│   ├── DTOs/              (CreateUserDto, CreateMangaDto)
│   ├── Mappers/           (UserMapper, MangaMapper)
│   ├── Services/          (UserService, MangaService, FavoriteService)
│   └── Validators/        (UserValidator, MangaValidator)
├── Infrastructure/
│   ├── MangaVault.Infrastructure.csproj
│   ├── Logging/           (ConsoleLogger)
│   ├── Persistence/
│   │   ├── Json/          (JsonFileStore, JsonUserRepository, JsonMangaRepository, JsonFavoriteRepository)
│   │   └── Sqlite/        (SqliteDatabase, SqliteSchemaInitializer, SqliteErrorCodes, SqliteUserRepository, SqliteMangaRepository, SqliteFavoriteRepository, SqliteGenreRepository, JsonToSqliteImporter)
│   └── Security/          (Pbkdf2PasswordHasher)
├── Presentation.Cli/
│   ├── MangaVault.Presentation.Cli.csproj
│   ├── Menus/             (MainMenu, MangaMenu, UserMenu)
│   └── Program.cs         (Composition Root)
├── MangaVault.Wpf/
│   ├── MangaVault.Wpf.csproj
│   ├── app.manifest       (DPI PerMonitorV2)
│   ├── App.xaml(.cs)      (Composition Root WPF, DataTemplates y manejo global de errores)
│   ├── Converters/        (Converters.cs)
│   ├── Resources/
│   │   ├── Themes/        (DarkTheme.xaml)
│   │   └── Styles/        (Controls.xaml)
│   ├── Services/          (AppEnvironment, FileAppLogger, ThreadPoolBackgroundExecutor, DialogService, NotificationService, PageFactory, ServiceRegistration, SessionService, UserMessageTranslator)
│   ├── ViewModels/        (PageViewModel, AuthViewModel, MainWindowViewModel, DashboardViewModel, LibraryViewModel, FavoritesViewModel, SettingsViewModel, Models/UiModels.cs)
│   └── Views/             (MainWindow, AuthView, DashboardView, LibraryView, FavoritesView, SettingsView, ConfirmDialog)
├── Tests/
│   ├── MangaVault.Tests.csproj
│   ├── UserServiceTests.cs
│   ├── MangaServiceTests.cs
│   ├── FavoriteServiceTests.cs
│   ├── DomainAndSecurityTests.cs
│   └── SqlitePersistenceTests.cs
├── Tests.Wpf/
│   ├── MangaVault.Wpf.Tests.csproj
│   ├── TestDoubles.cs
│   ├── AuthAndNavigationTests.cs
│   ├── LibraryAndFavoritesViewModelTests.cs
│   └── DashboardAndSettingsViewModelTests.cs
├── doc/                   (Visión WPF/SQL, errores y plan de 5 MVP)
├── Directory.Build.props  (C# 14, Nullable, ImplicitUsings, Deterministic)
├── .editorconfig          (Reglas de estilo y formato)
├── LICENSE                (MIT)
└── MangaVault.slnx
```

---

## 🧩 Dominio principal

- **`User`**: identidad y rol (`Role`), creación/verificación/cambio de contraseña vía `IPasswordHasher`.
- **`Manga`**: soporta **mangas y novelas ligeras** (`MediaType.Manga` / `MediaType.LightNovel`), `Title` (editable con `SetTitle`/`UpdateDetails`), `Mangaka`, `Illustrator`, `VolumeCount`, `FinalChapters`, `Status`, `CoverPath`, `Synopsis` y `Genres`.
- **`MangaEntry`**: progreso de lectura por usuario (`CurrentChapter`, `ReadingStatus`, `OwnedVolumes`, `Rating` 1–10, `IsFavorite`, `DateAdded` en UTC).
- **Otros**: `Genre`, `Session`, `Collection`, `Character`, y los Value Objects `Email` y `RatingValue` (`readonly record struct`).

---

## 🗄️ Persistencia y seguridad

- **Base de datos SQLite (`%LOCALAPPDATA%\MangaVault\mangavault.db`):** motor `Microsoft.Data.Sqlite 10.0.0` con consultas SQL parametrizadas, inicialización automática e idempotente de esquema (`Users`, `Works`, `Genres`, `WorkGenres`, `UserWorks`, `Collections`, `CollectionWorks`, índices y `PRAGMA foreign_keys = ON`), modo `WAL` en disco y seed automático de 20 géneros literarios.
- **Migración desde JSON:** `JsonToSqliteImporter` permite importar de forma idempotente datos heredados (`users.json`, `mangas.json`, `favorites.json`) desde consola o desde **Configuración** en WPF; los repositorios JSON atómicos (`JsonFileStore<T>`) permanecen disponibles como soporte de migración y pruebas.
- **Registros de escritorio (`FileAppLogger`):** la aplicación WPF guarda logs diarios con timestamp UTC en `%LOCALAPPDATA%\MangaVault\logs\`.
- **Integridad:** comparación case-insensitive (`COLLATE NOCASE` / `OrdinalIgnoreCase`), guardado transaccional de obras y géneros, cascadas `ON DELETE CASCADE` y excepciones explícitas ante duplicados o registros inexistentes.
- **Contraseñas (`Pbkdf2PasswordHasher`):** PBKDF2-SHA256 (`100_000` iteraciones, salt aleatorio de 16 bytes) y verificación en tiempo constante (`CryptographicOperations.FixedTimeEquals`).

---

## ▶️ Compilación, pruebas y ejecución

```powershell
dotnet build MangaVault.slnx
dotnet test MangaVault.slnx
dotnet run --project MangaVault.Wpf/MangaVault.Wpf.csproj
dotnet run --project Presentation.Cli/MangaVault.Presentation.Cli.csproj
```

---

## 🛣️ Progreso

### ✅ Hecho (MVP 1 + MVP 2 + MVP 3 + MVP 4)
- Auditoría completa de [doc/02-errores.md](doc/02-errores.md) cerrada (29/29 ítems resueltos).
- Solución multi-proyecto `MangaVault.slnx` (`Core`, `Application`, `Infrastructure`, `Presentation.Cli`, `MangaVault.Wpf`, `Tests`, `Tests.Wpf`).
- Capa `Application` completa: `UserService` (registro/login PBKDF2/cambio de clave), `MangaService` (CRUD + búsqueda por título/tipo), `FavoriteService` (biblioteca, progreso, favoritos, calificación y tomos poseídos), DTOs, validadores y mappers.
- Persistencia **SQLite** (`SqliteDatabase`, `SqliteSchemaInitializer`, repositorios `Sqlite*`, 20 géneros iniciales e importador idempotente desde JSON).
- Interfaz de consola modular (`Presentation.Cli`) e interfaz de escritorio **WPF** (`MangaVault.Wpf`: `AuthView`, `Dashboard`, `Biblioteca`, `Favoritos` y `Configuración`) con MVVM (`CommunityToolkit.Mvvm`), DI (`Microsoft.Extensions.DependencyInjection`) y tema oscuro.
- Suites `Tests` y `Tests.Wpf` (xUnit) con **41 pruebas unitarias y de integración en verde** (`29` + `12`).

### 🔨 Siguiente (MVP 5)
Ver detalle en [doc/03-mvp-5.md](doc/03-mvp-5.md):
- **MVP 5:** Publicación de `MangaVault.exe` independiente sin IDE (`dotnet publish`, acceso directo y exportación/backup de `.db`).

---

## 🛠️ Tecnologías

- **Lenguaje / Runtime:** C# 14 / .NET 10 (`net10.0` / `net10.0-windows`)
- **UI de escritorio / Consola:** WPF + `CommunityToolkit.Mvvm 8.4.0` + `Microsoft.Extensions.DependencyInjection 10.0.0` / `Presentation.Cli`
- **Persistencia:** SQLite (`Microsoft.Data.Sqlite 10.0.0`) + importación desde JSON (`System.Text.Json`)
- **Pruebas:** xUnit 2.9.3
- **Arquitectura:** Clean Architecture
