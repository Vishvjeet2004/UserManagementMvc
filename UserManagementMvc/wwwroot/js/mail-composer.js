"use strict";

document.addEventListener(
    "DOMContentLoaded",
    function () {

        const form =
            document.getElementById(
                "mailComposeForm"
            );

        const editor =
            document.getElementById(
                "mailEditor"
            );

        const bodyField =
            document.getElementById(
                "mailBodyField"
            );

        const toolbar =
            document.getElementById(
                "mailComposeToolbar"
            );

        const attachmentInput =
            document.getElementById(
                "mailAttachmentInput"
            );

        const ccField =
            document.getElementById(
                "ccField"
            );

        const bccField =
            document.getElementById(
                "bccField"
            );

        const showCcButton =
            document.getElementById(
                "showCcButton"
            );

        const showBccButton =
            document.getElementById(
                "showBccButton"
            );

        const fontPopover =
            document.getElementById(
                "fontPopover"
            );

        const fontSizePopover =
            document.getElementById(
                "fontSizePopover"
            );

        const colorPopover =
            document.getElementById(
                "colorPopover"
            );

        const alignPopover =
            document.getElementById(
                "alignPopover"
            );

        const config =
            window.mailComposeConfig || {
                tools: [],
                signatures: []
            };

        if (
            !form ||
            !editor ||
            !bodyField ||
            !toolbar
        ) {
            console.error(
                "Mail composer: required elements are missing."
            );

            return;
        }

        let savedRange = null;

        /*
         * --------------------------------------------------
         * INITIALIZE EDITOR
         * --------------------------------------------------
         */

        if (
            bodyField.value &&
            bodyField.value.trim() !== ""
        ) {
            editor.innerHTML =
                bodyField.value;
        }

        /*
         * --------------------------------------------------
         * CC
         * --------------------------------------------------
         */

        if (
            showCcButton &&
            ccField
        ) {
            showCcButton.addEventListener(
                "click",
                function () {

                    ccField.classList.toggle(
                        "is-visible"
                    );

                    ccField.hidden =
                        !ccField.classList.contains(
                            "is-visible"
                        );
                }
            );
        }

        /*
         * --------------------------------------------------
         * BCC
         * --------------------------------------------------
         */

        if (
            showBccButton &&
            bccField
        ) {
            showBccButton.addEventListener(
                "click",
                function () {

                    bccField.classList.toggle(
                        "is-visible"
                    );

                    bccField.hidden =
                        !bccField.classList.contains(
                            "is-visible"
                        );
                }
            );
        }

        /*
         * --------------------------------------------------
         * SAVE SELECTION
         * --------------------------------------------------
         */

        function saveSelection() {

            const selection =
                window.getSelection();

            if (
                !selection ||
                selection.rangeCount === 0
            ) {
                return;
            }

            const range =
                selection.getRangeAt(0);

            if (
                editor.contains(
                    range.commonAncestorContainer
                )
            ) {
                savedRange =
                    range.cloneRange();
            }
        }

        /*
         * --------------------------------------------------
         * RESTORE SELECTION
         * --------------------------------------------------
         */

        function restoreSelection() {

            if (!savedRange) {
                editor.focus();
                return;
            }

            try {

                const selection =
                    window.getSelection();

                selection.removeAllRanges();

                selection.addRange(
                    savedRange
                );

                editor.focus();

            }
            catch (error) {

                console.error(
                    "Unable to restore selection:",
                    error
                );

                editor.focus();
            }
        }

        /*
         * --------------------------------------------------
         * SYNC BODY
         * --------------------------------------------------
         */

        function syncBody() {

            bodyField.value =
                editor.innerHTML.trim();
        }

        /*
         * --------------------------------------------------
         * FORMAT COMMAND
         * --------------------------------------------------
         */

        function execute(
            command,
            value = null
        ) {

            restoreSelection();

            try {

                document.execCommand(
                    command,
                    false,
                    value
                );

            }
            catch (error) {

                console.error(
                    "Mail editor command failed:",
                    error
                );
            }

            syncBody();
        }

        /*
         * --------------------------------------------------
         * POPOVERS
         * --------------------------------------------------
         */

        function closePopovers() {

            if (fontPopover) {
                fontPopover.hidden = true;
            }

            if (fontSizePopover) {
                fontSizePopover.hidden = true;
            }

            if (colorPopover) {
                colorPopover.hidden = true;
            }

            if (alignPopover) {
                alignPopover.hidden = true;
            }
        }

        /*
         * --------------------------------------------------
         * TOOLBAR
         * --------------------------------------------------
         */

        toolbar
            .querySelectorAll(
                ".mail-toolbar-button"
            )
            .forEach(
                function (button) {

                    const tool =
                        button.dataset.tool;

                    /*
                     * Save selection without cancelling
                     * the button click.
                     */
                    button.addEventListener(
                        "mousedown",
                        function () {
                            saveSelection();
                        }
                    );

                    button.addEventListener(
                        "click",
                        function () {

                            handleTool(
                                tool
                            );
                        }
                    );
                }
            );

        /*
         * --------------------------------------------------
         * HANDLE TOOL
         * --------------------------------------------------
         */

        function handleTool(tool) {

            closePopovers();

            switch (tool) {

                case "undo":

                    execute("undo");

                    break;

                case "redo":

                    execute("redo");

                    break;

                case "font":

                    saveSelection();

                    if (fontPopover) {
                        fontPopover.hidden = false;
                    }

                    break;

                case "fontSize":

                    saveSelection();

                    if (fontSizePopover) {
                        fontSizePopover.hidden = false;
                    }

                    break;

                case "bold":

                    execute("bold");

                    break;

                case "italic":

                    execute("italic");

                    break;

                case "underline":

                    execute("underline");

                    break;

                case "textColor":

                    saveSelection();

                    if (colorPopover) {
                        colorPopover.hidden = false;
                    }

                    break;

                case "align":

                    saveSelection();

                    if (alignPopover) {
                        alignPopover.hidden = false;
                    }

                    break;

                case "orderedList":

                    execute(
                        "insertOrderedList"
                    );

                    break;

                case "unorderedList":

                    execute(
                        "insertUnorderedList"
                    );

                    break;

                case "outdent":

                    execute("outdent");

                    break;

                case "indent":

                    execute("indent");

                    break;

                case "quote":

                    execute(
                        "formatBlock",
                        "blockquote"
                    );

                    break;

                case "strike":

                    execute(
                        "strikeThrough"
                    );

                    break;

                case "removeFormat":

                    execute(
                        "removeFormat"
                    );

                    break;

                case "attachment":

                    openAttachmentPicker(
                        false
                    );

                    break;

                case "image":

                    openAttachmentPicker(
                        true
                    );

                    break;

                case "link":

                    insertLink();

                    break;

                case "emoji":

                    openEmojiPicker();

                    break;

                case "signature":

                    insertSignature();

                    break;

                case "confidential":

                    insertConfidentialNotice();

                    break;

                default:

                    console.warn(
                        "Unknown mail tool:",
                        tool
                    );

                    break;
            }
        }

        /*
         * --------------------------------------------------
         * ATTACHMENT PICKER
         * --------------------------------------------------
         */

        function openAttachmentPicker(
            imageOnly
        ) {

            if (!attachmentInput) {

                console.error(
                    "Mail attachment input was not found."
                );

                return;
            }

            if (imageOnly) {

                attachmentInput.accept =
                    ".jpg,.jpeg,.png,.gif,.webp";

            }
            else {

                attachmentInput.accept =
                    ".jpg,.jpeg,.png,.gif,.webp,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.zip";
            }

            if (
                window.mailAttachmentManager &&
                typeof
                window.mailAttachmentManager.open ===
                "function"
            ) {

                window.mailAttachmentManager.open();

                return;
            }

            attachmentInput.click();
        }

        /*
         * --------------------------------------------------
         * FONT
         * --------------------------------------------------
         */

        if (fontPopover) {

            fontPopover
                .querySelectorAll(
                    "[data-font]"
                )
                .forEach(
                    function (button) {

                        button.addEventListener(
                            "mousedown",
                            function (event) {
                                event.preventDefault();
                            }
                        );

                        button.addEventListener(
                            "click",
                            function () {

                                execute(
                                    "fontName",
                                    button.dataset.font
                                );

                                closePopovers();
                            }
                        );
                    }
                );
        }

        /*
         * --------------------------------------------------
         * FONT SIZE
         * --------------------------------------------------
         */

        if (fontSizePopover) {

            fontSizePopover
                .querySelectorAll(
                    "[data-size]"
                )
                .forEach(
                    function (button) {

                        button.addEventListener(
                            "mousedown",
                            function (event) {
                                event.preventDefault();
                            }
                        );

                        button.addEventListener(
                            "click",
                            function () {

                                execute(
                                    "fontSize",
                                    button.dataset.size
                                );

                                closePopovers();
                            }
                        );
                    }
                );
        }

        /*
         * --------------------------------------------------
         * TEXT COLOR
         * --------------------------------------------------
         */

        if (colorPopover) {

            colorPopover
                .querySelectorAll(
                    "[data-color]"
                )
                .forEach(
                    function (button) {

                        button.addEventListener(
                            "mousedown",
                            function (event) {
                                event.preventDefault();
                            }
                        );

                        button.addEventListener(
                            "click",
                            function () {

                                execute(
                                    "foreColor",
                                    button.dataset.color
                                );

                                closePopovers();
                            }
                        );
                    }
                );
        }

        /*
         * --------------------------------------------------
         * ALIGNMENT
         * --------------------------------------------------
         */

        if (alignPopover) {

            alignPopover
                .querySelectorAll(
                    "[data-align]"
                )
                .forEach(
                    function (button) {

                        button.addEventListener(
                            "mousedown",
                            function (event) {
                                event.preventDefault();
                            }
                        );

                        button.addEventListener(
                            "click",
                            function () {

                                execute(
                                    button.dataset.align
                                );

                                closePopovers();
                            }
                        );
                    }
                );
        }

        /*
         * --------------------------------------------------
         * LINK
         * --------------------------------------------------
         */

        function insertLink() {

            saveSelection();

            const url =
                window.prompt(
                    "Enter URL:",
                    "https://"
                );

            if (
                !url ||
                url.trim() === ""
            ) {
                return;
            }

            execute(
                "createLink",
                url.trim()
            );
        }

        /*
         * --------------------------------------------------
         * SIGNATURE
         * --------------------------------------------------
         */

        function insertSignature() {

            if (
                !Array.isArray(
                    config.signatures
                ) ||
                config.signatures.length === 0
            ) {

                alert(
                    "No active signature is available."
                );

                return;
            }

            restoreSelection();

            let signature =
                config.signatures.find(
                    function (item) {
                        return item.isDefault;
                    }
                );

            if (!signature) {
                signature =
                    config.signatures[0];
            }

            const container =
                document.createElement(
                    "div"
                );

            container.innerHTML =
                signature.html || "";

            const selection =
                window.getSelection();

            let range;

            if (
                selection &&
                selection.rangeCount > 0 &&
                editor.contains(
                    selection.getRangeAt(0)
                        .commonAncestorContainer
                )
            ) {

                range =
                    selection.getRangeAt(0);

            }
            else {

                range =
                    document.createRange();

                range.selectNodeContents(
                    editor
                );

                range.collapse(false);
            }

            range.insertNode(
                container
            );

            range.setStartAfter(
                container
            );

            range.collapse(true);

            selection.removeAllRanges();

            selection.addRange(
                range
            );

            syncBody();
        }

        /*
         * --------------------------------------------------
         * CONFIDENTIAL NOTICE
         * --------------------------------------------------
         */

        function insertConfidentialNotice() {

            restoreSelection();

            const html =
                "<div><strong>Confidential:</strong> This message may contain confidential information intended only for the recipient.</div>";

            try {

                document.execCommand(
                    "insertHTML",
                    false,
                    html
                );

            }
            catch (error) {

                console.error(
                    "Unable to insert confidential notice:",
                    error
                );
            }

            syncBody();
        }

        /*
         * --------------------------------------------------
         * EMOJI
         * --------------------------------------------------
         */

        const fallbackEmojis = [
            "😀", "😃", "😄", "😁",
            "😆", "😅", "😂", "🤣",
            "😊", "😇", "🙂", "🙃",
            "😉", "😍", "🥰", "😘",
            "😎", "🤩", "🥳", "😏",
            "😞", "😔", "😟", "😕",
            "🙁", "😣", "😫", "🥺",
            "😢", "😭", "😤", "😠",
            "😡", "🤬", "🤯", "😳",
            "😱", "😨", "😰", "🤗",
            "🤔", "🤭", "🤫", "😐",
            "🙄", "😮", "😲", "🥱",
            "😴", "❤️", "💔", "💕",
            "💯", "✨", "⭐", "🔥",
            "🎉", "👍", "👎", "👏",
            "🙌", "🙏", "💪", "🤝",
            "👌", "✌️", "👋", "🌹",
            "☀️", "🌈", "❄️", "🌙",
            "🍎", "🍕", "🍔", "🍰",
            "☕", "⚽", "🏆", "🎁",
            "🚗", "✈️", "🏠", "💡",
            "📱", "💻", "📧", "📎",
            "🔗", "🔒", "🔑", "✅",
            "❌", "⚠️", "❗", "❓"
        ];

        function openEmojiPicker() {

            const oldPicker =
                document.getElementById(
                    "mailEmojiPicker"
                );

            if (oldPicker) {
                oldPicker.remove();
                return;
            }

            saveSelection();

            const picker =
                document.createElement(
                    "div"
                );

            picker.id =
                "mailEmojiPicker";

            picker.style.position =
                "fixed";

            picker.style.zIndex =
                "999999";

            picker.style.width =
                "340px";

            picker.style.maxHeight =
                "300px";

            picker.style.overflowY =
                "auto";

            picker.style.padding =
                "12px";

            picker.style.background =
                "#ffffff";

            picker.style.border =
                "1px solid #d1d5db";

            picker.style.borderRadius =
                "10px";

            picker.style.boxShadow =
                "0 10px 30px rgba(0,0,0,.18)";

            picker.style.display =
                "grid";

            picker.style.gridTemplateColumns =
                "repeat(8,1fr)";

            picker.style.gap =
                "5px";

            const emojiButton =
                toolbar.querySelector(
                    '[data-tool="emoji"]'
                );

            if (emojiButton) {

                const rect =
                    emojiButton.getBoundingClientRect();

                let left =
                    rect.left;

                let top =
                    rect.bottom + 8;

                if (
                    left + 340 >
                    window.innerWidth
                ) {
                    left =
                        window.innerWidth - 350;
                }

                if (
                    top + 300 >
                    window.innerHeight
                ) {
                    top =
                        rect.top - 308;
                }

                picker.style.left =
                    Math.max(5, left) + "px";

                picker.style.top =
                    Math.max(5, top) + "px";
            }

            fallbackEmojis.forEach(
                function (emoji) {

                    const button =
                        document.createElement(
                            "button"
                        );

                    button.type =
                        "button";

                    button.textContent =
                        emoji;

                    button.style.width =
                        "34px";

                    button.style.height =
                        "34px";

                    button.style.padding =
                        "0";

                    button.style.border =
                        "0";

                    button.style.borderRadius =
                        "6px";

                    button.style.background =
                        "#ffffff";

                    button.style.fontSize =
                        "21px";

                    button.style.cursor =
                        "pointer";

                    button.addEventListener(
                        "mousedown",
                        function (event) {
                            event.preventDefault();
                        }
                    );

                    button.addEventListener(
                        "click",
                        function () {

                            restoreSelection();

                            insertTextAtCursor(
                                emoji
                            );

                            picker.remove();
                        }
                    );

                    picker.appendChild(
                        button
                    );
                }
            );

            document.body.appendChild(
                picker
            );
        }

        /*
         * --------------------------------------------------
         * INSERT TEXT
         * --------------------------------------------------
         */

        function insertTextAtCursor(text) {

            restoreSelection();

            const selection =
                window.getSelection();

            if (
                selection &&
                selection.rangeCount > 0
            ) {

                const range =
                    selection.getRangeAt(0);

                if (
                    editor.contains(
                        range.commonAncestorContainer
                    )
                ) {

                    range.deleteContents();

                    const textNode =
                        document.createTextNode(
                            text
                        );

                    range.insertNode(
                        textNode
                    );

                    range.setStartAfter(
                        textNode
                    );

                    range.collapse(true);

                    selection.removeAllRanges();

                    selection.addRange(
                        range
                    );

                    syncBody();

                    return;
                }
            }

            editor.appendChild(
                document.createTextNode(text)
            );

            syncBody();
        }

        /*
         * --------------------------------------------------
         * EDITOR EVENTS
         * --------------------------------------------------
         */

        editor.addEventListener(
            "input",
            function () {
                syncBody();
            }
        );

        editor.addEventListener(
            "mouseup",
            function () {
                saveSelection();
            }
        );

        editor.addEventListener(
            "keyup",
            function () {
                saveSelection();
                syncBody();
            }
        );

        editor.addEventListener(
            "focus",
            function () {
                saveSelection();
            }
        );

        /*
         * --------------------------------------------------
         * OUTSIDE CLICK
         * --------------------------------------------------
         */

        document.addEventListener(
            "click",
            function (event) {

                const toolbarButton =
                    event.target.closest(
                        ".mail-toolbar-button"
                    );

                const popover =
                    event.target.closest(
                        ".mail-popover"
                    );

                const emojiPicker =
                    event.target.closest(
                        "#mailEmojiPicker"
                    );

                if (
                    toolbarButton ||
                    popover ||
                    emojiPicker
                ) {
                    return;
                }

                closePopovers();

                const picker =
                    document.getElementById(
                        "mailEmojiPicker"
                    );

                if (picker) {
                    picker.remove();
                }
            }
        );

        /*
         * --------------------------------------------------
         * FORM SUBMIT
         * --------------------------------------------------
         */

        form.addEventListener(
            "submit",
            function (event) {

                syncBody();

                if (
                    window.mailAttachmentManager &&
                    typeof
                    window.mailAttachmentManager.sync ===
                    "function"
                ) {

                    window.mailAttachmentManager.sync();
                }

                const plainText =
                    editor.innerText
                        .replace(
                            /\u00a0/g,
                            " "
                        )
                        .trim();

                if (!plainText) {

                    event.preventDefault();

                    alert(
                        "Please enter a message."
                    );

                    editor.focus();

                    return;
                }
            }
        );

        syncBody();
    }
);