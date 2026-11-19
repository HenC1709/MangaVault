## Proyecto desarrollado originalmente por mí y posteriormente refactorizado con asistencia de IA

# 📚 MangaVault

Sistema de gestión personal de **mangas y novelas ligeras** construido en **C# (.NET 10 / C# 14)** siguiendo **Clean Architecture** en una solución multi-proyecto.

---

## 📖 Documentación del proyecto

- [Visión WPF](doc/01-vision-wpf.md) — arquitectura MVVM, pantallas y publicación sin IDE.
- [Visión SQL](doc/01-vision-sql.md) — modelo relacional en SQLite y migración desde JSON.
- [Errores detectados](doc/02-errores.md) — auditoría técnica (29/29 ítems resueltos ✅).
- [Plan de 5 MVP](doc/03-mvp-5.md) — hoja de ruta paso a paso hasta el ejecutable independiente.

---

## 🏗️ Arquitectura

La solución (`MangaVault.slnx`) separa las responsabilidades en 5 proyectos donde las capas internas no dependen de las externas:

```
Core                  ← Dominio puro (sin dependencias de proyecto)
Application           ← Casos de uso, DTOs, mappers y validadores (→ Core)
Infrastructure        ← Persistencia JSON/atómica, PBKDF2 y logging (→ Core)
Presentation.Cli      ← Menús de consola y Composition Root (→ Core, Application, Infrastructure)
Tests                 ← Suite de 21 pruebas unitarias xUnit (→ Core, Application, Infrastructure)
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
│   ├── Interfaces/        (IUserRepository, IMangaRepository, IFavoriteRepository, IPasswordHasher)
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
│   ├── Persistence/Json/  (JsonFileStore, JsonUserRepository, JsonMangaRepository, JsonFavoriteRepository)
│   └── Security/          (Pbkdf2PasswordHasher)
├── Presentation.Cli/
│   ├── MangaVault.Presentation.Cli.csproj
│   ├── Menus/             (MainMenu, MangaMenu, UserMenu)
│   └── Program.cs         (Composition Root)
├── Tests/
│   ├── MangaVault.Tests.csproj
│   ├── UserServiceTests.cs
│   ├── MangaServiceTests.cs
│   ├── FavoriteServiceTests.cs
│   └── DomainAndSecurityTests.cs
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

- **Ruta por defecto:** `%LOCALAPPDATA%\MangaVault\` (`users.json`, `mangas.json`, `favorites.json`).
- **Concurrencia y escritura atómica (`JsonFileStore<T>`):** sincronización con `System.Threading.Lock` (.NET 10) y guardado vía archivo temporal `.tmp` con `FileOptions.WriteThrough` + `File.Move(..., overwrite: true)`.
- **Deserialización e integridad:** `[JsonInclude]` en propiedades con `private set`, tolerancia a JSON vacío, auto-generación de Ids en `Save()`, comparaciones `OrdinalIgnoreCase` y excepciones explícitas en duplicados o registros inexistentes.
- **Contraseñas (`Pbkdf2PasswordHasher`):** PBKDF2-SHA256 (`100_000` iteraciones, salt aleatorio de 16 bytes) y verificación en tiempo constante (`CryptographicOperations.FixedTimeEquals`).

---

## ▶️ Compilación, pruebas y ejecución

```powershell
dotnet build MangaVault.slnx
dotnet test MangaVault.slnx
dotnet run --project Presentation.Cli/MangaVault.Presentation.Cli.csproj
```

---

## 🛣️ Progreso

### ✅ Hecho (MVP 1 + MVP 2 + solución multi-proyecto)
- Auditoría completa de [doc/02-errores.md](doc/02-errores.md) cerrada (29/29 ítems resueltos).
- Solución multi-proyecto `MangaVault.slnx` (`Core`, `Application`, `Infrastructure`, `Presentation.Cli`, `Tests`).
- Capa `Application` completa: `UserService` (registro/login PBKDF2/cambio de clave), `MangaService` (CRUD + búsqueda por título/tipo), `FavoriteService` (biblioteca, progreso, favoritos, calificación y tomos poseídos), DTOs, validadores y mappers.
- Interfaz de consola modular (`MainMenu`, `UserMenu`, `MangaMenu`) con gestión de sesión activa.
- Suite `Tests` (xUnit) con **21 pruebas unitarias en verde**.

### 🔨 Siguiente (resto del MVP 3 → MVP 5)
Ver detalle en [doc/03-mvp-5.md](doc/03-mvp-5.md):
- **MVP 3:** Base de datos SQLite (DDL al primer arranque, repositorios SQL y seed de géneros).
- **MVP 4:** Interfaz de escritorio WPF (MVVM + DI).
- **MVP 5:** Publicación de `MangaVault.exe` independiente sin IDE.

---

## 🛠️ Tecnologías

- **Lenguaje / Runtime:** C# 14 / .NET 10
- **Persistencia:** JSON atómico (`System.Text.Json`) → SQLite (MVP 3)
- **Pruebas:** xUnit 2.9.3
- **UI:** Consola (`Presentation.Cli`) → WPF (MVP 4)
- **Arquitectura:** Clean Architecture
