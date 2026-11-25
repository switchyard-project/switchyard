# Arquitectura del proyecto

**Estado: registro de robots implementado; coordinación pendiente.** La primera entrega permite registrar y consultar robots mediante una API REST y conservar sus altas en SQLite. El resto de esta arquitectura describe el objetivo de la simulación. Este documento mantiene las decisiones técnicas fuera de la presentación principal.

## Lo que existe hoy

- `Switchyard.Api`: ASP.NET Core sobre .NET 10, con controladores para el registro y las consultas.
- `Switchyard.Robot`: cliente de consola .NET que realiza un alta por HTTP y termina.
- `Switchyard.Contracts`: tipos de los mensajes JSON; no contiene lógica de movimiento ni acceso a datos.
- `Switchyard.Tests`: pruebas xUnit del contrato, el cliente y la persistencia en SQLite real.

El servidor mantiene una tabla `robots` con el identificador y la fecha de alta UTC. La clave primaria impide duplicar registros, incluso ante solicitudes concurrentes. Esto todavía no permite conocer la posición o disponibilidad de un robot. El [documento de la funcionalidad](funcionalidades/01-registro-de-robots.md) recoge el contrato, los métodos y los límites de esta entrega.

## Punto de partida

Los requisitos de la asignatura son una aplicación distribuida con comunicación cliente-servidor mediante REST o SOAP, conservación del estado y un cliente .NET. La tecnología del frontend es libre.

Usamos REST con JSON. Cada robot es un ejecutable .NET lanzado como un proceso independiente. Ya existe un servidor central para registrar robots; más adelante coordinará sus tareas. SQLite conserva las altas. El panel mostrará el almacén y permitirá introducir eventos de prueba cuando se implemente.

Se podrá demostrar la comunicación entre procesos en un mismo ordenador. La API deberá poder configurarse por dirección para que también sea posible separar clientes y servidor en distintas máquinas.

## Simulación acotada

- Mapa bidimensional en cuadrícula, con estanterías, celdas transitables y puntos de recogida y entrega.
- Movimientos a celdas vecinas en los cuatro sentidos, sin diagonales.
- Avance por turnos lógicos coordinados por el servidor. El protocolo de autorización y confirmación de cada movimiento está pendiente de concretar.
- Un único servidor coordinador en la primera versión. Esto simplifica la exclusión mutua, pero introduce un punto único de fallo.
- Diez clientes independientes como objetivo de la demo. No es una capacidad medida todavía.

## Responsabilidades

| Componente | Responsabilidad propuesta |
| :--- | :--- |
| Cliente robot (.NET) | Identificarse, pedir trabajo, informar de su estado y ejecutar únicamente movimientos autorizados. |
| API y coordinador | Validar peticiones, asignar tareas, calcular rutas, gestionar reservas y registrar cambios. |
| Base de datos | Conservar mapas, tareas, asignaciones, último estado confirmado y eventos relevantes. |
| Panel | Consultar el estado e introducir acciones de demostración a través del servidor. |

El panel no sustituye a los procesos de los robots. Animar diez iconos dentro del navegador no demostraría diez clientes independientes.

## Rutas, reservas y bloqueos

Como punto de partida evaluaremos A* para la búsqueda de caminos. Encontrar un camino sobre un mapa estático no basta para coordinar robots: también hay que gestionar cuándo puede utilizarse cada espacio.

La reserva y la autorización de movimientos deberán impedir que dos robots ocupen la misma celda en el mismo turno y que intercambien posiciones atravesándose en un pasillo. La comprobación y la concesión de una reserva deberán ser atómicas frente a solicitudes concurrentes.

Para el primer escenario usaremos un mapa con apartaderos y reglas de prioridad explícitas. Eso reduce la complejidad, pero **no demuestra la ausencia de interbloqueos**. Tendremos que definir cómo detectar que no hay progreso y qué recuperación admite el simulador: esperar, recalcular o retroceder hacia un espacio disponible. Si no existe una salida segura, el sistema deberá mostrar el bloqueo.

## Paralelismo

Proponemos calcular varias rutas en paralelo sobre una instantánea del mapa. Antes de autorizar un movimiento, el coordinador deberá validar la propuesta contra las reservas y los obstáculos vigentes: la situación puede haber cambiado durante el cálculo.

Compararemos el cálculo secuencial y el paralelo con las mismas peticiones y mapas. Registraremos tiempos y condiciones de la prueba; con mapas pequeños, el coste de coordinar el trabajo podría superar la mejora. La concurrencia de los robots y el paralelismo del cálculo se demostrarán por separado.

## Estado y fallos

La base de datos deberá permitir reconstruir qué tareas estaban pendientes, cuáles estaban asignadas y cuál era el último estado confirmado de cada robot. Queda por decidir cómo almacenar las reservas y cómo reconciliarlas al reiniciar. Guardar datos no significa que sea seguro continuar automáticamente con ellos.

Los clientes enviarán señales periódicas de actividad. Un tiempo de espera permitirá marcar un robot como no disponible, pero la falta de respuesta no prueba que haya abandonado su celda. Su última posición y cualquier movimiento sin confirmar deberán mantenerse como ocupación posible hasta resolver la incertidumbre.

La primera recuperación se limitará a reasignar una tarea de recogida que el robot desconectado todavía no hubiera iniciado. Si ya transportaba una carga, la tarea quedará pendiente de una recuperación específica; no supondremos que la carga aparece en otro robot.

También habrá que definir identificadores de operación y reglas de idempotencia: reintentar una petición después de perder una respuesta no deberá duplicar una tarea ni ejecutar dos veces un movimiento.

## Decisiones pendientes

| Decisión | Qué tendremos en cuenta |
| :--- | :--- |
| Evolución de la persistencia | Empezamos con SQLite. Revisaremos migraciones y límites de concurrencia cuando incorporemos tareas y movimientos. |
| Frontend y actualización del panel | Claridad visual y coste de implementación; empezar con la opción más sencilla que cumpla. |
| Protocolo de turnos y reservas | Confirmaciones, tiempos de espera, prevención de conflictos y recuperación. |
| Política de prioridades | Evitar que un robot quede esperando indefinidamente. |
| Despliegue y arranque | Una ejecución local reproducible y configuración de la dirección del servidor. |

## Comprobaciones previstas

1. Una entrega completa desde un proceso .NET, pasando por la API y conservando su estado en la base de datos.
2. Peticiones simultáneas sobre la misma celda y movimientos enfrentados sin autorizaciones incompatibles.
3. Un obstáculo nuevo en una celda libre, con invalidación de rutas afectadas antes de avanzar.
4. Desconexión antes de recoger una carga, detección de ausencia y reasignación sin duplicados.
5. Reinicio del servidor con lectura del estado persistido y reconciliación antes de reanudar la simulación.
6. Un escenario de bloqueo que active la política de recuperación o muestre que no hay una salida segura.
7. Comparación reproducible del cálculo secuencial y paralelo.

Estas comprobaciones de coordinación son objetivos de desarrollo. Ya se han probado el registro y la consulta por HTTP, el rechazo de duplicados simultáneos y la conservación de las altas tras reiniciar. Todavía no hay tareas, movimientos ni reservas que reanudar.
