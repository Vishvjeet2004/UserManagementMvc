"use strict";

const currentUserId =
    Number(
        window.messengerConfig.currentUserId);


const searchInput =
    document.getElementById(
        "userSearchInput");

const userList =
    document.getElementById(
        "userList");

const notificationButton =
    document.getElementById(
        "enableNotificationsButton");


let searchTimer = null;


 
// SIGNALR
 

const connection =
    new signalR.HubConnectionBuilder()
        .withUrl("/messengerHub")
        .withAutomaticReconnect()
        .build();


 
// LOAD USERS
 

async function loadUsers(
    search = "") {

    try {

        userList.classList.add(
            "loading-list");


        const response =
            await fetch(
                `/Messenger/SearchUsers?search=${encodeURIComponent(search)}`,
                {
                    method: "GET",
                    headers: {
                        "Accept":
                            "application/json"
                    }
                });


        if (!response.ok) {
            throw new Error(
                "Search failed.");
        }


        const users =
            await response.json();


        renderUsers(users);

    }
    catch (error) {

        console.error(
            "User search error:",
            error);

    }
    finally {

        userList.classList.remove(
            "loading-list");
    }
}


 
// RENDER USERS
 

function renderUsers(users) {

    userList.innerHTML = "";


    if (
        !users ||
        users.length === 0
    ) {

        const empty =
            document.createElement(
                "div");

        empty.className =
            "empty-users";

        empty.innerHTML = `
            <div class="empty-icon">👤</div>
            <h5>No users found</h5>
            <p>
                Try searching with another
                name or username.
            </p>
        `;

        userList.appendChild(
            empty);

        return;
    }


    users.forEach(function (user) {

        const card =
            document.createElement("a");

        card.className =
            "user-card";

        card.href =
            `/Messenger/Chat/${user.id}`;


        // =======================
        // AVATAR
        // =======================

        const avatar =
            document.createElement(
                "div");

        avatar.className =
            "user-avatar";

        avatar.textContent =
            user.name
                ? user.name
                    .substring(0, 1)
                    .toUpperCase()
                : "?";


        // =======================
        // INFO
        // =======================

        const info =
            document.createElement(
                "div");

        info.className =
            "user-info";


        const name =
            document.createElement(
                "div");

        name.className =
            "user-name";

        name.textContent =
            user.name || "";


        if (
            Number(user.unreadCount) > 0
        ) {

            const badge =
                document.createElement(
                    "span");

            badge.className =
                "unread-badge";

            badge.textContent =
                user.unreadCount;

            name.appendChild(
                badge);
        }


        const username =
            document.createElement(
                "div");

        username.className =
            "user-username";

        username.textContent =
            user.userName
                ? "@" + user.userName
                : "";


        info.appendChild(name);

        info.appendChild(username);


        if (user.lastMessage) {

            const lastMessage =
                document.createElement(
                    "div");

            lastMessage.className =
                "last-message";

            lastMessage.textContent =
                user.lastMessage;

            info.appendChild(
                lastMessage);
        }
        else if (user.department) {

            const department =
                document.createElement(
                    "div");

            department.className =
                "user-department";

            department.textContent =
                user.department;

            info.appendChild(
                department);
        }


        // =======================
        // RIGHT SIDE
        // =======================

        const right =
            document.createElement(
                "div");

        right.className =
            "user-card-right";


        if (user.lastMessageAt) {

            const time =
                document.createElement(
                    "div");

            time.className =
                "last-message-time";

            time.textContent =
                user.lastMessageAt;

            right.appendChild(
                time);
        }


        const arrow =
            document.createElement(
                "div");

        arrow.className =
            "chat-arrow";

        arrow.textContent =
            "→";

        right.appendChild(
            arrow);


        card.appendChild(
            avatar);

        card.appendChild(
            info);

        card.appendChild(
            right);


        userList.appendChild(
            card);
    });
}


 
// LIVE SEARCH
 

searchInput.addEventListener(
    "input",
    function () {

        const value =
            searchInput.value.trim();


        clearTimeout(
            searchTimer);


        searchTimer =
            setTimeout(
                function () {

                    loadUsers(value);

                },
                180);
    });


 
// NOTIFICATION PERMISSION
 

notificationButton.addEventListener(
    "click",
    async function () {

        if (
            typeof Notification ===
            "undefined"
        ) {

            alert(
                "Your browser does not support notifications.");

            return;
        }


        const permission =
            await Notification
                .requestPermission();


        if (
            permission === "granted"
        ) {

            notificationButton.textContent =
                "🔔 Notifications Enabled";

            notificationButton.classList.add(
                "notification-enabled");
        }
        else {

            notificationButton.textContent =
                "🔕 Notifications Blocked";
        }
    });


 
// SHOW BROWSER NOTIFICATION
 

function showNotification(
    message) {

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


    new Notification(
        "💬 New Message",
        {
            body:
                message.messageText ||
                "📎 Attachment"
        });
}


 
// RECEIVE MESSAGE
 

connection.on(
    "ReceiveMessage",
    function (message) {

        if (
            Number(message.senderUserId) !==
            currentUserId
        ) {

            showNotification(
                message);
        }


        loadUsers(
            searchInput.value.trim());
    });


 
// CONVERSATION UPDATED
 

connection.on(
    "ConversationUpdated",
    function () {

        loadUsers(
            searchInput.value.trim());
    });


 
// CONNECT
 

async function startConnection() {

    try {

        await connection.start();

        console.log(
            "Messenger list connected.");

    }
    catch (error) {

        console.error(
            "Messenger list connection error:",
            error);

        setTimeout(
            startConnection,
            3000);
    }
}


 
// RECONNECT
 

connection.onreconnected(
    function () {

        loadUsers(
            searchInput.value.trim());
    });


 
// INITIAL LOAD
 

startConnection();