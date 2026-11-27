# 03 · Una tarea de transporte de principio a fin

La API puede crear una tarea con un lugar de recogida y un destino. Un robot registrado pide trabajo desde el cliente .NET y confirma la entrega al pulsar Intro. Los lugares son nombres para la demostración. El mapa, el movimiento y la detección de desconexiones quedan para entregas posteriores. [Issue #10](https://github.com/switchyard-project/switchyard/issues/10)

La [siguiente entrega](04-panel-de-tareas.md) añade creación y listado desde el panel. La [demo con panel y consola](../ejecucion.md#demo-completa-del-panel-a-la-consola) utiliza los mismos comandos .NET en macOS, Linux y Windows.

## Demostración en PowerShell

Desde la raíz del repositorio, compila en Release y arranca la API con un archivo de datos específico para la demostración. Si ya lo usaste y quieres comenzar con una base vacía, elige otro nombre.

```powershell
dotnet build Switchyard.sln --configuration Release
dotnet run --project src/Switchyard.Api --configuration Release --no-build -- --urls http://localhost:5080 --Database:Path data/demo-tareas.db
```

En otra terminal registra un robot y crea una tarea por HTTP.

```powershell
dotnet run --project src/Switchyard.Robot --configuration Release --no-build -- R01
$task = Invoke-RestMethod -Method Post -Uri 'http://localhost:5080/api/tasks' -ContentType 'application/json' -Body '{"pickup":"Estante A","destination":"Muelle 1"}'
Invoke-RestMethod "http://localhost:5080/api/tasks/$($task.id)"
dotnet run --project src/Switchyard.Robot --configuration Release --no-build -- --work R01
```

La tarea empieza en `pending`. Mientras el cliente espera Intro, otra terminal o el navegador puede consultar la URL de la tarea y ver `assigned` con `robotId` igual a `R01`. Al pulsar Intro, vuelve a consultar y verás `completed`. Detén el servidor con Ctrl+C, arráncalo con el mismo comando y comprueba que la consulta sigue devolviendo la entrega completada. Si cierras el cliente antes de pulsar Intro, `--work R01` vuelve a entregar la tarea que ya tenía asignada.

## Contrato HTTP

| Petición | Resultado |
| :--- | :--- |
| `POST /api/tasks` | `201` con la tarea `pending` y una cabecera `Location` con su URL. El JSON requiere `pickup` y `destination`, cada uno con entre 1 y 60 caracteres y algún contenido distinto de espacios. Un cuerpo inválido devuelve `400`. |
| `GET /api/tasks` | `200` con todas las tareas, ordenadas por identificador ascendente; `[]` si no hay ninguna. Añadido con el panel de tareas (#14). |
| `GET /api/tasks/{id}` | `200` con la tarea, o `404` si no existe. |
| `POST /api/robots/{robotId}/tasks/claim` | `200` con la tarea activa del robot o la primera pendiente por identificador. Devuelve `204` si no hay trabajo y `404` si el robot no está registrado. La petición no lleva cuerpo. |
| `PUT /api/robots/{robotId}/tasks/{id}/completion` | `200` con la tarea `completed`. Devuelve `404` si no existe la tarea y `409` si está pendiente o pertenece a otro identificador. La petición no lleva cuerpo. |

Las respuestas con una tarea incluyen `id`, `pickup`, `destination`, `status` y `robotId`. Una tarea pendiente tiene `robotId` nulo. Al completar una tarea se conserva el robot que la hizo. El identificador de la URL permite verificar la asignación guardada. Cualquier cliente que conozca ese identificador puede usarlo en esta demo, ya que la API carece de autenticación.

## Recorrido del código

| Archivo | Métodos que intervienen |
| :--- | :--- |
| [TransportTasksController.cs](../../src/Switchyard.Api/Controllers/TransportTasksController.cs) | `Create` recibe la recogida y el destino validados por ASP.NET Core. `GetById` devuelve el estado; `GetAll` devuelve el listado. `Claim` verifica el alta del robot. `Complete` distingue una tarea inexistente de un conflicto de asignación. |
| [TransportTaskRepository.cs](../../src/Switchyard.Api/Data/TransportTaskRepository.cs) | `Initialize` crea la tabla e índice si faltan. `Create` inserta la tarea. `GetById` lee su estado y `GetAll` obtiene todos los registros ordenados. `Claim` elige y asigna en una sola sentencia SQL. `Complete` marca la tarea como terminada si coincide el robot. |
| [RobotApiClient.cs](../../src/Switchyard.Robot/RobotApiClient.cs) | `ClaimTaskAsync` y `CompleteTaskAsync` envían las peticiones y comprueban las respuestas. `ReadTaskAsync` verifica que el servidor devuelve la tarea esperada. |
| [Program.cs del cliente](../../src/Switchyard.Robot/Program.cs) | `RunTaskAsync` muestra la asignación, espera Intro y envía la confirmación. |

SQLite guarda el identificador asignado y el indicador `completed`. La respuesta deriva el estado de esos dos campos. Al arrancar la API con una base de la etapa de registro, se añade la tabla de tareas y se conservan las altas existentes.

## Decisiones y preguntas para la defensa

- **¿Qué pasa si dos robots piden la misma tarea?** SQLite serializa las escrituras. La selección y la asignación ocurren dentro de un solo `UPDATE`. Cuando la segunda escritura puede ejecutarse, la tarea ya está asignada y deja de ser candidata para ese robot. Un índice único protege además el máximo de una tarea activa por robot.
- **¿Qué pasa si se pierde la respuesta?** Repetir la petición de trabajo devuelve la tarea activa, si aún existe. Repetir el `PUT` con el mismo robot e identificador de tarea conserva la finalización. Si ya tienes el identificador de la tarea, puedes consultar su estado antes de actuar de nuevo.
- **¿Qué pasa si se cierra el cliente o se reinicia la API?** La asignación queda en el archivo SQLite. El mismo robot puede recuperar su tarea pendiente de confirmar. Si lo abandonamos, esa tarea permanece asignada hasta que se implemente una política de recuperación.
- **¿Dónde entra lo visto en clase?** El tema 04 ayuda a explicar el acceso concurrente a un recurso compartido. El tema 05 se ve en los recursos y las respuestas HTTP. El tema 08 se aplica a la espera de red con `await`. El cálculo paralelo de rutas sigue pendiente.

## Comprobaciones

[TransportTaskTests.cs](../../tests/Switchyard.Tests/TransportTaskTests.cs) usa la API y archivos SQLite temporales. Comprueba las entradas inválidas, el robot desconocido, la competencia de dos robots por la misma tarea, las peticiones repetidas de un robot, el rechazo de una confirmación ajena y los estados recuperados al reiniciar.
