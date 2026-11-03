

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

// Search and sorting apply only to the products already loaded on this page.
(function () {
    const search = document.getElementById("product-search");
    const sort = document.getElementById("product-sort");
    const body = document.querySelector(".js-products-rows");
    if (!search || !sort || !body) return;
    const rows = Array.from(body.querySelectorAll(".js-product-row"));
    const count = document.getElementById("product-count");
    const noResults = document.getElementById("product-no-results");

    function updateProducts() {
        const query = search.value.trim().toLocaleLowerCase();
        const sorted = [...rows].sort(function (a, b) {
            if (sort.value === "title") return a.dataset.title.localeCompare(b.dataset.title);
            if (sort.value === "price-asc") return Number(a.dataset.price) - Number(b.dataset.price);
            if (sort.value === "price-desc") return Number(b.dataset.price) - Number(a.dataset.price);
            const dateOrder = a.dataset.created.localeCompare(b.dataset.created);
            return sort.value === "oldest" ? dateOrder : -dateOrder;
        });
        let visible = 0;
        sorted.forEach(function (row) {
            row.hidden = !row.dataset.title.toLocaleLowerCase().includes(query);
            if (!row.hidden) visible++;
            body.appendChild(row);
        });
        noResults.hidden = visible !== 0;
        count.textContent = query ? `${visible} of ${rows.length} products` : `${rows.length} ${rows.length === 1 ? "product" : "products"}`;
    }
    search.addEventListener("input", updateProducts);
    sort.addEventListener("change", updateProducts);
    document.getElementById("clear-product-search").addEventListener("click", function () {
        search.value = "";
        updateProducts();
        search.focus();
    });
    updateProducts();
})();
