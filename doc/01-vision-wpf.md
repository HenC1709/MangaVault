# Visión WPF – MangaVault

Biblioteca personal de **manga y novelas ligeras (LN)** con interfaz de escritorio WPF (.NET 10, Windows).

## Objetivo
Catalogar lo que posees/lees, ver progreso (capítulo/tomo), favoritos y colecciones, todo offline y sin abrir el IDE.

## Arquitectura propuesta
Reutilizar las capas existentes (`Core`, `Application`, `Infrastructure`) y sustituir `Presentation.Console` por un proyecto WPF **MVVM**.

```
MangaVault.sln
├── MangaVault.Core            (entidades, enums, interfaces)
├── MangaVault.Application     (servicios, DTOs, validadores)
├── MangaVault.Infrastructure  (SQLite/JSON, logging)
└── MangaVault.Wpf             (Views, ViewModels, Converters, Resources)
```

> Hoy todo está en UN solo `MangaVault.csproj`. Primero hay que separarlo en una solución con varios proyectos (ver `02-errores.md`).

- **MVVM**: `CommunityToolkit.Mvvm` (`ObservableObject`, `RelayCommand`).
- **DI**: `Microsoft.Extensions.DependencyInjection` / `Host` en `App.xaml.cs`.
- **Navegación**: `ContentControl` + `DataTemplate` por ViewModel (sin frameworks extra).
- **Tema**: `ResourceDictionary` con modo oscuro (lectores de manga suelen preferirlo).

## Pantallas
| Pantalla | Contenido |
|---|---|
| Login / Registro | Usuario + contraseña (hash PBKDF2/BCrypt) |
| Biblioteca | Grid de portadas, filtro por tipo (Manga/LN), género, estado de lectura, búsqueda |
| Detalle de obra | Portada, autor, tomos, capítulos, estado, géneros, nota, progreso editable |
| Agregar/Editar obra | Formulario con validación en vivo |
| Favoritos | Lista de favoritos del usuario |
| Colecciones | Listas personalizadas ("Pendientes 2026", "Isekai") |
| Estadísticas (post-MVP) | Leídos por mes, géneros top |
| Ajustes | Ruta de datos, exportar/importar backup, tema |

## Cambios de dominio necesarios para WPF
- Añadir `MediaType { Manga, LightNovel }` a la obra (renombrar `Manga` → `Work` o mantener `Manga` + campo).
- `CoverPath` (portada local) en la obra.
- Propiedades con **setters públicos o constructor mapeado** (ahora el JSON no deserializa, ver errores).
- `INotifyPropertyChanged` solo en ViewModels, **no** en las entidades.

## Distribución sin IDE
```powershell
dotnet publish MangaVault.Wpf -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -o publish
```
Resultado: `publish\MangaVault.exe` + acceso directo en el escritorio. Los datos van en `%LOCALAPPDATA%\MangaVault\`.

## Principios
1. Lógica nunca en code-behind.
2. Operaciones de I/O `async` (no congelar la UI).
3. Un servicio por caso de uso; los ViewModels solo llaman servicios.
4. Errores de dominio → mensaje amigable en la UI (no stack traces).
