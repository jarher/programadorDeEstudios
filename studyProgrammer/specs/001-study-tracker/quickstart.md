# Quickstart: StudyTracker

## Requisitos Previos
- Windows 10/11 x64
- .NET 8 SDK instalado (`dotnet --version`)

## Construcción y Ejecución

1. **Restaurar paquetes y compilar**:
   ```powershell
   dotnet restore
   dotnet build
   ```

2. **Ejecutar pruebas unitarias de persistencia**:
   ```powershell
   dotnet test
   ```

3. **Iniciar la aplicación**:
   ```powershell
   dotnet run --project src/StudyTracker.UI
   ```

## Flujo Básico de Uso
1. **Crear Temática**: En el Dashboard, presiona `+ Nueva Temática`, asigna un nombre y meta en horas.
2. **Iniciar Bloque**: En la pestaña del Temporizador, elige la temática, selecciona la duración (ej. 45 min) y presiona `Iniciar`.
3. **Notificación**: Al terminar el tiempo, Windows 11 desplegará un Toast nativo con la campana del sistema y opciones para registrar notas o tomar un descanso.
4. **Ver Progreso**: En la pestaña de Analítica, revisa el mapa de calor con tu racha de días y el gráfico de dona por temática.
