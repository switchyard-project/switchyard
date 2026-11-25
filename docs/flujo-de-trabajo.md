# Cómo trabajamos en Switchyard

Queremos que cualquiera pueda abrir el repositorio y entender qué estamos haciendo, quién se encarga y por qué se tomó cada decisión. Esta guía es el acuerdo de trabajo del equipo y el punto de partida para nuevos colaboradores y asistentes de IA.

El recorrido habitual será **issue → rama → commits → pull request → revisión → integración en `main`**. También se aplica a documentación, correcciones y configuración del repositorio.

## Un equipo de dos

Cada tarea tendrá una persona responsable y la otra revisará el resultado. No fijamos que uno haga siempre el servidor y el otro la interfaz: ambos debemos entender la aplicación y poder defenderla delante del profesor.

| Integrante | Cuenta de GitHub | Papel en cada tarea |
| :--- | :--- | :--- |
| Marcos | [@mglezz](https://github.com/mglezz) | Responsable o revisor, según la issue. |
| Carlos | Pendiente de registrar | Responsable o revisor, según la issue. |

El reparto real vive en las **issues**, no en esta tabla. Usamos el campo **Assignees** para indicar quién responde de la tarea. Hasta conocer la cuenta de Carlos y comprobar su acceso, podemos escribir su nombre en la descripción, dejando claro que la asignación en GitHub está pendiente. Nunca inventamos usuarios ni asignamos trabajo al compañero sin haberlo acordado.

## 1. Definir la tarea antes de empezar

Antes de cambiar archivos, buscamos si ya existe una issue que cubra el trabajo. Si no existe, la creamos con la plantilla de tarea o de error disponible en GitHub.

Una issue útil cuenta el problema, el resultado esperado, el alcance y cómo sabremos que está terminada. También indica responsable, revisor previsto y dependencias. Por ejemplo, «Registrar un robot a través de la API» es más fácil de implementar y revisar que «Hacer el backend».

Los criterios de aceptación deben poder comprobarse. «La API funciona bien» es demasiado ambiguo; «Un cliente .NET registra su identificador y el servidor lo conserva en la base de datos» describe un resultado concreto.

Para seguir el progreso usamos estas situaciones:

| Situación | Cómo se reconoce |
| :--- | :--- |
| Pendiente | Issue abierta con alcance definido. |
| En curso | Responsable asignado y rama enlazada en la descripción de la issue o mediante un PR en borrador. |
| En revisión | PR listo para revisar y revisión solicitada al compañero. |
| Bloqueada | La descripción de la issue explica el impedimento y qué hace falta para continuar. |
| Terminada | Cambio integrado en `main`, criterios cumplidos e issue cerrada. |

No hace falta un tablero para empezar. Si más adelante usamos GitHub Projects o etiquetas, deberán reflejar estas mismas situaciones.

## 2. Trabajar en una rama por tarea

`main` es la versión compartida y debe conservarse en un estado coherente. El trabajo nuevo sale de una copia actualizada de `main` y vuelve mediante un pull request. No necesitamos una rama `develop` ni ramas permanentes por persona.

Nombramos las ramas así:

```text
<tipo>/<numero-de-issue>-<descripcion-breve>
```

Usamos minúsculas, palabras en inglés y guiones, sin espacios ni tildes. Ejemplos **ficticios**, que deben sustituirse por issues reales:

```text
feat/12-register-robots
fix/18-prevent-duplicate-reservations
docs/23-document-demo-setup
```

Con el árbol de trabajo limpio y la issue ya creada, el inicio sería:

```bash
git switch main
git pull --ff-only origin main
git switch -c feat/12-register-robots
```

Antes de cambiar de rama, revisamos `git status`. Si hay trabajo pendiente, lo conservamos y aclaramos a qué tarea pertenece; no lo borramos ni lo mezclamos con otra entrega. En una rama compartida, incorporamos los cambios de `origin/main` mediante merge para evitar reescribir los commits del compañero. No hacemos force push sobre `main` ni sobre ramas compartidas.

## 3. Escribir commits que cuenten el cambio

Usamos [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) y añadimos una regla propia: **cada commit de trabajo debe referenciar una issue real** con `Refs: #numero` en el pie del mensaje.

Los mensajes de commit y los títulos de PR van en inglés, con una descripción breve en imperativo. Las issues, las explicaciones de los PR y la documentación van en español. Así mantenemos un historial consistente y una conversación cómoda para el equipo.

```text
feat(api): register robots with persistent identifiers

Store robot registrations so clients can reconnect with the same identity.

Refs: #12
```

| Tipo | Cuándo usarlo |
| :--- | :--- |
| `feat` | Añadir una capacidad nueva. |
| `fix` | Corregir un fallo. |
| `docs` | Cambiar documentación o recursos explicativos. |
| `refactor` | Reorganizar código sin cambiar su comportamiento. |
| `test` | Añadir o ajustar pruebas. |
| `perf` | Mejorar el rendimiento. |
| `build` | Cambiar compilación o dependencias. |
| `ci` | Cambiar automatizaciones de integración. |
| `chore` | Mantenimiento que no encaje en los tipos anteriores. |

El ámbito entre paréntesis identifica la parte afectada, por ejemplo `api`, `robot`, `simulation`, `db` o `repo`. Un cambio incompatible usa `!` y un pie `BREAKING CHANGE:` que explique qué deben adaptar sus consumidores.

Cada commit debe agrupar un cambio que tenga sentido por sí mismo. Evitamos mensajes como `cambios`, `fix stuff` o `final final`, y no mezclamos una funcionalidad con una limpieza ajena a la tarea. Tampoco añadimos contraseñas, tokens, archivos locales de configuración privada ni resultados de compilación.

Antes de confirmar, revisamos lo que vamos a incluir:

```bash
git diff
git add ruta/al/archivo
git diff --cached
git diff --cached --check
git commit -m "feat(api): register robots with persistent identifiers" -m "Refs: #12"
```

Las rutas y el número de estos ejemplos son orientativos. Seleccionamos los archivos de nuestra tarea; no incorporamos cambios del compañero por accidente. Conservamos la autoría real configurada en Git y no atribuimos revisiones ni contribuciones a personas que no las hayan realizado.

## 4. Abrir un pull request por entrega

Publicamos la rama y abrimos un PR hacia `main`. Si la implementación sigue en marcha, lo abrimos en borrador. Cada PR debe cubrir una tarea acotada; si una issue resulta demasiado grande, la dividimos en tareas menores enlazadas.

```bash
git push -u origin feat/12-register-robots
```

El título sigue el formato de los commits. La descripción usa la plantilla del repositorio para explicar qué cambia, cómo se comprobó y qué limitaciones quedan. Los cambios visuales incluyen una captura o ilustración cuando ayude a revisarlos.

Usamos `Closes #12` en la descripción cuando el PR completa la issue. GitHub la cerrará al integrar el PR en la rama predeterminada, que aquí es `main`. Si solo aporta una parte, usamos `Refs: #12` y mantenemos la issue abierta. Reservamos las palabras de cierre para el PR, en lugar de cerrar tareas desde commits intermedios. [Referencia de GitHub](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/linking-a-pull-request-to-an-issue).

No dejamos números de ejemplo ni apartados vacíos en un PR listo para revisar. Solicitamos la revisión del compañero y esperamos su aprobación antes de integrar. Una revisión automática o de IA puede ayudar, pero no sustituye esa revisión humana.

## 5. Revisar e integrar

Quien revisa comprueba el comportamiento y sus límites, además de leer el código. Prestará especial atención a solicitudes simultáneas, persistencia y desconexiones cuando el cambio afecte a esos aspectos.

Para considerar una entrega terminada:

- Cumple los criterios de aceptación de la issue.
- Se han ejecutado las comprobaciones apropiadas y se indica su resultado real en el PR.
- La documentación explica los cambios de uso o de arquitectura relevantes.
- No contiene secretos, archivos generados innecesarios ni cambios ajenos.
- El compañero ha aprobado la versión vigente y se han resuelto sus observaciones.
- Los controles automatizados exigidos pasan, cuando existan.

Si se modifica el comportamiento después de una aprobación, se solicita una nueva revisión. La integración habitual será **Squash and merge**: un commit por PR en `main`. Antes de confirmar el squash, revisamos su título y mantenemos `Refs: #numero` en el mensaje para conservar la referencia a la tarea.

Después de integrar, comprobamos que la issue se haya cerrado si correspondía, eliminamos la rama remota terminada y actualizamos nuestra copia local de `main`. No cerramos una tarea únicamente porque se haya subido una rama.

## Validar sin inventar resultados

Las comprobaciones dependen del cambio. En documentación revisamos enlaces, ejemplos y gráficos. Cuando exista código, incluiremos los comandos reales de compilación y pruebas, y verificaremos los escenarios relevantes. Los cambios en API o persistencia necesitarán comprobar la integración; los de coordinación, los conflictos entre clientes.

No escribimos «tests OK» si no se han ejecutado. Si algo no puede verificarse, indicamos qué falta y por qué. Las decisiones importantes de arquitectura se recogen en `docs/`, con su motivo y sus consecuencias, para que no dependan de recordar una conversación.

## Contexto para asistentes de IA

[AGENTS.md](../AGENTS.md) contiene las instrucciones de entrada para trabajar en este repositorio. Una IA debe leer esta guía, revisar el estado local y consultar la issue antes de empezar; respetar el alcance y la autoría; y dejar un resumen de lo cambiado, lo verificado y lo pendiente.

La persona responsable sigue siendo responsable del resultado. Antes de integrar código generado, debemos poder explicar qué hace y por qué está ahí. Si falta contexto, se registra la duda y se evita presentar suposiciones como decisiones del equipo.

## Qué está preparado y qué queda por configurar

Esta entrega incorpora la guía, `AGENTS.md`, plantillas de issues y una plantilla de PR. **Estos archivos orientan el trabajo; por sí solos no bloquean pushes ni obligan a revisar.**

Queda pendiente comprobar el acceso de Carlos y configurar en GitHub la protección de `main`: PR obligatorio, al menos una aprobación del compañero, resolución de conversaciones y revisión renovada tras cambios. Los checks obligatorios se añadirán cuando exista una automatización real. No damos por activada ninguna de estas protecciones en esta entrega. [Referencia sobre ramas protegidas](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches).

### Excepción de la entrega inicial

El 25 de noviembre de 2026, el propietario autorizó expresamente subir la documentación inicial directamente a `main`, sin PR. El trabajo se sigue en la [issue #1](https://github.com/mglezz/switchyard/issues/1) y sus commits deben referenciarla. Esta excepción solo cubre esa entrega; no autoriza futuros cambios directos en `main`.
