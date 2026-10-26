# Research: Decisiones de Diseño y Stack Tecnológico

## 1. Selección de Stack: C# (.NET 8) y SQLite
- **Motivación**: Máxima integración con Windows 11, tiempo de arranque casi nulo y consumo de memoria bajo (< 50 MB de RAM).
- **Persistencia**: `Microsoft.Data.Sqlite` con `Dapper`. Proporciona un rendimiento mucho más veloz que Entity Framework Core completo, permitiendo migraciones DDL explícitas y consultas con tipado fuerte.

## 2. Framework de Interfaz: WPF con `Wpf.Ui`
- **Motivación**: Permite aplicar el lenguaje de diseño **Fluent Design** de Windows 11 (efectos de fondo Mica, Acrílico, iconos Segoe Fluent y bordes redondeados) sin la sobrecarga ni los problemas de empaquetado MSIX que a veces presenta WinUI 3.

## 3. Sistema de Notificaciones: `Microsoft.Toolkit.Uwp.Notifications`
- **Motivación**: Invoca de manera directa el runtime WinRT / Windows Toast API de Windows 10/11.
- **Capacidades**: Permite interactividad completa en el Action Center (botones de "Descanso 5m", "Extender", "Finalizar") con sonidos nativos del sistema.

## 4. Visualización de Datos: `LiveCharts2` (SkiaSharp)
- **Motivación**: Gráficas aceleradas por hardware para WPF, con animaciones fluidas y reactivas al cambio de datos. Permite renderizar gráficos de dona, barras y cuadrículas tipo mapa de calor sin caídas de framerate.
