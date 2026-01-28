# Changelog

Todas las mejoras notables de este proyecto serán documentadas en este archivo.

## [v1.2.0] - 2026-01-28 "The Visual Update"

### Añadido

- **Soporte WebP Animado:** Integración completa con `ImageSharp` para reproducir archivos `.webp` con transparencia y tiempos de frame variables.
- **Soporte de Imagen Estática:** Ahora se pueden cargar archivos `.png`, `.jpg` y `.jpeg` como overlays fijos.
- **Chroma Key Avanzado:**
  - Opción "Usar Chroma Key" independiente por overlay.
  - **Herramienta Gotero:** Permite seleccionar el color exacto del fondo haciendo clic en el overlay.
  - **Selector de Color:** Selección manual de color mediante diálogo estándar.
  - **Slider de Tolerancia:** Ajuste de precisión (10-150) para suavizar bordes y eliminar colores similares.
- **Modo Gotero Interactivo:** Cursor en forma de cruz al seleccionar colores para mejor precisión.

### Mejorado

- El slider de tolerancia solo se habilita cuando el Chroma Key está activo.
- Persistencia de datos actualizada para incluir configuraciones de Chroma y tolerancia.

## [v1.1.0] - 2026-01-27 "Performance & Grid"

### Añadido

- **Grid UI:** Nueva interfaz de cuadrícula con miniaturas para gestionar los overlays.
- **Pausa Inteligente:** Opción en la bandeja del sistema para pausar/reanudar todas las animaciones y liberar CPU/GPU.
- **Previsualización Transparente:** Las miniaturas en el grid respetan la transparencia del archivo original.

### Corregido

- **Inicialización Fluida:** Eliminado el parpadeo blanco/negro al abrir nuevos overlays; ahora aparecen con el tamaño y posición correctos instantáneamente.
- **Iconos:** Integración de iconos como recursos embebidos para builds de un solo archivo.

## [v1.0.0] - 2026-01-26

### Lanzamiento Inicial

- Visualización de GIFs animados con fondo transparente.
- Posicionamiento y redimensionamiento básico.
- Menú de bandeja del sistema.
- Guardado automático de configuración.
