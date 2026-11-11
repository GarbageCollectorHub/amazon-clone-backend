

// Modal partial view
document.addEventListener("DOMContentLoaded", function () {
    const modalElement = document.getElementById("deleteConfirmModal");
    if (!modalElement) {
        return;
    }

    const deleteForm = document.getElementById("deleteConfirmForm");
    const deleteNameElement = modalElement.querySelector(".js-delete-entity-name");
    const modalTitleElement = document.getElementById("deleteConfirmModalLabel");

    modalElement.addEventListener("show.bs.modal", function (event) {
        const triggerButton = event.relatedTarget;
        if (!triggerButton || !deleteForm || !deleteNameElement || !modalTitleElement) {
            return;
        }

        const deleteUrl = triggerButton.getAttribute("data-delete-url");
        const entityName = triggerButton.getAttribute("data-entity-name");
        const entityLabel = triggerButton.getAttribute("data-entity-label");

        const safeEntityLabel = entityLabel && entityLabel.trim() !== ""
            ? entityLabel.trim().toLowerCase()
            : "item";

        deleteForm.action = deleteUrl || "";
        deleteNameElement.textContent = entityName || `this ${safeEntityLabel}`;
        modalTitleElement.textContent = `Delete ${safeEntityLabel}`;
    });

    modalElement.addEventListener("hidden.bs.modal", function () {
        if (deleteForm) {
            deleteForm.removeAttribute("action");
        }

        if (deleteNameElement) {
            deleteNameElement.textContent = "this item";
        }

        if (modalTitleElement) {
            modalTitleElement.textContent = "Delete item";
        }
    });
});
