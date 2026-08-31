"use strict";

const form =
    document.getElementById("addEmojiForm");

const emojiInput =
    document.getElementById("emojiInput");

const sortOrderInput =
    document.getElementById("sortOrderInput");

const formMessage =
    document.getElementById("emojiFormMessage");

const managementList =
    document.getElementById(
        "emojiManagementList");

const emojiCount =
    document.getElementById("emojiCount");

const token =
    document.querySelector(
        'input[name="__RequestVerificationToken"]');

function showMessage(
    message,
    success) {

    formMessage.textContent =
        message;

    formMessage.className =
        "emoji-form-message " +
        (
            success
                ? "success"
                : "error"
        );
}

function updateCount() {

    const items =
        managementList.querySelectorAll(
            ".emoji-management-item");

    const count =
        items.length;

    emojiCount.textContent =
        count + (
            count === 1
                ? " emoji"
                : " emojis"
        );
}

form.addEventListener(
    "submit",
    async function (event) {

        event.preventDefault();

        const emoji =
            emojiInput.value.trim();

        const sortOrder =
            Number(
                sortOrderInput.value) || 0;

        if (!emoji) {

            showMessage(
                "Please paste an emoji.",
                false);

            emojiInput.focus();

            return;
        }

        try {

            const body =
                new URLSearchParams();

            body.append(
                "emoji",
                emoji);

            body.append(
                "sortOrder",
                String(sortOrder));

            body.append(
                "__RequestVerificationToken",
                token.value);

            const response =
                await fetch(
                    form.action,
                    {
                        method: "POST",
                        headers: {
                            "Content-Type":
                                "application/x-www-form-urlencoded"
                        },
                        body: body
                    });

            const result =
                await response.json();

            if (
                !response.ok ||
                !result.success
            ) {

                throw new Error(
                    result.message ||
                    "Unable to add emoji.");
            }

            addEmojiToList(
                result);

            emojiInput.value =
                "";

            sortOrderInput.value =
                "0";

            showMessage(
                "Emoji added successfully.",
                true);

            emojiInput.focus();

            updateCount();

        }
        catch (error) {

            console.error(
                "Add emoji error:",
                error);

            showMessage(
                error.message ||
                "Unable to add emoji.",
                false);
        }
    });

function addEmojiToList(
    item) {

    const empty =
        managementList.querySelector(
            ".emoji-management-empty");

    if (empty) {
        empty.remove();
    }

    const row =
        document.createElement("div");

    row.className =
        "emoji-management-item";

    row.dataset.emojiId =
        item.id;

    row.innerHTML = `
        <div class="emoji-management-preview">
            ${escapeHtml(item.emoji)}
        </div>

        <div class="emoji-management-info">
            <strong>
                ${escapeHtml(item.emoji)}
            </strong>

            <span>
                Order: ${item.sortOrder}
            </span>

            <span class="emoji-status active">
                Active
            </span>
        </div>

        <div class="emoji-management-actions">

            <button type="button"
                    class="emoji-toggle-button"
                    data-id="${item.id}">
                Disable
            </button>

            <button type="button"
                    class="emoji-delete-button"
                    data-id="${item.id}">
                Delete
            </button>

        </div>
    `;

    managementList.prepend(row);
}

managementList.addEventListener(
    "click",
    async function (event) {

        const toggleButton =
            event.target.closest(
                ".emoji-toggle-button");

        const deleteButton =
            event.target.closest(
                ".emoji-delete-button");

        if (toggleButton) {

            await toggleEmoji(
                toggleButton.dataset.id,
                toggleButton);

            return;
        }

        if (deleteButton) {

            await deleteEmoji(
                deleteButton.dataset.id);

            return;
        }
    });

async function toggleEmoji(
    id,
    button) {

    try {

        button.disabled =
            true;

        const body =
            new URLSearchParams();

        body.append(
            "id",
            id);

        body.append(
            "__RequestVerificationToken",
            token.value);

        const response =
            await fetch(
                "/EmojiManagement/ToggleEmoji",
                {
                    method: "POST",
                    headers: {
                        "Content-Type":
                            "application/x-www-form-urlencoded"
                    },
                    body: body
                });

        const result =
            await response.json();

        if (
            !response.ok ||
            !result.success
        ) {

            throw new Error(
                result.message ||
                "Unable to update emoji.");
        }

        const row =
            document.querySelector(
                `[data-emoji-id="${id}"]`);

        if (!row) {
            return;
        }

        const status =
            row.querySelector(
                ".emoji-status");

        if (result.isActive) {

            status.textContent =
                "Active";

            status.classList.remove(
                "inactive");

            status.classList.add(
                "active");

            button.textContent =
                "Disable";

        }
        else {

            status.textContent =
                "Inactive";

            status.classList.remove(
                "active");

            status.classList.add(
                "inactive");

            button.textContent =
                "Enable";
        }

    }
    catch (error) {

        console.error(
            "Toggle emoji error:",
            error);

        alert(
            error.message ||
            "Unable to update emoji.");
    }
    finally {

        button.disabled =
            false;
    }
}

async function deleteEmoji(
    id) {

    const confirmed =
        window.confirm(
            "Are you sure you want to delete this emoji?");

    if (!confirmed) {
        return;
    }

    try {

        const body =
            new URLSearchParams();

        body.append(
            "id",
            id);

        body.append(
            "__RequestVerificationToken",
            token.value);

        const response =
            await fetch(
                "/EmojiManagement/DeleteEmoji",
                {
                    method: "POST",
                    headers: {
                        "Content-Type":
                            "application/x-www-form-urlencoded"
                    },
                    body: body
                });

        const result =
            await response.json();

        if (
            !response.ok ||
            !result.success
        ) {

            throw new Error(
                result.message ||
                "Unable to delete emoji.");
        }

        const row =
            document.querySelector(
                `[data-emoji-id="${id}"]`);

        if (row) {
            row.remove();
        }

        if (
            managementList.querySelectorAll(
                ".emoji-management-item").length === 0
        ) {

            managementList.innerHTML =
                '<div class="emoji-management-empty">No emojis have been added yet.</div>';
        }

        updateCount();

    }
    catch (error) {

        console.error(
            "Delete emoji error:",
            error);

        alert(
            error.message ||
            "Unable to delete emoji.");
    }
}

function escapeHtml(value) {

    const div =
        document.createElement("div");

    div.textContent =
        value;

    return div.innerHTML;
}

updateCount();