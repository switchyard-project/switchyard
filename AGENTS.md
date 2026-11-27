# Instrucciones para trabajar en Switchyard

## Contexto y lecturas

Switchyard es un proyecto de Sistemas Distribuidos y Programación en Paralelo para coordinar robots en un almacén simulado. Cada robot debe ser un cliente .NET independiente; el sistema debe comunicarse mediante una API y conservar su estado. La propuesta actual usa REST con JSON.

Antes de trabajar, lee:

1. [README.md](README.md): propósito, alcance y estado público del proyecto.
2. [Flujo de trabajo](docs/flujo-de-trabajo.md): acuerdo de colaboración y entrega.
3. [Índice técnico](docs/README.md) y los documentos relevantes para la tarea.
4. [Criterios de código](docs/criterios-de-codigo.md): legibilidad, alcance incremental, pruebas y documentación de cada funcionalidad.

El registro de robots está implementado con .NET 10, ASP.NET Core y SQLite. El panel web en `src/Switchyard.Api/wwwroot` usa HTML, CSS y módulos JavaScript, se sirve desde la API y consulta los registros cada cinco segundos con la pestaña visible. Las tareas de transporte ya se crean, asignan y completan desde la API y la consola; la coordinación espacial y la simulación siguen pendientes. El acceso móvil con roles es opcional y futuro; adaptar el diseño a móvil no implementa autenticación ni permisos. No des por elegidas tecnologías pendientes ni presentes funciones previstas como implementadas.

## Flujo esperado

- Revisa `git status`, la rama actual y los cambios existentes antes de editar. Conserva el trabajo del usuario o de otros colaboradores.
- Consulta la issue de la tarea. Si no existe y el encargo incluye implementarla en el repositorio, crea una issue acotada antes de empezar. Usa responsables acordados y cuentas verificadas; no inventes el usuario de Carlos.
- Trabaja en una rama `<tipo>/<issue>-<descripcion-en-ingles>` creada desde `develop` actualizado; el PR de tarea apunta a `develop`. `main` recibe promociones revisadas desde `develop`. No mezcles tareas sin relación.
- Usa Conventional Commits, con título en inglés y referencia real a la issue al final del título: `tipo(ambito): descripcion [refs #numero]`. Una referencia únicamente en el cuerpo no cumple esta regla. La documentación y las explicaciones al equipo van en español.
- Completa las comprobaciones pertinentes, revisa el diff y publica la rama y el PR cuando formen parte del encargo. Usa la plantilla de PR y explica resultados y limitaciones reales.
- Usa `Refs: #numero` en los PR de tareas hacia `develop`. Tras su integración y validación, cierra la issue indicando que está integrada en `develop`, todavía no publicada en `main`. Las promociones hacia `main` enlazan las issues incluidas; `Closes #numero` solo se usa allí para una issue aún abierta que se complete con la promoción.
- Si una entrega depende de otra pendiente de revisión, puede partir de esa rama y abrir un PR hacia ella, indicando la dependencia. No lo integres en la rama de tarea: espera a que se integre el requisito, incorpora `origin/develop` mediante merge, cambia la base del PR a `develop` y revisa el diff y la CI antes de integrar.
- La integración requiere revisión humana del compañero. No la simules ni integres automáticamente un PR sin ella y sin autorización para integrar.
- No hagas force push a `main` o ramas compartidas, no borres trabajo ajeno y no incluyas secretos ni archivos generados innecesarios.

## Calidad y continuidad

- Mantén el README comprensible para una persona que no conoce el código. Las explicaciones técnicas van en `docs/`.
- Documenta las decisiones relevantes y sus motivos. Diferencia acuerdos, propuestas y asuntos pendientes.
- Comprueba lo que cambies con herramientas adecuadas. Para documentación, revisa enlaces y gráficos; cuando exista código, usa sus comandos reales de compilación y pruebas.
- No inventes comandos, métricas, resultados de tests ni garantías sobre colisiones, bloqueos o recuperación.
- Mantén alineados los contratos y ejemplos cuando cambie una API o el modelo de datos.
- Trabaja por funcionalidades pequeñas. Documenta cada una en `docs/funcionalidades/`, incluyendo el recorrido del código, los métodos, las decisiones, las comprobaciones y preguntas que puedan surgir en la exposición.
- Comprobaciones del código actual: `dotnet build Switchyard.sln --configuration Release`, `dotnet test Switchyard.sln --configuration Release --no-build` y `dotnet format Switchyard.sln --verify-no-changes --no-restore`.
- Al terminar, resume los archivos o comportamientos modificados, las verificaciones, la issue y el PR o commit resultante, y cualquier impedimento pendiente.

## Excepción histórica y límites

La documentación inicial asociada a la issue #1 tiene autorización expresa para commit y push directo a `main` sin PR. La excepción termina al publicar esa primera entrega y no se extiende a trabajos posteriores.

El 25 de noviembre de 2026, el usuario pidió trasladar las referencias a los títulos de los commits ya publicados. Se corrigieron los mensajes de las entregas #1 y #2 y su integración, conservando los archivos y la autoría. Esta corrección puntual del historial no autoriza futuras reescrituras de `main`; la nueva convención se recoge en la issue #4.

Respeta las instrucciones expresas del usuario para la tarea actual. Si autoriza otra excepción al flujo, documenta su alcance; no conviertas una excepción puntual en una regla permanente. Las protecciones de ramas y la CI no deben darse por activas solo porque esta guía las describa.
