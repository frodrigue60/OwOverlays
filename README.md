# OwOverlays

OwOverlays es una aplicación ligera para Windows que permite superponer GIFs animados directamente sobre tu escritorio o cualquier otra ventana con transparencia real por píxel.

## Características

- **Formatos Soportados:** Soporte completo para **GIF, WebP** (animados), **PNG y JPG** (estáticos).
- **Chroma Key (Pantalla Verde):** Elimina cualquier color de fondo con un solo clic. Incluye **gotero** y slider de **tolerancia** para bordes perfectos.
- **Transparencia Real:** Los overlays se integran perfectamente utilizando el canal alfa o chroma key.
- **Cuadrícula de Gestión:** Interfaz visual moderna para previsualizar y organizar tus overlays.
- **Optimización de Rendimiento:** Sistema de pausa inteligente que reduce el consumo de CPU a cero cuando no se usa.
- **Persistencia:** Guarda automáticamente posiciones, tamaños, colores de chroma y configuraciones.
- **System Tray:** Se ejecuta discretamente en la bandeja del sistema.

## Inicio Rápido

1. Descarga la última versión desde la carpeta `dist`.
2. Ejecuta `OwOverlays.exe`.
3. Haz clic derecho en el icono de la bandeja (un círculo verde/rojo) para agregar GIFs o mostrar la ventana de gestión.

## Desarrollo

Si deseas compilar el proyecto manualmente:

```powershell
dotnet build
dotnet run
```

Para compilar una versión sin requerimiento de dependencias:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "./dist"
```

## Licencia

Este proyecto está bajo la licencia MIT.
