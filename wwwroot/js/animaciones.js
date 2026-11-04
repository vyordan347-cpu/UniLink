(() => {
    const gsap = window.gsap;
    if (!gsap || !window.ScrollTrigger) return;

    gsap.registerPlugin(window.ScrollTrigger);

    let animarEntrada;
    let animarAperturaAsistente;
    let animarCierreAsistente;
    const media = gsap.matchMedia();

    const animarHero = () => {
        const foto = document.querySelector(".hero-section .ul-hero-foto");
        const titulo = document.querySelector("[data-animar-hero-titulo]");
        const eslogan = document.querySelector("[data-animar-hero-eslogan]");
        const botones = document.querySelector("[data-animar-hero-botones]");
        const elementos = [titulo, eslogan, botones].filter(Boolean);

        if (foto) {
            gsap.fromTo(foto, { scale: 1.04 }, {
                scale: 1,
                duration: 0.8,
                ease: "power2.out"
            });
        }

        if (elementos.length) {
            gsap.fromTo(elementos, { autoAlpha: 0, y: 12 }, {
                autoAlpha: 1,
                y: 0,
                duration: 0.55,
                stagger: 0.08,
                ease: "power2.out"
            });
        }
    };

    const animarScroll = () => {
        [
            { selector: ".steps-grid", tarjetas: ".step-item" },
            { selector: ".pillars-grid", tarjetas: ".pillar" }
        ].forEach(({ selector, tarjetas }) => {
            const contenedor = document.querySelector(selector);
            const elementos = contenedor?.querySelectorAll(tarjetas);
            if (!contenedor || !elementos?.length) return;

            gsap.fromTo(elementos, { autoAlpha: 0, y: 18 }, {
                autoAlpha: 1,
                y: 0,
                duration: 0.55,
                stagger: 0.08,
                ease: "power2.out",
                scrollTrigger: {
                    trigger: contenedor,
                    start: "top 85%",
                    once: true
                }
            });
        });
    };

    const animarContadores = () => {
        document.querySelectorAll("[data-animar-contador]").forEach((elemento) => {
            const valor = Number(elemento.dataset.animarContador);
            if (!Number.isFinite(valor)) return;

            const contador = { valor: 0 };
            gsap.to(contador, {
                valor,
                duration: 0.8,
                ease: "power2.out",
                scrollTrigger: {
                    trigger: elemento,
                    start: "top 90%",
                    once: true
                },
                onUpdate: () => {
                    elemento.textContent = String(Math.round(contador.valor));
                }
            });
        });
    };

    const animarFormularios = () => {
        document.querySelectorAll(".ul-auth-card").forEach((tarjeta) => {
            gsap.fromTo(tarjeta, { autoAlpha: 0, scale: 0.96 }, {
                autoAlpha: 1,
                scale: 1,
                duration: 0.6,
                ease: "power2.out"
            });

            const formulario = tarjeta.querySelector("form");
            if (!formulario) return;

            const tieneErrores = () => Boolean(
                formulario.querySelector(".input-validation-error, .validation-summary-errors")
                || [...formulario.querySelectorAll(".field-validation-error, .ul-auth-validation")]
                    .some((elemento) => elemento.textContent.trim())
            );

            const sacudir = () => {
                if (!tieneErrores()) return;

                gsap.to(formulario, {
                    keyframes: [
                        { x: -4, duration: 0.06 },
                        { x: 4, duration: 0.06 },
                        { x: -3, duration: 0.06 },
                        { x: 3, duration: 0.06 },
                        { x: 0, duration: 0.06 }
                    ],
                    ease: "power2.out"
                });
            };

            formulario.addEventListener("invalid", sacudir, true);
            formulario.addEventListener("submit", () => window.setTimeout(sacudir, 0));
            if (tieneErrores()) sacudir();
        });
    };

    const animarTarjetasIniciales = () => {
        const materiales = document.querySelectorAll(
            "[data-explorar-tarjetas] .tarjeta-material");
        const clases = document.querySelectorAll("[data-clases-lista] > *");
        animarEntrada?.([...materiales, ...clases]);
    };

    const animarAsistenteInicial = () => {
        const panel = document.querySelector("[data-asistente-panel]");
        if (panel && !panel.hidden) animarAperturaAsistente?.(panel);
    };

    media.add("(prefers-reduced-motion: reduce)", () => {
        animarEntrada = undefined;
        animarAperturaAsistente = undefined;
        animarCierreAsistente = undefined;
    });

    media.add("(prefers-reduced-motion: no-preference)", (contexto) => {
        contexto.add("animarEntrada", (elementos) => {
            const tarjetas = [...(elementos ?? [])].filter((elemento) => elemento instanceof Element);
            if (!tarjetas.length) return;

            gsap.fromTo(tarjetas, { autoAlpha: 0, y: 16 }, {
                autoAlpha: 1,
                y: 0,
                duration: 0.55,
                stagger: 0.08,
                ease: "power2.out"
            });
        });
        animarEntrada = contexto.animarEntrada;

        contexto.add("animarAperturaAsistente", (panel) => {
            if (!(panel instanceof Element)) return;
            gsap.fromTo(panel, { autoAlpha: 0, x: 18 }, {
                autoAlpha: 1,
                x: 0,
                duration: 0.45,
                ease: "power2.out",
                onComplete: () => gsap.set(panel, { clearProps: "transform,opacity,visibility" })
            });
        });
        animarAperturaAsistente = contexto.animarAperturaAsistente;

        contexto.add("animarCierreAsistente", (panel, alTerminar) => {
            if (!(panel instanceof Element)) return false;
            gsap.to(panel, {
                autoAlpha: 0,
                x: 18,
                duration: 0.4,
                ease: "power2.out",
                onComplete: () => {
                    gsap.set(panel, { clearProps: "transform,opacity,visibility" });
                    alTerminar();
                }
            });
            return true;
        });
        animarCierreAsistente = contexto.animarCierreAsistente;

        animarHero();
        animarScroll();
        animarFormularios();
        animarContadores();
        animarTarjetasIniciales();
        animarAsistenteInicial();
    });

    window.unilinkAnimaciones = {
        animarEntrada: (elementos) => animarEntrada?.(elementos) ?? false,
        abrirAsistente: (panel) => animarAperturaAsistente?.(panel) ?? false,
        cerrarAsistente: (panel, alTerminar) => animarCierreAsistente?.(panel, alTerminar) ?? false
    };
})();
