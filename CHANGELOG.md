# Changelog

Todos los cambios notables de este proyecto se documentarán en este archivo.

## [1.2.0] - 2026-01-28

### Agregado

- **Soporte Multi-Monitor:** Nuevo selector de pantalla para mover overlays entre monitores.
- **Auto-Rotación:** Los overlays rotan automáticamente al arrastrarse a los bordes de pantalla.
- **Slider de Tolerancia Contextual:** El slider de tolerancia de Chroma Key solo se habilita cuando Chroma Key está activo.
- **Tolerancia Extendida:** Rango de tolerancia aumentado de 10-100 a 10-150.

### Cambiado

- Reemplazado el control de orientación manual por selector de pantalla.
- Los overlays ahora persisten su configuración de monitor entre sesiones.

### Eliminado

- ComboBox de orientación manual (ahora es automático por snap a bordes).

---

## [1.1.0] - 2026-01-28

### Agregado

- **Chroma Key con Tolerancia:** Algoritmo mejorado de eliminación de fondo con slider de tolerancia.
- **Gotero de Color:** Selección visual del color de chroma directamente desde el overlay.
- **Soporte WebP:** Animaciones WebP ahora son soportadas junto con GIF.
- **Grid UI:** Nueva cuadrícula visual para gestionar overlays con miniaturas.

### Mejorado

- Bordes suavizados en Chroma Key para resultados más naturales.
- Optimización de rendimiento en animaciones.

---

## [1.0.0] - 2026-01-27

### Lanzamiento Inicial

- Overlays de GIF animados con transparencia real por píxel.
- Snap automático a bordes de pantalla.
- Bloqueo/desbloqueo de overlays.
- Persistencia de configuración.
- Ejecución en System Tray.
