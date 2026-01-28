# OwOverlays

OwOverlays es una aplicación ligera para Windows que permite superponer GIFs animados directamente sobre tu escritorio o cualquier otra ventana con transparencia real por píxel.

## Características

- **Transparencia Real:** Los GIFs se integran perfectamente sin bordes ni fondos molestos.
- **Cuadrícula de Gestión:** Interfaz visual moderna para previsualizar y organizar tus overlays.
- **Optimización de Rendimiento:** Sistema de pausa inteligente que detiene las animaciones para ahorrar recursos.
- **Persistencia:** Guarda tus posiciones y configuraciones automáticamente.
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
