---
name: diseno-unilink
description: Sistema de diseño de UniLink. Úsala siempre que crees o modifiques vistas Razor, CSS, layout, navegación, formularios o tarjetas.
---

# Sistema de diseño UniLink

## Principios
- Mucho espacio en blanco. Nunca apiñar elementos. Una idea principal por sección.
- Jerarquía clara: un solo título principal por página, subtítulo en gris, acción principal destacada.
- Mobile-first: diseñar primero para 390px y luego escritorio.

## Tokens (usar SIEMPRE las variables CSS, nunca colores sueltos)
- --ul-primario: #3B4CCA (marca, títulos, enlaces)
- --ul-accion: #14B8A6 (botón principal de cada pantalla)
- --ul-solidario: #F97362 (SOLO lo gratuito/solidario)
- --ul-fondo: #F7F8FC; --ul-superficie: #FFFFFF; --ul-texto: #1E2235; --ul-texto-suave: #6B7280; --ul-borde: #E5E7EB
- Espaciado en múltiplos de 8px: 8, 16, 24, 32, 48, 64. Secciones con padding vertical de 64px en escritorio y 40px en móvil.
- Radios: 12px en tarjetas e inputs, 999px en badges.
- Sombras suaves: 0 1px 2px rgba(0,0,0,.04), 0 4px 16px rgba(30,34,53,.06).
- Tipografía: Inter. Tamaños: h1 40px (móvil 30px), h2 28px, h3 20px, texto 16px, secundario 14px. Interlineado 1.6.

## Navegación
- Máximo 4 o 5 elementos visibles en la barra superior.
- Sin sesión: Explorar, Solidario | Iniciar sesión, Crear cuenta (botón).
- Con sesión: Explorar, Solidario, dropdown "Mi actividad" (Mis alquileres, Solicitudes, Mis materiales), botón "Publicar" (color acción) y avatar con dropdown (Mi panel, Cerrar sesión).
- En móvil: menú colapsable (offcanvas de Bootstrap).
- Marcar el enlace de la página activa.

## Componentes
- Tarjetas: fondo blanco, borde 1px var(--ul-borde), radio 12px, padding 24px, hover con sombra leve.
- Grillas de tarjetas: 1 columna en móvil, 2 en tablet, 3 en escritorio, con gap de 24px.
- Formularios: ancho máximo de 640px, etiquetas encima del input, inputs de 48px de alto, 16px entre campos y errores en rojo debajo del campo.
- Botones: alto de 44px, radio 10px; solo UN botón primario por vista.
- Badges de estado: Disponible verde, Pendiente ámbar, Aprobado azul, Activo turquesa, Devuelto gris, Rechazado rojo, Gratuito coral.
- Estados vacíos: ícono, mensaje amable y botón de acción.
- Alertas TempData: arriba del contenido, que se cierren solas a los 5 segundos.

## Prohibido
- Más de 5 enlaces sueltos en la barra.
- Colores fuera de los tokens.
- Texto menor a 14px.
- Carrito, checkout o cualquier estética de tienda.

## Estilo glass (v2)
Usar SOLO en: login, registro, hero de la landing, barra de navegación, tarjetas de estadísticas del panel y widget del asistente. El resto de pantallas se mantiene limpio (fondo claro y tarjetas blancas).

- Fondo con foto del campus (wwwroot/img/campus/) + capa de degradado: linear-gradient(135deg, rgba(30,34,53,.75), rgba(59,76,202,.55)).
- Panel de vidrio: background rgba(255,255,255,.12); backdrop-filter: blur(16px) saturate(140%); borde 1px rgba(255,255,255,.28); radio 20px; sombra 0 8px 32px rgba(0,0,0,.25).
- Texto sobre vidrio: blanco (#FFFFFF) y secundario rgba(255,255,255,.75). Contraste mínimo AA.
- Inputs en vidrio: alto 52px, fondo rgba(255,255,255,.10), ícono a la izquierda, borde rgba(20,184,166,.6); en foco, brillo: box-shadow 0 0 0 3px rgba(20,184,166,.35).
- Botón principal en vidrio: turquesa sólido con brillo suave.
- Navbar: sticky, rgba(255,255,255,.75) + blur(12px), borde inferior sutil.
- Fallback: si el navegador no soporta backdrop-filter, usar un fondo más opaco (rgba(30,34,53,.85)).
- Respetar prefers-reduced-motion. Sin animaciones exageradas (solo fade-in de 300ms).
- Créditos de fotos en el pie: "Fotos: campus USMP".

## Animaciones (GSAP)
- Usar GSAP + ScrollTrigger desde CDN (cdn.jsdelivr.net/npm/gsap), cargados solo en _Layout al final del body.
- Todo el código de animación va en wwwroot/js/animaciones.js, organizado por funciones (animarHero, animarScroll, animarContadores, etc.) que solo se ejecutan si el elemento existe en la página.
- Duraciones: 0.4–0.8s; ease "power2.out"; escalonado (stagger) de 0.08s.
- Envolver todo en gsap.matchMedia(): con "(prefers-reduced-motion: reduce)" no animar (mostrar todo directamente).
- Nunca ocultar contenido con CSS esperando a GSAP: si el JS falla, el contenido debe verse igual.
- Prohibido: animaciones en bucle, parallax exagerado y animar cada elemento de la página.

## Scroll suave (Lenis)
- Lenis desde CDN (cdn.jsdelivr.net/npm/lenis), cargado en _Layout antes de animaciones.js.
- Integrarlo con GSAP: lenis.on('scroll', ScrollTrigger.update) y gsap.ticker.add para el raf; gsap.ticker.lagSmoothing(0).
- Duración suave y natural (lerp ~0.1). No exagerar.
- Desactivar Lenis con prefers-reduced-motion y en dispositivos táctiles si genera problemas.
- Paneles con scroll propio (chat del asistente, modales, dropdowns, menú offcanvas) deben llevar data-lenis-prevent.
- Los enlaces de anclas (#seccion) deben usar lenis.scrollTo.