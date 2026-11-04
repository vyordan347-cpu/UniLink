(() => {
    const oferta = document.querySelector("[data-oferta-clase]");
    if (oferta) {
        const gratuito = oferta.querySelector("[data-clase-solidaria]");
        const precio = oferta.querySelector("[data-precio-clase]");
        const campoPrecio = oferta.querySelector("[name='PrecioHora']");
        const actualizarPrecio = () => {
            precio.hidden = gratuito.checked;
            campoPrecio.disabled = gratuito.checked;
            if (gratuito.checked) {
                campoPrecio.value = "0";
            } else if (Number(campoPrecio.value) === 0) {
                campoPrecio.value = "";
            }
        };
        gratuito.addEventListener("change", actualizarPrecio);
        actualizarPrecio();
    }

    const reserva = document.querySelector("[data-reserva-clase]");
    if (reserva) {
        const fecha = reserva.querySelector("[data-fecha-reserva]");
        const desfase = reserva.querySelector("[data-desfase-horario]");
        const duracion = reserva.querySelector("[data-duracion-reserva]");
        const costo = reserva.querySelector("[data-costo-reserva]");
        const acepta = reserva.querySelector("[data-acepta-condiciones]");
        const precioHora = Number(reserva.dataset.precioHora);
        const ahora = new Date();
        const formatoLocal = (fechaHora) => {
            const dos = (valor) => String(valor).padStart(2, "0");
            return `${fechaHora.getFullYear()}-${dos(fechaHora.getMonth() + 1)}-${dos(fechaHora.getDate())}T${dos(fechaHora.getHours())}:${dos(fechaHora.getMinutes())}`;
        };
        const minimo = formatoLocal(ahora);
        fecha.min = minimo;
        if (fecha.dataset.inicializarFecha === "True" || fecha.dataset.inicializarFecha === "true" || !fecha.value) {
            const propuesta = new Date(ahora.getTime() + 60 * 60 * 1000);
            propuesta.setSeconds(0, 0);
            fecha.value = formatoLocal(propuesta);
        }
        const actualizarCosto = () => {
            desfase.value = String(ahora.getTimezoneOffset());
            const total = precioHora * Number(duracion.value);
            costo.textContent = total === 0
                ? "Costo total: GRATUITO"
                : `Costo total: S/ ${total.toFixed(2)}`;
        };
        duracion.addEventListener("change", actualizarCosto);
        reserva.addEventListener("submit", (evento) => {
            desfase.value = String(new Date().getTimezoneOffset());
            if (!acepta.checked) {
                evento.preventDefault();
                acepta.focus();
            }
        });
        actualizarCosto();
    }

    const formulario = document.querySelector("[data-clases-filtros]");
    if (formulario) {
        formulario.addEventListener("submit", async (evento) => {
            evento.preventDefault();
            const datos = new FormData(formulario);
            const parametros = new URLSearchParams();
            ["Curso", "Carrera", "SoloGratuitas"].forEach((nombre) => {
                const valor = datos.get(nombre);
                if (valor && valor !== "false") {
                    parametros.set(nombre[0].toLowerCase() + nombre.slice(1), String(valor));
                }
            });
            try {
                const respuesta = await fetch(`${formulario.dataset.apiUrl}?${parametros}`);
                if (!respuesta.ok) {
                    throw new Error(`La búsqueda de clases respondió ${respuesta.status}.`);
                }
                const clases = await respuesta.json();
                const lista = document.querySelector("[data-clases-lista]");
                const vacio = document.querySelector("[data-clases-vacio]");
                const conteo = document.querySelector("[data-clases-conteo]");
                lista.replaceChildren(...clases.map(crearTarjeta));
                window.unilinkAnimaciones?.animarEntrada(lista.children);
                vacio.hidden = clases.length !== 0;
                conteo.textContent = `${clases.length} ${clases.length === 1 ? "clase encontrada" : "clases encontradas"}`;
            } catch (error) {
                console.error("No se pudieron filtrar las clases.", error);
                formulario.submit();
            }
        });
    }

    function crearTarjeta(clase) {
        const columna = document.createElement("div");
        columna.className = "col";
        const tarjeta = document.createElement("article");
        tarjeta.className = "card tarjeta-clase h-100";
        const cuerpo = document.createElement("div");
        cuerpo.className = "card-body d-flex flex-column";
        const insignias = document.createElement("div");
        insignias.className = "d-flex flex-wrap gap-2 mb-3";
        if (clase.esSolidaria) {
            const gratuita = document.createElement("span");
            gratuita.className = "badge badge-solidario";
            gratuita.textContent = "GRATUITO";
            insignias.append(gratuita);
        }
        const modalidad = document.createElement("span");
        modalidad.className = "badge badge-modalidad";
        modalidad.textContent = clase.modalidad;
        insignias.append(modalidad);

        const carrera = document.createElement("p");
        carrera.className = "small text-primary fw-semibold mb-1";
        carrera.textContent = clase.carreraTutor;
        const titulo = document.createElement("h2");
        titulo.className = "h3";
        titulo.textContent = clase.curso;
        const tutor = document.createElement("p");
        tutor.className = "text-secondary mb-3";
        tutor.textContent = `Tutor: ${clase.tutor}`;
        const pie = document.createElement("div");
        pie.className = "mt-auto";
        const precio = document.createElement("p");
        precio.className = `precio-clase${clase.esSolidaria ? " precio-solidario" : ""}`;
        precio.textContent = clase.esSolidaria ? "GRATUITO" : `S/ ${Number(clase.precioHora).toFixed(2)} por hora`;
        const enlace = document.createElement("a");
        enlace.className = "btn btn-outline-primary w-100";
        enlace.href = `/Clases/Detalle/${encodeURIComponent(clase.id)}`;
        enlace.textContent = "Ver clase";

        pie.append(precio, enlace);
        cuerpo.append(insignias, carrera, titulo, tutor, pie);
        tarjeta.append(cuerpo);
        columna.append(tarjeta);
        return columna;
    }
})();
