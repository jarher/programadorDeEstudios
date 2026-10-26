# MEMORY.md — Memoria Operativa e Índice del Sistema Multi-Agente
**Proyecto**: StudyProgrammer (Programador y Monitor de Estudios)  
**Entorno**: Windows 11 | C# (.NET 8 LTS - `8.0.427`) | SQLite Local  
**Última Actualización**: 2026-10-26  
**Rama Activa**: `001-study-tracker`  

---

## 1. Índice de Agentes y Estado de Asignación

| ID del Agente | Rol Operativo | Especialidad / Límites | Estado Actual | Ficha de Especificación |
| :--- | :--- | :--- | :---: | :--- |
| **`00-coordinator`** | Lead Orchestrator | Despacho de tareas, control de ramas, aprobación de calidad | **ACTIVO** | [00-coordinator.agent.md](file:///c:/Users/jeffr/OneDrive/Documentos/INFORMATICA/PROYECTOS_EN_DESARROLLO/programadorDeEstudios/studyProgrammer/.specify/agents/00-coordinator.agent.md) |
| **`01-data-core`** | SQLite & Dominio | DDL SQLite, Dapper, modelos C#, repositorios asíncronos | *En espera (Fase 1)* | [01-data-core.agent.md](file:///c:/Users/jeffr/OneDrive/Documentos/INFORMATICA/PROYECTOS_EN_DESARROLLO/programadorDeEstudios/studyProgrammer/.specify/agents/01-data-core.agent.md) |
| **`02-timer-notifications`** | Servicios & WinRT | `TimerService`, Stopwatch, Toasts interactivos de Windows 11 | *En espera (Fase 2)* | [02-timer-notifications.agent.md](file:///c:/Users/jeffr/OneDrive/Documentos/INFORMATICA/PROYECTOS_EN_DESARROLLO/programadorDeEstudios/studyProgrammer/.specify/agents/02-timer-notifications.agent.md) |
| **`03-ui-presentation`** | Fluent UI & MVVM | WPF, `Wpf.Ui` (Mica/Acrílico), ViewModels, `LiveCharts2` | *En espera (Fases 3-5)* | [03-ui-presentation.agent.md](file:///c:/Users/jeffr/OneDrive/Documentos/INFORMATICA/PROYECTOS_EN_DESARROLLO/programadorDeEstudios/studyProgrammer/.specify/agents/03-ui-presentation.agent.md) |
| **`04-qa-verification`** | QA & Compliance | xUnit, validación de criterios *Given-When-Then*, límites de RAM | *En espera (Gates)* | [04-qa-verification.agent.md](file:///c:/Users/jeffr/OneDrive/Documentos/INFORMATICA/PROYECTOS_EN_DESARROLLO/programadorDeEstudios/studyProgrammer/.specify/agents/04-qa-verification.agent.md) |

---

## 2. Decisiones Arquitectónicas Registradas (ADR Log)

* **ADR-001 (Ecosistema y Lenguaje)**: Se seleccionó **C# con .NET 8 (LTS)** frente a Python o Node/Electron para obtener máximo rendimiento (< 50 MB RAM), arranque en < 1s y soporte nativo del Action Center de Windows 11.
* **ADR-002 (Capa de Presentación)**: Se seleccionó **WPF con `Wpf.Ui`** por ofrecer diseño nativo Fluent Design (Mica, Acrílico, iconos Segoe Fluent) con estabilidad probada en .NET 8 sin los problemas de empaquetado MSIX de WinUI 3.
* **ADR-003 (Persistencia Local)**: Uso de **SQLite con `Microsoft.Data.Sqlite` y Dapper** con activación forzosa de claves foráneas (`PRAGMA foreign_keys = ON;`). 100% offline y local-first.
* **ADR-004 (Analítica de Progreso)**: Enfoque visual híbrido: **Mapa de Calor estilo GitHub** para consistencia diaria (racha de hábitos) + **Gráficos de dona/barras** para metas de temáticas.
* **ADR-005 (Metodología Híbrida)**: **SDD (Spec-Driven)** para blindar Core, esquema y temporizador; **ODD (Organic-Driven)** para iterar la ergonomía de la interfaz de usuario.
* **ADR-006 (Integración de Agentes)**: Implementación de **GitHub Spec-Kit** configurado para **Gemini/Antigravity** con scripts en PowerShell (`ps`).

---

## 3. Estado de Ejecución de Tareas (`tasks.md`)

```
[ ] FASE 1: Solución .NET 8 y Capa de Datos SQLite (Asignada a: 01-data-core)
    [ ] T-1.1: Crear solución StudyTracker.sln y proyectos Core, UI, Tests
    [ ] T-1.2: Instalar NuGet (Microsoft.Data.Sqlite, Dapper, Wpf.Ui, CommunityToolkit.Mvvm)
    [ ] T-1.3: Crear modelos Topic, StudySession, TopicSummaryDto
    [ ] T-1.4: Implementar DatabaseInitializer con DDL de SQLite
    [ ] T-1.5: Implementar TopicRepository
    [ ] T-1.6: Implementar SessionRepository
    [ ] T-1.7: Pruebas unitarias de persistencia xUnit (Asignada a: 04-qa-verification)

[ ] FASE 2: Temporizador & Notificaciones Windows (Asignada a: 02-timer-notifications)
[ ] FASE 3: Dashboard & Temáticas UI (Asignada a: 03-ui-presentation)
[ ] FASE 4: Temporizador Bajo Demanda & Widget Flotante (Asignada a: 03-ui-presentation)
[ ] FASE 5: Mapa de Calor & Analítica LiveCharts2 (Asignada a: 03-ui-presentation)
```

---

## 4. Mapa Rápido de Archivos del Proyecto

```
programadorDeEstudios/
├── PLAN_ESPECIFICACION.md              # Documento maestro de especificación
├── studyProgrammer/
│   ├── MEMORY.md                       # Este archivo (memoria e índice vivo)
│   ├── AGENTS.md                       # Topología y gobernanza multi-agente
│   ├── .specify/
│   │   ├── memory/constitution.md      # Constitución del proyecto
│   │   └── agents/                     # Fichas declarativas de agentes (00 al 04)
│   └── specs/001-study-tracker/
│       ├── spec.md                     # Requisitos e historias de usuario
│       ├── plan.md                     # Arquitectura técnica y DDL
│       ├── tasks.md                    # Desglose de tareas ejecutables
│       ├── data-model.md               # Modelo relacional y DTOs
│       ├── quickstart.md               # Guía de compilación y ejecución
│       └── contracts/interfaces.md     # Interfaces de servicios C#
```

---

## 5. Próxima Acción Inmediata
El **Agente Coordinador (`00-coordinator`)** debe autorizar al **Subagente Data & Core (`01-data-core`)** para ejecutar las tareas **T-1.1 a T-1.6** correspondientes a la creación de la solución `StudyTracker.sln`, proyectos y repositorios SQLite.
