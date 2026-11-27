# Ejecutar Switchyard

Puedes dar de alta robots desde el panel web o el cliente .NET. El panel también permite crear tareas de transporte y consultar sus estados. El cliente también puede pedir y confirmar una entrega simulada. Los movimientos y las señales periódicas de actividad siguen pendientes.

## Requisitos

- SDK de **.NET 10**. Puedes comprobarlo con `dotnet --list-sdks`.
- Acceso a NuGet para la primera restauración de paquetes.
- Una terminal para la API y otra para el cliente, situadas en la raíz del repositorio.

No hace falta instalar un servidor de base de datos: SQLite guarda los datos en un archivo local del servidor. Los proyectos usan el SDK y pueden ejecutarse desde la terminal en macOS, Windows y Linux.

`global.json` solicita la familia de SDK 10.0 y permite las bandas de características posteriores de esa misma versión (`latestFeature`). No selecciona versiones preliminares. `Directory.Build.props` fija `net10.0`, los tipos anulables y las advertencias como errores para todos los proyectos.

Los apuntes del tema 01 presentan Visual Studio como herramienta obligatoria del curso. El funcionamiento por terminal está comprobado; confirmar si el profesor exige además usar ese IDE en la entrega es una cuestión académica pendiente. Incluimos una solución `.sln` para poder abrir los proyectos en un entorno compatible con .NET 10.

## 1. Compilar

Desde la raíz:

```bash
dotnet build Switchyard.sln --configuration Release
```

El comando restaura los paquetes necesarios y compila los cuatro proyectos. Las dependencias directas están versionadas en los `.csproj`; los archivos de compilación quedan fuera de Git.

## 2. Arrancar el servidor

```bash
dotnet run --project src/Switchyard.Api --configuration Release --no-build -- --urls http://localhost:5080
```

Deja esta terminal abierta. La API crea automáticamente el directorio de datos y la tabla si todavía no existen. Abre <http://localhost:5080> en el navegador: verás el panel y, en una base nueva, su estado sin registros. El JSON sigue disponible en <http://localhost:5080/api/robots>.

El primer `--` separa las opciones de `dotnet run` de los argumentos de la aplicación. `--no-build` reutiliza lo compilado en el paso anterior; si cambias el código, vuelve a compilar.

## 3. Dar de alta un robot

Desde el panel, escribe `R01` en **Identificador del robot** y pulsa **Registrar robot**. La confirmación indica que el alta está guardada. La lista vuelve a consultar el servidor después de un alta correcta.

Para demostrar que ambos clientes comparten los datos, deja el panel visible y registra `R02` desde la consola. Aparecerá en una próxima consulta automática, aproximadamente cada cinco segundos si el servidor responde normalmente. **Actualizar** permite consultar antes. Las consultas no se solapan y se omiten mientras hay un alta en curso o la pestaña está oculta; al volver a ella se solicita una actualización.

Si el servidor falla, se conservan los últimos registros recibidos y aparece un aviso. La hora de la última consulta correcta permite reconocer una vista desactualizada. Si todavía no hubo una consulta correcta, no se muestra un cero que pueda confundirse con una base vacía.

### Desde el cliente .NET

En otra terminal:

```bash
dotnet run --project src/Switchyard.Robot --configuration Release --no-build -- R02 http://localhost:5080
```

Verás una confirmación con el identificador y la fecha de alta UTC. El cliente termina con código de salida `0`. Usa otro identificador para registrar más robots. Si omites la URL, se utiliza `http://localhost:5080`.

Un identificador debe tener entre 1 y 32 caracteres: letras ASCII mayúsculas, números, guiones o guiones bajos. El primer carácter debe ser una letra o un número. `R01` y `ROBOT-02_A` son válidos; `r01`, un espacio o `-R01` no lo son. El servidor valida el dato; no lo transforma silenciosamente.

El cliente acepta la dirección raíz del servidor, como `http://localhost:5080`, sin rutas, parámetros, fragmentos ni credenciales dentro de la URL.

## 4. Consultar y comprobar la persistencia

Abre estas direcciones en el navegador:

- <http://localhost:5080/api/robots>: todos los robots, ordenados por identificador.
- <http://localhost:5080/api/robots/R01>: el registro de `R01`.

Para demostrar que el estado no está solo en memoria:

1. Guarda o anota el identificador y la fecha que devuelve la consulta.
2. Detén la API con `Ctrl+C` en su terminal.
3. Arráncala de nuevo con **el mismo comando y archivo de datos**.
4. Consulta `R01`: conserva su identificador y fecha originales.

Con `dotnet run --project src/Switchyard.Api`, la ruta predeterminada es `src/Switchyard.Api/data/switchyard.db`. Las rutas relativas se resuelven desde el directorio de contenido del servidor, no desde el cliente. SQLite puede crear también archivos auxiliares `-wal` y `-shm`; Git los ignora.

Si publicas la API o la ejecutas desde otro directorio, configura una ruta absoluta para mantener clara la ubicación de los datos.

Para comprobar también la persistencia de una asignación y su confirmación, sigue la [demostración de una tarea de transporte](funcionalidades/03-tareas-de-transporte.md#demostración-en-powershell).

## Demo completa: del panel a la consola

Usa un archivo nuevo, como `--Database:Path data/demo-panel-tareas.db`, si quieres que no haya tareas de demostraciones anteriores. Arranca la API como en el paso 2 y abre <http://localhost:5080>.

1. En **Robots**, registra `R01`. Si ya existe en esa base, puedes utilizarlo.
2. En **Tareas**, crea una tarea con recogida `Estante A` y destino `Muelle 1`. Aparecerá como **Pendiente**.
3. En otra terminal, ejecuta este comando (igual en macOS, Linux y PowerShell):

```bash
dotnet run --project src/Switchyard.Robot --configuration Release --no-build -- --work R01 http://localhost:5080
```

4. Deja la consola esperando Intro. En el panel, la tarea aparecerá **Asignada** a `R01` en la próxima consulta; también puedes pulsar **Actualizar**.
5. Pulsa Intro en la consola para confirmar la entrega simulada. El panel mostrará **Completada**, conservando el robot que la realizó.
6. Reinicia la API con el mismo archivo y actualiza el panel: los estados siguen guardados.

El robot retoma primero su tarea activa; si no tiene una, recibe la pendiente con menor identificador. Si hay tareas anteriores, no tiene por qué recibir la que acabas de crear. El panel no ofrece botones para asignar o completar: esas acciones se demuestran desde el proceso .NET.

También puedes consultar <http://localhost:5080/api/tasks> para ver el listado en JSON. Se incluyen pendientes, asignadas y completadas, ordenadas por identificador. El formulario admite entre 1 y 60 caracteres por campo, con contenido distinto de espacios; el servidor elimina los espacios de los extremos.

Si una creación no recibe respuesta, actualiza y comprueba la lista antes de repetirla: el servidor podría haberla guardado. Dos tareas con los mismos lugares son válidas; no existe todavía una clave de operación que evite duplicarlas tras un reintento. No se reintenta el POST automáticamente.

## 5. Mostrar los errores previstos

| Acción | Resultado |
| :--- | :--- |
| Registrar otra vez `R01` | HTTP `409`; el cliente explica que ya existe y termina con salida `1`. La fecha original no cambia. |
| Registrar `r01` | HTTP `400`; el cliente explica el formato admitido. No se guarda. |
| Consultar un identificador inexistente | HTTP `404`. |
| Arrancar el cliente sin la API | Mensaje de conexión fallida y salida `1`. |
| Pasar una URL incorrecta | Mensaje de uso y salida `1`, antes de hacer la petición. |
| No recibir respuesta en 10 segundos | El cliente informa del tiempo de espera y termina con salida `1`. |

Un error de red o un tiempo de espera no prueba que el servidor no haya guardado el registro. Antes de repetir el alta, consulta el identificador cuando la API vuelva a estar disponible. No hay reintentos automáticos.

## Configuración y otro equipo

Para usar un archivo distinto, por ejemplo en una demostración aislada:

```bash
dotnet run --project src/Switchyard.Api --configuration Release --no-build -- --urls http://localhost:5080 --Database:Path data/demo.db
```

También puede configurarse la variable de entorno `Database__Path`; las opciones de línea de comandos tienen prioridad sobre `appsettings.json` y las variables de entorno. Para empezar una demo limpia, usa un nombre de archivo nuevo. No hace falta borrar tus registros anteriores.

Para separar cliente y servidor en dos equipos de una red de prácticas, inicia la API así:

```bash
dotnet run --project src/Switchyard.Api --configuration Release --no-build -- --urls http://0.0.0.0:5080
```

En el cliente sustituye `localhost` por la IP o el nombre del equipo servidor, y comprueba que el puerto sea accesible. Para probar el panel en un móvil, conecta ambos dispositivos a la misma red y abre `http://IP-DEL-SERVIDOR:5080` en su navegador, sustituyendo ese nombre por la IP real. No necesitas instalar .NET en el teléfono. La IP del servidor debe ser accesible desde esa red; algunas redes de invitados aíslan los dispositivos. `0.0.0.0` es una dirección de escucha, no el destino que debes pasar al cliente. Cada proceso cliente solo necesita la API; no accede al archivo SQLite.

Esta entrega usa HTTP para la demo local y no implementa autenticación. El identificador no es una contraseña ni prueba la identidad de un robot. El despliegue con acceso público y sus controles de seguridad quedan fuera de este incremento.

## Pruebas y formato

```bash
dotnet test Switchyard.sln --configuration Release --no-build
dotnet format Switchyard.sln --verify-no-changes --no-restore
```

Ejecuta primero la compilación si todavía no la has hecho o si has cambiado código. Las pruebas crean archivos SQLite temporales propios, no necesitan una API arrancada y no utilizan la base de la demo. El workflow [`.github/workflows/dotnet.yml`](../.github/workflows/dotnet.yml) ejecuta restauración, compilación, pruebas y revisión de formato en Linux y Windows para todos los PR y los cambios integrados en `develop` o `main`. Su resultado debe consultarse en cada PR; tener el workflow no configura por sí solo un check obligatorio.

## Comprobar la interfaz

Los documentos del [panel de robots](funcionalidades/02-panel-web.md) y del [panel de tareas](funcionalidades/04-panel-de-tareas.md) detallan los escenarios de navegador: teclado, alta desde consola, duplicados, fallo de red y recuperación. Las pruebas .NET comprueban que se sirven los recursos del panel; no ejecutan su JavaScript. La CI actual cubre esas pruebas .NET. La comprobación visual y de interacción se realiza además en navegador y se documenta en el PR.

El frontend no necesita una compilación propia ni dependencias de Node para ejecutarse. `dotnet publish src/Switchyard.Api --configuration Release` incluye `wwwroot` en la publicación; los datos permanecen fuera de esa carpeta pública.

## Si algo no arranca

| Síntoma | Qué revisar |
| :--- | :--- |
| `dotnet` no existe o no encuentra un SDK compatible | Instala el SDK 10.0; tener solo el runtime no permite compilar. |
| El puerto 5080 está ocupado | Usa otro puerto en `--urls` y pasa la misma dirección al cliente. |
| SQLite no puede abrir el archivo | Revisa la ruta y los permisos de escritura de su directorio. |
| Aparece `409` al repetir la demo | El registro anterior sigue guardado. Usa otro identificador o un archivo nuevo. |
| El cliente no conecta desde otro ordenador | `localhost` apunta al propio equipo; utiliza la dirección del servidor y comprueba su puerto. |
