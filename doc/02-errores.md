# Revisión del proyecto – Errores detectados

Fecha de revisión: 2026-11-18. Auditoría técnica **100 % completada** (29/29 ítems resueltos).

> [!NOTE]
> Solución verificada con `dotnet restore MangaVault.slnx`, `dotnet build MangaVault.slnx --no-restore --nologo --disable-build-servers -m:1` (0 advertencias, 0 errores) y `dotnet test MangaVault.slnx --no-build` (**21/21 pruebas en verde**).

## Resumen
Todos los hallazgos detectados en la revisión inicial quedaron resueltos y tachados: críticos (`C1`–`C5`), lógica/datos (`L1`–`L11`), estructura/convenciones (`E1`–`E8`) y repositorio/configuración (`G1`–`G5`). La solución está separada en 5 proyectos (`Core`, `Application`, `Infrastructure`, `Presentation.Cli`, `Tests`) sobre **.NET 10 / C# 14**.

## 🔴 Críticos (rompen la app)

| # | Estado | Problema | Dónde | Arreglo |
|---|---|---|---|---|
| ~~C1~~ | ✅ | ~~**No hay punto de entrada.** `OutputType=Exe` pero `Program.cs` estaba vacío.~~ | `Presentation.Cli/Program.cs` | Composition Root limpio que arma repositorios, hasher, logger, servicios y lanza `MainMenu`. |
| ~~C2~~ | ✅ | ~~**El JSON no deserializaba setters `private`.** `User`, `Manga` y `MangaEntry` perdían valores al leer.~~ | `User.cs`, `Manga.cs`, `MangaEntry.cs`, `Genre.cs`, `Collection.cs`, `Character.cs` | Añadido `[JsonInclude]` en todas las propiedades con `private set`. |
| ~~C3~~ | ✅ | ~~**Ruta de almacenamiento relativa (`Storage/`).** Cambiaba según el `cwd`.~~ | `JsonFileStore.cs`, `Json*Repository.cs` | Centralizado en `%LOCALAPPDATA%\MangaVault\` con ruta inyectable por constructor. |
| ~~C4~~ | ✅ | ~~**JSON vacíos (0 bytes) lanzaban `JsonException`.**~~ | `JsonFileStore.cs` | Comprobación `string.IsNullOrWhiteSpace(json)` al leer e inicialización segura con `[]`. |
| ~~C5~~ | ✅ | ~~**Interfaces/servicios sin implementar**: `IFavoriteRepository`, `UserService`, `MangaService`, `FavoriteService`, validadores, mappers, DTOs, menús vacíos.~~ | `Core`, `Application`, `Infrastructure`, `Presentation.Cli` | Implementados `IFavoriteRepository`, `JsonFavoriteRepository`, DTOs (`CreateUserDto`, `CreateMangaDto`), validadores, mappers, servicios (`UserService`, `MangaService`, `FavoriteService`) y menús (`MainMenu`, `UserMenu`, `MangaMenu`). |

## 🟠 Lógica / datos

| # | Estado | Problema | Dónde | Arreglo |
|---|---|---|---|---|
| ~~L1~~ | ✅ | ~~`JsonMangaRepository.Save` no validaba duplicados y nadie generaba Ids (`Id` quedaba en 0).~~ | `JsonMangaRepository`, `JsonUserRepository`, `User.cs` | Auto-generación de Ids en `Save` (`Id <= 0`), `User.AssignId`, y validación de duplicados con `DuplicateMangaException` y `DuplicateUserException`. |
| ~~L2~~ | ✅ | ~~`Update`/`Delete` fallaban en silencio si el Id no existía; `UserNotFoundException` estaba vacía.~~ | `Core/Exceptions/*`, `Json*Repository` | Implementadas `UserNotFoundException`, `MangaNotFoundException`, `DuplicateMangaException` y `DuplicateFavoriteException`; excepciones explícitas en `Update`/`Delete`. |
| ~~L3~~ | ✅ | ~~`MangaEntry.UpdateProgress` ignoraba silenciosamente capítulos negativos y no validaba contra `FinalChapters`.~~ | `MangaEntry.cs`, `FavoriteService.cs` | Valida negativos, enum y tope `maxChapters` con `ArgumentOutOfRangeException`. |
| ~~L4~~ | ✅ | ~~`Manga.Title` con `private set` y sin método para cambiarlo → no se podía editar el título.~~ | `Manga.cs`, `MangaService.cs` | Añadidos `SetTitle(string)` y `UpdateDetails(...)` con validación y `Trim()`. |
| ~~L5~~ | ✅ | ~~Entidades sin validación (título vacío, volúmenes/capítulos negativos). `Genre` no se relacionaba con `Manga`.~~ | `Manga.cs`, `Genre.cs`, `User.cs`, `MangaEntry.cs` | Validaciones de invariantes y relación `Manga.Genres` con `AddGenre` / `RemoveGenre`. |
| ~~L6~~ | ✅ | ~~**No existe concepto de novela ligera**: falta `MediaType`. Tampoco hay portada, nota, tomos poseídos.~~ | `MediaType.cs`, `Manga.cs`, `MangaEntry.cs` | Añadido `MediaType` (`Manga`/`LightNovel`), `Illustrator`, `CoverPath`, `Synopsis` en `Manga`; `OwnedVolumes`, `Rating` (1–10) e `IsFavorite` en `MangaEntry`. |
| ~~L7~~ | ✅ | ~~`PasswordHash` existe pero no hay ningún hasher; riesgo de guardar contraseñas en claro.~~ | `IPasswordHasher.cs`, `Pbkdf2PasswordHasher.cs`, `User.cs` | Hash PBKDF2-SHA256 con salt aleatorio (`Rfc2898DeriveBytes.Pbkdf2`) y verificación constante (`CryptographicOperations.FixedTimeEquals`). |
| ~~L8~~ | ✅ | ~~`DateTime.Now` en `MangaEntry` → mejor `DateTime.UtcNow`.~~ | `MangaEntry.cs` | Cambiado a `DateTime.UtcNow`. |
| ~~L9~~ | ✅ | ~~Comparación con `ToLower()` sensible a cultura → usar `StringComparison.OrdinalIgnoreCase`.~~ | `JsonUserRepository.cs`, `JsonMangaRepository.cs` | Comparación uniforme con `StringComparison.OrdinalIgnoreCase`. |
| ~~L10~~ | ✅ | ~~Lecturas/escrituras completas sin lock ni escritura atómica.~~ | `JsonFileStore.cs` | Sincronización con `System.Threading.Lock` (.NET 10) y escritura atómica `.tmp` (`FileOptions.WriteThrough` + `File.Move(..., overwrite: true)`). |
| ~~L11~~ | ✅ | ~~Los repos instanciaban su propia ruta (`_path` hardcodeado) → no testeable.~~ | `JsonFileStore.cs`, `Json*Repository.cs` | Primary constructors con parámetro opcional `storageDirectory`. |

## 🟡 Estructura / convenciones

| # | Estado | Problema | Arreglo |
|---|---|---|---|
| ~~E1~~ | ✅ | ~~README describía **4 proyectos** pero había **un solo** `.csproj` y sin solución.~~ | Separado en `MangaVault.slnx` con 5 proyectos (`Core`, `Application`, `Infrastructure`, `Presentation.Cli`, `Tests`) respetando dependencias de Clean Architecture. |
| ~~E2~~ | ✅ | ~~`Presentation.Console` chocaba potencialmente con `System.Console`.~~ | Renombrado a `Presentation.Cli` (namespace `MangaVault.Presentation.Cli`, ensamblado `MangaVault`). |
| ~~E3~~ | ✅ | ~~`Sesion.cs` mal escrito y tipos de dominio/logging vacíos.~~ | Renombrado a `Session.cs` e implementados `Session`, `Collection`, `Character`, `Email`, `RatingValue` y `ConsoleLogger`. |
| ~~E4~~ | ✅ | ~~`UserRole.cs` contenía el enum `Role`.~~ | Renombrado a `Role.cs` con file-scoped namespace. |
| ~~E5~~ | ✅ | ~~`ErrorMessages`/`ValidationRules` vacías; `DuplicateFavoriteException` vacía.~~ | Implementadas constantes en `ValidationRules` y `ErrorMessages`, y excepción `DuplicateFavoriteException`. |
| ~~E6~~ | ✅ | ~~README desactualizado.~~ | Sincronizado con la solución multi-proyecto, capas y estado actual. |
| ~~E7~~ | ✅ | ~~Estilo inconsistente (`Id` vs `id`, espacios, usings sin usar, indentación mixta).~~ | Limpiados interfaces, enums, entidades y excepciones con file-scoped namespaces, C# 14 y `.editorconfig`. |
| ~~E8~~ | ✅ | ~~No hay tests.~~ | Creado `Tests/MangaVault.Tests.csproj` (xUnit) con 21 pruebas unitarias en verde para servicios, dominio, seguridad y persistencia. |

## 🟢 Repositorio / configuración

| # | Estado | Problema | Arreglo |
|---|---|---|---|
| ~~G1~~ | ✅ | ~~`.gitignore` tenía `vs/` en vez de `.vs/`.~~ | Corregido en `.gitignore`. |
| ~~G2~~ | ✅ | ~~`.vscode/` ignorado por completo aunque tenía `launch.json` y `tasks.json`.~~ | Política explícita en `.gitignore` (`.vscode/*`, `!.vscode/launch.json`, `!.vscode/tasks.json`). |
| ~~G3~~ | ✅ | ~~`users.json` y archivos `Storage/*.json` versionables.~~ | Ignorados `*.db`, `*.db-shm`, `*.db-wal`, `Storage/*.json`, `publish/` y eliminada la carpeta obsoleta `Infrastructure/Persistence/Storage/`. |
| ~~G4~~ | ✅ | ~~`launch.json` y `tasks.json` apuntaban al `.csproj` monolítico anterior.~~ | Actualizados a `MangaVault.slnx` y `Presentation.Cli/MangaVault.Presentation.Cli.csproj` (incluyendo tarea `test` y `integratedTerminal`). |
| ~~G5~~ | ✅ | ~~Sin `Directory.Build.props`, ni `.editorconfig`, ni `LICENSE`.~~ | Añadidos `Directory.Build.props` (`LangVersion 14`, `Nullable`, `ImplicitUsings`, `Deterministic`), `.editorconfig` y `LICENSE` (MIT). |

## Estado final
1. ~~C1–C5 (arranque, persistencia y aplicación completa).~~ ✅
2. ~~L1–L11 (dominio, seguridad, hilos y persistencia atómica).~~ ✅
3. ~~E1–E8 (solución multi-proyecto, tipos completos, convenciones y tests).~~ ✅
4. ~~G1–G5 (higiene del repo, `.vscode`, props, estilo y licencia).~~ ✅

**Siguiente etapa en [03-mvp-5.md](03-mvp-5.md):** persistencia en **SQLite** (MVP 3), interfaz **WPF** (MVP 4) y publicación del **`.exe` independiente** (MVP 5).
