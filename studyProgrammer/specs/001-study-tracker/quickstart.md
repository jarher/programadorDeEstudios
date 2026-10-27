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

4. **Publicar el ejecutable autónomo Windows x64**:
   ```powershell
   dotnet publish src/StudyTracker.UI -p:PublishProfile=win-x64
   ```
   El ejecutable autocontenido se genera en `artifacts/StudyProgrammer-win-x64/StudyTracker.UI.exe`.
   No requiere instalar el runtime de .NET; la primera ejecución puede extraer las bibliotecas nativas de SkiaSharp.

## Flujo Básico de Uso
1. **Crear Temática**: En el Dashboard, presiona `+ Nueva Temática`, asigna un nombre y meta en horas.
2. **Iniciar Bloque**: En la pestaña del Temporizador, elige la temática, selecciona la duración (ej. 45 min) y presiona `Iniciar`.
3. **Notificación**: Al terminar el tiempo, Windows 11 desplegará un Toast nativo con la campana del sistema y opciones para registrar notas o tomar un descanso.
4. **Ver Progreso**: En Analítica, revisa el mapa de calor, la racha, la distribución por temática y el avance de las metas.
