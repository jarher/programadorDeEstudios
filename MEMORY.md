# MEMORY.md — Memoria General del Proyecto
**Proyecto**: StudyProgrammer (Programador y Monitor de Estudios)  
**Entorno**: Windows 11 | C# (.NET 8 LTS - `8.0.427`) | SQLite Local  
**Última Actualización**: 2026-10-26  
**Rama Activa**: `main` | **Remoto**: `https://github.com/jarher/programadorDeEstudios.git`  
**Último hito cerrado**: T-0.0 — Equipo opencode local (2026-10-26)  
**Estado global**: Fase 2 completada (3/3); agentes locales configurados; próxima T-3.1

> Este es el **único** libro de desarrollo del proyecto. No existen memorias por submódulo.

---

## 1. Estado Actual del Desarrollo

| Elemento | Estado |
| :--- | :--- |
| Solución `StudyTracker.sln` | Creada en la raíz con 3 proyectos |
| `StudyTracker.Core` | net8.0 + net8.0-windows10.0.19041.0; dominio, datos, temporizador y notificaciones |
| `StudyTracker.UI` | net8.0-windows10.0.19041.0, WPF; ventana por defecto |
| `StudyTracker.Core.Tests` | net8.0, xUnit; 24 pruebas (14 datos + 10 temporizador) |
| Gate de calidad | `dotnet build` 0 errores / 0 warnings; `dotnet test` 24/24 |
| Modelos de dominio | Implementados en `src/StudyTracker.Core/Models` (T-1.3) |
| Esquema SQLite | `DatabaseInitializer` con tablas, índices y FK (T-1.4) |
| Repositorios | `TopicRepository` y `SessionRepository` implementados (T-1.5/T-1.6) |
| Temporizador | `TimerService` + 10 pruebas deterministas (T-2.1/T-2.2) |
| Notificaciones Windows | `WindowsNotificationService` con toasts y acciones interactivas (T-2.3) |
| Equipo de agentes | `coordinator` + 4 subagentes opencode locales (no versionados) |
| UI Fluent / analítica | Pendiente (Fases 3-5) |

---

## 2. Progreso por Fase

| Fase | Tareas | Avance | Estado |
| :--- | :---: | :---: | :--- |
| Fase 1 — Solución y datos SQLite | 7 | 7/7 | **Completada** |
| Fase 2 — Temporizador y Toasts | 3 | 3/3 | **Completada** |
| Fase 3 — Dashboard y temáticas | 4 | 0/4 | Pendiente |
| Fase 4 — Temporizador y mini-widget | 4 | 0/4 | Pendiente |
| Fase 5 — Mapa de calor y analítica | 4 | 0/4 | Pendiente |

**Próxima tarea**: `T-3.1` — dashboard y CRUD de temáticas en la UI (Fase 3).

---

## 3. Bitácora de Desarrollo

| Fecha | Tarea | Commit | Agente | Gate | Resultado |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 2026-10-26 | T-0.0 | `2f41f0c` | 00-coordinator | — | Baseline documental, `.gitignore`, `opencode.json` y scripts de commit |
| 2026-10-26 | T-1.1 | `f205ba7` | 01-data-core | build 0/0; tests 1/1 | Solución, proyectos Core/UI/Tests y referencias |
| 2026-10-26 | T-1.2 | `541e055` | 01-data-core | build 0/0; tests 1/1 | NuGet Core/UI, TFM de UI y corrección WPF-UI |
| 2026-10-26 | Memoria | `59f2d4f` | 00-coordinator | — | Primera sincronización de MEMORY con el estado real |
| 2026-10-26 | Memoria | `031fc8c` | 00-coordinator | — | Consolidación en un único MEMORY raíz y eliminación del duplicado |
| 2026-10-26 | T-1.3 | `d6e6354` | 01-data-core | build 0/0; tests 1/1 | Modelos de dominio en Core y retiro de `Class1.cs` |
| 2026-10-26 | T-1.4 | `9ba6650` | 01-data-core | build 0/0; smoke test OK | `DatabaseInitializer`, tablas, índices y FK con cascada |
| 2026-10-26 | T-1.5 | `2f7e452` | 01-data-core | build 0/0; tests 5/5 | `ITopicRepository`, `TopicRepository` y pruebas de CRUD/estado |
| 2026-10-26 | T-1.6 | `eda825a` | 01-data-core | build 0/0; tests 9/9 | `ISessionRepository`, `SessionRepository` y actividad diaria |
| 2026-10-26 | T-1.7 | `79e4900` | 04-qa-verification | build 0/0; tests 14/14 | Pruebas de FK, cascada y vacíos; retiro de `UnitTest1.cs` |
| 2026-10-26 | T-2.1 | `a9f50e2` | 02-timer-notifications | build 0/0; tests 14/14 | `ITimerService` y `TimerService` con `TimeProvider` |
| 2026-10-26 | T-2.2 | `0092d5e` | 04-qa-verification | build 0/0; tests 24/24 | 10 pruebas deterministas con `FakeTimeProvider` |
| 2026-10-26 | T-2.3 | `1d59666` | 02-timer-notifications | build 0/0; tests 24/24 | Toasts con acciones interactivas y Core multi-target |
| 2026-10-26 | T-0.0 | `0af9b1c` | 00-coordinator | build 0/0; tests 24/24 | Agentes opencode locales y `.opencode/` ignorado |

---

## 4. Decisiones Arquitectónicas (ADR Log)

* **ADR-001 (Ecosistema y Lenguaje)**: **C# con .NET 8 (LTS)** frente a Python o Node/Electron por rendimiento (< 50 MB RAM), arranque < 1 s y soporte nativo del Action Center.
* **ADR-002 (Capa de Presentación)**: **WPF con WPF-UI** para Fluent Design (Mica, Acrílico, Segoe Fluent) sin el empaquetado MSIX de WinUI 3. El ID correcto en NuGet es **`WPF-UI`** (el paquete `Wpf.Ui` es legacy y solo trae `net451`); el namespace C# sigue siendo `Wpf.Ui`.
* **ADR-003 (Persistencia Local)**: **SQLite con `Microsoft.Data.Sqlite` + Dapper**, con `PRAGMA foreign_keys = ON;`. 100% offline y local-first.
* **ADR-004 (Analítica de Progreso)**: **Mapa de calor estilo GitHub** para consistencia diaria + **dona/barras** para distribución y metas.
* **ADR-005 (Metodología Híbrida)**: **SDD** para Core, esquema y temporizador; **ODD** para la ergonomía de la UI.
* **ADR-006 (Integración de Agentes)**: **GitHub Spec-Kit** configurado para **Gemini/Antigravity** con scripts PowerShell.
* **ADR-007 (Target Framework)**: `Core` y `Tests` en `net8.0`; `UI` en `net8.0-windows10.0.19041.0` con `SupportedOSPlatformVersion=10.0.19041.0`. Requerido por WPF-UI 4.x y por WinRT en Fase 2.
* **ADR-008 (Ubicación de la Base de Datos)**: `%LOCALAPPDATA%\StudyTracker\study_tracker.db`, fuera de OneDrive. Nunca se versiona (`*.db*` en `.gitignore`).
* **ADR-009 (Automatización Git)**: Un commit por tarea con `complete-task.ps1` (gate build+test → marcar `[x]` → `git add .` → `git commit -m` → `git push origin main`). MCP `git`/`github` en `opencode.json`.
* **ADR-010 (Memoria Única)**: `MEMORY.md` en la raíz es el único libro de desarrollo; `tasks.md` es la fuente de verdad del estado de tareas y `MEMORY.md` resume progreso, bitácora y decisiones.
* **ADR-011 (Marcas de Tiempo)**: se persisten en hora local con formato ISO 8601 sin zona; si `CreatedAt` llega vacío, los repositorios lo completan con `DateTime.Now`. Las consultas por día usan `date(started_at)`.
* **ADR-012 (Intensidad del Mapa de Calor)**: cuatro niveles según minutos diarios acumulados: 0 = 0; 1 = 1-29; 2 = 30-59; 3 = 60-119; 4 = 120 o más.
* **ADR-013 (Semántica del Temporizador)**: `ElapsedTime` conserva el tiempo efectivo al pausar o detener; `Start` reinicia; el evento `Tick` entrega el tiempo restante cada 250 ms y se inyecta `TimeProvider` para pruebas deterministas.
* **ADR-014 (Notificaciones y Multi-target)**: Core apunta a `net8.0` y `net8.0-windows10.0.19041.0`; `Microsoft.Toolkit.Uwp.Notifications` 7.1.3 solo se referencia en el TFM de Windows y `WindowsNotificationService` se excluye del TFM portable. Las acciones de los toasts se exponen mediante `ActionInvoked`.
* **ADR-015 (Equipo opencode Local)**: `coordinator` es el agente primario y delega en `data-core`, `timer-notifications`, `ui-presentation` y `qa-verification`. Los permisos por rutas impiden que el coordinador edite código y que los especialistas salgan de su capa. `.opencode/` está ignorado por Git; las fichas de `.specify/agents` siguen siendo la fuente canónica y el `default_agent` se define en `.opencode/opencode.json` local.

---

## 5. Equipo de Agentes

| ID | Rol | Agente opencode | Estado | Ficha |
| :--- | :--- | :--- | :--- | :--- |
| **`00-coordinator`** | Orquestación, ramas, gates | `coordinator` (primary) | **ACTIVO** | [00-coordinator.agent.md](file:///c:/Users/jeffr/OneDrive/Documentos/INFORMATICA/PROYECTOS_EN_DESARROLLO/programadorDeEstudios/studyProgrammer/.specify/agents/00-coordinator.agent.md) |
| **`01-data-core`** | SQLite, Dapper, dominio, Stats | `data-core` (subagent) | Fase 1 completada | [01-data-core.agent.md](file:///c:/Users/jeffr/OneDrive/Documentos/INFORMATICA/PROYECTOS_EN_DESARROLLO/programadorDeEstudios/studyProgrammer/.specify/agents/01-data-core.agent.md) |
| **`02-timer-notifications`** | TimerService, WinRT Toasts | `timer-notifications` (subagent) | Fase 2 completada | [02-timer-notifications.agent.md](file:///c:/Users/jeffr/OneDrive/Documentos/INFORMATICA/PROYECTOS_EN_DESARROLLO/programadorDeEstudios/studyProgrammer/.specify/agents/02-timer-notifications.agent.md) |
| **`03-ui-presentation`** | WPF, WPF-UI, LiveCharts2 | `ui-presentation` (subagent) | Siguiente (Fase 3) | [03-ui-presentation.agent.md](file:///c:/Users/jeffr/OneDrive/Documentos/INFORMATICA/PROYECTOS_EN_DESARROLLO/programadorDeEstudios/studyProgrammer/.specify/agents/03-ui-presentation.agent.md) |
| **`04-qa-verification`** | xUnit, criterios Given-When-Then | `qa-verification` (subagent) | Fase 2 completada | [04-qa-verification.agent.md](file:///c:/Users/jeffr/OneDrive/Documentos/INFORMATICA/PROYECTOS_EN_DESARROLLO/programadorDeEstudios/studyProgrammer/.specify/agents/04-qa-verification.agent.md) |

Los agentes operativos viven en `.opencode/agents/` y están ignorados por Git. Las fichas de `.specify/agents/` siguen siendo la fuente canónica. `default_agent: coordinator` se define en `.opencode/opencode.json`; en un clon sin esa carpeta, opencode cae en `build` con advertencia.

---

## 6. Estructura Real del Repositorio

```
programadorDeEstudios/
├── .gitignore
├── opencode.json                       # MCP git + GitHub y permisos
├── StudyTracker.sln
├── MEMORY.md                           # Este archivo (memoria única)
├── .opencode/                          # local, ignorado por Git
│   ├── agents/                         # coordinator, data-core, timer-notifications, ui-presentation, qa-verification
│   └── opencode.json                   # default_agent: coordinator
├── PLAN_ESPECIFICACION.md
├── src/
│   ├── StudyTracker.Core/              # net8.0 + net8.0-windows10.0.19041.0
│   │   ├── StudyTracker.Core.csproj    # Dapper + SQLite; Notifications 7.1.3 solo en windows
│   │   ├── Data/                       # DatabaseInitializer, TopicRepository, SessionRepository
│   │   ├── Interfaces/                 # ITopicRepository, ISessionRepository, ITimerService, INotificationService
│   │   ├── Models/                     # Topic, StudySession, TopicSummaryDto, DailyActivityDto
│   │   ├── Notifications/              # NotificationAction, NotificationActionEventArgs
│   │   └── Services/                   # TimerService, WindowsNotificationService
│   └── StudyTracker.UI/                # net8.0-windows10.0.19041.0
│       ├── StudyTracker.UI.csproj      # WPF-UI 4.3.0 + CommunityToolkit.Mvvm 8.4.2
│       ├── App.xaml / App.xaml.cs
│       └── MainWindow.xaml / .xaml.cs  # ventana placeholder
├── tests/
│   └── StudyTracker.Core.Tests/        # net8.0
│       ├── StudyTracker.Core.Tests.csproj  # xUnit 2.5.3, Test.Sdk 17.8.0, TimeProvider.Testing 8.10.0
│       ├── TestDatabase.cs             # BD temporal aislada por prueba
│       ├── TopicRepositoryTests.cs     # CRUD, estado y resúmenes
│       ├── SessionRepositoryTests.cs   # sesiones y actividad diaria
│       └── TimerServiceTests.cs        # 10 pruebas con FakeTimeProvider
└── studyProgrammer/
    ├── AGENTS.md                       # Gobernanza multi-agente
    ├── .specify/
    │   ├── feature.json                # Feature activa: 001-study-tracker
    │   ├── memory/constitution.md
    │   ├── agents/                     # Fichas 00-04
    │   └── scripts/powershell/         # commit-task.ps1, complete-task.ps1
    └── specs/001-study-tracker/
        ├── spec.md
        ├── plan.md
        ├── tasks.md                    # Fuente de verdad de tareas
        ├── data-model.md
        ├── quickstart.md
        └── contracts/interfaces.md
```

---

## 7. Notas, Deuda Técnica y Pendientes

* **Placeholders**: ninguno; `Class1.cs` se retiró en T-1.3 y `UnitTest1.cs` en T-1.7.
* **Conexión SQLite**: `DatabaseInitializer` crea el directorio, aplica DDL idempotente y usa `ForeignKeys=true` en el connection string.
* **Pruebas con BD temporal**: llamar `SqliteConnection.ClearAllPools()` antes de borrar el archivo, por el pooling de conexiones.
* **Pruebas del temporizador**: `FakeTimeProvider` (8.10.0) permite avanzar el reloj sin esperas reales; los tests cubren pausa, stop, reinicio y finalización única.
* **Notificaciones**: `Microsoft.Toolkit.Uwp.Notifications` 7.1.3 funciona sin MSIX en Windows 10 1809+; el servicio solo se compila en el TFM de Windows y las acciones llegan por `ActionInvoked`.
* **Agentes opencode**: `.opencode/` está ignorado por Git; los agentes son locales. Se recrean desde `AGENTS.md` §3.2 y las fichas canónicas. El coordinador no edita código y los subagentes no hacen commits.
* **Cierre con scripts**: invocar `complete-task.ps1`/`commit-task.ps1` directamente con `&`, nunca a través de `rtk` (falla con `.ps1`). Solo el coordinador cierra tareas.
* **Alcance del esquema**: `plan.md` define solo `topics` + `study_sessions` + índices. `topic_milestones` y `session_notes` (presentes en `PLAN_ESPECIFICACION.md`) quedaron diferidos hasta confirmación.
* **Documentación desalineada**: `tasks.md` menciona `Wpf.Ui`; el paquete instalado es `WPF-UI` 4.3.0.
* **Rama de feature**: los documentos citan `001-study-tracker`, pero el trabajo se publica en `main`.
* **MCP GitHub**: el servidor remoto requiere `GITHUB_PERSONAL_ACCESS_TOKEN` en el entorno; el MCP `git` local funciona con el Credential Manager.
* **Sin `packages.lock.json`**: la restauración usa versiones fijadas en los `.csproj`.

---

## 8. Protocolo de Mantenimiento de esta Memoria

1. Al iniciar cualquier sesión de desarrollo: leer `MEMORY.md` y `studyProgrammer/specs/001-study-tracker/tasks.md`.
2. Al cerrar cada tarea: actualizar progreso por fase, próxima tarea, bitácora (fecha, tarea, commit, agente, gate) y, si hubo decisión, el ADR correspondiente.
3. `tasks.md` manda sobre el estado de tareas; `MEMORY.md` resume y contextualiza.
4. No crear `MEMORY.md` en submódulos ni duplicar el checklist completo.
5. Todo commit de cierre se hace con `complete-task.ps1` (o `commit-task.ps1` para mantenimiento documental).

---

## 9. Próxima Acción Inmediata

Ejecutar **T-3.1** con el agente `coordinator` (requiere reiniciar opencode para cargar `.opencode/`). El coordinador debe delegar en `ui-presentation`, validar con `qa-verification` y cerrar con:

```powershell
.\studyProgrammer\.specify\scripts\powershell\complete-task.ps1 `
  -TaskId T-3.1 -Type feat -Scope ui `
  -Message "dashboard and topic management" -Agent 03-ui-presentation
```
