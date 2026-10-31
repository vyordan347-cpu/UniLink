document.addEventListener("DOMContentLoaded", () => {
    const checkboxSolidario = document.querySelector("[data-solidario-toggle]");
    const contenedorPrecio = document.querySelector("[data-precio-container]");

    if (checkboxSolidario && contenedorPrecio) {
        const actualizarPrecio = () => {
            contenedorPrecio.hidden = checkboxSolidario.checked;
        };

        checkboxSolidario.addEventListener("change", actualizarPrecio);
        actualizarPrecio();
    }

    const modal = document.getElementById("modalEliminarMaterial");
    if (modal) {
        modal.addEventListener("show.bs.modal", (evento) => {
            const boton = evento.relatedTarget;
            if (!(boton instanceof HTMLElement)) {
                return;
            }

            const campoId = modal.querySelector("[data-material-id-input]");
            const nombre = modal.querySelector("[data-material-nombre-modal]");
            if (campoId instanceof HTMLInputElement) {
                campoId.value = boton.dataset.materialId ?? "";
            }
            if (nombre) {
                nombre.textContent = boton.dataset.materialNombre ?? "este material";
            }
        });
    }
});
