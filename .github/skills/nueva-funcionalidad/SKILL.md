---
name: nueva-funcionalidad
description: "Diseña e implementa una funcionalidad nueva en UniLink siguiendo AGENTS.md, las skills aplicables y las convenciones del proyecto; registra el prompt en docs/prompts."
disable-model-invocation: true
argument-hint: "Describe la funcionalidad, sus criterios de aceptación y cualquier limitación."
---
Implementa la siguiente funcionalidad en UniLink:

${input:funcionalidad:Describe la funcionalidad y sus criterios de aceptación}

Sigue estos pasos:

1. Lee `AGENTS.md` y revisa el código relacionado antes de proponer o cambiar archivos. Haz solo lo pedido; no adelantes funcionalidades.
2. Identifica los componentes y patrones existentes que correspondan. Si el trabajo toca vistas Razor, CSS, layout, navegación, formularios o tarjetas, carga y sigue `.github/skills/diseno-unilink/SKILL.md`. Carga también cualquier otra skill pertinente.
3. Respeta las reglas de negocio, seguridad, nombres en español, validación, acceso asíncrono a datos y configuración sin credenciales descritas en `AGENTS.md`. No agregues carrito, checkout ni procesamiento de pagos.
4. Implementa el cambio completo en las superficies afectadas y actualiza la documentación directamente relacionada. Agrega o ajusta pruebas cuando corresponda.
5. Ejecuta las pruebas adecuadas y `dotnet build` para verificar la compilación. Informa explícitamente de cualquier validación que no se pudo ejecutar; no afirmes que pasó si no se ejecutó.
6. Al terminar, crea un registro en `docs/prompts/` usando el siguiente número disponible y un nombre breve relacionado con la funcionalidad. Incluye la rama si se conoce, agente utilizado, skills utilizadas, el prompt completo que guio el trabajo y un resumen de lo realizado. No inventes datos que no conozcas.
7. No ejecutes comandos de Git.

Al finalizar, resume los archivos creados o modificados, las pruebas ejecutadas y el nombre del archivo de registro creado en `docs/prompts/`.
