"use strict";

document.addEventListener("DOMContentLoaded", function () {

    const fileInput =
        document.getElementById("mailAttachmentInput");

    const fileList =
        document.getElementById("mailAttachmentList");

    const emptyMessage =
        document.getElementById("mailAttachmentEmpty");

    const countElement =
        document.getElementById("mailAttachmentCount");

    const form =
        document.getElementById("mailComposeForm");

    if (!fileInput || !fileList || !emptyMessage || !countElement) {
        console.warn("Mail attachment elements were not found.");
        return;
    }

    const MAX_FILES = 10;
    const MAX_FILE_SIZE = 25 * 1024 * 1024;

    /*
     * IMPORTANT:
     * Keep selected files separately.
     * Do not depend on the browser's original FileList
     * because selecting another file normally replaces it.
     */
    let selectedFiles = [];

    function fileKey(file) {
        return [
            file.name,
            file.size,
            file.lastModified,
            file.type
        ].join("|");
    }

    function formatFileSize(bytes) {

        if (bytes < 1024) {
            return bytes + " B";
        }

        if (bytes < 1024 * 1024) {
            return (bytes / 1024).toFixed(1) + " KB";
        }

        if (bytes < 1024 * 1024 * 1024) {
            return (bytes / 1024 / 1024).toFixed(1) + " MB";
        }

        return (bytes / 1024 / 1024 / 1024).toFixed(1) + " GB";
    }

    function getFileIcon(fileName) {

        const extension =
            fileName
                .split(".")
                .pop()
                .toLowerCase();

        switch (extension) {

            case "pdf":
                return "📕";

            case "jpg":
            case "jpeg":
            case "png":
            case "gif":
            case "webp":
                return "🖼️";

            case "doc":
            case "docx":
                return "📘";

            case "xls":
            case "xlsx":
                return "📗";

            case "ppt":
            case "pptx":
                return "📙";

            case "txt":
                return "📄";

            case "zip":
                return "🗜️";

            default:
                return "📎";
        }
    }

    /*
     * Put all selected files back into the actual
     * <input type="file" multiple>.
     *
     * This is the important part:
     * ASP.NET Core will receive every file as Attachments.
     */
    function syncInputFiles() {

        try {

            const dataTransfer =
                new DataTransfer();

            selectedFiles.forEach(function (file) {
                dataTransfer.items.add(file);
            });

            fileInput.files =
                dataTransfer.files;

        }
        catch (error) {

            console.error(
                "Unable to synchronize attachment files:",
                error
            );
        }
    }

    function renderAttachments() {

        fileList.innerHTML = "";

        countElement.textContent =
            selectedFiles.length.toString();

        if (selectedFiles.length === 0) {

            emptyMessage.style.display = "block";

            return;
        }

        emptyMessage.style.display = "none";

        selectedFiles.forEach(function (file, index) {

            const row =
                document.createElement("div");

            row.className =
                "mail-attachment-item";

            const info =
                document.createElement("div");

            info.className =
                "mail-attachment-info";

            const icon =
                document.createElement("span");

            icon.className =
                "mail-attachment-icon";

            icon.textContent =
                getFileIcon(file.name);

            const name =
                document.createElement("span");

            name.className =
                "mail-attachment-name";

            name.textContent =
                file.name;

            const size =
                document.createElement("span");

            size.className =
                "mail-attachment-size";

            size.textContent =
                formatFileSize(file.size);

            const remove =
                document.createElement("button");

            remove.type = "button";

            remove.className =
                "mail-attachment-remove";

            remove.title =
                "Remove attachment";

            remove.setAttribute(
                "aria-label",
                "Remove " + file.name
            );

            remove.textContent = "×";

            remove.dataset.index =
                index.toString();

            info.appendChild(icon);
            info.appendChild(name);
            info.appendChild(size);

            row.appendChild(info);
            row.appendChild(remove);

            fileList.appendChild(row);
        });
    }

    function addFiles(files) {

        if (!files || files.length === 0) {
            return;
        }

        const existingKeys =
            new Set(
                selectedFiles.map(fileKey)
            );

        let added = 0;

        Array.from(files).forEach(function (file) {

            if (selectedFiles.length >= MAX_FILES) {

                return;
            }

            if (!file || file.size <= 0) {

                return;
            }

            if (file.size > MAX_FILE_SIZE) {

                alert(
                    '"' +
                    file.name +
                    '" is larger than 25 MB.'
                );

                return;
            }

            const key =
                fileKey(file);

            if (existingKeys.has(key)) {

                return;
            }

            selectedFiles.push(file);

            existingKeys.add(key);

            added++;
        });

        /*
         * Do NOT set fileInput.value = "" here.
         *
         * That was causing problems with maintaining
         * the selected files.
         */

        syncInputFiles();

        renderAttachments();

        if (selectedFiles.length >= MAX_FILES) {

            if (added > 0) {

                alert(
                    "Maximum 10 files can be attached to one email."
                );
            }
        }
    }

    /*
     * File picker
     */
    fileInput.addEventListener(
        "change",
        function (event) {

            const files =
                event.target.files;

            if (!files || files.length === 0) {
                return;
            }

            addFiles(files);
        }
    );

    /*
     * Remove individual attachment
     */
    fileList.addEventListener(
        "click",
        function (event) {

            const removeButton =
                event.target.closest(
                    ".mail-attachment-remove"
                );

            if (!removeButton) {
                return;
            }

            const index =
                Number(
                    removeButton.dataset.index
                );

            if (
                Number.isNaN(index) ||
                index < 0 ||
                index >= selectedFiles.length
            ) {
                return;
            }

            selectedFiles.splice(index, 1);

            syncInputFiles();

            renderAttachments();
        }
    );

    /*
     * Before submit, synchronize once again.
     * This guarantees ASP.NET Core receives all files.
     */
    if (form) {

        form.addEventListener(
            "submit",
            function () {

                syncInputFiles();

            },
            true
        );
    }

    /*
     * Expose a small helper so mail-composer.js
     * can open the attachment picker.
     */
    window.mailAttachmentManager = {

        open: function () {

            fileInput.click();

        },

        getFiles: function () {

            return selectedFiles.slice();

        },

        clear: function () {

            selectedFiles = [];

            syncInputFiles();

            renderAttachments();

        }
    };

    renderAttachments();

});