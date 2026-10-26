# StudyProgrammer Constitution
<!-- Reglas y Principios Fundamentales del Sistema de Programación y Seguimiento de Estudios -->

## Core Principles

### I. Local-First & Privacidad Absoluta (NON-NEGOTIABLE)
Todos los datos de estudio, temáticas, notas y métricas residen exclusivamente en una base de datos local SQLite (`study_tracker.db`). La aplicación nunca envía telemetría ni requiere conexión a internet para operar.

### II. Integración Nativa con Windows 11 & Fluent Design
La interfaz y el ciclo de vida de la aplicación deben respetar las guías de diseño de Windows 11:
- Soporte para tema claro y oscuro del sistema operativo.
- Uso de Fluent Design (Mica, Acrílico, bordes redondeados).
- Integración nativa con el Action Center de Windows mediante Toast Notifications interactivas con sonido del sistema.
- Minimización opcional en la bandeja del sistema (System Tray).

### III. Arquitectura Limpia MVVM & Separación Core-UI (Spec-Driven Core)
- **StudyTracker.Core**: Biblioteca de clases pura en .NET 8, sin dependencias de UI, conteniendo la lógica de dominio, repositorios SQLite, cálculo de rachas/métricas y motor de temporizador.
- **StudyTracker.UI**: Capa de presentación WPF con Fluent UI (`Wpf.Ui`) desacoplada mediante `CommunityToolkit.Mvvm`.
- **StudyTracker.Tests**: Pruebas unitarias que validan la persistencia SQLite, el avance del temporizador y los cálculos estadísticos antes de conectar la UI.

### IV. Rendimiento y Cero Fricción (< 50 MB RAM)
- El arranque en frío debe ser menor a 1 segundo.
- El consumo de memoria en segundo plano no debe superar los 50 MB de RAM.
- El motor de SQLite debe utilizar índices sobre fechas y claves foráneas (`PRAGMA foreign_keys = ON;`).

### V. Desarrollo Guiado por Especificaciones (SDD)
Cada funcionalidad sigue el ciclo formal: Especificación (`spec.md`) -> Plan Técnico (`plan.md`) -> Tareas Atómicas (`tasks.md`) -> Implementación (`tasks-execution`).

## Restricciones Tecnológicas
- **Lenguaje**: C# (.NET 8 LTS)
- **Base de Datos**: SQLite vía `Microsoft.Data.Sqlite` + Dapper
- **UI Framework**: WPF con `Wpf.Ui` (Windows 11 Fluent Design)
- **Notificaciones**: `Microsoft.Toolkit.Uwp.Notifications`
- **Gráficos**: `LiveChartsCore.SkiaSharpView.WPF` (aceleración por GPU)

**Version**: 1.0.0 | **Ratified**: 2026-10-26
