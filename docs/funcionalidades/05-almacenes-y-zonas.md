# 05 · Un espacio para cada almacén

Antes de registrar robots, elegimos dónde vamos a trabajar. La página inicial permite crear un almacén con nombre y dimensiones; al abrirlo vemos su plano y podemos añadir estanterías, bahías y puntos de carga. Cada almacén conserva sus propios robots y tareas.

Es un primer paso hacia un simulador configurable para distintas empresas. No es una plataforma de control de robots físicos ni un sistema de cuentas por empresa: los almacenes separan el trabajo, pero no son una barrera de permisos.

El [editor visual](07-editor-visual-del-almacen.md) amplía esta entrega: permite dibujar por arrastre y consultar fichas. La escala actual es 1 × 1 metro por celda; ancho y largo se muestran en metros.

## Una demo corta

1. Arranca la API como indica [la guía](../ejecucion.md) y abre `/`.
2. Crea «Almacén norte», con 10 filas y 12 columnas, y ábrelo.
3. Añade «Estantería A»: tipo estantería, fila 2, columna 3, alto 5 y ancho 2.
4. Añade «Bahía de salida»: fila 9, columna 8, alto 1 y ancho 3.
5. En **Robots**, registra `NORTE-01`. En **Tareas**, crea una recogida y un destino.
6. Vuelve a **Almacenes** y abre el de demostración: sus listas son independientes.
7. Detén y reinicia la API con la misma base. El plano, las zonas y los registros siguen ahí.

El número de robots se calcula a partir de las altas reales. Crear «7 robots» de golpe sería confundir siete registros con siete procesos conectados. Para ejecutar el robot registrado en la web:

```bash
dotnet run --project src/Switchyard.Robot --configuration Release --no-build -- --work NORTE-01 http://localhost:5080
```

El servidor obtiene el almacén a partir de ese identificador. El alta por consola anterior sigue usando el almacén de demostración; para otro almacén, utiliza el panel o envía `warehouseId` en `POST /api/robots`.

## Contratos y reglas

| Petición | Resultado |
| :--- | :--- |
| `GET /api/warehouses` | Almacenes con dimensiones y número de robots registrados. |
| `POST /api/warehouses` | Crea con `{ "name": "Norte", "rows": 10, "columns": 12 }`; devuelve 201 y Location. |
| `GET /api/warehouses/{id}` | Consulta un almacén; 404 si no existe. |
| `GET /api/warehouses/{id}/zones` | Lista sus zonas. |
| `POST /api/warehouses/{id}/zones` | Crea con nombre, `kind`, `startRow`, `startColumn`, `rows` y `columns`. |
| `GET /api/robots?warehouseId=2` | Lista solo los robots de ese almacén. |
| `GET /api/tasks?warehouseId=2` | Lista solo sus tareas. |

`warehouseId` se admite también al crear robots y tareas. Si se omite, vale 1 para conservar la compatibilidad. Los identificadores de robot siguen siendo únicos en toda la instalación. Consultar directamente un robot o una tarea por su identificador devuelve además su almacén.

Las dimensiones y coordenadas van de 1 a 50. La celda superior izquierda es (1, 1). `shelf`, `dock` y `charging` representan estantería, bahía y carga. Los nombres se recortan en sus extremos; un nombre de almacén es único, y un nombre de zona es único dentro del suyo. La comparación de nombres distingue mayúsculas y acentos, como SQLite por defecto.

Una zona debe caber en el plano y no solaparse con otra. Dos zonas pueden tocarse por un borde. Campos inválidos o fuera de límites devuelven 400; duplicados y solapamientos, 409; almacén desconocido, 404. No hay edición ni borrado en esta entrega: primero dejamos estable la creación y la consulta.

## Recorrido y métodos

| Pieza | Qué hace y por qué |
| :--- | :--- |
| `CreateWarehouseRequest`, `CreateZoneRequest` | Validación de campos en la frontera HTTP mediante DataAnnotations. |
| `WarehousesController` | `Create` y `CreateZone` traducen las operaciones en respuestas HTTP; `GetAll`, `GetById` y `GetZones` consultan. |
| `WarehouseRepository` | SQL parametrizado. `Create` evita nombres repetidos; `GetAll`/`GetById` cuentan altas; `GetZones` lee rectángulos. |
| `CreateZone` del repositorio | Comprueba límites y ausencia de intersecciones dentro de la misma escritura que inserta. |
| `DatabaseInitializer.Initialize` | Crea o migra el esquema antes de aceptar peticiones, dentro de una transacción. |
| `AddWarehouseColumn` | Añade la columna solo si falta, con valor 1 para los datos anteriores. Los nombres de tabla son constantes internas. |
| Repositorios de robots y tareas | Guardan y filtran `warehouse_id`; `Claim` restringe la selección al almacén del robot. |
| `warehouse-api.js` | Peticiones HTTP y comprobación de respuestas. |
| `warehouse-context.js` | Lee `?warehouse=ID`, consulta el almacén y conserva su contexto en los enlaces. Sin selección vuelve al inicio; ante fallo deshabilita la operación y explica cómo reintentar. |
| `warehouses-dashboard.js` | Dibuja enlaces a los almacenes y conecta el formulario con el ciclo de consultas compartido. |
| `warehouse-dashboard.js` | Dibuja celdas y rectángulos con CSS Grid, añade una descripción textual de cada zona y conecta su formulario. |
| `collection-panel.js` | Reutiliza consulta cada cinco segundos, envío, errores y conservación del último resultado; ahora admite también selects. |

La selección está en la URL: dos pestañas pueden abrir almacenes distintos sin cambiarse mutuamente. Los textos recibidos se insertan con `textContent`, no como HTML.

## Migración sin borrar la demo anterior

Antes había dos tablas. Ahora se crean `warehouses` y `warehouse_zones` y se añade `warehouse_id` a `robots` y `transport_tasks`. El almacén 1 se crea como «Almacén de demostración», de 12 × 12. Los identificadores, fechas, asignaciones y entregas anteriores se conservan.

La conexión de migración desactiva las claves foráneas porque SQLite no permite añadir una referencia con valor por defecto no nulo cuando están activadas. Toda la modificación ocurre en una transacción y `foreign_key_check` comprueba las referencias antes de confirmar. Si falla, se revierte y la API no arranca. Las conexiones de los repositorios sí activan las claves foráneas. No se reutiliza la conexión de migración para atender clientes. [Reglas de ALTER TABLE de SQLite](https://www.sqlite.org/lang_altertable.html).

El proceso se puede repetir al reiniciar: comprueba la presencia de las columnas y utiliza `CREATE ... IF NOT EXISTS`. No hace falta borrar una base antigua para ejecutar esta versión.

## Preguntas para la exposición

**¿Cómo evitamos que dos personas dibujen sobre el mismo sitio?** La comprobación del rectángulo y la inserción forman una sola sentencia de escritura. SQLite serializa las escrituras; la segunda ve el resultado de la primera. Comprobar con un SELECT independiente y luego insertar dejaría una ventana de carrera.

**¿Cómo se detecta la intersección?** Dos intervalos se solapan si el inicio de cada uno está antes del final exclusivo del otro. Se aplica a filas y columnas a la vez. Usar desigualdad estricta permite zonas adyacentes.

**¿Esto calcula rutas?** Todavía no. Las celdas vacías representan espacio de circulación, pero no se comprueba su conectividad. Las recogidas y destinos siguen siendo nombres libres, sin vínculo obligatorio con una zona. El próximo incremento espacial deberá relacionarlos con coordenadas y calcular caminos.

**¿Un robot puede recoger trabajo de otro almacén?** `Claim` filtra las tareas usando el almacén guardado del robot, no un almacén enviado por quien pide trabajo. No cambia de almacén durante esta entrega.

**¿Sirve para cualquier empresa?** Permite describir varios espacios con una cuadrícula común. Distintos fabricantes, unidades físicas, permisos y seguridad operativa requieren decisiones adicionales; no se presentan como resueltos.

## Comprobaciones

`WarehouseTests` cubre creación, validación, duplicados, zonas adyacentes y concurrentes, almacenes desconocidos, separación de tareas, reinicio y migración repetida de una base anterior con tareas asignadas y completadas. `WebDashboardTests` comprueba que los recursos públicos se sirven. La revisión de navegador añade el recorrido real de formularios, navegación con contexto, recarga, pantalla estrecha y accesibilidad automática. Estas últimas comprobaciones no sustituyen una auditoría completa de accesibilidad.
