# Progreso del proyecto – MangaVault

Última actualización: **2026-11-19** · Stack actual: **.NET 10 / C# 14** · Arquitectura: **Clean Architecture (`MangaVault.slnx`)**

---

## 📊 Resumen de avance

| Bloque / MVP | Estado | Progreso | Detalle |
|---|---|---|---|
| Auditoría técnica (`02-errores.md`) | ✅ Completado | `29 / 29` | Críticos, lógica/datos, estructura/convenciones y repo |
| **MVP 1** – Núcleo + persistencia consola | ✅ Completado | `5 / 5` | Arranque, `[JsonInclude]`, `%LOCALAPPDATA%`, Ids/excepciones, `MediaType` |
| **MVP 2** – Cuentas + biblioteca personal | ✅ Completado | `5 / 5` | `UserService`, `MangaService`, `FavoriteService`, menús CLI, 21 tests |
| **MVP 3** – Base de datos SQLite | ✅ Completado | `4 / 4` | `Microsoft.Data.Sqlite 10.0.0`, DDL al arranque, repos SQLite, seed + importador JSON |
| **MVP 4** – Interfaz de escritorio WPF | ✅ Completado | `4 / 4` | `MangaVault.Wpf` (MVVM + DI + tema oscuro), 4 secciones funcionales, 12 tests WPF |
| **MVP 5** – Ejecutable independiente (`.exe`) | ⬜ Pendiente | `0 / 6` | `dotnet publish` single-file, acceso directo y uso sin IDE |

---

## ✅ Lo que ya está construido y verificado

### 1. Arquitectura y configuración (`MangaVault.slnx`)
- **7 proyectos desacoplados**:
  - `Core/MangaVault.Core.csproj` (dominio puro sin referencias externas)
  - `Application/MangaVault.Application.csproj` (`→ Core`)
  - `Infrastructure/MangaVault.Infrastructure.csproj` (`→ Core`, paquete `Microsoft.Data.Sqlite 10.0.0`)
  - `Presentation.Cli/MangaVault.Presentation.Cli.csproj` (`→ Core, Application, Infrastructure`, ensamblado `MangaVault`)
  - `MangaVault.Wpf/MangaVault.Wpf.csproj` (`net10.0-windows`, `UseWPF=true`, `→ Core, Application, Infrastructure`, paquetes `CommunityToolkit.Mvvm 8.4.0` y `Microsoft.Extensions.DependencyInjection 10.0.0`)
  - `Tests/MangaVault.Tests.csproj` (`→ Core, Application, Infrastructure`, xUnit `2.9.3`)
  - `Tests.Wpf/MangaVault.Wpf.Tests.csproj` (`net10.0-windows`, `UseWPF=true`, `→ Core, Application, Infrastructure, MangaVault.Wpf`, xUnit `2.9.3`)
- **Configuración compartida**: `Directory.Build.props` (`LangVersion 14`, `Nullable`, `ImplicitUsings`, `Deterministic`), `.editorconfig`, `.gitignore`, `LICENSE` (MIT) y `.vscode/launch.json` + `tasks.json`.

### 2. Dominio y seguridad (`Core` + `Infrastructure/Security`)
- **Mangas y novelas ligeras**: enum `MediaType` (`Manga`, `LightNovel`), entidad `Manga` (con `field` de C# 14, `Title` editable con `SetTitle`/`UpdateDetails`, `Illustrator`, `CoverPath`, `Synopsis`, `Genres` con `AddGenre`/`RemoveGenre`) y `MangaEntry` (`CurrentChapter`, `ReadingStatus`, `OwnedVolumes`, `Rating` 1–10, `IsFavorite`, `DateAdded` en UTC).
- **Otros tipos de dominio**: `User` (`CreateWithPassword`, `VerifyPassword`, `ChangePassword`, `AssignId`), `Genre`, `Session`, `Collection`, `Character`, y los Value Objects `Email` y `RatingValue` (`readonly record struct`).
- **Contratos, constantes y excepciones**: `IUserRepository`, `IMangaRepository`, `IFavoriteRepository`, `IGenreRepository`, `IPasswordHasher`, `ValidationRules`, `ErrorMessages`, `DuplicateUserException`, `UserNotFoundException`, `DuplicateMangaException`, `MangaNotFoundException`, `DuplicateFavoriteException`.
- **Seguridad**: `Pbkdf2PasswordHasher` con `PBKDF2-SHA256` (`100_000` iteraciones, salt aleatorio de 16 bytes) y comparación en tiempo constante (`CryptographicOperations.FixedTimeEquals`).

### 3. Persistencia SQLite, migración JSON y aplicación (`Infrastructure` + `Application`)
- **Motor SQLite (`Infrastructure/Persistence/Sqlite/`)**:
  - `SqliteDatabase`: gestión de conexiones en `%LOCALAPPDATA%\MangaVault\mangavault.db` (con soporte `:memory:` compartido para tests, limpieza de pool en `Dispose()` y `PRAGMA foreign_keys = ON` en cada conexión).
  - `SqliteSchemaInitializer`: inicialización transaccional idempotente (`CREATE TABLE IF NOT EXISTS`, modo `WAL` en disco) con **7 tablas** (`Users`, `Works`, `Genres`, `WorkGenres`, `UserWorks`, `Collections`, `CollectionWorks`), **2 índices** (`IX_Works_Title`, `IX_UserWorks_Status`) y **20 géneros literarios iniciales** (`DefaultGenres`).
  - **Repositorios SQLite con consultas parametrizadas**: `SqliteUserRepository`, `SqliteMangaRepository` (transacciones obra + géneros y consultas sin N+1), `SqliteFavoriteRepository` (tabla `UserWorks` con `IsFavorite`, manejo de claves foráneas/duplicados y fechas ISO-8601 UTC) y `SqliteGenreRepository`.
  - **Migración opcional desde JSON**: `JsonToSqliteImporter` importa de forma idempotente `users.json`, `mangas.json` y `favorites.json` conservando hashes, géneros y progreso.
- **Almacenamiento JSON heredado (`Infrastructure/Persistence/Json/`)**: `JsonFileStore<T>` con sincronización `System.Threading.Lock`, escritura atómica `.tmp` y repositorios JSON conservados como fuente de migración/pruebas.
- **Logging**: `ConsoleLogger` (consola) y `FileAppLogger` (escritorio WPF en `%LOCALAPPDATA%\MangaVault\logs\`) con niveles y timestamp UTC.
- **Capa Application**:
  - DTOs: `CreateUserDto`, `CreateMangaDto`
  - Validadores: `UserValidator`, `MangaValidator`
  - Mappers: `UserMapper`, `MangaMapper`
  - Servicios: `UserService` (registro, login, cambio de contraseña), `MangaService` (CRUD, búsqueda por título/tipo), `FavoriteService` (biblioteca personal, progreso validado contra `FinalChapters`, favoritos, calificación y tomos poseídos).

### 4. Interfaces (`Presentation.Cli` + `MangaVault.Wpf`) y pruebas (`Tests` + `Tests.Wpf`)
- **Consola (`Presentation.Cli`)**: `MainMenu`, `UserMenu`, `MangaMenu` y Composition Root en `Presentation.Cli/Program.cs` conectado a SQLite.
- **Escritorio WPF (`MangaVault.Wpf`)**:
  - **MVVM + DI**: `App.xaml.cs` configura `ServiceCollection` (`ServiceRegistration.AddMangaVaultServices`), inicializa `SqliteDatabase`, maneja excepciones globales y gestiona vistas vía `DataTemplate`s en `App.xaml`.
  - **Sistema visual oscuro**: `Resources/Themes/DarkTheme.xaml` (paleta oficial `#0B0D12` / `#11131B` / `#171923` / `#242735` / `#8B5CF6` / `#22D3EE`) y estilos nativos reutilizables en `Resources/Styles/Controls.xaml` + `app.manifest` PerMonitorV2.
  - **Pantallas funcionales**: `AuthView` (Login y Registro como `Role.User`, con `IsNotBusy` en controles), `MainWindow` (Shell con barra lateral, encabezado e indicador de sesión + toast de notificaciones), `DashboardView` (KPIs reales, desglose por estado y favoritos recientes), `LibraryView` (catálogo, búsqueda, filtros por tipo/estado/género, modal Crear/Editar con selector de portada y chips de género, seguimiento personal y eliminación confirmada), `FavoritesView` (filtro por estado, `+1 cap`, edición de progreso y quitar favorito) y `SettingsView` (sesión, cambio de contraseña, rutas SQLite, importación JSON y listado solo para `Role.Admin`).
  - **Pulido post-MVP 4**: `FilterOption<T>.ToString()` + `ContentTemplateSelector` en `DarkComboBoxStyle` para mostrar etiquetas limpias en los desplegables cerrados, y binding directo `IsNotBusy` en `AuthView`.
- **Suites xUnit (`Tests/` + `Tests.Wpf/`)**: **41/41 pruebas superadas** (`29/29` en `MangaVault.Tests.dll` + `12/12` en `MangaVault.Wpf.Tests.dll`).

### 5. Checklist completado de MVP 3 y MVP 4
- [x] SQLite completo (`Microsoft.Data.Sqlite 10.0.0`, esquema al arranque, repositorios SQLite, seed de géneros, importador JSON y tests).
- [x] Proyecto `MangaVault.Wpf` (`net10.0-windows`) añadido a `MangaVault.slnx` (`→ Core, Application, Infrastructure`).
- [x] MVVM (`CommunityToolkit.Mvvm`), inyección de dependencias (`Microsoft.Extensions.DependencyInjection`) y navegación `ContentControl` + `DataTemplate` con tema oscuro (`ResourceDictionary`).
- [x] Pantallas y ViewModels implementados: Login/Registro, Dashboard, Biblioteca (búsqueda, filtros, detalle editable, modal Crear/Editar), Favoritos y Configuración.
- [x] Operaciones en segundo plano vía `IBackgroundExecutor`, registro en archivo (`FileAppLogger`) y mensajes amigables (`UserMessageTranslator`).

---

## 🔨 Siguiente paso inmediato: completar MVP 5 (Ejecutable independiente `.exe`)

Según [03-mvp-5.md](03-mvp-5.md):

- [ ] `dotnet publish MangaVault.Wpf -c Release -r win-x64` (`PublishSingleFile`) → `publish\MangaVault.exe`.
- [ ] Decidir entre distribución *framework-dependent* (requiere .NET 10 Desktop Runtime) o *self-contained*.
- [ ] Configurar icono de aplicación y acceso directo en escritorio (script `.ps1` o instalador).
- [ ] Crear script `build.ps1` de publicación en un solo comando.
- [ ] Agregar backup/exportación de `mangavault.db` desde Ajustes en `MangaVault.Wpf` (los logs en `%LOCALAPPDATA%\MangaVault\logs\` ya están activos con `FileAppLogger`).
- [ ] Prueba en limpio del `.exe` en otra carpeta/equipo sin IDE ni SDK.

---

## ▶️ Comandos útiles

```powershell
dotnet build MangaVault.slnx
dotnet test MangaVault.slnx
dotnet run --project MangaVault.Wpf/MangaVault.Wpf.csproj
dotnet run --project Presentation.Cli/MangaVault.Presentation.Cli.csproj
```
