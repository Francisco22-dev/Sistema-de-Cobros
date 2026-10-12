# AGENTS.md — Sistema de Cobros 2.0
Sistema de cobros robusto que sirva para registrar los pagos de estudiantes de una academia de cursos, ver quiénes están al día y quiénes están morosos. Proyecto para venta: el código debe poder entenderlo alguien que tiene un conocimiento intermedio de programación.

## Convenciones
- Textos de la interfaz en español.
- Código simple y robusto provisto de las medidas de seguridad para una aplicación que se ejecuta en una red local, nombres descriptivos y comentarios solo donde aporten.
- Diseño limpio, original e intuitivo.

## Datos
- Todos los datos se guardarán en una base de datos.
- Si cambias la forma de los datos, mantén compatibilidad con lo ya guardado o el usuario
perderá sus sesiones.

## Fechas (fácil equivocarse)
- Trabaja siempre con la fecha local del usuario. Nunca uses `toISOString()` ni `new
Date("AAAA-MM-DD")`: se interpretan en UTC y desplazan el día.

## Forma de trabajar
- Haz solo lo que se pide: no añadas funcionalidades por tu cuenta.
- Cambios pequeños y enfocados; no reescribas lo que ya funciona.
- Al terminar, resume qué has cambiado y cualquier decisión que deba revisar.

## Límites
- ✅ Siempre: respetar las reglas de fechas, mantener los textos en español, actualizar `MEMORY.md` al terminar cada tarea.
- ⚠️ Pregunta antes: crear archivos nuevos, cambiar el formato de los datos guardados.
- 🚫 Nunca: añadir dependencias, frameworks o un paso de build.

## Memoria
- Al empezar, lee `MEMORY.md` para conocer el estado del proyecto y las decisiones
tomadas.
- Al terminar una tarea, actualízalo: estado actual, decisiones importantes (con su
porqué) y errores a evitar.
- Mantenlo breve (máximo ~50 líneas): resume o elimina lo que ya no aporte.
- Si algo se convierte en una regla permanente, propón moverlo a `AGENTS.md` en lugar de
dejarlo en la memoria.
- No guardes nunca datos sensibles (claves, tokens, datos personales).

## Project

Desktop billing/payment system ("Sistema de Cobros") built as a **.NET Framework 4.8 WPF** application (C# + XAML). Windows-only; not cross-platform .NET (Core/5+).

## Build

**Do NOT use `dotnet build`** — it fails on this old-style (non-SDK) csproj because the .NET 9 SDK's MSBuild cannot run the `GenerateResource` task for .NET Framework 4.8 resx files.

Use Visual Studio 2022's MSBuild instead:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "Sistema de Cobros 2.0.sln" /t:Build /p:Configuration=Debug /v:minimal /nologo
```

- No NuGet packages — only framework references, nothing to restore.
- Output: `Sistema de Cobros 2.0\bin\Debug\Sistema de Cobros 2.0.exe`

## Architecture

- Single-project solution (`Sistema de Cobros 2.0.sln` → one `.csproj`).
- Entry point: `App.xaml` (`StartupUri="MainWindow.xaml"`).
- Namespace: `Sistema_de_Cobros_2._0` — dots in the assembly name become underscores.
- Assembly name contains spaces: `Sistema de Cobros 2.0`.
- `MainWindow` is currently an empty `Grid` — the project is in early/skeleton stage.

## Codebase conventions

- **Spanish-language codebase**: comments, XAML `x:Class`, assembly info, and UI text are in Spanish. Match this when adding new code.
- Old-style csproj (ToolsVersion 15.0, `Microsoft.Common.props` import) — not SDK-style. Do not attempt to migrate to SDK-style without explicit instruction.

## Testing

- No test projects or test framework exist. Do not assume `dotnet test` or any test runner is available.

## Tooling

- `opencode.json` configures two MCP servers: `supabase` (HTTP, Supabase docs/database) and `context7` (remote, placeholder API key — replace before use).
- Only `dotnet` SDK 9.0.312 is on PATH; standalone MSBuild is not. Use the full path to VS 2022 MSBuild (see above).
