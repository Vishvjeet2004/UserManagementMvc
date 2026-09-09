"use strict";

document.addEventListener("DOMContentLoaded", function () {

    const fileInput =
        document.getElementById(
            "mailAttachmentInput"
        );

    const fileList =
        document.getElementById(
            "mailAttachmentList"
        );

    const emptyMessage =
        document.getElementById(
            "mailAttachmentEmpty"
        );

    const countElement =
        document.getElementById(
            "mailAttachmentCount"
        );

    const attachmentPanel =
        document.getElementById(
            "mailAttachmentsPanel"
        );

    const form =
        document.getElementById(
            "mailComposeForm"
        );

    if (
        !fileInput ||
        !fileList ||
        !emptyMessage ||
        !countElement
    ) {
        console.warn(
            "Mail attachment elements were not found."
        );

        return;
    }

    const MAX_FILES = 10;

    const MAX_FILE_SIZE =
        25 * 1024 * 1024;

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

        if (
            bytes <
            1024 * 1024
        ) {
            return (
                bytes / 1024
            ).toFixed(1) +
                " KB";
        }

        if (
            bytes <
            1024 * 1024 * 1024
        ) {
            return (
                bytes /
                1024 /
                1024
            ).toFixed(1) +
                " MB";
        }

        return (
            bytes /
            1024 /
            1024 /
            1024
        ).toFixed(1) +
            " GB";
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

            case "doc":
            case "docx":
                return "📘";

            case "xls":
            case "xlsx":
                return "📗";

            case "ppt":
            case "pptx":
                return "📙";

            case "jpg":
            case "jpeg":
            case "png":
            case "gif":
            case "webp":
                return "🖼️";

            case "zip":
                return "🗜️";

            case "txt":
                return "📄";

            default:
                return "📎";
        }
    }

    function updateAttachmentPanel() {

        if (!attachmentPanel) {
            return;
        }

        if (selectedFiles.length === 0) {

            attachmentPanel.hidden = true;

            attachmentPanel.style.display =
                "none";

            return;
        }

        attachmentPanel.hidden = false;

        attachmentPanel.style.display =
            "";
    }

    function syncInputFiles() {

        const dataTransfer =
            new DataTransfer();

        selectedFiles.forEach(
            function (file) {

                dataTransfer.items.add(
                    file
                );
            }
        );

        fileInput.files =
            dataTransfer.files;
    }

    function renderAttachments() {

        fileList.innerHTML =
            "";

        countElement.textContent =
            selectedFiles.length
                .toString();

        if (
            selectedFiles.length === 0
        ) {

            emptyMessage.style.display =
                "block";

            updateAttachmentPanel();

            return;
        }

        emptyMessage.style.display =
            "none";

        selectedFiles.forEach(
            function (file, index) {

                const row =
                    document.createElement(
                        "div"
                    );

                row.className =
                    "mail-attachment-item";

                const info =
                    document.createElement(
                        "div"
                    );

                info.className =
                    "mail-attachment-info";

                const icon =
                    document.createElement(
                        "span"
                    );

                icon.className =
                    "mail-attachment-icon";

                icon.textContent =
                    getFileIcon(
                        file.name
                    );

                const name =
                    document.createElement(
                        "span"
                    );

                name.className =
                    "mail-attachment-name";

                name.textContent =
                    file.name;

                name.title =
                    file.name;

                const size =
                    document.createElement(
                        "span"
                    );

                size.className =
                    "mail-attachment-size";

                size.textContent =
                    formatFileSize(
                        file.size
                    );

                const removeButton =
                    document.createElement(
                        "button"
                    );

                removeButton.type =
                    "button";

                removeButton.className =
                    "mail-attachment-remove";

                removeButton.dataset.index =
                    index.toString();

                removeButton.title =
                    "Remove attachment";

                removeButton.textContent =
                    "×";

                info.appendChild(
                    icon
                );

                info.appendChild(
                    name
                );

                info.appendChild(
                    size
                );

                row.appendChild(
                    info
                );

                row.appendChild(
                    removeButton
                );

                fileList.appendChild(
                    row
                );
            }
        );

        updateAttachmentPanel();
    }

    function addFiles(files) {

        if (
            !files ||
            files.length === 0
        ) {
            return;
        }

        const existingKeys =
            new Set(
                selectedFiles.map(
                    fileKey
                )
            );

        let added = 0;

        Array.from(files).forEach(
            function (file) {

                if (
                    selectedFiles.length >=
                    MAX_FILES
                ) {
                    return;
                }

                if (
                    !file ||
                    file.size <= 0
                ) {
                    return;
                }

                if (
                    file.size >
                    MAX_FILE_SIZE
                ) {

                    alert(
                        '"' +
                        file.name +
                        '" is larger than 25 MB.'
                    );

                    return;
                }

                const key =
                    fileKey(file);

                if (
                    existingKeys.has(
                        key
                    )
                ) {
                    return;
                }

                selectedFiles.push(
                    file
                );

                existingKeys.add(
                    key
                );

                added++;
            }
        );

        syncInputFiles();

        renderAttachments();

        if (
            selectedFiles.length >=
            MAX_FILES &&
            added > 0
        ) {

            alert(
                "Maximum 10 files can be attached to one email."
            );
        }
    }

    fileInput.addEventListener(
        "change",
        function (event) {

            const files =
                Array.from(
                    event.target.files || []
                );

            fileInput.value =
                "";

            addFiles(files);
        }
    );

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

            selectedFiles.splice(
                index,
                1
            );

            syncInputFiles();

            renderAttachments();
        }
    );

    if (form) {

        form.addEventListener(
            "submit",
            function () {

                syncInputFiles();
            },
            true
        );
    }

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