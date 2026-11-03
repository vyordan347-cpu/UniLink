# Rediseño con la skill de diseño

- **Rama:** `feature/diseno-skill`
- **Agente:** Copilot SDK en VS Code
- **Skill usada:** `diseno-unilink`

## Prompt

Usa la skill diseno-unilink. Estamos en la rama feature/diseno-skill. Rediseña la interfaz para que no se vea apiñada:
1. Actualiza wwwroot/css/site.css con todos los tokens de la skill (colores, espaciado, radios, sombras, tipografía Inter).
2. Rehaz la barra de navegación de _Layout según la sección "Navegación" de la skill, incluido el menú offcanvas en móvil y el enlace activo.
3. Aplica la skill a todas las vistas existentes: landing, login, registro, panel, explorar, detalle, publicar, mis materiales, solicitar alquiler, confirmación, mis alquileres y solicitudes recibidas (espaciado, grillas, formularios, badges y estados vacíos).
4. No cambies la lógica de los controladores, solo vistas y CSS.
5. Verifica con dotnet build.
6. Crea docs/prompts/08-diseno-skill.md con la rama, el agente, la skill usada y este prompt.
7. NO ejecutes git.

## Resumen

Se actualizaron los tokens y componentes visuales de UniLink, se simplificó la navegación de escritorio y se añadió el menú offcanvas para móvil con resaltado de la sección activa. Se aplicaron los estilos a las vistas de cuenta, panel, materiales y alquileres, incluyendo formularios, badges y estados vacíos. No se modificó la lógica de los controladores.
