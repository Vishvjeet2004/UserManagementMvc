"use strict";

document.addEventListener("DOMContentLoaded", function () {

    const form =
        document.getElementById("mailComposeForm");

    const editor =
        document.getElementById("mailEditor");

    const bodyField =
        document.getElementById("mailBodyField");

    /*
     * IMPORTANT:
     * Current Compose.cshtml uses:
     * mailComposeToolbar
     * mail-toolbar-button
     */
    const toolbar =
        document.getElementById("mailComposeToolbar");

    const attachmentInput =
        document.getElementById("mailAttachmentInput");

    const ccField =
        document.getElementById("ccField");

    const bccField =
        document.getElementById("bccField");

    const showCcButton =
        document.getElementById("showCcButton");

    const showBccButton =
        document.getElementById("showBccButton");

    const fontPopover =
        document.getElementById("fontPopover");

    const fontSizePopover =
        document.getElementById("fontSizePopover");

    const colorPopover =
        document.getElementById("colorPopover");

    const alignPopover =
        document.getElementById("alignPopover");

    const config =
        window.mailComposeConfig || {
            tools: [],
            signatures: []
        };

    if (!form || !editor || !bodyField || !toolbar) {

        console.error(
            "Mail composer: required elements are missing."
        );

        return;
    }

    let savedRange = null;

    /*
     * --------------------------------------------------
     * BODY INITIALIZATION
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

    if (showCcButton && ccField) {

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

    if (showBccButton && bccField) {

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
     * EXECUTE FORMAT COMMAND
     * --------------------------------------------------
     */

    function execute(
        command,
        value = null
    ) {

        editor.focus();

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
     * TOOL ENABLED
     * --------------------------------------------------
     */

    function toolEnabled(toolKey) {

        if (
            !Array.isArray(
                config.tools
            )
        ) {

            return true;
        }

        return config.tools.includes(
            toolKey
        );
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
     * TOOLBAR BUTTONS
     * --------------------------------------------------
     */

    toolbar
        .querySelectorAll(
            ".mail-toolbar-button"
        )
        .forEach(function (button) {

            const tool =
                button.dataset.tool;

            /*
             * Prevent editor selection from disappearing
             * when toolbar is clicked.
             */
            button.addEventListener(
                "mousedown",
                function (event) {

                    event.preventDefault();

                    saveSelection();
                }
            );

            button.addEventListener(
                "click",
                async function () {

                    await handleTool(tool);

                }
            );
        });

    /*
     * --------------------------------------------------
     * HANDLE TOOL
     * --------------------------------------------------
     */

    async function handleTool(tool) {

        if (!toolEnabled(tool)) {
            return;
        }

        /*
         * Attachment and emoji need special handling.
         */
        if (tool !== "emoji") {
            closePopovers();
        }

        switch (tool) {

            case "undo":

                execute("undo");

                break;


            case "redo":

                execute("redo");

                break;


            case "font":

                if (fontPopover) {

                    restoreSelection();

                    fontPopover.hidden =
                        false;
                }

                break;


            case "fontSize":

                if (fontSizePopover) {

                    restoreSelection();

                    fontSizePopover.hidden =
                        false;
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

                if (colorPopover) {

                    restoreSelection();

                    colorPopover.hidden =
                        false;
                }

                break;


            case "align":

                if (alignPopover) {

                    restoreSelection();

                    alignPopover.hidden =
                        false;
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

                openAttachmentPicker();

                break;


            case "image":

                openAttachmentPicker();

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


            default:

                break;
        }
    }

    /*
     * --------------------------------------------------
     * ATTACHMENT PICKER
     * --------------------------------------------------
     */

    function openAttachmentPicker() {

        if (
            window.mailAttachmentManager &&
            typeof window.mailAttachmentManager.open ===
            "function"
        ) {

            window.mailAttachmentManager.open();

            return;
        }

        if (attachmentInput) {

            attachmentInput.click();

            return;
        }

        console.error(
            "Attachment input was not found."
        );
    }

    /*
     * --------------------------------------------------
     * FONT POPOVER
     * --------------------------------------------------
     */

    if (fontPopover) {

        fontPopover
            .querySelectorAll(
                "[data-font]"
            )
            .forEach(function (button) {

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

                        execute(
                            "fontName",
                            button.dataset.font
                        );

                        closePopovers();
                    }
                );
            });
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
            .forEach(function (button) {

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

                        execute(
                            "fontSize",
                            button.dataset.size
                        );

                        closePopovers();
                    }
                );
            });
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
            .forEach(function (button) {

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

                        execute(
                            "foreColor",
                            button.dataset.color
                        );

                        closePopovers();
                    }
                );
            });
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
            .forEach(function (button) {

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

                        execute(
                            button.dataset.align
                        );

                        closePopovers();
                    }
                );
            });
    }

    /*
     * --------------------------------------------------
     * LINK
     * --------------------------------------------------
     */

    function insertLink() {

        restoreSelection();

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
            document.createElement("div");

        container.innerHTML =
            signature.html || "";

        const range =
            savedRange ||
            document.createRange();

        if (!savedRange) {

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

        const selection =
            window.getSelection();

        selection.removeAllRanges();

        selection.addRange(
            range
        );

        syncBody();
    }

    /*
     * --------------------------------------------------
     * EMOJI PICKER
     * --------------------------------------------------
     *
     * IMPORTANT:
     * This version does NOT depend completely on
     * /EmojiManagement/GetActiveEmojis.
     *
     * If the server endpoint is unavailable,
     * the normal emoji list still appears.
     */

    const fallbackEmojis = [

        "😀", "😃", "😄", "😁", "😆", "😅",
        "😂", "🤣", "😊", "😇", "🙂", "🙃",
        "😉", "😌", "😍", "🥰", "😘", "😗",
        "😙", "😚", "😋", "😛", "😝", "😜",
        "🤪", "🤨", "🧐", "🤓", "😎", "🤩",
        "🥳", "😏", "😒", "😞", "😔", "😟",
        "😕", "🙁", "☹️", "😣", "😖", "😫",
        "😩", "🥺", "😢", "😭", "😤", "😠",
        "😡", "🤬", "🤯", "😳", "🥵", "🥶",
        "😱", "😨", "😰", "😥", "😓", "🤗",
        "🤔", "🫡", "🤭", "🤫", "🤥", "😶",
        "😐", "😑", "😬", "🙄", "😯", "😦",
        "😧", "😮", "😲", "🥱", "😴", "🤤",

        "❤️", "🧡", "💛", "💚", "💙", "💜",
        "🖤", "🤍", "🤎", "💔", "❣️", "💕",
        "💞", "💓", "💗", "💖", "💘", "💝",
        "💟", "💯", "💥", "💫", "✨", "⭐",
        "🌟", "🔥", "🎉", "🎊", "👍", "👎",
        "👏", "🙌", "🙏", "💪", "🤝", "👌",
        "✌️", "🤞", "🤟", "🤘", "👋", "💐",
        "🌹", "🌸", "🌺", "🌻", "🌷",

        "☀️", "🌈", "☁️", "❄️", "☔",
        "🌙", "🌍", "🍎", "🍕", "🍔", "🍟",
        "🍰", "🎂", "🍩", "☕", "🍵", "🥤",
        "⚽", "🏏", "🏆", "🎯", "🎁", "🎈",
        "🚗", "✈️", "🏠", "💡", "📱", "💻",
        "📧", "📎", "🔗", "🔒", "🔑", "✅",
        "❌", "⚠️", "❗", "❓", "💬", "📌"
    ];

    function createEmojiPicker() {

        const existing =
            document.getElementById(
                "mailEmojiPicker"
            );

        if (existing) {

            existing.remove();

            return;
        }

        saveSelection();

        const picker =
            document.createElement("div");

        picker.id =
            "mailEmojiPicker";

        /*
         * Inline styling ensures that emoji picker
         * works even if no special CSS exists.
         */
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
            "0 10px 30px rgba(0,0,0,0.18)";

        picker.style.display =
            "grid";

        picker.style.gridTemplateColumns =
            "repeat(8, 1fr)";

        picker.style.gap =
            "5px";

        const toolbarButton =
            toolbar.querySelector(
                '[data-tool="emoji"]'
            );

        if (toolbarButton) {

            const rect =
                toolbarButton.getBoundingClientRect();

            let top =
                rect.bottom + 8;

            let left =
                rect.left;

            if (
                left + 340 >
                window.innerWidth
            ) {

                left =
                    window.innerWidth - 350;
            }

            if (left < 5) {
                left = 5;
            }

            if (
                top + 300 >
                window.innerHeight
            ) {

                top =
                    rect.top - 308;
            }

            if (top < 5) {
                top = 5;
            }

            picker.style.top =
                top + "px";

            picker.style.left =
                left + "px";
        }
        else {

            picker.style.top =
                "80px";

            picker.style.left =
                "20px";
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

                button.title =
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
                    "mouseenter",
                    function () {

                        button.style.background =
                            "#f3f4f6";
                    }
                );

                button.addEventListener(
                    "mouseleave",
                    function () {

                        button.style.background =
                            "#ffffff";
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

    function openEmojiPicker() {

        /*
         * First show the local picker immediately.
         * This guarantees the button works.
         */
        createEmojiPicker();
    }

    /*
     * --------------------------------------------------
     * INSERT TEXT AT CURSOR
     * --------------------------------------------------
     */

    function insertTextAtCursor(text) {

        restoreSelection();

        editor.focus();

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

        /*
         * Fallback:
         * append emoji at the end.
         */
        editor.appendChild(
            document.createTextNode(
                text
            )
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
        "keyup",
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
     * CLOSE POPOVERS / EMOJI
     * --------------------------------------------------
     */

    document.addEventListener(
        "click",
        function (event) {

            const clickedToolbar =
                event.target.closest(
                    ".mail-toolbar-button"
                );

            const clickedPopover =
                event.target.closest(
                    ".mail-popover"
                );

            const clickedEmoji =
                event.target.closest(
                    "#mailEmojiPicker"
                );

            if (
                clickedToolbar ||
                clickedPopover ||
                clickedEmoji
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

            /*
             * Make absolutely sure all attachments
             * are synchronized before form submission.
             */
            if (
                window.mailAttachmentManager &&
                typeof window.mailAttachmentManager.getFiles ===
                "function"
            ) {

                /*
                 * mail-attachments.js already keeps
                 * the real input synchronized.
                 */
            }
        }
    );

});