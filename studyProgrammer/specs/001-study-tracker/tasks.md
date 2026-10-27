# Tasks: Programador y Monitor de Estudios

**Feature Branch**: `001-study-tracker`  
**Status**: Ready for Implementation  
**Specification**: [spec.md](file:///c:/Users/jeffr/OneDrive/Documentos/INFORMATICA/PROYECTOS_EN_DESARROLLO/programadorDeEstudios/studyProgrammer/specs/001-study-tracker/spec.md) | **Plan**: [plan.md](file:///c:/Users/jeffr/OneDrive/Documentos/INFORMATICA/PROYECTOS_EN_DESARROLLO/programadorDeEstudios/studyProgrammer/specs/001-study-tracker/plan.md)

---

## Fase 1: Configuración de la Solución y Capa de Datos SQLite (SDD)

- [x] **T-1.1**: Inicializar la solución .NET 8 `StudyTracker.sln` con los proyectos `StudyTracker.Core`, `StudyTracker.UI` (WPF) y `StudyTracker.Core.Tests`.
- [x] **T-1.2**: Instalar paquetes NuGet esenciales: `Microsoft.Data.Sqlite`, `Dapper`, `CommunityToolkit.Mvvm`, `Wpf.Ui`.
- [x] **T-1.3**: Implementar modelos de dominio en `StudyTracker.Core`: `Topic`, `StudySession`, `TopicSummaryDto`.
- [x] **T-1.4**: Implementar `DatabaseInitializer` para crear automáticamente las tablas `topics`, `study_sessions` e índices en SQLite.
- [x] **T-1.5**: Implementar repositorio `TopicRepository` (CRUD, cálculo de horas acumuladas, cambio de estado activo/completado).
- [x] **T-1.6**: Implementar repositorio `SessionRepository` (inserción de sesión, consulta de historial por fechas).
- [x] **T-1.7**: Escribir y ejecutar pruebas unitarias en `StudyTracker.Core.Tests` para verificar la persistencia y cálculos de SQLite.

---

## Fase 2: Motor de Temporizador y Notificaciones Windows 11 (SDD)

- [x] **T-2.1**: Implementar `ITimerService` y `TimerService` en `StudyTracker.Core` con soporte para cuenta regresiva, pausas y eventos `Tick` y `Completed`.
- [x] **T-2.2**: Escribir pruebas unitarias para `TimerService` (verificar cálculo exacto de tiempo efectivo transcurrido al pausar o terminar).
- [x] **T-2.3**: Implementar `WindowsNotificationService` usando `Microsoft.Toolkit.Uwp.Notifications` para enviar Toasts de inicio y Toasts interactivos de fin de sesión con botones (`Descanso`, `Extender`, `Finalizar`).

---

## Fase 3: Dashboard Principal y Gestión de Temáticas (ODD / UI)

- [x] **T-3.1**: Configurar la ventana principal con `Wpf.Ui` y soporte para Fluent Design (Mica de Windows 11 y tema claro/oscuro).
- [x] **T-3.2**: Implementar `DashboardViewModel` con comandos para listar temáticas, alternar estado a *Completada* y calcular el resumen general.
- [x] **T-3.3**: Diseñar la vista `DashboardView` con tarjetas de temáticas, barra de progreso hacia la meta de horas, y checkbox de marcaje.
- [x] **T-3.4**: Crear el diálogo modal ligero `AddTopicDialog` para registrar nuevas temáticas con selector de color.

---

## Fase 4: Vista de Temporizador Bajo Demanda (ODD / UI)

- [ ] **T-4.1**: Implementar `TimerViewModel` que conecta la temática seleccionada con `TimerService`.
- [ ] **T-4.2**: Diseñar la interfaz del temporizador con selector de duración (presets de 25m, 45m, 60m o entrada manual), barra circular de progreso y botones `Iniciar`, `Pausar`, `Detener`.
- [ ] **T-4.3**: Implementar diálogo de micro-journaling al finalizar el bloque para registrar notas de lo aprendido.
- [ ] **T-4.4**: Implementar modo mini-widget o minimización en la bandeja del sistema (System Tray).

---

## Fase 5: Analítica de Progreso y Métricas (ODD / UI)

- [ ] **T-5.1**: Implementar `StatsService` para calcular el historial de actividad diaria de los últimos 3 meses y la racha de días activos.
- [ ] **T-5.2**: Diseñar el control visual del **Mapa de Calor** (cuadrícula estilo GitHub) con gradiente de intensidad de estudio.
- [ ] **T-5.3**: Integrar gráficos con `LiveCharts2`: Gráfica de dona (distribución porcentual por temática) y barras de avance de metas.
- [ ] **T-5.4**: Pruebas de integración de extremo a extremo y empaquetado del ejecutable.
