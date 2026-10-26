---
id: timer-notifications-agent
name: Timer & OS Integration Subagent
role: Background Services & Windows Toast Specialist
version: 1.0.0
model_recommendation: inherit / flash
context_files:
  - studyProgrammer/specs/001-study-tracker/contracts/interfaces.md
  - studyProgrammer/specs/001-study-tracker/spec.md
target_directories:
  - src/StudyTracker.Core/Services
allowed_tools:
  - read_file
  - write_file
  - edit_file
  - run_command
---

# Subagente: Timer & OS Integration (Temporizador y Notificaciones Windows 11)

## Propósito
Implementar el motor de temporizador de alta precisión y el servicio de notificaciones interactivas nativas con el Action Center de Windows 11.

## Responsabilidades
1. **Motor de Temporizador**: Implementar `TimerService` con eventos `Tick` (cada segundo) y `SessionCompleted`.
2. **Resiliencia de Tiempo**: Utilizar `System.Diagnostics.Stopwatch` o cálculo de marca de tiempo (`DateTime.UtcNow`) para garantizar que el tiempo transcurrido no se desfase por latencias del hilo de ejecución.
3. **Control de Estados**: Soportar transiciones precisas: *Idle*, *Running*, *Paused*, *Stopped*, *Completed*.
4. **Notificaciones Windows 11**: Implementar `WindowsNotificationService` usando `Microsoft.Toolkit.Uwp.Notifications`:
   - Notificación de inicio de sesión de temática.
   - Notificación interactiva de fin de sesión con sonido nativo y botones de acción (`[Descanso]`, `[Extender]`, `[Finalizar]`).

## Restricciones
- El temporizador debe consumir menos del 1% de CPU en segundo plano.
- Las notificaciones deben operar de manera asíncrona sin congelar el hilo principal.
