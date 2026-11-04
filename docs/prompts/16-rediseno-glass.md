# Rediseño glass v2

## Prompt

Usa la skill diseno-unilink, en especial la sección "Estilo glass (v2)". Estamos en la rama feature/rediseno-glass. Rediseña SOLO la parte visual (no cambies controladores ni lógica):
1. Login y Registro: pantalla completa con wwwroot/img/campus/campus-edificio.webp de fondo + degradado; tarjeta de vidrio centrada con logo UniLink, título "Bienvenido de vuelta" / "Únete a UniLink", subtítulo "Comunidad estudiantil USMP", inputs con íconos (correo, candado, usuario), botón turquesa con brillo, enlaces "¿Olvidaste tu contraseña?" y "¿No tienes cuenta? Regístrate". Responsivo: en móvil la tarjeta ocupa el 92% del ancho.
2. Landing: hero de altura mínima 85vh con campus-aereo.webp de fondo + degradado; título, eslogan y botones dentro de un panel de vidrio; la tarjeta "Oportunidades cerca de ti" también en vidrio. Las demás secciones de la landing se quedan limpias pero con más presencia: íconos en los pasos y tarjetas de pilares con hover.
3. Navbar con efecto vidrio sticky; Panel con tarjetas de estadísticas en vidrio sobre una franja con degradado índigo; widget del asistente con encabezado en vidrio.
4. Agrega las variables CSS del estilo glass en site.css como clases reutilizables (.ul-glass, .ul-glass-input, .ul-hero-foto) con fallback para navegadores sin backdrop-filter.
5. Pie de página con "Fotos: campus USMP".
6. Verifica contraste y legibilidad en escritorio y móvil, y que las imágenes carguen con loading y tamaños adecuados.
7. Ejecuta dotnet build. Crea docs/prompts/16-rediseno-glass.md con este prompt y un resumen.
8. NO ejecutes git.

## Resumen

- Login y registro usan un fondo de campus con degradado, tarjeta de vidrio centrada, marca, iconografía, inputs de 52 px y botón turquesa con brillo. En móvil la tarjeta ocupa el 92% del ancho.
- El hero de inicio usa la foto aérea con altura mínima de 85vh; el panel principal y la tarjeta de oportunidades tienen efecto glass. Los pasos incluyen iconos y las tarjetas de pilares tienen hover moderado.
- La barra de navegación es sticky y translúcida; las estadísticas del panel tienen tarjetas de vidrio sobre un degradado índigo; el encabezado del asistente también usa glass.
- Se agregaron estilos y variables reutilizables, degradados con suficiente opacidad para mejorar el contraste y fallback opaco cuando no se soporta `backdrop-filter`.
- Las fotos de hero cargan eager con `fetchpriority="high"` y dimensiones intrínsecas declaradas. Los recursos disponibles tienen las rutas reales `campus-edificio.webp.png` (PNG, 814×668) y `campus-aereo.webp.webp` (WebP, 759×600).
- El enlace de recuperación se muestra como elemento visual; no existe una acción de recuperación en `CuentaController`, y no se agregó lógica de autenticación.
- El pie ahora incluye el crédito “Fotos: campus USMP”.

## Validación

- `dotnet build`: correcto, 0 advertencias y 0 errores.
- Se calcularon contrastes de texto en las superficies glass; el mínimo verificado es 4.61:1. El botón turquesa con texto índigo alcanza 6.32:1.
- No fue posible inspeccionar el renderizado en el navegador: la aplicación no inicia sin la cadena `DefaultConnection` configurada. No se consultaron ni alteraron secretos o configuración.

## Prompt adicional: Scroll suave (Lenis)

Usa la skill diseno-unilink, sección "Scroll suave (Lenis)". Estamos en la rama feature/rediseno-glass. Integra Lenis para scroll suave en todo el sitio, sincronizado con GSAP y ScrollTrigger en wwwroot/js/animaciones.js. Asegura que: el chat del asistente, los modales, los dropdowns y el menú offcanvas sigan haciendo scroll normal (data-lenis-prevent); los enlaces de anclas usen lenis.scrollTo; con prefers-reduced-motion Lenis no se active. Verifica que las animaciones de ScrollTrigger sigan funcionando. Ejecuta dotnet build. Agrega este prompt al final de docs/prompts/16-rediseno-glass.md. No ejecutes git.
