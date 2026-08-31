"use strict";

const connection =
    new signalR.HubConnectionBuilder()
        .withUrl("/messengerHub")
        .withAutomaticReconnect()
        .build();

const messageInput =
    document.getElementById("messageInput");

const sendButton =
    document.getElementById("sendMessageButton");

const messagesContainer =
    document.getElementById("messagesContainer");

const emptyChat =
    document.getElementById("emptyChat");

const emojiButton =
    document.getElementById("emojiButton");

const emojiPicker =
    document.getElementById("emojiPicker");

const emojiPickerGrid =
    document.getElementById("emojiPickerGrid");

const emojiLoading =
    document.getElementById("emojiLoading");

const emojiButtonIcon =
    document.getElementById("emojiButtonIcon");

const attachmentButton =
    document.getElementById("attachmentButton");

const attachmentInput =
    document.getElementById("attachmentInput");

const attachmentUploadForm =
    document.getElementById("attachmentUploadForm");

const currentUserId =
    Number(window.chatConfig.currentUserId);

const otherUserId =
    Number(window.chatConfig.otherUserId);

let emojisLoaded = false;

// LOAD EMOJIS
async function loadEmojis() {
    if (!emojiPickerGrid) {
        return;
    }

    try {
        const response =
            await fetch(
                "/EmojiManagement/GetActiveEmojis",
                {
                    method: "GET",
                    headers: {
                        "Accept": "application/json"
                    }
                });

        if (!response.ok) {
            throw new Error(
                "Emoji list could not be loaded.");
        }

        const emojis =
            await response.json();

        emojiPickerGrid.innerHTML = "";

        if (!emojis.length) {
            emojiPickerGrid.innerHTML =
                '<div class="emoji-empty">No emojis available.</div>';

            return;
        }

        emojis.forEach(function (item, index) {

            const button =
                document.createElement("button");

            button.type = "button";

            button.className =
                "emoji-item";

            button.textContent =
                item.emoji;

            button.dataset.emoji =
                item.emoji;

            button.setAttribute(
                "aria-label",
                "Insert emoji");

            button.addEventListener(
                "click",
                function () {

                    insertEmoji(
                        item.emoji);
                });

            emojiPickerGrid.appendChild(
                button);

            if (index === 0 &&
                emojiButtonIcon) {

                emojiButtonIcon.textContent =
                    item.emoji;
            }
        });

        emojisLoaded = true;

    }
    catch (error) {

        console.error(
            "Emoji loading error:",
            error);

        emojiPickerGrid.innerHTML =
            '<div class="emoji-empty">Unable to load emojis.</div>';
    }
}

// INSERT EMOJI
function insertEmoji(emoji) {

    if (!messageInput) {
        return;
    }

    const start =
        messageInput.selectionStart ??
        messageInput.value.length;

    const end =
        messageInput.selectionEnd ??
        messageInput.value.length;

    const before =
        messageInput.value.substring(
            0,
            start);

    const after =
        messageInput.value.substring(
            end);

    messageInput.value =
        before +
        emoji +
        after;

    const cursorPosition =
        start +
        emoji.length;

    messageInput.focus();

    messageInput.setSelectionRange(
        cursorPosition,
        cursorPosition);
}

// START SIGNALR
async function startConnection() {

    try {

        await connection.start();

        console.log(
            "Messenger connected.");

        await connection.invoke(
            "MarkDelivered",
            otherUserId);

        await connection.invoke(
            "MarkSeen",
            otherUserId);

    }
    catch (error) {

        console.error(
            "SignalR connection error:",
            error);

        setTimeout(
            startConnection,
            3000);
    }
}

// SEND MESSAGE
async function sendMessage(
    attachment = null) {

    const text =
        messageInput.value.trim();

    if (!text && !attachment) {
        return;
    }

    if (
        connection.state !==
        signalR.HubConnectionState.Connected
    ) {

        console.warn(
            "SignalR is not connected.");

        return;
    }

    try {

        await connection.invoke(
            "SendMessage",

            otherUserId,

            text,

            attachment
                ? attachment.url
                : null,

            attachment
                ? attachment.name
                : null,

            attachment
                ? attachment.contentType
                : null,

            attachment
                ? attachment.size
                : null
        );

        messageInput.value = "";

        messageInput.focus();

    }
    catch (error) {

        console.error(
            "Message send failed:",
            error);

        alert(
            "Message could not be sent.");
    }
}

// ENTER KEY
messageInput.addEventListener(
    "keydown",
    function (event) {

        if (
            event.key === "Enter" &&
            !event.shiftKey
        ) {

            event.preventDefault();

            sendMessage();
        }
    });

// SEND BUTTON
sendButton.addEventListener(
    "click",
    function () {

        sendMessage();
    });

// EMOJI BUTTON
emojiButton.addEventListener(
    "click",
    async function (event) {

        event.stopPropagation();

        if (!emojisLoaded) {
            await loadEmojis();
        }

        const isOpen =
            emojiPicker.classList.toggle(
                "show");

        emojiPicker.setAttribute(
            "aria-hidden",
            String(!isOpen));

        emojiButton.setAttribute(
            "aria-expanded",
            String(isOpen));
    });

// CLOSE EMOJI PICKER
document.addEventListener(
    "click",
    function (event) {

        if (
            !emojiPicker.contains(
                event.target) &&
            event.target !== emojiButton &&
            !emojiButton.contains(
                event.target)
        ) {

            emojiPicker.classList.remove(
                "show");

            emojiPicker.setAttribute(
                "aria-hidden",
                "true");

            emojiButton.setAttribute(
                "aria-expanded",
                "false");
        }
    });

// ATTACHMENT BUTTON
attachmentButton.addEventListener(
    "click",
    function () {

        attachmentInput.click();
    });

// UPLOAD ATTACHMENT
attachmentInput.addEventListener(
    "change",
    async function () {

        const file =
            attachmentInput.files[0];

        if (!file) {
            return;
        }

        attachmentButton.disabled =
            true;

        const originalText =
            attachmentButton.textContent;

        attachmentButton.textContent =
            "⏳";

        try {

            const formData =
                new FormData(
                    attachmentUploadForm);

            const response =
                await fetch(
                    attachmentUploadForm.action,
                    {
                        method: "POST",
                        body: formData
                    });

            const result =
                await response.json();

            if (
                !response.ok ||
                !result.success
            ) {

                throw new Error(
                    result.message ||
                    "Upload failed.");
            }

            await sendMessage({

                url:
                    result.url,

                name:
                    result.name,

                contentType:
                    result.contentType,

                size:
                    result.size
            });

        }
        catch (error) {

            console.error(
                "Attachment upload error:",
                error);

            alert(
                error.message ||
                "Attachment upload failed.");
        }
        finally {

            attachmentInput.value =
                "";

            attachmentButton.disabled =
                false;

            attachmentButton.textContent =
                originalText;
        }
    });

// RECEIVE MESSAGE
connection.on(
    "ReceiveMessage",
    function (message) {

        if (
            Number(message.senderUserId) ===
            otherUserId
        ) {

            addMessage(
                message,
                false);

            connection.invoke(
                "MarkDelivered",
                otherUserId);

            connection.invoke(
                "MarkSeen",
                otherUserId);

            return;
        }

        showBrowserNotification(
            "New message",
            message.messageText ||
            "Attachment");
    });

// MESSAGE SENT
connection.on(
    "MessageSent",
    function (message) {

        addMessage(
            message,
            true);
    });

// DELIVERED
connection.on(
    "MessagesDelivered",
    function (userId) {

        if (
            Number(userId) !==
            otherUserId
        ) {
            return;
        }

        document
            .querySelectorAll(
                ".message-mine .message-ticks")
            .forEach(
                function (element) {

                    if (
                        !element.classList.contains(
                            "ticks-seen")
                    ) {

                        element.innerHTML =
                            "✓✓";
                    }
                });
    });

// SEEN
connection.on(
    "MessagesSeen",
    function (userId) {

        if (
            Number(userId) !==
            otherUserId
        ) {
            return;
        }

        document
            .querySelectorAll(
                ".message-mine .message-ticks")
            .forEach(
                function (element) {

                    element.innerHTML =
                        "✓✓";

                    element.classList.add(
                        "ticks-seen");
                });
    });

// ADD MESSAGE
function addMessage(
    message,
    mine) {

    if (emptyChat) {
        emptyChat.remove();
    }

    if (
        document.querySelector(
            `[data-message-id="${message.id}"]`)
    ) {
        return;
    }

    const row =
        document.createElement("div");

    row.className =
        "message-row " +
        (
            mine
                ? "message-mine"
                : "message-theirs"
        );

    row.dataset.messageId =
        message.id;

    const bubble =
        document.createElement("div");

    bubble.className =
        "message-bubble";

    if (
        message.messageText &&
        message.messageText.trim()
    ) {

        const text =
            document.createElement("div");

        text.className =
            "message-text";

        text.textContent =
            message.messageText;

        bubble.appendChild(text);
    }

    if (message.attachmentUrl) {

        const contentType =
            message.attachmentContentType ||
            "";

        if (
            contentType.startsWith("image/")
        ) {

            const image =
                document.createElement("img");

            image.src =
                message.attachmentUrl;

            image.alt =
                message.attachmentName ||
                "Image";

            image.className =
                "message-image";

            image.loading =
                "lazy";

            bubble.appendChild(image);
        }
        else if (
            contentType.startsWith("video/")
        ) {

            const video =
                document.createElement("video");

            video.className =
                "message-video";

            video.controls =
                true;

            const source =
                document.createElement("source");

            source.src =
                message.attachmentUrl;

            source.type =
                contentType;

            video.appendChild(source);

            bubble.appendChild(video);
        }
        else {

            const file =
                document.createElement("a");

            file.href =
                message.attachmentUrl;

            file.target =
                "_blank";

            file.rel =
                "noopener noreferrer";

            file.className =
                "message-file";

            file.textContent =
                "Attachment: " +
                (
                    message.attachmentName ||
                    "File"
                );

            bubble.appendChild(file);
        }
    }

    const meta =
        document.createElement("div");

    meta.className =
        "message-meta";

    const time =
        document.createElement("span");

    time.textContent =
        message.sentAt;

    meta.appendChild(time);

    if (mine) {

        const ticks =
            document.createElement("span");

        ticks.className =
            "message-ticks";

        ticks.innerHTML =
            "✓";

        meta.appendChild(ticks);
    }

    bubble.appendChild(meta);

    row.appendChild(bubble);

    messagesContainer.appendChild(row);

    scrollToBottom();
}

// BROWSER NOTIFICATION
function showBrowserNotification(
    title,
    body) {

    if (
        typeof Notification ===
        "undefined"
    ) {
        return;
    }

    if (
        Notification.permission !==
        "granted"
    ) {
        return;
    }

    try {

        new Notification(
            title,
            {
                body: body
            });

    }
    catch (error) {

        console.error(
            "Notification error:",
            error);
    }
}

// SCROLL
function scrollToBottom() {

    if (!messagesContainer) {
        return;
    }

    messagesContainer.scrollTop =
        messagesContainer.scrollHeight;
}

// RECONNECT
connection.onreconnected(
    async function () {

        try {

            await connection.invoke(
                "MarkDelivered",
                otherUserId);

            await connection.invoke(
                "MarkSeen",
                otherUserId);

        }
        catch (error) {

            console.error(
                "Reconnect update failed:",
                error);
        }
    });

// INITIALIZE
loadEmojis();

startConnection();

scrollToBottom();