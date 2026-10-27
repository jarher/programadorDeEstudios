# Data Model: StudyTracker

## Entidades Principales

### 1. Topic (Temática de Estudio)
Representa un área o materia que el usuario desea estudiar.

| Campo | Tipo | Restricción | Descripción |
| :--- | :--- | :--- | :--- |
| `Id` | INTEGER | PRIMARY KEY AUTOINCREMENT | Identificador único |
| `Title` | TEXT | NOT NULL | Nombre de la temática |
| `Description` | TEXT | NULLABLE | Detalle o propósito |
| `ColorHex` | TEXT | DEFAULT '#0078D4' | Color de la etiqueta |
| `TargetHours` | REAL | DEFAULT 0 | Meta en horas |
| `Status` | TEXT | CHECK(status IN ('ACTIVE','COMPLETED','ARCHIVED')) | Estado actual |
| `CompletedAt` | TEXT | NULLABLE (ISO 8601) | Fecha de conclusión |
| `CreatedAt` | TEXT | NOT NULL DEFAULT CURRENT_TIMESTAMP | Fecha de creación |

---

### 2. StudySession (Sesión de Estudio Registrada)
Representa un bloque de tiempo de estudio efectivamente realizado.

| Campo | Tipo | Restricción | Descripción |
| :--- | :--- | :--- | :--- |
| `Id` | INTEGER | PRIMARY KEY AUTOINCREMENT | Identificador único |
| `TopicId` | INTEGER | FOREIGN KEY -> topics(Id) | Temática asociada |
| `StartedAt` | TEXT | NOT NULL (ISO 8601) | Inicio de la sesión |
| `EndedAt` | TEXT | NOT NULL (ISO 8601) | Fin de la sesión |
| `PlannedMinutes` | INTEGER | NOT NULL | Minutos fijados al inicio |
| `ActualMinutes` | INTEGER | NOT NULL | Minutos reales estudiados |
| `SessionType` | TEXT | DEFAULT 'TIMED' | 'TIMED' o 'POMODORO' |
| `Notes` | TEXT | NULLABLE | Micro-journaling |
| `CreatedAt` | TEXT | NOT NULL DEFAULT CURRENT_TIMESTAMP | Fecha de registro |

---

### 3. DTOs de Vista y Agregación

#### `TopicSummaryDto`
- `Topic`: Entidad base.
- `TotalMinutesStudied`: Suma acumulada de `ActualMinutes`.
- `ProgressPercentage`: `(TotalMinutesStudied / (TargetHours * 60)) * 100`.
- `LastStudiedAt`: Fecha más reciente en `study_sessions`.

#### `DailyActivityDto` (Para Mapa de Calor)
- `Date`: `yyyy-MM-dd`.
- `TotalMinutes`: Minutos acumulados en ese día calendario.
- `IntensityLevel`: Nivel del 0 al 4 para pintar la cuadrícula.

#### `StudyStatisticsDto` (Resumen de analítica)
- `FromDate` y `ToDate`: límites inclusivos del período reciente de tres meses.
- `DailyActivity`: una entrada por cada día del período; los días sin sesiones tienen 0 minutos e intensidad 0.
- `CurrentStreak`: días activos consecutivos hasta hoy, o hasta ayer si hoy aún no hay actividad.
