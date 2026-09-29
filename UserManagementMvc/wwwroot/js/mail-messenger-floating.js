"use strict";

document.addEventListener(
    "DOMContentLoaded",
    function () {

        const widget =
            document.getElementById(
                "mailMessengerWidget"
            );

        const launcher =
            document.getElementById(
                "mailMessengerLauncher"
            );

        const panel =
            document.getElementById(
                "mailMessengerPanel"
            );

        const frame =
            document.getElementById(
                "mailMessengerFrame"
            );

        const closeButton =
            document.getElementById(
                "mailMessengerClose"
            );

        const openFullButton =
            document.getElementById(
                "mailMessengerOpenFull"
            );

        const header =
            document.getElementById(
                "mailMessengerHeader"
            );

        const unread =
            document.getElementById(
                "mailMessengerUnread"
            );

        if (
            !widget ||
            !launcher ||
            !panel ||
            !frame
        ) {
            return;
        }

        let messengerLoaded = false;

        let isDragging = false;

        let dragOffsetX = 0;

        let dragOffsetY = 0;

        function loadMessenger() {

            if (messengerLoaded) {
                return;
            }

            frame.src =
                "/Messenger/Index";

            messengerLoaded = true;
        }

        function openMessenger() {

            loadMessenger();

            panel.classList.add(
                "is-open"
            );

            panel.setAttribute(
                "aria-hidden",
                "false"
            );

            launcher.setAttribute(
                "aria-expanded",
                "true"
            );
        }

        function closeMessenger() {

            panel.classList.remove(
                "is-open"
            );

            panel.setAttribute(
                "aria-hidden",
                "true"
            );

            launcher.setAttribute(
                "aria-expanded",
                "false"
            );
        }

        launcher.addEventListener(
            "click",
            function () {

                if (
                    panel.classList.contains(
                        "is-open"
                    )
                ) {
                    closeMessenger();
                }
                else {
                    openMessenger();
                }
            }
        );

        closeButton.addEventListener(
            "click",
            function () {

                closeMessenger();
            }
        );

        openFullButton.addEventListener(
            "click",
            function () {

                window.location.href =
                    "/Messenger/Index";
            }
        );

        function setUnreadCount(count) {

            const numericCount =
                Number(count) || 0;

            if (numericCount <= 0) {

                unread.textContent =
                    "0";

                unread.classList.remove(
                    "has-unread"
                );

                unread.setAttribute(
                    "aria-hidden",
                    "true"
                );

                return;
            }

            unread.textContent =
                numericCount > 99
                    ? "99+"
                    : String(numericCount);

            unread.classList.add(
                "has-unread"
            );

            unread.setAttribute(
                "aria-hidden",
                "false"
            );
        }

        setUnreadCount(0);

        header.addEventListener(
            "mousedown",
            function (event) {

                if (
                    event.target.closest(
                        "button"
                    )
                ) {
                    return;
                }

                if (
                    window.innerWidth <= 768
                ) {
                    return;
                }

                isDragging = true;

                panel.classList.add(
                    "is-dragging"
                );

                const rect =
                    panel.getBoundingClientRect();

                dragOffsetX =
                    event.clientX -
                    rect.left;

                dragOffsetY =
                    event.clientY -
                    rect.top;

                event.preventDefault();
            }
        );

        document.addEventListener(
            "mousemove",
            function (event) {

                if (!isDragging) {
                    return;
                }

                let left =
                    event.clientX -
                    dragOffsetX;

                let top =
                    event.clientY -
                    dragOffsetY;

                const width =
                    panel.offsetWidth;

                const height =
                    panel.offsetHeight;

                const maxLeft =
                    window.innerWidth -
                    width -
                    8;

                const maxTop =
                    window.innerHeight -
                    height -
                    8;

                left =
                    Math.max(
                        8,
                        Math.min(
                            left,
                            maxLeft
                        )
                    );

                top =
                    Math.max(
                        8,
                        Math.min(
                            top,
                            maxTop
                        )
                    );

                panel.style.left =
                    left + "px";

                panel.style.top =
                    top + "px";

                panel.style.right =
                    "auto";

                panel.style.bottom =
                    "auto";
            }
        );

        document.addEventListener(
            "mouseup",
            function () {

                if (!isDragging) {
                    return;
                }

                isDragging = false;

                panel.classList.remove(
                    "is-dragging"
                );
            }
        );

        window.addEventListener(
            "resize",
            function () {

                if (
                    window.innerWidth <= 768
                ) {

                    panel.style.left =
                        "";

                    panel.style.top =
                        "";

                    panel.style.right =
                        "";

                    panel.style.bottom =
                        "";
                }
            }
        );

        window.addEventListener(
            "message",
            function (event) {

                if (
                    event.origin !==
                    window.location.origin
                ) {
                    return;
                }

                if (
                    !event.data ||
                    typeof event.data !==
                    "object"
                ) {
                    return;
                }

                if (
                    event.data.type ===
                    "messenger-unread"
                ) {

                    setUnreadCount(
                        event.data.count
                    );
                }
            }
        );

    }
);