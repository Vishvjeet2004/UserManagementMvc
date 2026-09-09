"use strict";

document.addEventListener("DOMContentLoaded", function () {

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
            "emojiManagementList"
        );

    const emojiCount =
        document.getElementById("emojiCount");

    const token =
        document.querySelector(
            'input[name="__RequestVerificationToken"]'
        );

    if (
        !form ||
        !emojiInput ||
        !sortOrderInput ||
        !formMessage ||
        !managementList ||
        !emojiCount ||
        !token
    ) {

        console.warn(
            "Emoji management elements were not found."
        );

        return;
    }

    // Display a success or error message.
    function showMessage(
        message,
        success
    ) {

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

    // Update the total emoji count.
    function updateCount() {

        const items =
            managementList.querySelectorAll(
                ".emoji-management-item"
            );

        const count =
            items.length;

        emojiCount.textContent =
            count +
            (
                count === 1
                    ? " emoji"
                    : " emojis"
            );
    }

    // Submit a new emoji to the server.
    form.addEventListener(
        "submit",
        async function (event) {

            event.preventDefault();

            const emoji =
                emojiInput.value.trim();

            const sortOrder =
                Number(
                    sortOrderInput.value
                ) || 0;

            if (!emoji) {

                showMessage(
                    "Please paste an emoji.",
                    false
                );

                emojiInput.focus();

                return;
            }

            try {

                const body =
                    new URLSearchParams();

                body.append(
                    "emoji",
                    emoji
                );

                body.append(
                    "sortOrder",
                    String(sortOrder)
                );

                body.append(
                    "__RequestVerificationToken",
                    token.value
                );

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
                        }
                    );

                const result =
                    await response.json();

                if (
                    !response.ok ||
                    !result.success
                ) {

                    throw new Error(
                        result.message ||
                        "Unable to add emoji."
                    );
                }

                addEmojiToList(
                    result
                );

                emojiInput.value =
                    "";

                sortOrderInput.value =
                    "0";

                showMessage(
                    "Emoji added successfully.",
                    true
                );

                emojiInput.focus();

                updateCount();

            }
            catch (error) {

                console.error(
                    "Add emoji error:",
                    error
                );

                showMessage(
                    error.message ||
                    "Unable to add emoji.",
                    false
                );
            }
        }
    );

    // Add a newly created emoji to the management list.
    function addEmojiToList(item) {

        const empty =
            managementList.querySelector(
                ".emoji-management-empty"
            );

        if (empty) {
            empty.remove();
        }

        const row =
            document.createElement("div");

        row.className =
            "emoji-management-item";

        row.dataset.emojiId =
            item.id;

        const preview =
            document.createElement("div");

        preview.className =
            "emoji-management-preview";

        preview.textContent =
            item.emoji;

        const info =
            document.createElement("div");

        info.className =
            "emoji-management-info";

        const strong =
            document.createElement("strong");

        strong.textContent =
            item.emoji;

        const order =
            document.createElement("span");

        order.textContent =
            "Order: " +
            item.sortOrder;

        const status =
            document.createElement("span");

        status.className =
            "emoji-status active";

        status.textContent =
            "Active";

        info.appendChild(strong);
        info.appendChild(order);
        info.appendChild(status);

        const actions =
            document.createElement("div");

        actions.className =
            "emoji-management-actions";

        const toggleButton =
            document.createElement("button");

        toggleButton.type =
            "button";

        toggleButton.className =
            "emoji-toggle-button";

        toggleButton.dataset.id =
            item.id;

        toggleButton.textContent =
            "Disable";

        const deleteButton =
            document.createElement("button");

        deleteButton.type =
            "button";

        deleteButton.className =
            "emoji-delete-button";

        deleteButton.dataset.id =
            item.id;

        deleteButton.textContent =
            "Delete";

        actions.appendChild(
            toggleButton
        );

        actions.appendChild(
            deleteButton
        );

        row.appendChild(
            preview
        );

        row.appendChild(
            info
        );

        row.appendChild(
            actions
        );

        managementList.prepend(
            row
        );
    }

    // Handle toggle and delete actions from the emoji list.
    managementList.addEventListener(
        "click",
        async function (event) {

            const toggleButton =
                event.target.closest(
                    ".emoji-toggle-button"
                );

            const deleteButton =
                event.target.closest(
                    ".emoji-delete-button"
                );

            if (toggleButton) {

                await toggleEmoji(
                    toggleButton.dataset.id,
                    toggleButton
                );

                return;
            }

            if (deleteButton) {

                await deleteEmoji(
                    deleteButton.dataset.id
                );
            }
        }
    );

    // Toggle the active state of an emoji.
    async function toggleEmoji(
        id,
        button
    ) {

        try {

            button.disabled =
                true;

            const body =
                new URLSearchParams();

            body.append(
                "id",
                id
            );

            body.append(
                "__RequestVerificationToken",
                token.value
            );

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
                    }
                );

            const result =
                await response.json();

            if (
                !response.ok ||
                !result.success
            ) {

                throw new Error(
                    result.message ||
                    "Unable to update emoji."
                );
            }

            const row =
                document.querySelector(
                    `[data-emoji-id="${id}"]`
                );

            if (!row) {
                return;
            }

            const status =
                row.querySelector(
                    ".emoji-status"
                );

            if (!status) {
                return;
            }

            if (result.isActive) {

                status.textContent =
                    "Active";

                status.classList.remove(
                    "inactive"
                );

                status.classList.add(
                    "active"
                );

                button.textContent =
                    "Disable";
            }
            else {

                status.textContent =
                    "Inactive";

                status.classList.remove(
                    "active"
                );

                status.classList.add(
                    "inactive"
                );

                button.textContent =
                    "Enable";
            }

        }
        catch (error) {

            console.error(
                "Toggle emoji error:",
                error
            );

            alert(
                error.message ||
                "Unable to update emoji."
            );
        }
        finally {

            button.disabled =
                false;
        }
    }

    // Delete an emoji after user confirmation.
    async function deleteEmoji(id) {

        const confirmed =
            window.confirm(
                "Are you sure you want to delete this emoji?"
            );

        if (!confirmed) {
            return;
        }

        try {

            const body =
                new URLSearchParams();

            body.append(
                "id",
                id
            );

            body.append(
                "__RequestVerificationToken",
                token.value
            );

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
                    }
                );

            const result =
                await response.json();

            if (
                !response.ok ||
                !result.success
            ) {

                throw new Error(
                    result.message ||
                    "Unable to delete emoji."
                );
            }

            const row =
                document.querySelector(
                    `[data-emoji-id="${id}"]`
                );

            if (row) {
                row.remove();
            }

            const remaining =
                managementList.querySelectorAll(
                    ".emoji-management-item"
                );

            if (remaining.length === 0) {

                managementList.innerHTML =
                    '<div class="emoji-management-empty">No emojis have been added yet.</div>';
            }

            updateCount();

        }
        catch (error) {

            console.error(
                "Delete emoji error:",
                error
            );

            alert(
                error.message ||
                "Unable to delete emoji."
            );
        }
    }

    updateCount();
});