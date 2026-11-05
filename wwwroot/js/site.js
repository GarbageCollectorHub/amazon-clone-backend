// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assdo
document.addEventListener("submit", e => {
    const form = e.target;
    if (form.id === "user-registration-form") {
        e.preventDefault();

        const feedback = document.getElementById("signup-feedback");
        const submitBtn = form.querySelector('[type="submit"]');

        if (submitBtn) submitBtn.disabled = true;

        fetch("/Admin/Register", {
            method: "POST",
            body: new FormData(form)
        })
            .then(r => r.json())
            .then(j => {
                if (j.success) {
                    const toastEl = document.getElementById("signup-toast");
                    const toastBody = document.getElementById("signup-toast-body");

                    if (toastEl && toastBody && typeof bootstrap !== "undefined") {
                        toastBody.textContent = "Account created successfully!";
                        toastEl.classList.add("text-bg-success");
                        toastEl.classList.remove("text-bg-danger");

                        bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 5000 }).show();
                    }

                    if (feedback) feedback.innerHTML = "";
                    form.reset();
                } else {
                    if (feedback) {
                        const alert = document.createElement("div");
                        alert.className = "alert-box alert-box--danger";
                        const messages = Array.isArray(j.message)
                            ? j.message
                            : [j.message || "Something went wrong."];
                        messages.forEach(message => {
                            const line = document.createElement("div");
                            line.textContent = message;
                            alert.appendChild(line);
                        });
                        feedback.replaceChildren(alert);
                    }
                }
            })
            .catch(() => {
                if (feedback) {
                    feedback.innerHTML =
                        '<div class="alert-box alert-box--danger">Request failed. Please try again.</div>';
                }
            })
            .finally(() => {
                if (submitBtn) submitBtn.disabled = false;
            });
    }
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
    //Category
    if (form.id === "add-category-form") {
        e.preventDefault();

        const feedback = document.getElementById("category-feedback");
        const submitBtn = form.querySelector('[type="submit"]');

        if (submitBtn) submitBtn.disabled = true;

        fetch("/Admin/AddCategory", {
            method: "POST",
            body: new FormData(form)
        })
            .then(r => r.json())
            .then(j => {
                if (j.success) {
                    if (feedback) {
                        feedback.innerHTML =
                            '<div class="alert-box alert-box--success">Category added successfully!</div>';
                    }
                    form.reset();
                } else {
                    if (feedback) {
                        feedback.innerHTML =
                            '<div class="alert-box alert-box--danger">' +
                            (j.message || "Something went wrong.") +
                            '</div>';
                    }
                }
            })
            .catch(() => {
                if (feedback) {
                    feedback.innerHTML =
                        '<div class="alert-box alert-box--danger">Request failed. Please try again.</div>';
                }
            })
            .finally(() => {
                if (submitBtn) submitBtn.disabled = false;
            });
    }
    // Promotion
    if (form.id === "add-promotion-form") {
        e.preventDefault();

        const feedback = document.getElementById("promotion-feedback");
        const submitBtn = form.querySelector('[type="submit"]');

        if (submitBtn) submitBtn.disabled = true;

        fetch("/Admin/AddPromotion", {
            method: "POST",
            body: new FormData(form)
        })
            .then(r => r.json())
            .then(j => {
                if (j.success) {
                    if (feedback) {
                        feedback.innerHTML =
                            '<div class="alert-box alert-box--success">Promotion added successfully!</div>';
                    }
                    form.reset();
                } else {
                    if (feedback) {
                        feedback.innerHTML =
                            '<div class="alert-box alert-box--danger">' +
                            (j.message || "Something went wrong.") +
                            '</div>';
                    }
                }
            })
            .catch(() => {
                if (feedback) {
                    feedback.innerHTML =
                        '<div class="alert-box alert-box--danger">Request failed. Please try again.</div>';
                }
            })
            .finally(() => {
                if (submitBtn) submitBtn.disabled = false;
            });
    }

})
