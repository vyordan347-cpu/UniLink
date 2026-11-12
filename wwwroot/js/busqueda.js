document.addEventListener("DOMContentLoaded", () => {
    const buscador = document.querySelector("[data-busqueda]");
    if (!buscador) {
        return;
    }

    const entrada = buscador.querySelector("[data-busqueda-input]");
    const estado = buscador.querySelector("[data-busqueda-estado]");
    const etiquetaMotor = buscador.querySelector("[data-busqueda-motor]");
    const resultados = document.querySelector("[data-busqueda-resultados]");
    const conteo = document.querySelector("[data-busqueda-conteo]");
    const filtroGratis = document.querySelector("[name='SoloGratuitos']");
    if (!(entrada instanceof HTMLInputElement) || !estado || !etiquetaMotor || !resultados || !conteo) {
        return;
    }

    const contenidoInicial = [...resultados.childNodes].map(nodo => nodo.cloneNode(true));
    const conteoInicial = conteo.textContent;
    let temporizador;
    let solicitudActiva;

    const buscar = async () => {
        const q = entrada.value.trim();
        solicitudActiva?.abort();
        etiquetaMotor.hidden = true;

        if (!q) {
            resultados.replaceChildren(...contenidoInicial.map(nodo => nodo.cloneNode(true)));
            conteo.textContent = conteoInicial;
            estado.textContent = "Escribe para ver resultados mientras buscas.";
            return;
        }

        const solicitud = new AbortController();
        solicitudActiva = solicitud;
        estado.textContent = "Buscando...";

        const parametros = new URLSearchParams({
            q,
            tipo: buscador.dataset.tipo,
            soloGratuitos: filtroGratis?.checked ? "true" : "false"
        });

        try {
            const respuesta = await fetch(`${buscador.dataset.endpoint}?${parametros}`, {
                signal: solicitud.signal,
                headers: { Accept: "application/json" }
            });
            if (!respuesta.ok) {
                throw new Error(`La búsqueda respondió ${respuesta.status}.`);
            }

            const datos = await respuesta.json();
            const tarjetas = datos.resultados.map(crearTarjeta);
            mostrarResultados(tarjetas);
            etiquetaMotor.hidden = datos.motor !== "algolia";
            conteo.textContent = `${tarjetas.length} ${textoConteo(tarjetas.length)}`;
            estado.textContent = tarjetas.length === 0
                ? "No encontramos resultados. Prueba con otras palabras."
                : `${tarjetas.length} resultados encontrados.`;
        } catch (error) {
            if (error.name === "AbortError") {
                return;
            }

            console.error("No se pudo completar la búsqueda.", error);
            estado.textContent = "No se pudo completar la búsqueda. Inténtalo nuevamente.";
        }
    };

    const programarBusqueda = () => {
        clearTimeout(temporizador);
        temporizador = setTimeout(buscar, 300);
    };

    entrada.addEventListener("input", programarBusqueda);
    filtroGratis?.addEventListener("change", () => {
        if (entrada.value.trim()) {
            programarBusqueda();
        }
    });

    function crearTarjeta(resultado) {
        return buscador.dataset.tipo === "materiales"
            ? crearTarjetaMaterial(resultado)
            : crearTarjetaClase(resultado);
    }

    function mostrarResultados(tarjetas) {
        const cuadrícula = document.createElement("div");
        cuadrícula.className = "row row-cols-1 row-cols-sm-2 row-cols-xl-3 g-4";
        if (buscador.dataset.tipo === "clases") {
            cuadrícula.dataset.clasesLista = "";
        }
        cuadrícula.append(...tarjetas);
        const vacio = crearEstadoVacio();
        vacio.hidden = tarjetas.length !== 0;
        resultados.replaceChildren(cuadrícula, vacio);
    }

    function crearTarjetaMaterial(material) {
        const columna = document.createElement("div");
        columna.className = "col";
        const tarjeta = document.createElement("article");
        tarjeta.className = "card tarjeta-material h-100 border-0 shadow-sm rounded-4 overflow-hidden";
        const imagen = document.createElement("img");
        imagen.className = "tarjeta-material-imagen";
        imagen.src = material.imagenUrl || "/img/material-default.svg";
        imagen.alt = material.titulo;
        imagen.loading = "lazy";
        tarjeta.append(imagen);

        const cuerpo = document.createElement("div");
        cuerpo.className = "card-body d-flex flex-column p-4";
        const insignias = crearInsignias(material);
        const carrera = document.createElement("p");
        carrera.className = "small text-primary fw-semibold mb-1";
        carrera.textContent = `${material.categoria ?? ""}${material.categoria ? " · " : ""}${material.carrera}`;
        const titulo = document.createElement("h2");
        titulo.className = "h5 fw-bold";
        titulo.textContent = material.titulo;
        const descripcion = document.createElement("p");
        descripcion.className = "text-secondary small mb-2";
        descripcion.textContent = material.descripcion;
        const pie = document.createElement("div");
        pie.className = "mt-auto";
        const precio = document.createElement("p");
        precio.className = `precio-material mb-3${material.esSolidario ? " precio-solidario" : ""}`;
        precio.textContent = material.esSolidario ? "GRATUITO" : `S/ ${Number(material.precio).toFixed(2)} por día`;
        const enlace = crearEnlace(material.id, "Ver detalle", "btn btn-outline-primary w-100");
        pie.append(precio, enlace);
        cuerpo.append(insignias, carrera, titulo, descripcion, pie);
        tarjeta.append(cuerpo);
        columna.append(tarjeta);
        return columna;
    }

    function crearTarjetaClase(clase) {
        const columna = document.createElement("div");
        columna.className = "col";
        const tarjeta = document.createElement("article");
        tarjeta.className = "card tarjeta-clase h-100";
        const cuerpo = document.createElement("div");
        cuerpo.className = "card-body d-flex flex-column";
        const insignias = document.createElement("div");
        insignias.className = "d-flex flex-wrap gap-2 mb-3";
        if (clase.esSolidario) {
            const gratuita = document.createElement("span");
            gratuita.className = "badge badge-solidario";
            gratuita.textContent = "GRATUITO";
            insignias.append(gratuita);
        }
        const modalidad = document.createElement("span");
        modalidad.className = "badge badge-modalidad";
        modalidad.textContent = etiquetaModalidad(clase.modalidad);
        insignias.append(modalidad);

        const carrera = document.createElement("p");
        carrera.className = "small text-primary fw-semibold mb-1";
        carrera.textContent = clase.carrera;
        const titulo = document.createElement("h2");
        titulo.className = "h3";
        titulo.textContent = clase.titulo;
        const tutor = document.createElement("p");
        tutor.className = "text-secondary mb-3";
        tutor.textContent = `Tutor: ${clase.tutor}`;
        const pie = document.createElement("div");
        pie.className = "mt-auto";
        const precio = document.createElement("p");
        precio.className = `precio-clase${clase.esSolidario ? " precio-solidario" : ""}`;
        precio.textContent = clase.esSolidario ? "GRATUITO" : `S/ ${Number(clase.precio).toFixed(2)} por hora`;
        pie.append(precio, crearEnlace(clase.id, "Ver clase", "btn btn-outline-primary w-100"));
        cuerpo.append(insignias, carrera, titulo, tutor, pie);
        tarjeta.append(cuerpo);
        columna.append(tarjeta);
        return columna;
    }

    function crearInsignias(material) {
        const insignias = document.createElement("div");
        insignias.className = "d-flex flex-wrap gap-2 mb-3";
        if (material.esSolidario) {
            const gratuita = document.createElement("span");
            gratuita.className = "badge badge-solidario";
            gratuita.textContent = "GRATUITO";
            insignias.append(gratuita);
        }
        const disponible = document.createElement("span");
        disponible.className = `badge estado-badge ${material.disponible ? "estado-disponible" : "estado-devuelto"}`;
        disponible.textContent = material.disponible ? "Disponible" : "Prestado";
        insignias.append(disponible);
        return insignias;
    }

    function crearEstadoVacio() {
        const vacio = document.createElement("div");
        vacio.className = "estado-vacio text-center mt-4";
        if (buscador.dataset.tipo === "clases") {
            vacio.dataset.clasesVacio = "";
        }
        const icono = document.createElement("span");
        icono.className = "estado-vacio-icono";
        icono.setAttribute("aria-hidden", "true");
        icono.textContent = "⌕";
        const titulo = document.createElement("h2");
        titulo.className = "h3 mt-3";
        titulo.textContent = `Aún no encontramos ${buscador.dataset.tipo === "materiales" ? "materiales" : "clases"}`;
        const mensaje = document.createElement("p");
        mensaje.textContent = "Prueba con otras palabras o ajusta los filtros.";
        vacio.append(icono, titulo, mensaje);
        return vacio;
    }

    function crearEnlace(id, texto, clase) {
        const enlace = document.createElement("a");
        const destino = new URL(buscador.dataset.detalleUrl, window.location.origin);
        destino.searchParams.set("id", String(id));
        enlace.className = clase;
        enlace.href = destino.pathname + destino.search;
        enlace.textContent = texto;
        return enlace;
    }

    function textoConteo(cantidad) {
        const singular = buscador.dataset.tipo === "materiales" ? "material encontrado" : "clase encontrada";
        const plural = buscador.dataset.tipo === "materiales" ? "materiales encontrados" : "clases encontradas";
        return cantidad === 1 ? singular : plural;
    }

    function etiquetaModalidad(modalidad) {
        if (modalidad === "GoogleMeet") {
            return "Google Meet";
        }
        if (modalidad === "VideollamadaUniLink") {
            return "Videollamada UniLink";
        }
        return modalidad ?? "Clase";
    }
});
