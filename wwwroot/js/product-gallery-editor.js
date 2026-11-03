document.querySelectorAll(".js-product-gallery-editor").forEach(initProductGalleryEditor);

function initProductGalleryEditor(root) {
    const dropZone = root.querySelector(".js-pge-drop-zone");
    const prompt = root.querySelector(".js-pge-prompt");
    const browseButton = root.querySelector(".js-pge-browse-button");

    const existingSection = root.querySelector(".js-pge-existing-section");
    const existingGrid = root.querySelector(".js-pge-existing-grid");
    const newGrid = root.querySelector(".js-pge-new-grid");

    const addCard = root.querySelector(".js-pge-add-card");
    const countLabel = root.querySelector(".js-pge-count");
    const toastContainer = root.querySelector(".js-pge-toast-container");
    const newFilesInput = root.querySelector(".js-pge-new-files-input");
    const existingIdsContainer = root.querySelector(".js-pge-existing-ids-container");

    // settings для data-attributes
    const maxGalleryImages = Number(root.dataset.maxGalleryImages || "10");
    const maxFileBytes = Number(root.dataset.maxFileBytes || (5 * 1024 * 1024));
    const allowExistingReorder = root.dataset.allowExistingReorder === "true";
    const existingIdsFieldPrefix = root.dataset.existingIdsFieldPrefix || "";
    const isEditMode = root.dataset.isEditMode === "true";

    const allowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    const allowedMimeTypes = ["image/jpeg", "image/png", "image/webp"];

    // runtime state
    let pendingNewFiles = [];
    let fileDragDepth = 0;

    function showToast(messages, variant = "warning") {
        if (!toastContainer) {
            return;
        }

        clearToasts();

        const items = Array.isArray(messages)
            ? messages.filter(Boolean)
            : [messages].filter(Boolean);

        if (items.length === 0) {
            return;
        }

        const toast = document.createElement("div");
        toast.className = `toast align-items-center border-0 text-bg-${variant}`;
        toast.setAttribute("role", "alert");
        toast.setAttribute("aria-live", "assertive");
        toast.setAttribute("aria-atomic", "true");

        const bodyWrapper = document.createElement("div");
        bodyWrapper.className = "d-flex";

        const body = document.createElement("div");
        body.className = "toast-body";

        if (items.length === 1) {
            body.textContent = items[0];
        } else {
            const list = document.createElement("ul");
            list.className = "mb-0 ps-3";

            items.forEach(message => {
                const listItem = document.createElement("li");
                listItem.textContent = message;
                list.appendChild(listItem);
            });

            body.appendChild(list);
        }

        const closeButton = document.createElement("button");
        closeButton.type = "button";
        closeButton.className = variant === "warning" || variant === "light"
            ? "btn-close me-2 m-auto"
            : "btn-close btn-close-white me-2 m-auto";
        closeButton.setAttribute("data-bs-dismiss", "toast");
        closeButton.setAttribute("aria-label", "Close");

        bodyWrapper.appendChild(body);
        bodyWrapper.appendChild(closeButton);
        toast.appendChild(bodyWrapper);
        toastContainer.appendChild(toast);

        if (typeof bootstrap !== "undefined" && bootstrap.Toast) {
            const toastInstance = new bootstrap.Toast(toast, {
                animation: true,
                autohide: true,
                delay: 4500
            });

            toast.addEventListener("hidden.bs.toast", function () {
                toast.remove();
            });

            toastInstance.show();
        } else {
            toast.classList.add("show");
            setTimeout(() => {
                toast.remove();
            }, 4500);
        }
    }

    function clearToasts() {
        if (!toastContainer) {
            return;
        }

        toastContainer.innerHTML = "";
    }


    function hasAllowedExtension(fileName) {
        const normalizedFileName = (fileName || "").toLowerCase();
        return allowedExtensions.some(extension => normalizedFileName.endsWith(extension));
    }

    function hasAllowedMimeType(fileType) {
        if (!fileType) {
            return true;
        }

        return allowedMimeTypes.includes(fileType.toLowerCase());
    }

    function isValidImageFile(file) {
        if (!file || file.size <= 0) {
            return false;
        }

        if (file.size > maxFileBytes) {
            return false;
        }

        if (!hasAllowedExtension(file.name)) {
            return false;
        }

        if (!hasAllowedMimeType(file.type)) {
            return false;
        }

        return true;
    }

    function isDuplicatePendingFile(file) {
        return pendingNewFiles.some(existingFile =>
            existingFile.name === file.name &&
            existingFile.size === file.size &&
            existingFile.lastModified === file.lastModified
        );
    }

    function getExistingItems() {
        if (!existingGrid) {
            return [];
        }

        return Array.from(existingGrid.querySelectorAll(".js-pge-existing-item"));
    }

    function getExistingCount() {
        return getExistingItems().length;
    }

    function getTotalCount() {
        return getExistingCount() + pendingNewFiles.length;
    }

    function ensureAddCardIsLast() {
        if (!newGrid || !addCard) {
            return;
        }

        newGrid.appendChild(addCard);
    }


    function updateState() {
        const existingCount = getExistingCount();
        const newCount = pendingNewFiles.length;
        const totalCount = existingCount + newCount;

        if (existingSection) {
            existingSection.hidden = isEditMode && existingCount === 0;
        }

        if (countLabel) {
            if (totalCount === 0) {
                countLabel.hidden = true;
            } else if (isEditMode) {
                countLabel.textContent = `${totalCount} / ${maxGalleryImages} images`;
                countLabel.hidden = false;
            } else {
                countLabel.textContent = `${newCount} / ${maxGalleryImages} images selected`;
                countLabel.hidden = false;
            }
        }

        if (addCard) {
            addCard.hidden = totalCount >= maxGalleryImages;
        }
        const newNote = root.querySelector(".gallery-new-note");
        if (newNote) newNote.hidden = newCount === 0;
    }

    function renderExistingHiddenInputs() {
        if (!existingIdsContainer || !existingIdsFieldPrefix) {
            updateState();
            return;
        }

        const imageIds = getExistingItems()
            .map(item => item.getAttribute("data-image-id"))
            .filter(Boolean);

        existingIdsContainer.innerHTML = "";

        imageIds.forEach((imageId, index) => {
            const input = document.createElement("input");
            input.type = "hidden";
            input.name = `${existingIdsFieldPrefix}[${index}]`;
            input.value = imageId;
            existingIdsContainer.appendChild(input);
        });

        updateState();
    }

    function syncNewFilesInput() {
        if (!newFilesInput) {
            return;
        }

        const dataTransfer = new DataTransfer();

        pendingNewFiles.forEach(file => {
            dataTransfer.items.add(file);
        });

        newFilesInput.files = dataTransfer.files;
    }

    function preventNativeDragForNewPreview(element) {
        element.setAttribute("draggable", "false");

        element.addEventListener("dragstart", function (event) {
            event.preventDefault();
        });
    }

    function renderNewPreviews() {
        if (!newGrid) {
            return;
        }

        newGrid.querySelectorAll(".js-pge-new-preview-item").forEach(item => item.remove());

        pendingNewFiles.forEach((file, index) => {
            const previewItem = document.createElement("div");
            previewItem.className = "product-gallery-editor__card product-gallery-editor__card--image js-pge-new-preview-item position-relative";
            previewItem.setAttribute("draggable", "false");

            const removeButton = document.createElement("button");
            removeButton.type = "button";
            removeButton.className = "product-gallery-editor__remove-btn js-pge-remove-new";
            removeButton.setAttribute("data-file-index", index.toString());
            removeButton.title = "Remove image";
            removeButton.setAttribute("aria-label", `Remove ${file.name}`);
            removeButton.innerHTML = '<svg class="icon icon--small" aria-hidden="true"><use href="#i-close"></use></svg>';
            removeButton.setAttribute("draggable", "false");

            const image = document.createElement("img");
            image.className = "product-gallery-editor__thumb";
            image.alt = "";
            image.setAttribute("draggable", "false");

            const previewUrl = URL.createObjectURL(file);
            image.src = previewUrl;
            image.onload = function () {
                URL.revokeObjectURL(previewUrl);
            };

            const fileName = document.createElement("div");
            fileName.className = "small text-muted text-truncate mt-1 product-gallery-editor__file-name";
            fileName.title = file.name;
            fileName.textContent = file.name;
            fileName.setAttribute("draggable", "false");

            previewItem.appendChild(removeButton);
            previewItem.appendChild(image);
            previewItem.appendChild(fileName);

            preventNativeDragForNewPreview(previewItem);
            preventNativeDragForNewPreview(removeButton);
            preventNativeDragForNewPreview(image);
            preventNativeDragForNewPreview(fileName);

            if (addCard) {
                newGrid.insertBefore(previewItem, addCard);
            } else {
                newGrid.appendChild(previewItem);
            }
        });

        ensureAddCardIsLast();
        updateState();
    }

    // Open native file picker
    function openFilePicker() {
        if (!newFilesInput) {
            return;
        }

        if (getTotalCount() >= maxGalleryImages) {
            showToast(`Too many gallery images. Max ${maxGalleryImages} in total.`, "warning");
            return;
        }

        newFilesInput.value = "";
        newFilesInput.click();
    }

    // Add files from file picker or drag and drop
    function addFiles(fileList) {
        clearToasts();

        const incomingFiles = Array.from(fileList || []);

        if (incomingFiles.length === 0) {
            return;
        }

        const availableSlots = maxGalleryImages - getTotalCount();

        if (availableSlots <= 0) {
            showToast(`Too many gallery images. Max ${maxGalleryImages} in total.`, "warning");
            return;
        }

        let invalidFilesCount = 0;
        let duplicateFilesCount = 0;
        let overflowFilesCount = 0;
        let addedFilesCount = 0;

        incomingFiles.forEach(file => {
            if (!isValidImageFile(file)) {
                invalidFilesCount++;
                return;
            }

            if (isDuplicatePendingFile(file)) {
                duplicateFilesCount++;
                return;
            }

            if (addedFilesCount >= availableSlots) {
                overflowFilesCount++;
                return;
            }

            pendingNewFiles.push(file);
            addedFilesCount++;
        });

        const messages = [];

        if (invalidFilesCount > 0) {
            messages.push(`${invalidFilesCount} file(s) skipped: invalid format or file too large.`);
        }

        if (duplicateFilesCount > 0) {
            messages.push(`${duplicateFilesCount} file(s) skipped: already selected.`);
        }

        if (overflowFilesCount > 0) {
            messages.push(`${overflowFilesCount} file(s) skipped: gallery limit is ${maxGalleryImages}.`);
        }

        if (messages.length > 0) {
            showToast(messages, "warning");
        }

        syncNewFilesInput();
        renderNewPreviews();
        updateState();
    }

    function removePendingFile(index) {
        pendingNewFiles = pendingNewFiles.filter((_, fileIndex) => fileIndex !== index);

        syncNewFilesInput();
        renderNewPreviews();
        updateState();
    }


    // Check that current drag event really contains files
    function isFileDragEvent(event) {
        if (!event.dataTransfer) {
            return false;
        }

        return Array.from(event.dataTransfer.types || []).includes("Files");
    }

    if (allowExistingReorder && typeof Sortable !== "undefined" && existingGrid) {
        new Sortable(existingGrid, {
            animation: 180,
            draggable: ".js-pge-existing-item",
            filter: ".js-pge-remove-existing",
            preventOnFilter: false,
            ghostClass: "sortable-ghost",
            chosenClass: "product-gallery-editor__card--chosen",
            onSort: renderExistingHiddenInputs
        });
    }

    if (existingGrid) {
        existingGrid.addEventListener("keydown", function (event) {
            const item = event.target.closest(".js-pge-existing-item");
            if (!allowExistingReorder || !item || event.target !== item) return;
            if (event.key !== "ArrowLeft" && event.key !== "ArrowRight") return;
            event.preventDefault();
            const neighbor = event.key === "ArrowLeft" ? item.previousElementSibling : item.nextElementSibling;
            if (!neighbor) return;
            if (event.key === "ArrowLeft") existingGrid.insertBefore(item, neighbor);
            else neighbor.insertAdjacentElement("afterend", item);
            item.focus();
            renderExistingHiddenInputs();
            const status = root.querySelector(".js-pge-reorder-status");
            if (status) status.textContent = `Image moved to position ${getExistingItems().indexOf(item) + 1}.`;
        });
        existingGrid.addEventListener("click", function (event) {
            const removeExistingButton = event.target.closest(".js-pge-remove-existing");
            if (!removeExistingButton) {
                return;
            }

            const existingItem = removeExistingButton.closest(".js-pge-existing-item");

            if (existingItem) {
                existingItem.remove();
                renderExistingHiddenInputs();
            }
        });
    }

    if (newGrid) {
        newGrid.addEventListener("click", function (event) {
            const removeNewButton = event.target.closest(".js-pge-remove-new");
            if (!removeNewButton) {
                return;
            }

            const fileIndex = Number(removeNewButton.getAttribute("data-file-index"));

            if (!Number.isNaN(fileIndex)) {
                removePendingFile(fileIndex);
            }
        });
    }

    if (prompt) {
        prompt.addEventListener("click", function () {
            openFilePicker();
        });

        prompt.addEventListener("keydown", function (event) {
            if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                openFilePicker();
            }
        });
    }

    if (browseButton) {
        browseButton.addEventListener("click", function (event) {
            event.preventDefault();
            event.stopPropagation();
            openFilePicker();
        });
    }

    if (addCard) {
        addCard.addEventListener("click", function () {
            openFilePicker();
        });
    }

    if (newFilesInput) {
        newFilesInput.addEventListener("change", function () {
            addFiles(newFilesInput.files);
        });
    }

    if (dropZone) {
        dropZone.addEventListener("dragenter", function (event) {
            if (!isFileDragEvent(event)) {
                return;
            }

            event.preventDefault();
            fileDragDepth++;
            dropZone.classList.add("product-gallery-editor__drop-zone--over");
        });

        dropZone.addEventListener("dragover", function (event) {
            if (!isFileDragEvent(event)) {
                return;
            }

            event.preventDefault();
            dropZone.classList.add("product-gallery-editor__drop-zone--over");
        });

        dropZone.addEventListener("dragleave", function (event) {
            if (!isFileDragEvent(event)) {
                return;
            }

            event.preventDefault();
            fileDragDepth = Math.max(0, fileDragDepth - 1);

            if (fileDragDepth === 0) {
                dropZone.classList.remove("product-gallery-editor__drop-zone--over");
            }
        });

        dropZone.addEventListener("drop", function (event) {
            if (!isFileDragEvent(event)) {
                return;
            }

            event.preventDefault();
            event.stopPropagation();

            fileDragDepth = 0;
            dropZone.classList.remove("product-gallery-editor__drop-zone--over");

            if (event.dataTransfer.files && event.dataTransfer.files.length > 0) {
                addFiles(event.dataTransfer.files);
            }
        });
    }


    const form = root.closest("form");
    if (form) {
        form.addEventListener("submit", function () {
            renderExistingHiddenInputs();
            syncNewFilesInput();
            clearToasts();
        });
    }

    renderExistingHiddenInputs();
    renderNewPreviews();
    updateState();
}
