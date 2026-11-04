(() => {
    const contenedor = document.querySelector("[data-asistente]");
    if (!contenedor) return;

    const panel = contenedor.querySelector("[data-asistente-panel]");
    const botonAbrir = contenedor.querySelector("[data-asistente-abrir]");
    const botonCerrar = contenedor.querySelector("[data-asistente-cerrar]");
    const botonReiniciar = contenedor.querySelector("[data-asistente-reiniciar]");
    const listaMensajes = contenedor.querySelector("[data-asistente-mensajes]");
    const indicador = contenedor.querySelector("[data-asistente-escribiendo]");
    const sugerencias = contenedor.querySelector("[data-asistente-sugerencias]");
    const formulario = contenedor.querySelector("[data-asistente-formulario]");
    const campoMensaje = formulario.elements.mensaje;
    const botonEnviar = formulario.querySelector("button[type='submit']");

    const mostrarEnlaces = (elemento, texto) => {
        const enlaces = /\[([^\]]+)\]\(([^)]+)\)|(https?:\/\/[^\s]+)/g;
        let posicion = 0;
        let coincidencia;

        const agregarTexto = (fragmento) => {
            const lineas = fragmento.split("\n");
            lineas.forEach((linea, indice) => {
                if (indice > 0) elemento.append(document.createElement("br"));
                if (linea) elemento.append(document.createTextNode(linea));
            });
        };

        while ((coincidencia = enlaces.exec(texto)) !== null) {
            agregarTexto(texto.slice(posicion, coincidencia.index));
            const urlOriginal = coincidencia[2] || coincidencia[3];
            const url = urlOriginal.replace(/[.,!?]+$/, "");
            const etiqueta = coincidencia[1] || url;
            const relativa = url.startsWith("/") && !url.startsWith("//");
            let urlValida = relativa;

            if (!relativa) {
                try {
                    const destino = new URL(url);
                    urlValida = destino.protocol === "https:" || destino.protocol === "http:";
                } catch {
                    urlValida = false;
                }
            }

            if (urlValida) {
                const enlace = document.createElement("a");
                enlace.href = url;
                enlace.textContent = etiqueta;
                if (!relativa) {
                    enlace.target = "_blank";
                    enlace.rel = "noopener noreferrer";
                }
                elemento.append(enlace);
            } else {
                agregarTexto(coincidencia[0]);
            }

            if (url.length < urlOriginal.length) {
                agregarTexto(urlOriginal.slice(url.length));
            }
            posicion = coincidencia.index + coincidencia[0].length;
        }

        agregarTexto(texto.slice(posicion));
    };

    const agregarMensaje = (texto, esUsuario, herramientas = [], proveedorUsado = "", modeloUsado = "") => {
        const mensaje = document.createElement("div");
        mensaje.className = `ul-chat-mensaje ${esUsuario ? "usuario" : "asistente"}`;

        const burbuja = document.createElement("div");
        burbuja.className = "ul-chat-burbuja";
        mostrarEnlaces(burbuja, texto);
        mensaje.append(burbuja);

        if (!esUsuario && (proveedorUsado || herramientas.length > 0)) {
            const usados = document.createElement("p");
            usados.className = "ul-chat-herramientas";
            const proveedor = proveedorUsado
                ? `⚡ ${proveedorUsado} · ${modeloUsado}`
                : "";
            const herramientasTexto = herramientas.length > 0
                ? `🔧 ${herramientas.join(" · ")}`
                : "";
            usados.textContent = [proveedor, herramientasTexto].filter(Boolean).join(" · ");
            mensaje.append(usados);
        }

        listaMensajes.append(mensaje);
        listaMensajes.scrollTop = listaMensajes.scrollHeight;
    };

    const alternarPanel = (abierto) => {
        panel.hidden = !abierto;
        botonAbrir.setAttribute("aria-expanded", String(abierto));
        if (abierto) campoMensaje.focus();
        else botonAbrir.focus();
    };

    botonAbrir.addEventListener("click", () => alternarPanel(panel.hidden));
    botonCerrar.addEventListener("click", () => alternarPanel(false));

    sugerencias.addEventListener("click", (evento) => {
        const boton = evento.target.closest("button");
        if (!boton || botonEnviar.disabled) return;
        campoMensaje.value = boton.textContent.trim();
        formulario.requestSubmit();
    });

    formulario.addEventListener("submit", async (evento) => {
        evento.preventDefault();
        const mensaje = campoMensaje.value.trim();
        if (!mensaje || botonEnviar.disabled) return;

        agregarMensaje(mensaje, true);
        campoMensaje.value = "";
        indicador.hidden = false;
        botonEnviar.disabled = true;
        campoMensaje.disabled = true;
        sugerencias.hidden = true;

        try {
            const respuesta = await fetch("/api/asistente/chat", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ mensaje })
            });
            let contenido;
            try {
                contenido = await respuesta.json();
            } catch {
                contenido = null;
            }
            if (!respuesta.ok) {
                throw new Error(
                    contenido?.respuesta
                    || "El asistente está muy solicitado en este momento. Intenta de nuevo en unos segundos 🙏");
            }
            agregarMensaje(
                contenido.respuesta || "No pude obtener una respuesta en este momento.",
                false,
                contenido.herramientasUsadas || [],
                contenido.proveedorUsado || "",
                contenido.modeloUsado || "");
        } catch (error) {
            agregarMensaje(
                error instanceof Error ? error.message : "Ocurrió un error al enviar tu mensaje.",
                false);
        } finally {
            indicador.hidden = true;
            botonEnviar.disabled = false;
            campoMensaje.disabled = false;
            campoMensaje.focus();
        }
    });

    botonReiniciar.addEventListener("click", async () => {
        botonReiniciar.disabled = true;
        try {
            const respuesta = await fetch("/api/asistente/reiniciar", { method: "POST" });
            if (!respuesta.ok) {
                throw new Error("No se pudo reiniciar la conversación. Inténtalo nuevamente.");
            }
            listaMensajes.replaceChildren();
            sugerencias.hidden = false;
            agregarMensaje("¡Hola! Puedo ayudarte a encontrar materiales, clases y recomendaciones de UniLink.", false);
        } catch (error) {
            agregarMensaje(
                error instanceof Error ? error.message : "Ocurrió un error al reiniciar la conversación.",
                false);
        } finally {
            botonReiniciar.disabled = false;
        }
    });
})();
