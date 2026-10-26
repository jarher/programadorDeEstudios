# Feature Specification: Programador y Monitor de Estudios

**Feature Branch**: `001-study-tracker`  
**Created**: 2026-10-26  
**Status**: Ready  
**Input**: Aplicación de escritorio local en Windows 11 para programar bloques de estudio por temática bajo demanda, notificar inicio y fin con Windows Toasts nativos, dashboard con estado/marcaje de temáticas, y métricas con mapa de calor de hábitos y gráficas de distribución.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Dashboard y Gestión Ágil de Temáticas (Priority: P1)
Como estudiante, quiero una pantalla principal tipo dashboard donde pueda visualizar todas mis temáticas de estudio, ver el tiempo acumulado dedicado a cada una, su estado actual (*Activa*, *Completada*), marcarlas como terminadas y añadir nuevas temáticas rápidamente.

**Why this priority**: Es la base del sistema y el punto de entrada diario. Sin temáticas registradas no se puede asociar ninguna sesión de estudio.

**Independent Test**: Puede probarse creando 3 temáticas con metas de horas, alternando el estado de completado y verificando que persistan en SQLite.

**Acceptance Scenarios**:
1. **Given** la aplicación abierta en el Dashboard, **When** el usuario hace clic en `+ Nueva Temática` e ingresa título, color y meta de horas, **Then** la temática aparece de inmediato en la lista y se almacena en SQLite.
2. **Given** una temática activa con horas acumuladas, **When** el usuario marca el checkbox de `Completada`, **Then** el estado visual cambia a *Completada*, se registra la fecha de finalización y se actualiza el resumen del dashboard.
3. **Given** una lista de temáticas, **When** se reinicia la aplicación, **Then** todas las temáticas conservan su estado, tiempo acumulado y colores.

---

### User Story 2 - Temporizador Bajo Demanda y Notificaciones Windows 11 (Priority: P1)
Como estudiante, quiero seleccionar una temática, fijar un tiempo de estudio (en minutos u horas, o mediante accesos rápidos de 25m, 45m o 60m), iniciar el cronómetro y recibir una notificación Toast interactiva en Windows 11 al terminar el bloque para registrar mi tiempo.

**Why this priority**: Es la mecánica funcional central del producto que disciplina el tiempo de estudio.

**Independent Test**: Iniciar un bloque de prueba de 5 segundos, comprobar que corre en segundo plano y verificar que al vencer se dispare la notificación Toast con sonido y botones de acción.

**Acceptance Scenarios**:
1. **Given** una temática seleccionada, **When** el usuario selecciona `45 min` y presiona `Iniciar`, **Then** el cronómetro inicia la cuenta regresiva y se emite una notificación discreta de inicio.
2. **Given** una sesión en curso, **When** el temporizador llega a cero, **Then** suena la alerta del sistema, aparece una notificación en el Action Center de Windows 11 con botones `[Descanso 5m]`, `[Extender 15m]` y `[Finalizar]`, y los minutos efectivos se guardan en la tabla `study_sessions`.
3. **Given** una sesión en curso, **When** el usuario presiona `Pausa` o `Detener`, **Then** el cronómetro se detiene y solo registra el tiempo real efectivamente estudiado.

---

### User Story 3 - Analítica de Progreso: Mapa de Calor y Gráficas de Rendimiento (Priority: P2)
Como estudiante, quiero ver un panel de estadísticas con un mapa de calor estilo GitHub que muestre mi racha diaria de estudio y gráficos de distribución de horas para saber qué temáticas he priorizado y cuánto me falta para mis metas.

**Why this priority**: Genera el bucle de retroalimentación y motivación psicológica ("no romper la cadena").

**Independent Test**: Cargar sesiones históricas ficticias en SQLite y verificar que el mapa de calor pinte las casillas con la intensidad correcta según los minutos del día.

**Acceptance Scenarios**:
1. **Given** sesiones registradas en los últimos 3 meses, **When** el usuario abre la pestaña `Estadísticas`, **Then** el mapa de calor muestra la cuadrícula semanal/mensual con celdas coloreadas según intensidad de estudio.
2. **Given** múltiples temáticas estudiadas, **When** se visualiza el gráfico de dona, **Then** se muestra el porcentaje exacto de horas asignadas a cada temática.
3. **Given** una temática con meta fijada (ej. 20 horas), **When** se acumulan sesiones, **Then** la barra de progreso refleja el avance porcentual hacia la meta.

---

### User Story 4 - Mini-Widget Flotante y Micro-Journaling de Sesión (Priority: P3)
Como estudiante, quiero que al iniciar un bloque de estudio pueda minimizar la ventana principal a un mini-contador flotante translúcido y, al finalizar la sesión, ingresar una breve nota sobre lo que aprendí.

**Why this priority**: Aumenta la concentración (Deep Work) y apoya la retención de memoria (Active Recall).

**Acceptance Scenarios**:
1. **Given** una sesión activa, **When** el usuario presiona `Modo Flotante`, **Then** la ventana principal se oculta y aparece una píldora compacta semi-transparente en la esquina con el tiempo restante.
2. **Given** una sesión concluida, **When** se registra la sesión, **Then** un campo de texto rápido permite ingresar una nota ("Comprendí interfaces en C#") que queda vinculada al historial.

---

## Requisitos No Funcionales (RNF)

1. **Rendimiento**: Consumo de memoria RAM inferior a 50 MB en segundo plano.
2. **Persistencia**: Base de datos local SQLite (`study_tracker.db`) con claves foráneas e índices automáticos.
3. **Diseño**: Aplicación de tema claro/oscuro compatible con Fluent Design en Windows 11.
4. **Portabilidad**: Ejecutable autónomo sin necesidad de configurar servidores ni conexión a internet.
