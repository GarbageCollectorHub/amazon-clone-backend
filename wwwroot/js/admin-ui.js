(function () {
    const placeholder = document.body.dataset.imagePlaceholder;
    function usePlaceholder(image) {
        if (!placeholder || image.dataset.fallbackApplied === "true") return;
        image.dataset.fallbackApplied = "true";
        image.src = placeholder;
    }
    document.addEventListener("error", function (event) {
        if (event.target instanceof HTMLImageElement) usePlaceholder(event.target);
    }, true);
    document.querySelectorAll("img").forEach(function (image) {
        if (image.complete && image.naturalWidth === 0) usePlaceholder(image);
    });

    document.querySelectorAll(".js-primary-image-input").forEach(function (input) {
        const maxFileBytes = Number(input.dataset.maxFileBytes);
        const panel = input.closest(".primary-image-panel");
        const preview = panel.querySelector(".js-primary-image-preview");
        const fileName = panel.querySelector(".js-primary-image-name");
        const originalSource = preview.src;
        const originalAlt = preview.alt;
        let previewUrl;
        function resetPreview() {
            if (previewUrl) URL.revokeObjectURL(previewUrl);
            previewUrl = null;
            delete preview.dataset.fallbackApplied;
            preview.src = originalSource;
            preview.alt = originalAlt;
        }
        input.addEventListener("change", function () {
            const file = input.files[0];
            if (!file) {
                resetPreview();
                fileName.textContent = "";
                return;
            }
            const valid = /\.(jpe?g|png|webp)$/i.test(file.name)
                && (!file.type || ["image/jpeg", "image/png", "image/webp"].includes(file.type.toLowerCase()))
                && file.size > 0 && file.size <= maxFileBytes;
            if (!valid) {
                fileName.textContent = `Choose a JPG, PNG or WebP image up to ${maxFileBytes / (1024 * 1024)} MiB.`;
                fileName.classList.add("text-danger");
                input.value = "";
                resetPreview();
                return;
            }
            input.setCustomValidity("");
            fileName.classList.remove("text-danger");
            if (previewUrl) URL.revokeObjectURL(previewUrl);
            previewUrl = URL.createObjectURL(file);
            delete preview.dataset.fallbackApplied;
            preview.src = previewUrl;
            preview.alt = "Selected primary image preview";
            fileName.textContent = file.name;
        });
        window.addEventListener("pagehide", function () { if (previewUrl) URL.revokeObjectURL(previewUrl); });
    });
})();
