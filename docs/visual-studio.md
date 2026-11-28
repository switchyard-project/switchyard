# Arrancar Switchyard en Visual Studio

**Pon `Switchyard.Api` como proyecto de inicio.** La web se sirve desde ese mismo proyecto. Después de preparar el almacén, las altas y las tareas, inicia `Switchyard.Robot` como un segundo proceso.

Necesitas Visual Studio para Windows compatible con .NET 10: Visual Studio 2026, versión 18.0 o posterior, actualizado para el SDK instalado, y la carga de trabajo **Desarrollo de ASP.NET y web**. La solución usa el SDK indicado en `global.json`, con avance permitido a otras bandas de .NET 10. [Compatibilidad oficial de Microsoft](https://learn.microsoft.com/en-us/dotnet/core/install/windows).

## Primera ejecución

1. Abre `Switchyard.sln` y compila la solución.
2. En el explorador de soluciones, botón derecho sobre **Switchyard.Api → Establecer como proyecto de inicio**.
3. En el selector junto al botón de ejecución, elige el perfil **Switchyard.Api**. Pulsa F5, o Ctrl+F5 para ejecutar sin depuración.
4. El perfil configura `http://localhost:5080` y abre el navegador. Elige o crea un almacén.
5. En **Robots**, registra `R01`; si ya existe, usa el almacén al que pertenece o cambia el identificador del perfil del robot.
6. En **Tareas**, crea una entrega y asígnala a `R01`.
7. Mantén la API abierta. Para ejecutar el cliente de forma sencilla, abre la terminal de Visual Studio en la raíz del repositorio y escribe:

```powershell
dotnet run --project src/Switchyard.Robot --launch-profile Robot.Work
```

La consola recoge la tarea asignada. Pulsa Intro para completarla y mira cómo cambia su estado en el navegador. El cliente termina después de esa entrega; el servidor sigue en ejecución.

También puedes depurar el robot con Visual Studio: con la API ya en ejecución, selecciona el perfil `Robot.Work` en las propiedades de depuración del proyecto y usa **Depurar → Iniciar nueva instancia** desde el menú contextual de `Switchyard.Robot`. Si tu configuración no permite iniciar otra sesión, ejecuta la API sin depuración y deja `Switchyard.Robot` como proyecto de inicio para depurarlo.

## Perfiles compartidos

| Proyecto / perfil | Qué inicia |
| :--- | :--- |
| `Switchyard.Api` / `Switchyard.Api` | API en el puerto 5080 y navegador en la página inicial. |
| `Switchyard.Robot` / `Robot.Work` | `--work R01 http://localhost:5080`: recoge y completa una tarea. |
| `Switchyard.Robot` / `Robot.Register` | `R01 http://localhost:5080`: registra R01 en el almacén de demostración y termina. |
| `Switchyard.Contracts` | Biblioteca de mensajes; no se inicia. |
| `Switchyard.Tests` | Se ejecuta desde el explorador de pruebas o `dotnet test`; no es un servicio. |

Los perfiles están en `Properties/launchSettings.json` de cada proyecto. Puedes cambiar `R01` en **Propiedades → Depurar → Abrir interfaz de perfiles de inicio**. Para trabajar con un almacén distinto al de demostración, registra el robot desde la web de ese almacén y usa su identificador en `Robot.Work`. No vuelvas a registrarlo con `Robot.Register`: los identificadores son únicos para toda la instalación.

No se configura un arranque simultáneo de API y robot: el orden de inicio de proyectos no garantiza que el servidor ya acepte conexiones cuando el cliente haga su única petición. Primero arranca la API y prepara el trabajo; después inicia tantos procesos de cliente como necesites, con identificadores distintos. Cada proceso real es un cliente independiente.

## Los mismos perfiles en macOS o por terminal

```bash
# Terminal 1: deja la API ejecutándose.
dotnet run --project src/Switchyard.Api --launch-profile Switchyard.Api

# Terminal 2: después de registrar R01 y preparar su tarea desde la web.
dotnet run --project src/Switchyard.Robot --launch-profile Robot.Work
```

La apertura automática del navegador depende del IDE; con `dotnet run` abre tú `http://localhost:5080`. Los argumentos explícitos después de `--` tienen prioridad sobre los del perfil. `--no-launch-profile` permite ejecutar sin aplicarlo.

Si 5080 ya está ocupado, detén tu otra instancia o cambia tanto la URL de la API como la del robot. Si `R01` ya está registrado, el alta devuelve conflicto: no borres la base para solucionarlo; usa el registro existente o un ID nuevo. Si no hay tareas, el robot lo explica y termina.

## Decisiones y comprobaciones

`commandName: Project` ejecuta el proyecto; `applicationUrl` fija el puerto de la API; `commandLineArgs` proporciona los argumentos al robot. Son ajustes de desarrollo: no cambian el contrato HTTP, no arrancan una flota y no se usan como configuración de despliegue. El perfil de API marca el entorno Development solo para ese arranque local.

Se han comprobado los perfiles con el SDK de .NET en macOS: API en el puerto previsto, registro, petición de tarea, confirmación y persistencia. La interfaz de Visual Studio para Windows no se ha ejecutado en este equipo; Carlos puede verificar los pasos de F5 durante la revisión. Las comprobaciones de GitHub cubren compilación y pruebas en Windows y Linux, pero no los clics del IDE.

Referencias: [perfiles de depuración de Visual Studio](https://learn.microsoft.com/en-us/visualstudio/debugger/project-settings-for-csharp-debug-configurations-dotnetcore) y [perfiles de dotnet run](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run).
