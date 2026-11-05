// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assdo
document.addEventListener("submit", e => {
    const form = e.target;
    if (form.id === "auth-modal-form") {
        e.preventDefault();
        if (form.getAttribute("aria-busy") === "true") return;

        const messageEl = document.getElementById("message");
        const submitBtn = document.querySelector('[form="auth-modal-form"][type="submit"]');
        const submitLabel = submitBtn?.querySelector(".auth-submit__label");
        const progressEl = document.getElementById("auth-progress");
        const modal = document.getElementById("authModal");
        const controller = new AbortController();
        const cancelRequest = () => controller.abort();
        let signedIn = false;

        form.setAttribute("aria-busy", "true");
        if (submitBtn) submitBtn.disabled = true;
        submitBtn?.classList.add("is-loading");
        if (submitLabel) submitLabel.textContent = "Signing in…";
        if (progressEl) progressEl.hidden = true;
        if (messageEl) { messageEl.textContent = ""; messageEl.className = ""; }
        modal?.addEventListener("hidden.bs.modal", cancelRequest, { once: true });
        const progressTimer = setTimeout(() => {
            if (progressEl) progressEl.hidden = false;
        }, 4000);

        fetch("/Admin/Login", {
            method: 'POST',
            body: new FormData(form),
            signal: controller.signal
        })
            .then(r => {
                if (r.status >= 500) {
                    return { status: r.status, message: "Server error. Please try again later." };
                }
                return r.json();
            })
            .then(j => {
                if (j.status === 200) {
                    signedIn = true;
                    window.location.reload();
                } else {
                    if (messageEl) {
                        messageEl.textContent = j.message || "Login failed.";
                        messageEl.className = "text-danger small";
                    }
                }
            })
            .catch(error => {
                if (error.name === "AbortError") return;
                if (messageEl) {
                    messageEl.textContent = error instanceof TypeError
                        ? "Unable to reach the server. Check your connection and try again."
                        : "Request failed. Please try again.";
                    messageEl.className = "text-danger small";
                }
            })
            .finally(() => {
                clearTimeout(progressTimer);
                modal?.removeEventListener("hidden.bs.modal", cancelRequest);
                if (progressEl) progressEl.hidden = true;
                if (!signedIn) {
                    form.removeAttribute("aria-busy");
                    if (submitBtn) submitBtn.disabled = false;
                    submitBtn?.classList.remove("is-loading");
                    if (submitLabel) submitLabel.textContent = "Sign in";
                }
            });
    }

})
