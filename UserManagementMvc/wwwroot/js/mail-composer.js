"use strict";

document.addEventListener("DOMContentLoaded", function () {

    const form =
        document.getElementById("mailComposeForm");

    const editor =
        document.getElementById("mailEditor");

    const bodyField =
        document.getElementById("mailBodyField");

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

    if (
        bodyField.value &&
        bodyField.value.trim() !== ""
    ) {
        editor.innerHTML =
            bodyField.value;
    }

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

    function syncBody() {

        bodyField.value =
            editor.innerHTML.trim();
    }

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

    toolbar
        .querySelectorAll(
            ".mail-toolbar-button"
        )
        .forEach(function (button) {

            const tool =
                button.dataset.tool;

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

    async function handleTool(tool) {

        if (!toolEnabled(tool)) {
            return;
        }

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

                    positionPopover(
                        fontPopover,
                        '[data-tool="font"]'
                    );

                    fontPopover.hidden =
                        false;
                }

                break;

            case "fontSize":

                if (fontSizePopover) {

                    restoreSelection();

                    positionPopover(
                        fontSizePopover,
                        '[data-tool="fontSize"]'
                    );

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

                    positionPopover(
                        colorPopover,
                        '[data-tool="textColor"]'
                    );

                    colorPopover.hidden =
                        false;
                }

                break;

            case "align":

                if (alignPopover) {

                    restoreSelection();

                    positionPopover(
                        alignPopover,
                        '[data-tool="align"]'
                    );

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

            case "confidential":

                break;

            default:

                break;
        }
    }

    function positionPopover(
        popover,
        selector
    ) {

        const button =
            toolbar.querySelector(
                selector
            );

        if (!button) {
            return;
        }

        const rect =
            button.getBoundingClientRect();

        const width =
            popover.offsetWidth || 180;

        let left =
            rect.left;

        let top =
            rect.top - 8;

        if (
            left + width >
            window.innerWidth - 8
        ) {
            left =
                window.innerWidth -
                width -
                8;
        }

        if (left < 8) {
            left = 8;
        }

        popover.style.left =
            left + "px";

        popover.style.bottom =
            (window.innerHeight - rect.top + 8) + "px";

        popover.style.top =
            "auto";
    }

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

    function openEmojiPicker() {

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

        picker.style.boxSizing =
            "border-box";

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
                rect.top - 308;

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

        editor.appendChild(
            document.createTextNode(
                text
            )
        );

        syncBody();
    }

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

    window.addEventListener(
        "resize",
        function () {

            const picker =
                document.getElementById(
                    "mailEmojiPicker"
                );

            if (picker) {

                picker.remove();

            }
        }
    );

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
        }
    );
});