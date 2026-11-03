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