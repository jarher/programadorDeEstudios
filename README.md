# StudyProgrammer

StudyProgrammer es una aplicación de escritorio para Windows que ayuda a
organizar temáticas, concentrar bloques de estudio y revisar el progreso.
Funciona de forma local: los datos permanecen en una base SQLite del usuario
y no se necesita una cuenta ni un servidor.

## Capturas

Las imágenes se tomaron de la aplicación de Windows en ejecución. Para mostrar
el dashboard y las gráficas, se cargaron datos ficticios de demostración que
se eliminaron después; la base de desarrollo quedó vacía.

### Dashboard

![Dashboard con temáticas y metas de demostración](docs/screenshots/dashboard.png)

### Temporizador

![Temporizador con una temática seleccionada](docs/screenshots/temporizador.png)

### Analítica

![Mapa de actividad, distribución del tiempo y avance de metas](docs/screenshots/analitica.png)

## Funciones principales

- Crear temáticas con descripción, color y meta de horas; consultar su avance
  y marcarlas como completadas.
- Programar sesiones con accesos rápidos de 25, 45 o 60 minutos, o indicar
  una duración personalizada. Pausar o detener el temporizador cuando sea
  necesario.
- Registrar el tiempo efectivo y una nota al cerrar una sesión.
- Usar el temporizador flotante y los controles de la bandeja del sistema.
- Consultar actividad diaria, racha de estudio, tiempo por temática y avance
  de las metas.
- Cambiar entre los temas claro y oscuro.

## Requisitos

- Windows 10 de 64 bits, versión 19041 o posterior, o Windows 11.
- Para desarrollar o compilar: .NET 8 SDK.
- La publicación para Windows x64 es autocontenida; no requiere instalar el
  runtime de .NET por separado.

SQLite está integrado en la aplicación mediante `Microsoft.Data.Sqlite`.
No hace falta instalar un motor, servicio o contenedor de base de datos.
La aplicación crea la carpeta, el archivo y el esquema al iniciarse por
primera vez.

## Ejecutar la aplicación publicada

Desde la raíz del proyecto:

```powershell
.\artifacts\StudyProgrammer-final-win-x64\StudyTracker.UI.exe
```

La carpeta `artifacts` está excluida de Git. Para generar la publicación,
consulta las instrucciones siguientes.

## Desarrollo

Restaurar, compilar y ejecutar las pruebas desde la raíz:

```powershell
dotnet restore .\StudyTracker.sln
dotnet build .\StudyTracker.sln
dotnet test .\StudyTracker.sln
```

Iniciar la interfaz durante el desarrollo:

```powershell
dotnet run --project .\src\StudyTracker.UI\StudyTracker.UI.csproj
```

Publicar la versión autónoma para Windows x64:

```powershell
dotnet publish .\src\StudyTracker.UI\StudyTracker.UI.csproj -p:PublishProfile=win-x64
```

El ejecutable se genera en
`artifacts\StudyProgrammer-final-win-x64\StudyTracker.UI.exe`.
La primera ejecución puede extraer bibliotecas nativas de gráficos.

## Datos y privacidad

La base de datos se crea en:

```text
%LOCALAPPDATA%\StudyTracker\study_tracker.db
```

Temáticas, sesiones y notas se guardan en ese archivo local. No se incluyen
en la publicación ni se comparten con el repositorio. Para hacer una copia de
seguridad, cierra la aplicación y copia el archivo de base de datos.

## Estructura del proyecto

- `src/StudyTracker.Core`: modelos, persistencia SQLite, temporizador,
  notificaciones y servicios de analítica.
- `src/StudyTracker.UI`: aplicación WPF, vistas, navegación y ventanas.
- `tests/StudyTracker.Core.Tests`: pruebas automatizadas de Core.
- `studyProgrammer/specs`: especificación y guía de inicio rápido.
- `docs/screenshots`: capturas incluidas en este README.
