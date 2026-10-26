# Implementation Plan: Programador y Monitor de Estudios

**Branch**: `001-study-tracker`  
**Specification**: [spec.md](file:///c:/Users/jeffr/OneDrive/Documentos/INFORMATICA/PROYECTOS_EN_DESARROLLO/programadorDeEstudios/studyProgrammer/specs/001-study-tracker/spec.md)  
**Status**: Approved

---

## 1. Arquitectura del Sistema

El sistema implementa una arquitectura desacoplada basada en el patrón **MVVM** (Model-View-ViewModel) dividida en 3 proyectos:

```
StudyTracker.sln
├── src/
│   ├── StudyTracker.Core/             (Biblioteca .NET 8 / Dominio y Datos)
│   │   ├── Models/                    (Topic, StudySession, DailyHeatmapPoint)
│   │   ├── Interfaces/                (ITopicRepository, ISessionRepository, ITimerService, INotificationService)
│   │   ├── Services/                  (TimerService, StatsService, WindowsNotificationService)
│   │   └── Data/                      (DatabaseInitializer, TopicRepository, SessionRepository)
│   │
│   └── StudyTracker.UI/               (WPF con Fluent Design Wpf.Ui)
│       ├── ViewModels/                (MainViewModel, DashboardViewModel, TimerViewModel, StatsViewModel)
│       ├── Views/                     (DashboardView, TimerView, StatsView, AddTopicDialog)
│       └── Controls/                  (HeatmapGridControl, RadialProgress)
│
└── tests/
    └── StudyTracker.Core.Tests/       (Pruebas unitarias xUnit / SQLite in-memory o archivo temporal)
```

---

## 2. Tecnologías y Dependencias NuGet

1. **`Microsoft.Data.Sqlite`**: Conexión nativa a SQLite sin drivers externos.
2. **`Dapper`**: Micro-ORM ultraligero y de alto rendimiento para mapear consultas SQL a objetos C#.
3. **`CommunityToolkit.Mvvm`**: Implementación moderna de `ObservableObject`, `RelayCommand` y mensajería reactiva.
4. **`Wpf.Ui`**: Controles y estilos nativos de Fluent Design para Windows 11 (Mica, Acrílico, iconos Segoe Fluent).
5. **`Microsoft.Toolkit.Uwp.Notifications`**: Despachador de Toast Notifications nativas con sonido y botones de acción en el Action Center de Windows.
6. **`LiveChartsCore.SkiaSharpView.WPF`**: Gráficas reactivas aceleradas por hardware.

---

## 3. Modelo de Datos SQLite (DDL)

```sql
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS topics (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,
    description TEXT,
    color_hex TEXT DEFAULT '#0078D4',
    target_hours REAL DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK(status IN ('ACTIVE', 'COMPLETED', 'ARCHIVED')),
    completed_at TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS study_sessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    topic_id INTEGER NOT NULL,
    started_at TEXT NOT NULL,
    ended_at TEXT NOT NULL,
    planned_minutes INTEGER NOT NULL,
    actual_minutes INTEGER NOT NULL,
    session_type TEXT DEFAULT 'TIMED',
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(topic_id) REFERENCES topics(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_sessions_topic ON study_sessions(topic_id);
CREATE INDEX IF NOT EXISTS idx_sessions_started_at ON study_sessions(started_at);
```

---

## 4. Estrategia de Pruebas y Validación (SDD)

1. **Pruebas de Repositorios (Core)**:
   - Inserción, actualización de estado y lectura de temáticas.
   - Cálculo de horas acumuladas por temática.
   - Registro de sesiones efectivas y agregaciones por día para el mapa de calor.
2. **Pruebas del Motor de Temporizador (Core)**:
   - Precisión de eventos `OnTick`, `OnCompleted`, `OnPaused`.
   - Cálculo correcto de minutos efectivos cuando el usuario cancela antes de tiempo.
3. **Validación de UI (Presentación)**:
   - Cambio de tema claro/oscuro.
   - Disparo de notificación Toast interactiva en Windows 11.
