# Revisión del proyecto – Errores detectados

Fecha de revisión: 2026-11-18. Revisión estática y seguimiento de correcciones.

> [!NOTE]
> El entorno sandbox bloquea el acceso a `%APPDATA%\NuGet\NuGet.Config`, por lo que la compilación en local se verifica con `dotnet build --no-restore`.

## Resumen
De ~43 archivos `.cs`, los de capas superiores (`Application`, `Presentation.Console`) siguen pendientes como esqueleto para el MVP 2. Ya quedaron resueltos todos los críticos de arranque/persistencia (`C1`–`C4`), **todo el bloque de lógica y datos (`L1`–`L11`)**, documentación (`E6`) y `.gitignore` (`G1`).

## 🔴 Críticos (rompen la app)

| # | Estado | Problema | Dónde | Arreglo |
|---|---|---|---|---|
| C1 | ✅ | **No hay punto de entrada.** `OutputType=Exe` pero `Program.cs` estaba vacío. | `Program.cs` | Implementado menú mínimo en consola con listar, agregar, editar título y eliminar. |
| C2 | ✅ | **El JSON no deserializaba setters `private`.** `User`, `Manga` y `MangaEntry` perdían valores al leer. | `User.cs`, `Manga.cs`, `MangaEntry.cs`, `Genre.cs` | Añadido `[JsonInclude]` en las propiedades con `private set`. |
| C3 | ✅ | **Ruta de almacenamiento relativa (`Storage/`).** Cambiaba según el `cwd`. | `Json*Repository.cs` | Centralizado en `%LOCALAPPDATA%\MangaVault\` con ruta inyectable por constructor. |
| C4 | ✅ | **JSON vacíos (0 bytes) lanzaban `JsonException`.** | `users.json`, `mangas.json`, `favorites.json`, `Json*Repository.cs` | Comprobación `string.IsNullOrWhiteSpace(json)` en `GetAll()` e inicialización con `[]`. |
| C5 | ⬜ | **Interfaces/servicios sin implementar**: `IFavoriteRepository`, `UserService`, `MangaService`, `FavoriteService`, validadores, mappers, DTOs, menús vacíos. | `Application/*`, `Presentation.Console/*` | Implementar en MVP 2. |

## 🟠 Lógica / datos

| # | Estado | Problema | Dónde | Arreglo |
|---|---|---|---|---|
| L1 | ✅ | `JsonMangaRepository.Save` no validaba duplicados y nadie generaba Ids (`Id` quedaba en 0). | `JsonMangaRepository`, `JsonUserRepository`, `User.cs` | Auto-generación de Ids en `Save` (`Id <= 0`), `User.AssignId`, y validación de Id/título/nombre duplicado con `DuplicateMangaException` y `DuplicateUserException`. |
| L2 | ✅ | `Update`/`Delete` fallaban en silencio si el Id no existía; `UserNotFoundException` estaba vacía. | `Core/Exceptions/*`, `Json*Repository` | Implementadas `UserNotFoundException`, `MangaNotFoundException` y `DuplicateMangaException`; lanzadas en `Update`/`Delete` si el Id no existe. |
| L3 | ✅ | `MangaEntry.UpdateProgress` ignoraba silenciosamente capítulos negativos y no validaba contra `FinalChapters`. | `MangaEntry.cs` | Lanza `ArgumentOutOfRangeException` en valores negativos, enum inválido o si supera `maxChapters` opcional; constructor también valida invariantes. |
| L4 | ✅ | `Manga.Title` con `private set` y sin método para cambiarlo → no se podía editar el título. | `Manga.cs` | Añadidos `SetTitle(string)` y `UpdateDetails(...)` con validación de no vacío y `Trim()`. |
| L5 | ✅ | Entidades sin validación (título vacío, volúmenes/capítulos negativos). `Genre` no se relacionaba con `Manga`. | `Manga.cs`, `Genre.cs`, `User.cs`, `MangaEntry.cs` | Validaciones en constructores y setters; `Manga` incorpora `Genres` (`[JsonInclude]`) con `AddGenre` y `RemoveGenre`. |
| L6 | ✅ | **No existe concepto de novela ligera** (objetivo del proyecto): falta `MediaType`. Tampoco hay portada, nota, tomos poseídos. | `MediaType.cs`, `Manga.cs`, `MangaEntry.cs`, `Program.cs` | Añadido `MediaType` (`Manga`/`LightNovel`), `Illustrator`, `CoverPath`, `Synopsis` en `Manga`; `OwnedVolumes`, `Rating` (1–10) e `IsFavorite` en `MangaEntry`; y soporte en consola. |
| L7 | ✅ | `PasswordHash` existe pero no hay ningún hasher; riesgo de guardar contraseñas en claro. | `IPasswordHasher.cs`, `Pbkdf2PasswordHasher.cs`, `User.cs` | Implementado hash PBKDF2-SHA256 con salt aleatorio (`Rfc2898DeriveBytes.Pbkdf2`) y comparación constante (`CryptographicOperations.FixedTimeEquals`), además de métodos en `User`. |
| L8 | ✅ | `DateTime.Now` en `MangaEntry` → mejor `DateTime.UtcNow`. | `MangaEntry.cs` | Cambiado a `DateTime.UtcNow`. |
| L9 | ✅ | Comparación con `ToLower()` sensible a cultura → usar `StringComparison.OrdinalIgnoreCase`. | `JsonUserRepository.cs` | `GetByName` ahora ignora nulos/espacios y compara con `StringComparison.OrdinalIgnoreCase` igual que `Save` y `Update`. |
| L10 | ✅ | Lecturas/escrituras de archivo completas en cada operación, sin lock ni escritura atómica; riesgo de corrupción si se corta el proceso. | `JsonFileStore.cs`, `Json*Repository.cs` | Centralizado en `JsonFileStore<T>` con `System.Threading.Lock` (.NET 10) y escritura atómica vía archivo temporal `.tmp` con `FileOptions.WriteThrough` + `File.Move(..., overwrite: true)`. |
| L11 | ✅ | Los repos instanciaban su propia ruta (`_path` hardcodeado) → no testeable. | `JsonFileStore.cs`, `Json*Repository.cs` | Primary constructors con parámetro opcional `storageDirectory` delegado a `JsonFileStore<T>`. |

## 🟡 Estructura / convenciones

| # | Estado | Problema |
|---|---|---|
| E1 | ⬜ | README describe **4 proyectos** (`MangaVault.Core`, `.Application`, ...) pero hay **un solo** `.csproj` y sin `.sln`. Las capas no se imponen por compilación (previsto en MVP 3). |
| E2 | ⬜ | Namespaces vs carpetas: `Presentation.Console` carpeta con punto; `Console` como namespace chocará con `System.Console`. |
| E3 | ⬜ | `Sesion.cs` mal escrito (→ `Session.cs`); `Character.cs`, `Collection.cs`, `Email.cs`, `RatingValue.cs`, `ConsoleLogger.cs` vacíos. |
| E4 | ⬜ | `UserRole.cs` contiene el enum `Role` → renombrar archivo a `Role.cs`. |
| E5 | ⬜ | `ErrorMessages`/`ValidationRules` vacías; `DuplicateFavoriteException` vacía. |
| E6 | ✅ | README desactualizado: ya sincronizado con la estructura actual y `ReadingStatus`. |
| E7 | ⬜ | Estilo inconsistente: parámetro `Id` vs `id` en `IMangaRepository`, `opciones`, indentación mixta, usings sin usar (`MangaVault.Core.Enums` en `IMangaRepository`). |
| E8 | ⬜ | No hay tests. |

## 🟢 Repositorio / configuración

| # | Estado | Problema | Arreglo |
|---|---|---|---|
| G1 | ✅ | `.gitignore` tenía `vs/` en vez de `.vs/`. | Ya corregido en `.gitignore`. |
| G2 | ⬜ | `.vscode/` ignorado pero existe en el repo (opcional definir política). | opcional |
| G3 | ⬜ | `users.json` (con hashes) está versionable. | ignorar datos de usuario (`*.db`, `Storage/*.json`) |
| G4 | ⬜ | `launch.json` apunta a `bin/Debug/net10.0/MangaVault.dll` (al pasar a solución multi-proyecto habrá que actualizarlo). | actualizar en MVP 3 |
| G5 | ⬜ | Sin `Directory.Build.props`, ni `.editorconfig`, ni `LICENSE`. | añadir |

## Orden sugerido de corrección
1. ~~C1 + C2 + C3 + C4 (la app arranca y persiste bien).~~ ✅
2. ~~L1–L11 (dominio, seguridad, hilos y persistencia atómica).~~ ✅ — **MVP 1 completado.**
3. E3, E4, E7, G3, G5 (limpieza rápida) y luego C5, E5, E8 (MVP 2).
4. E1, E2, G4 (solución multi-proyecto en MVP 3) antes de empezar WPF.
