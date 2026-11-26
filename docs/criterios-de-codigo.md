# Criterios para escribir el código

Queremos código que podamos entender, modificar y explicar. Tomamos como guía principios de Clean Code: nombres expresivos, responsabilidades claras, funciones pequeñas, tratamiento explícito de errores y pruebas útiles. No se trata de añadir patrones por cumplir una lista.

## Una entrega, un comportamiento concreto

Antes de programar, definimos en la issue qué podrá hacer el usuario al terminar. Si una tarea mezcla registro, navegación y recuperación ante fallos, la dividimos. Lo que todavía no necesitamos se queda como propuesta en la documentación, no como clases vacías en el código.

## Nombres y responsabilidades

- Código y nombres de proyectos, clases y métodos en inglés. Documentación y mensajes de consola en español.
- Clases y métodos en `PascalCase`, variables y parámetros en `camelCase`; campos privados con `_camelCase`.
- En C#, métodos asíncronos terminados en `Async`. Devuelven `Task` o `Task<T>`; `async void` se reserva para manejadores de eventos que lo exijan.
- Cada método hace una operación reconocible. Preferimos condiciones de salida claras y poca anidación.
- Los comentarios explican motivos o límites que el código no revela; no narran cada instrucción.

El controlador traduce peticiones y respuestas HTTP, el repositorio concentra SQL y el cliente HTTP concentra la comunicación. Cuando exista lógica de coordinación suficiente, se extraerá a sus propias clases. En el registro actual no añadimos una capa de servicios que solo reenviaría llamadas.

En el panel web usamos funciones y variables en `camelCase`, módulos JavaScript y textos de interfaz en español. Separamos comunicación HTTP y manipulación de la página. La validación del formulario mejora la experiencia; la API conserva la validación definitiva.

## Reutilizar con criterio

Los contratos de mensajes compartidos evitan repetir su estructura en el servidor y el cliente .NET. Compartimos datos, no el acceso a la base ni una llamada directa al servidor. La frontera real entre procesos sigue siendo HTTP y JSON.

No creamos una interfaz por cada clase ni repositorios genéricos para anticipar necesidades futuras. Añadimos abstracciones cuando haya comportamientos alternativos o una responsabilidad real que separar. La repetición mecánica de unas pocas instrucciones de apertura y cierre no obliga a ocultar SQL sencillo detrás de delegados o capas adicionales.

## Datos y errores

- El servidor valida toda entrada externa. La interfaz del cliente no es una barrera suficiente.
- El SQL usa parámetros; no se concatenan datos recibidos dentro de una sentencia.
- Las invariantes de almacenamiento, como la unicidad, se apoyan en restricciones de base de datos.
- Los recursos que implementan `IDisposable` tienen un propietario claro y se liberan con `using`.
- Las excepciones no se silencian. Se traducen en el límite adecuado: HTTP en el servidor, mensaje y código de salida en la consola.
- La configuración variable se pasa por configuración o argumentos. Las constantes de protocolo y los valores de una demo local no son secretos.

## Concurrencia y asincronía

Esperar una respuesta de red con `await` no equivale a calcular en paralelo. No envolvemos cualquier operación en `Task.Run` para darle aspecto asíncrono. Antes de compartir un objeto, identificamos qué estado mutable contiene y cómo se protege.

Las pruebas de concurrencia deben comprobar resultados observables: por ejemplo, una sola alta ante diez solicitudes del mismo identificador. Una prueba que pasa no demuestra que todos los futuros escenarios de coordinación sean seguros.

## Verificación y documentación

Cada cambio de comportamiento incluye comprobaciones proporcionadas al riesgo. Para HTTP y persistencia preferimos probar el recorrido completo con una base temporal real; para respuestas erróneas del servidor podemos sustituir el transporte del cliente. No escribimos pruebas que solo comprueben que un constructor asigna sus propiedades.

Antes del PR ejecutamos los comandos de [la guía de ejecución](ejecucion.md), revisamos el diff y documentamos resultados y límites. Las advertencias de compilación se tratan como errores y `.editorconfig` mantiene el formato común.

Cada funcionalidad tendrá un documento en `funcionalidades/` con:

1. Qué hace, qué queda fuera y cómo demostrarla.
2. Contrato HTTP y estado almacenado, si aplica.
3. Recorrido de una petición y mapa de clases y métodos.
4. Decisiones, alternativas descartadas y límites.
5. Pruebas y preguntas que puedan surgir en la exposición.

La documentación debe permitir razonar sobre el código. Si ninguno de los dos puede explicar una parte, debemos revisarla antes de integrar.
