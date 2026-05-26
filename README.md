# 📚 MangaVault

Sistema de gestión personal de mangas construido en C# con Clean Architecture.

---

## 🏗️ Arquitectura

El proyecto sigue **Clean Architecture** dividido en 4 capas:

```
MangaVault.Core              ← Dominio puro (sin dependencias externas)
MangaVault.Application       ← Casos de uso y lógica de negocio
MangaVault.Infrastructure    ← Persistencia, logging, seguridad
MangaVault.Presentation      ← Interfaz de consola
```

La regla principal: **las capas internas no conocen las externas**.  
`Core` no sabe nada de JSON, consola, ni bases de datos.

---

## 📁 Estructura actual

```
MangaVault/
│
├── Core/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Manga.cs
│   │   ├── Genre.cs
│   │   ├── MangaEntry.cs
│   │   ├── Favorite.cs
│   │   └── Collection.cs
│   │
│   ├── Enums/
│   │   ├── Role.cs
│   │   ├── MangaStatus.cs
│   │   └── FavoriteType.cs
│   │
│   ├── Exceptions/
│   │   ├── DuplicateUserException.cs
│   │   └── UserNotFoundException.cs
│   │
│   ├── Interfaces/
│   │   └── Repositories/
│   │       ├── IUserRepository.cs
│   │       ├── IMangaRepository.cs
│   │       └── IFavoriteRepository.cs
│   │
│   ├── ValueObjects/
│   │   ├── Email.cs
│   │   └── RatingValue.cs
│   │
│   └── Constants/
│       ├── ErrorMessages.cs
│       └── ValidationRules.cs
│
├── Application/
│   ├── DTOs/
│   │   ├── CreateUserDto.cs
│   │   └── CreateMangaDto.cs
│   │
│   ├── Services/
│   │   ├── UserService.cs
│   │   ├── MangaService.cs
│   │   └── FavoriteService.cs
│   │
│   ├── Validators/
│   │   ├── UserValidator.cs
│   │   └── MangaValidator.cs
│   │
│   └── Mappers/
│       ├── UserMapper.cs
│       └── MangaMapper.cs
│
├── Infrastructure/
│   ├── Persistence/
│   │   ├── Json/
│   │   │   ├── JsonUserRepository.cs
│   │   │   ├── JsonMangaRepository.cs
│   │   │   └── JsonFavoriteRepository.cs
│   │   └── Storage/
│   │       ├── users.json
│   │       ├── mangas.json
│   │       └── favorites.json
│   └── Logging/
│       └── ConsoleLogger.cs
│
└── Presentation.Console/
    ├── Menus/
    │   ├── MainMenu.cs
    │   ├── MangaMenu.cs
    │   └── UserMenu.cs
    ├── Screens/
    ├── Handlers/
    └── Program.cs
```

---

## 🧩 Entidades del dominio

### `User`
Representa un usuario del sistema.

| Propiedad      | Tipo     | Descripción                        |
|----------------|----------|------------------------------------|
| `UserId`       | `int`    | Identificador único                |
| `Name`         | `string` | Nombre de usuario                  |
| `PasswordHash` | `string` | Contraseña hasheada                |
| `Role`         | `Role`   | Rol del usuario en el sistema      |

### `Manga`
Representa un manga en el catálogo.

| Propiedad       | Tipo          | Descripción                     |
|-----------------|---------------|---------------------------------|
| `Id`            | `int`         | Identificador único             |
| `Title`         | `string`      | Título del manga                |
| `Mangaka`       | `string`      | Autor del manga                 |
| `VolumeCount`   | `int`         | Cantidad de tomos               |
| `FinalChapters` | `int`         | Total de capítulos              |
| `Status`        | `MangaStatus` | Estado de publicación           |

### `Genre`
Representa un género literario/artístico.

| Propiedad     | Tipo     | Descripción              |
|---------------|----------|--------------------------|
| `Name`        | `string` | Nombre del género        |
| `Description` | `string` | Descripción del género   |

### `MangaEntry`
Representa el progreso de lectura de un usuario sobre un manga.

| Propiedad        | Tipo           | Descripción                          |
|------------------|----------------|--------------------------------------|
| `UserId`         | `int`          | Usuario dueño del registro           |
| `MangaId`        | `int`          | Manga al que hace referencia         |
| `CurrentChapter` | `int`          | Capítulo actual de lectura           |
| `ReadingStatus`  | `FavoriteType` | Estado actual de lectura             |
| `DateAdded`      | `DateTime`     | Fecha en que se agregó a la lista    |

---

## 🔢 Enums

### `Role`
```csharp
User      // Usuario estándar
Mangaka   // Autor de manga
Admin     // Administrador del sistema
```

### `MangaStatus`
```csharp
Ongoing    // En publicación
Completed  // Terminado
Hiatus     // En pausa por el autor
Cancelled  // Cancelado
```

### `FavoriteType` (Reading Status)
```csharp
Reading    = 1  // Leyendo activamente
PlanToRead = 2  // Pendiente por leer
Completed  = 3  // Terminado
OnHold     = 4  // En pausa temporal
Dropped    = 5  // Abandonado
```

> ⚠️ **Nota pendiente:** Renombrar `FavoriteType` a `ReadingStatus` para reflejar mejor su propósito.

---

## 🗄️ Persistencia

La capa de Infrastructure implementa los repositorios usando **archivos JSON** como almacenamiento.

### Patrón usado
Cada repositorio carga el archivo completo en memoria, aplica la operación, y lo guarda de nuevo.

```csharp
// Ejemplo: JsonUserRepository
GetAll()    → lee users.json
GetById()   → GetAll() + FirstOrDefault
GetByName() → GetAll() + FirstOrDefault (case-insensitive)
Save()      → valida duplicado → agrega → guarda
Update()    → busca por índice → reemplaza → guarda
```

### Validaciones en repositorio
`JsonUserRepository.Save()` lanza `DuplicateUserException` si ya existe un usuario con el mismo `Id` o `Name`.

---

## ⚠️ Excepciones personalizadas

| Excepción                 | Cuándo se lanza                              |
|---------------------------|----------------------------------------------|
| `DuplicateUserException`  | Al guardar un usuario que ya existe          |
| `UserNotFoundException`   | Al buscar un usuario que no existe           |

Todas heredan de `Exception` y llevan un mensaje descriptivo con el dato que causó el error.

---

## 🔌 Interfaces (Contratos)

### `IUserRepository`
```csharp
List<User> GetAll();
User? GetById(int id);
User? GetByName(string name);
void Save(User user);
void Update(User user);
```

### `IMangaRepository`
```csharp
List<Manga> GetAll();
Manga? GetById(int id);
void Save(Manga manga);
void Update(Manga manga);
void Delete(int id);
```

---

## 🛣️ Roadmap

### ✅ Hecho
- Estructura de proyectos y capas
- Entidades: `User`, `Manga`, `Genre`, `MangaEntry`
- Enums: `Role`, `MangaStatus`, `FavoriteType`
- Excepciones: `DuplicateUserException`, `UserNotFoundException`
- Repositorios JSON: `JsonUserRepository`, `JsonMangaRepository`
- Interfaces: `IUserRepository`, `IMangaRepository`

### 🔨 En progreso
- Application Services (`UserService`, `MangaService`)
- DTOs y Mappers
- Validadores

### 📋 Pendiente
- `FavoriteService` y `JsonFavoriteRepository`
- Presentation (menús y pantallas de consola)
- Excepciones restantes (`MangaNotFoundException`, etc.)
- Seeders con datos de prueba
- Renombrar `FavoriteType` → `ReadingStatus`

---

## 🛠️ Tecnologías

- **Lenguaje:** C# (.NET)
- **Persistencia:** JSON (System.Text.Json)
- **Arquitectura:** Clean Architecture
- **IDE:** VS Code

---

*Proyecto en desarrollo activo 🚀*