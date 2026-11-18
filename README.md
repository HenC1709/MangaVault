# 📚 MangaVault

Sistema de gestión personal de **mangas y novelas ligeras** construido en C# (.NET 10) siguiendo principios de Clean Architecture.

---

## 📖 Documentación del proyecto

- [Visión WPF](doc/01-vision-wpf.md) — arquitectura MVVM, pantallas y publicación sin IDE.
- [Visión SQL](doc/01-vision-sql.md) — modelo relacional en SQLite y migración desde JSON.
- [Errores detectados](doc/02-errores.md) — auditoría técnica del código base.
- [Plan de 5 MVP](doc/03-mvp-5.md) — hoja de ruta paso a paso hasta el ejecutable independiente.

---

## 🏗️ Arquitectura

El proyecto está organizado en 4 capas (actualmente dentro de un único `.csproj`, con separación prevista en solución multi-proyecto en el MVP 3):

```
Core                  ← Dominio puro (entidades, enums, excepciones, interfaces)
Application           ← Casos de uso, DTOs, mappers y validadores
Infrastructure        ← Persistencia (JSON → SQLite) y logging
Presentation.Console  ← Interfaz de consola (previa a WPF)
```

La regla principal: **las capas internas no conocen las externas**.

---

## 📁 Estructura actual

```
MangaVault/
├── Core/
│   ├── Entities/          (User, Manga, Genre, MangaEntry, Character*, Collection*, Sesion*)
│   ├── Enums/             (UserRole.cs [Role], MangaStatus.cs, ReadingStatus.cs, MediaType.cs)
│   ├── Exceptions/        (DuplicateUserException, UserNotFoundException, DuplicateMangaException, MangaNotFoundException, DuplicateFavoriteException*)
│   ├── Interfaces/        (IUserRepository, IMangaRepository, IPasswordHasher, IFavoriteRepository*)
│   ├── ValueObjects/      (Email*, RatingValue*)
│   └── Constants/         (ErrorMessages*, ValidationRules*)
├── Application/           (DTOs*, Services*, Validators*, Mappers*)
├── Infrastructure/
│   ├── Persistence/Json/  (JsonFileStore, JsonUserRepository, JsonMangaRepository, JsonFavoriteRepository*)
│   ├── Security/          (Pbkdf2PasswordHasher)
│   └── Logging/           (ConsoleLogger*)
├── Presentation.Console/
│   └── Menus/             (MainMenu*, MangaMenu*, UserMenu*)
├── doc/                   (Visión WPF/SQL, errores y plan de 5 MVP)
├── Program.cs             (Punto de entrada con menú mínimo MVP 1)
└── MangaVault.csproj
```

> `*` Archivos creados como esqueleto pendientes de implementación.

---

## 🧩 Entidades del dominio

### `User`
| Propiedad      | Tipo     | Descripción                   |
|----------------|----------|-------------------------------|
| `UserId`       | `int`    | Identificador único           |
| `Name`         | `string` | Nombre de usuario             |
| `PasswordHash` | `string` | Contraseña hasheada           |
| `Role`         | `Role`   | Rol del usuario en el sistema |

### `Manga`
| Propiedad       | Tipo          | Descripción                             |
|-----------------|---------------|-----------------------------------------|
| `Id`            | `int`         | Identificador único                     |
| `Title`         | `string`      | Título de la obra (editable con `SetTitle` / `UpdateDetails`) |
| `Type`          | `MediaType`   | Tipo de obra (`Manga` o `LightNovel`)   |
| `Mangaka`       | `string`      | Autor / mangaka / escritor              |
| `Illustrator`   | `string?`     | Ilustrador (útil en novelas ligeras)    |
| `VolumeCount`   | `int`         | Cantidad de tomos (`>= 0`)              |
| `FinalChapters` | `int`         | Total de capítulos (`>= 0`)             |
| `Status`        | `MangaStatus` | Estado de publicación                   |
| `CoverPath`     | `string?`     | Ruta de portada local                   |
| `Synopsis`      | `string?`     | Sinopsis de la obra                     |
| `Genres`        | `List<Genre>` | Géneros asociados (`AddGenre` / `RemoveGenre`) |

### `Genre`
| Propiedad     | Tipo     | Descripción                       |
|---------------|----------|-----------------------------------|
| `Name`        | `string` | Nombre del género (no vacío)      |
| `Description` | `string` | Descripción del género            |

### `MangaEntry`
| Propiedad        | Tipo            | Descripción                               |
|------------------|-----------------|-------------------------------------------|
| `UserId`         | `int`           | Usuario dueño del registro                |
| `MangaId`        | `int`           | Obra a la que hace referencia             |
| `CurrentChapter` | `int`           | Capítulo actual de lectura                |
| `ReadingStatus`  | `ReadingStatus` | Estado actual de lectura                  |
| `OwnedVolumes`   | `int`           | Tomos poseídos (`>= 0`)                   |
| `Rating`         | `int?`          | Puntuación personal (`1`–`10`, opcional)  |
| `IsFavorite`     | `bool`          | Indica si está marcada como favorita      |
| `DateAdded`      | `DateTime`      | Fecha UTC en que se agregó a la lista     |

---

## 🔢 Enums

- **`Role`**: `User`, `Mangaka`, `Admin`
- **`MediaType`**: `Manga = 0`, `LightNovel = 1`
- **`MangaStatus`**: `Ongoing`, `Completed`, `Hiatus`, `Cancelled`
- **`ReadingStatus`**: `Reading = 1`, `PlanToRead = 2`, `Completed = 3`, `OnHold = 4`, `Dropped = 5`

---

## 🗄️ Persistencia y seguridad

Actualmente la capa `Infrastructure` persiste los datos en JSON usando `System.Text.Json`:

- **Ruta por defecto:** `%LOCALAPPDATA%\MangaVault\` (`mangas.json`, `users.json`).
- **Concurrencia y escritura atómica (`JsonFileStore<T>`):** sincronización con `System.Threading.Lock` (.NET 10) y guardado mediante archivo temporal `.tmp` con `FileOptions.WriteThrough` seguido de `File.Move(..., overwrite: true)`.
- **Tolerancia:** si el archivo no existe o está vacío, devuelve una lista vacía sin lanzar `JsonException`.
- **Deserialización:** las propiedades con `private set` en `User`, `Manga`, `Genre` y `MangaEntry` están anotadas con `[JsonInclude]` para conservar los datos al leer desde disco.
- **Integridad:** `Save()` auto-genera el Id si llega `<= 0` y valida duplicados (`DuplicateUserException`, `DuplicateMangaException` por Id o `(Title, Type)` con comparación `OrdinalIgnoreCase`). `Update()` y `Delete()` lanzan `UserNotFoundException` o `MangaNotFoundException` si el Id no existe.
- **Inyección de ruta:** los repositorios aceptan un directorio opcional en el constructor para facilitar pruebas.
- **Contraseñas (`Pbkdf2PasswordHasher`):** hashing con PBKDF2-SHA256 (`100_000` iteraciones, salt aleatorio de 16 bytes) y verificación en tiempo constante (`CryptographicOperations.FixedTimeEquals`).

---

## ▶️ Ejecución

```powershell
dotnet run
```

---

## 🛣️ Progreso (MVP 1 completado ✅)

### ✅ Hecho (MVP 1 + bloque lógico L1–L11)
- `Program.cs` con punto de entrada y menú de consola (listar, agregar Manga/LN, editar título y eliminar obras).
- Soporte completo para **mangas y novelas ligeras** (`MediaType`, `Illustrator`, `CoverPath`, `Synopsis`) y datos de colección en `MangaEntry` (`OwnedVolumes`, `Rating`, `IsFavorite`, `DateAdded` en UTC).
- Deserialización JSON corregida con `[JsonInclude]` en `User`, `Manga`, `Genre` y `MangaEntry`.
- Persistencia centralizada en `%LOCALAPPDATA%\MangaVault\`, tolerancia a JSON vacíos, bloqueo con `System.Threading.Lock` y escritura atómica (`JsonFileStore<T>`).
- Auto-generación de Ids en `Save()`, validación de duplicados (`OrdinalIgnoreCase`) y excepciones explícitas en `Update`/`Delete` (`UserNotFoundException`, `MangaNotFoundException`, `DuplicateMangaException`).
- Validaciones de invariantes en entidades (con `field` de C# 14 y guard clauses de .NET), edición de título en `Manga`, relación `Manga`–`Genre` y validación de capítulos en `MangaEntry.UpdateProgress`.
- Seguridad de contraseñas lista en dominio/infraestructura (`IPasswordHasher` + `Pbkdf2PasswordHasher`).

### 🔨 Siguiente (MVP 2)
- Servicios de aplicación (`UserService`, `MangaService`, `FavoriteService`), DTOs, validadores, mappers, menús dedicados y suite de tests.

### 📋 Próximos MVP
Ver detalle completo en [doc/03-mvp-5.md](doc/03-mvp-5.md):
- **MVP 2:** Servicios de aplicación, autenticación con hash PBKDF2, favoritos y tests.
- **MVP 3:** Solución multi-proyecto y migración a SQLite.
- **MVP 4:** Interfaz de escritorio WPF (MVVM).
- **MVP 5:** Publicación de `MangaVault.exe` independiente sin IDE.

---

## 🛠️ Tecnologías

- **Lenguaje:** C# (.NET 10)
- **Persistencia:** JSON (`System.Text.Json`) → SQLite (MVP 3)
- **UI:** Consola → WPF (MVP 4)
- **Arquitectura:** Clean Architecture
