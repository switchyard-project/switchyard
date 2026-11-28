# Documentación de Switchyard

El [README principal](../README.md) presenta la aplicación. Aquí guardaremos las explicaciones necesarias para construirla, mantenerla y defender sus decisiones durante la exposición.

## Documentos disponibles

| Documento | Qué encontrarás |
| :--- | :--- |
| [Arranque en Visual Studio](visual-studio.md) | Proyecto de inicio, perfiles de API y robot, y pasos para la primera entrega. |
| [Ejecución y primera demo](ejecucion.md) | Requisitos, comandos, configuración, persistencia y resolución de problemas. |
| [01 · Registro de robots](funcionalidades/01-registro-de-robots.md) | Contrato HTTP, recorrido del código, métodos, decisiones, pruebas y preguntas para la exposición. |
| [02 · Panel web](funcionalidades/02-panel-web.md) | Interfaz adaptable, consultas periódicas, estados de error y decisiones para el futuro acceso móvil. |
| [03 · Tareas de transporte](funcionalidades/03-tareas-de-transporte.md) | Creación, asignación, finalización y persistencia. Demo de consola y preguntas para la defensa. |
| [04 · Panel de tareas](funcionalidades/04-panel-de-tareas.md) | Crear tareas, consultar todos los estados y demostrar cambios desde el cliente .NET; módulos compartidos y límites de la interfaz. |
| [05 · Almacenes y zonas](funcionalidades/05-almacenes-y-zonas.md) | Planos configurables, separación de trabajo, migración y preguntas para la defensa. |
| [06 · Asignación desde la web](funcionalidades/06-asignacion-desde-la-web.md) | Elección de robot, concurrencia con la consola y defensa de las decisiones. |
| [07 · Editor visual](funcionalidades/07-editor-visual-del-almacen.md) | Dibujo por arrastre, fichas, escala en metros y pruebas del navegador. |
| [Criterios de código](criterios-de-codigo.md) | Cómo mantener el código sencillo, legible y verificable en las siguientes entregas. |
| [Cómo trabajamos](flujo-de-trabajo.md) | Responsables, issues, ramas, commits, pull requests y criterios de revisión. Punto de entrada para colaboradores. |
| [Arquitectura del proyecto](arquitectura.md) | Qué componentes existen y qué sigue propuesto para la coordinación de robots. |
| [Identidad visual](identidad-visual.md) | Paleta, símbolo y recursos gráficos del proyecto. |

## Lo que iremos documentando

Cada funcionalidad tendrá su documento en `funcionalidades/`, con el comportamiento, los métodos que lo implementan, las decisiones y sus comprobaciones. El primer documento cubre el registro; los algoritmos de rutas y reservas se documentarán cuando se implementen.

Cada documento deberá dejar claro qué está implementado, cómo se comprueba y qué limitaciones tiene. Los comandos de instalación y arranque se escribirán y verificarán con el código real.

Los asistentes de IA también deben consultar [AGENTS.md](../AGENTS.md) antes de trabajar en el repositorio.
