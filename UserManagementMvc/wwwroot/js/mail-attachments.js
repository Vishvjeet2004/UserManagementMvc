"use strict";

document.addEventListener(
    "DOMContentLoaded",
    function () {

        const form =
            document.getElementById(
                "mailComposeForm"
            );

        const fileInput =
            document.getElementById(
                "mailAttachmentInput"
            );

        const fileList =
            document.getElementById(
                "mailAttachmentList"
            );

        const selectedContainer =
            document.getElementById(
                "mailSelectedAttachments"
            );

        if (!fileInput) {

            console.error(
                "Mail attachments: file input was not found."
            );

            return;
        }


        const MAX_FILES = 10;

        const MAX_FILE_SIZE =
            25 * 1024 * 1024;


        const ALLOWED_EXTENSIONS = [
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp",
            ".pdf",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx",
            ".ppt",
            ".pptx",
            ".txt",
            ".zip"
        ];


        let selectedFiles = [];


        /*
         * --------------------------------------------------
         * FILE KEY
         * --------------------------------------------------
         */

        function getFileKey(file) {

            return [
                file.name,
                file.size,
                file.lastModified,
                file.type
            ].join("|");

        }


        /*
         * --------------------------------------------------
         * FILE EXTENSION
         * --------------------------------------------------
         */

        function getExtension(fileName) {

            const name =
                String(
                    fileName || ""
                )
                    .trim()
                    .toLowerCase();

            const lastDot =
                name.lastIndexOf(".");

            if (lastDot < 0) {

                return "";

            }

            return name.substring(
                lastDot
            );

        }


        /*
         * --------------------------------------------------
         * FILE SIZE
         * --------------------------------------------------
         */

        function formatFileSize(bytes) {

            if (bytes < 1024) {

                return (
                    bytes +
                    " B"
                );

            }

            if (
                bytes <
                1024 * 1024
            ) {

                return (
                    (
                        bytes / 1024
                    )
                        .toFixed(1)
                        .replace(
                            ".0",
                            ""
                        ) +
                    " KB"
                );

            }

            if (
                bytes <
                1024 *
                1024 *
                1024
            ) {

                return (
                    (
                        bytes /
                        1024 /
                        1024
                    )
                        .toFixed(1)
                        .replace(
                            ".0",
                            ""
                        ) +
                    " MB"
                );

            }

            return (
                (
                    bytes /
                    1024 /
                    1024 /
                    1024
                )
                    .toFixed(1)
                    .replace(
                        ".0",
                        ""
                    ) +
                " GB"
            );

        }


        /*
         * --------------------------------------------------
         * IMAGE CHECK
         * --------------------------------------------------
         */

        function isImage(file) {

            const mimeType =
                String(
                    file.type || ""
                ).toLowerCase();


            if (
                mimeType.startsWith(
                    "image/"
                )
            ) {

                return true;

            }


            return [
                ".jpg",
                ".jpeg",
                ".png",
                ".gif",
                ".webp"
            ].includes(
                getExtension(
                    file.name
                )
            );

        }


        /*
         * --------------------------------------------------
         * FILE ICON
         * --------------------------------------------------
         */

        function getFileIcon(
            fileName
        ) {

            switch (
            getExtension(
                fileName
            )
            ) {

                case ".pdf":

                    return "📕";


                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".webp":

                    return "🖼️";


                case ".doc":
                case ".docx":

                    return "📘";


                case ".xls":
                case ".xlsx":

                    return "📗";


                case ".ppt":
                case ".pptx":

                    return "📙";


                case ".txt":

                    return "📄";


                case ".zip":

                    return "🗜️";


                default:

                    return "📎";

            }

        }


        /*
         * --------------------------------------------------
         * FILE VALIDATION
         * --------------------------------------------------
         */

        function validateFile(
            file
        ) {

            if (!file) {

                return {
                    valid: false,
                    message:
                        "Invalid file."
                };

            }


            if (
                file.size <= 0
            ) {

                return {
                    valid: false,
                    message:
                        file.name +
                        " is empty."
                };

            }


            if (
                file.size >
                MAX_FILE_SIZE
            ) {

                return {
                    valid: false,
                    message:
                        file.name +
                        " exceeds the 25 MB limit."
                };

            }


            const extension =
                getExtension(
                    file.name
                );


            if (
                !ALLOWED_EXTENSIONS.includes(
                    extension
                )
            ) {

                return {
                    valid: false,
                    message:
                        file.name +
                        " is not an allowed file type."
                };

            }


            return {
                valid: true,
                message: ""
            };

        }


        /*
         * --------------------------------------------------
         * SYNCHRONIZE REAL INPUT
         * --------------------------------------------------
         */

        function syncInputFiles() {

            try {

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
            catch (error) {

                console.error(
                    "Attachment synchronization failed:",
                    error
                );

            }

        }


        /*
         * --------------------------------------------------
         * OPEN FILE PICKER
         * --------------------------------------------------
         */

        function openFilePicker() {

            /*
             * Reset native input only.
             * Existing selected files remain.
             */
            fileInput.value = "";

            fileInput.click();

        }


        /*
         * --------------------------------------------------
         * ADD FILES
         * --------------------------------------------------
         */

        function addFiles(
            files
        ) {

            if (
                !files ||
                files.length === 0
            ) {

                return;

            }


            const existingKeys =
                new Set(
                    selectedFiles.map(
                        getFileKey
                    )
                );


            const rejected =
                [];


            Array.from(files).forEach(
                function (file) {

                    if (
                        selectedFiles.length >=
                        MAX_FILES
                    ) {

                        rejected.push(
                            "Maximum 10 files are allowed."
                        );

                        return;

                    }


                    const validation =
                        validateFile(
                            file
                        );


                    if (
                        !validation.valid
                    ) {

                        rejected.push(
                            validation.message
                        );

                        return;

                    }


                    const key =
                        getFileKey(
                            file
                        );


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

                }
            );


            syncInputFiles();

            renderAttachments();


            if (
                rejected.length > 0
            ) {

                alert(
                    rejected.join("\n")
                );

            }

        }


        /*
         * --------------------------------------------------
         * RENDER ATTACHMENTS
         * --------------------------------------------------
         */

        function renderAttachments() {

            if (fileList) {

                fileList.innerHTML =
                    "";

            }


            if (!selectedContainer) {

                return;

            }


            selectedContainer.innerHTML =
                "";


            /*
             * Show the attachment area only
             * when at least one file exists.
             */
            if (
                selectedFiles.length === 0
            ) {

                selectedContainer.style.display =
                    "none";

                selectedContainer.classList.remove(
                    "has-files"
                );

                return;

            }


            selectedContainer.style.display =
                "flex";

            selectedContainer.style.flexDirection =
                "column";

            selectedContainer.style.flexWrap =
                "nowrap";

            selectedContainer.style.alignItems =
                "stretch";

            selectedContainer.style.gap =
                "8px";

            selectedContainer.style.width =
                "100%";

            selectedContainer.style.boxSizing =
                "border-box";

            selectedContainer.classList.add(
                "has-files"
            );


            /*
             * Render every file separately
             * in original attachment order.
             */
            selectedFiles.forEach(
                function (
                    file,
                    index
                ) {

                    const row =
                        document.createElement(
                            "div"
                        );


                    row.className =
                        "mail-selected-file";


                    /*
                     * Each attachment gets
                     * its own complete row.
                     */
                    row.style.width =
                        "100%";

                    row.style.maxWidth =
                        "100%";

                    row.style.minHeight =
                        "70px";

                    row.style.boxSizing =
                        "border-box";

                    row.style.flex =
                        "0 0 auto";

                    row.style.display =
                        "flex";

                    row.style.alignItems =
                        "center";

                    row.style.position =
                        "relative";

                    row.style.overflow =
                        "hidden";


                    /*
                     * --------------------------------------------------
                     * IMAGE ATTACHMENT
                     * --------------------------------------------------
                     *
                     * Important:
                     * The complete image is displayed.
                     * No cropping is performed.
                     */
                    if (
                        isImage(file)
                    ) {

                        const imageBox =
                            document.createElement(
                                "div"
                            );


                        imageBox.className =
                            "mail-selected-file-image-box";


                        imageBox.style.width =
                            "240px";

                        imageBox.style.minWidth =
                            "240px";

                        imageBox.style.maxWidth =
                            "240px";

                        imageBox.style.height =
                            "150px";

                        imageBox.style.minHeight =
                            "150px";

                        imageBox.style.display =
                            "flex";

                        imageBox.style.alignItems =
                            "center";

                        imageBox.style.justifyContent =
                            "center";

                        imageBox.style.overflow =
                            "hidden";

                        imageBox.style.background =
                            "#f8fafc";

                        imageBox.style.border =
                            "1px solid #e5e7eb";

                        imageBox.style.borderRadius =
                            "8px";


                        const image =
                            document.createElement(
                                "img"
                            );


                        image.className =
                            "mail-selected-file-image";


                        image.alt =
                            file.name;


                        /*
                         * IMPORTANT:
                         * contain keeps the complete
                         * photo visible without crop.
                         */
                        image.style.width =
                            "100%";

                        image.style.height =
                            "100%";

                        image.style.maxWidth =
                            "100%";

                        image.style.maxHeight =
                            "100%";

                        image.style.objectFit =
                            "contain";

                        image.style.objectPosition =
                            "center";

                        image.style.display =
                            "block";


                        const objectUrl =
                            URL.createObjectURL(
                                file
                            );


                        image.src =
                            objectUrl;


                        image.addEventListener(
                            "load",
                            function () {

                                URL.revokeObjectURL(
                                    objectUrl
                                );

                            },
                            {
                                once: true
                            }
                        );


                        imageBox.appendChild(
                            image
                        );


                        row.appendChild(
                            imageBox
                        );

                    }


                    /*
                     * --------------------------------------------------
                     * NON-IMAGE ATTACHMENT
                     * --------------------------------------------------
                     */

                    else {

                        const iconBox =
                            document.createElement(
                                "div"
                            );


                        iconBox.className =
                            "mail-selected-file-icon-box";


                        iconBox.style.width =
                            "72px";

                        iconBox.style.minWidth =
                            "72px";

                        iconBox.style.height =
                            "72px";

                        iconBox.style.display =
                            "flex";

                        iconBox.style.alignItems =
                            "center";

                        iconBox.style.justifyContent =
                            "center";

                        iconBox.style.fontSize =
                            "38px";


                        const icon =
                            document.createElement(
                                "span"
                            );


                        icon.className =
                            "mail-selected-file-icon";


                        icon.textContent =
                            getFileIcon(
                                file.name
                            );


                        iconBox.appendChild(
                            icon
                        );


                        row.appendChild(
                            iconBox
                        );

                    }


                    /*
                     * --------------------------------------------------
                     * FILE INFORMATION
                     * --------------------------------------------------
                     */

                    const info =
                        document.createElement(
                            "div"
                        );


                    info.className =
                        "mail-selected-file-info";


                    info.style.flex =
                        "1 1 auto";

                    info.style.minWidth =
                        "0";

                    info.style.marginLeft =
                        "12px";

                    info.style.paddingRight =
                        "40px";

                    info.style.display =
                        "flex";

                    info.style.flexDirection =
                        "column";

                    info.style.justifyContent =
                        "center";


                    const name =
                        document.createElement(
                            "div"
                        );


                    name.className =
                        "mail-selected-file-name";


                    name.textContent =
                        file.name;


                    name.title =
                        file.name;


                    name.style.maxWidth =
                        "100%";

                    name.style.overflow =
                        "hidden";

                    name.style.textOverflow =
                        "ellipsis";

                    name.style.whiteSpace =
                        "nowrap";

                    name.style.fontSize =
                        "14px";


                    const size =
                        document.createElement(
                            "div"
                        );


                    size.className =
                        "mail-selected-file-size";


                    size.textContent =
                        formatFileSize(
                            file.size
                        );


                    size.style.marginTop =
                        "4px";

                    size.style.fontSize =
                        "12px";


                    info.appendChild(
                        name
                    );

                    info.appendChild(
                        size
                    );


                    row.appendChild(
                        info
                    );


                    /*
                     * --------------------------------------------------
                     * REMOVE BUTTON
                     * --------------------------------------------------
                     */

                    const removeButton =
                        document.createElement(
                            "button"
                        );


                    removeButton.type =
                        "button";


                    removeButton.className =
                        "mail-selected-file-remove";


                    removeButton.dataset.index =
                        index.toString();


                    removeButton.title =
                        "Remove attachment";


                    removeButton.setAttribute(
                        "aria-label",
                        "Remove " +
                        file.name
                    );


                    removeButton.textContent =
                        "×";


                    removeButton.style.position =
                        "absolute";

                    removeButton.style.right =
                        "12px";

                    removeButton.style.top =
                        "50%";

                    removeButton.style.transform =
                        "translateY(-50%)";

                    removeButton.style.width =
                        "28px";

                    removeButton.style.height =
                        "28px";

                    removeButton.style.flex =
                        "0 0 28px";


                    row.appendChild(
                        removeButton
                    );


                    /*
                     * Add this row only after
                     * everything is completely built.
                     */
                    selectedContainer.appendChild(
                        row
                    );

                }
            );

        }


        /*
         * --------------------------------------------------
         * REMOVE ATTACHMENT
         * --------------------------------------------------
         */

        if (selectedContainer) {

            selectedContainer.addEventListener(
                "click",
                function (event) {

                    const removeButton =
                        event.target.closest(
                            ".mail-selected-file-remove"
                        );


                    if (!removeButton) {

                        return;

                    }


                    event.preventDefault();

                    event.stopPropagation();


                    const index =
                        Number(
                            removeButton.dataset.index
                        );


                    if (
                        Number.isNaN(index) ||
                        index < 0 ||
                        index >=
                        selectedFiles.length
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

        }


        /*
         * --------------------------------------------------
         * FILE INPUT CHANGE
         * --------------------------------------------------
         */

        fileInput.addEventListener(
            "change",
            function (event) {

                const files =
                    event.target.files;


                if (
                    files &&
                    files.length > 0
                ) {

                    addFiles(
                        files
                    );

                }


                /*
                 * Clear native input so
                 * the same file can be
                 * selected again.
                 */
                fileInput.value =
                    "";

            }
        );


        /*
         * --------------------------------------------------
         * FINAL FORM SUBMIT
         * --------------------------------------------------
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
         * --------------------------------------------------
         * PUBLIC ATTACHMENT MANAGER
         * --------------------------------------------------
         */

        window.mailAttachmentManager = {

            open:
                function () {

                    openFilePicker();

                },


            getFiles:
                function () {

                    return selectedFiles.slice();

                },


            sync:
                function () {

                    syncInputFiles();

                },


            clear:
                function () {

                    selectedFiles = [];

                    fileInput.value =
                        "";

                    syncInputFiles();

                    renderAttachments();

                },


            addFiles:
                function (
                    files
                ) {

                    addFiles(
                        files
                    );

                }

        };


        /*
         * --------------------------------------------------
         * INITIAL STATE
         * --------------------------------------------------
         */

        syncInputFiles();

        renderAttachments();

    }
);