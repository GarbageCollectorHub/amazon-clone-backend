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

        const messageEl = document.getElementById("message");
        const submitBtn = document.querySelector('[form="auth-modal-form"][type="submit"]');

        if (submitBtn) submitBtn.disabled = true;
        if (messageEl) { messageEl.textContent = ""; messageEl.className = ""; }

        fetch("/Admin/Login", {
            method: 'POST',
            body: new FormData(form)
        })
            .then(r => r.json())
            .then(j => {
                if (j.status === 200) {
                    window.location.reload();
                } else {
                    if (messageEl) {
                        messageEl.textContent = j.message || "Login failed.";
                        messageEl.className = "text-danger small";
                    }
                }
            })
            .catch(() => {
                if (messageEl) {
                    messageEl.textContent = "Request failed. Please try again.";
                    messageEl.className = "text-danger small";
                }
            })
            .finally(() => {
                if (submitBtn) submitBtn.disabled = false;
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
