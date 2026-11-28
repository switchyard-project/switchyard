# 06 · Elegir qué robot recibe una tarea

En **Tareas** puedes crear una entrega y, debajo de la lista, elegir una tarea pendiente y un robot disponible. «Disponible» significa que no tiene otra tarea activa guardada. Todavía no sabemos si ese proceso está conectado.

El servidor guarda la asignación. Cuando ese robot ejecuta `--work ID`, recoge primero su tarea asignada, aunque haya pendientes más antiguas. La confirmación de entrega sigue ocurriendo desde la consola del cliente .NET.

## Demo y contrato

1. Elige un almacén y registra `R01` y `R02` en su panel de robots, usando otros identificadores si ya existen.
2. Crea dos tareas en **Tareas**.
3. En **Asignar una tarea**, elige la segunda y `R02`.
4. Ejecuta `dotnet run --project src/Switchyard.Robot --configuration Release --no-build -- --work R02 http://localhost:5080`.
5. La consola muestra esa segunda tarea. Pulsa Intro para confirmar la entrega y comprueba el cambio en el panel.

La operación es `PUT /api/tasks/{id}/assignment` con JSON `{ "robotId": "R02" }`.

| Resultado | Significado |
| :--- | :--- |
| 200 | Asignación guardada; devuelve la tarea. Repetir la misma asignación activa también devuelve 200. |
| 400 | Identificador ausente o con formato inválido. |
| 404 | No existe la tarea o el robot. |
| 409 | Otro almacén, robot ocupado, tarea de otro robot o tarea completada. |

No se cambia el dueño de una tarea activa. La finalización sigue comprobando al robot asignado. Si llega una respuesta perdida, repetir el PUT no crea otra asignación; si entretanto se completó la tarea, se recibe 409 y se consulta su estado actual.

## Dónde está cada responsabilidad

| Método o módulo | Responsabilidad |
| :--- | :--- |
| `AssignTransportTaskRequest` | Valida `RobotId` al entrar en la API. |
| `TransportTasksController.Assign` | Distingue recursos inexistentes y devuelve el resultado o conflicto. |
| `TransportTaskRepository.Assign` | Una sentencia UPDATE comprueba pertenencia, estado y ausencia de otra tarea activa, y devuelve la fila asignada. |
| `assignTask` en `task-api.js` | Envía el PUT y verifica identificador, robot, estado y almacén de la respuesta. |
| `mountAssignmentForm` | Mantiene formulario, envío, bloqueo temporal y mensaje de resultado. |
| `update` | Ofrece tareas pendientes y robots sin tarea activa según las listas recibidas. El servidor vuelve a comprobarlo. |
| `replaceOptions` | Solo cambia opciones si cambian los datos y conserva la selección cuando sigue disponible. Evita reiniciar el formulario cada cinco segundos. |
| `tasks-dashboard.js` | Consulta tareas y robots; entrega ambos resultados al formulario y dibuja la lista. |
| `collection-panel.js` | Expone `refresh` para consultar de nuevo después de la asignación. |

Mientras se envía, los controles se deshabilitan. Al terminar se espera una lectura que pudiera estar en curso y se fuerza otra posterior a la escritura. Los mensajes de error explican que una respuesta perdida no implica que la escritura haya fallado.

## Preguntas para defenderlo

**¿Basta con mostrar solo robots disponibles en un desplegable?** No: otro cliente puede asignar trabajo entre la consulta y el clic. El filtro del navegador ayuda a elegir; la condición del UPDATE resuelve la concurrencia real.

**¿Por qué no se hace SELECT, comprobar y luego UPDATE?** Entre ambos podría intervenir otra petición. Las condiciones de la sentencia de escritura se evalúan con la escritura. Además permanece el índice único de una tarea activa por robot, como respaldo de la invariante.

**¿Compite con la asignación automática?** Sí. `Claim` y `Assign` escriben sobre la misma base. Solo uno puede quedarse una tarea que estuviera pendiente. Si trabajan para el mismo robot, tampoco pueden dejarle dos tareas activas.

**¿Cómo recibe la orden un proceso .NET?** Al pedir trabajo por HTTP, `Claim` devuelve primero su asignación. No hay push ni bucle permanente en el cliente actual. Si el proceso no se ejecuta, la tarea queda asignada hasta que lo haga; todavía no hay detección de fallos ni reasignación automática.

**¿Hay autorización?** No. Como en las operaciones anteriores, la API es una demo sin cuentas. La pertenencia al almacén es una regla funcional, no una comprobación de permisos.

## Comprobaciones

`TaskAssignmentTests` comprueba repetición idempotente, recogida y entrega por `RobotApiClient`, conflictos de estado, recursos inexistentes, entrada inválida, otro almacén y peticiones concurrentes. Las pruebas anteriores siguen cubriendo persistencia y finalización. La comprobación en navegador recorre la asignación y su entrega con un proceso real de consola, conserva la selección al actualizar y revisa pantalla estrecha.
