window.devSyncNotifications = {
    playMention: () => {
        const AudioContext = window.AudioContext || window.webkitAudioContext;
        if (!AudioContext) {
            return;
        }

        const context = new AudioContext();
        const gain = context.createGain();
        gain.gain.setValueAtTime(0.0001, context.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.08, context.currentTime + 0.02);
        gain.gain.exponentialRampToValueAtTime(0.0001, context.currentTime + 0.32);
        gain.connect(context.destination);

        [740, 980].forEach((frequency, index) => {
            const oscillator = context.createOscillator();
            oscillator.type = "sine";
            oscillator.frequency.value = frequency;
            oscillator.connect(gain);
            oscillator.start(context.currentTime + index * 0.1);
            oscillator.stop(context.currentTime + index * 0.1 + 0.16);
        });

        window.setTimeout(() => context.close(), 500);
    },

    highlightBell: () => {
        const bell = document.querySelector(".notification-button");
        if (!bell) {
            return;
        }

        bell.classList.remove("ringing");
        window.requestAnimationFrame(() => bell.classList.add("ringing"));
        window.setTimeout(() => bell.classList.remove("ringing"), 800);
    },

    scrollToMessage: (id) => {
        const element = document.getElementById(id);
        if (!element) {
            return;
        }

        element.scrollIntoView({ behavior: "smooth", block: "center" });
        element.classList.add("message-focus");
        window.setTimeout(() => element.classList.remove("message-focus"), 1500);
    },

    scrollToLatestMessage: () => {
        const messagesContainer = document.querySelector("#chat-messages");
        if (!messagesContainer) {
            return;
        }

        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    },

    bindChatViewport: (roomId) => {
        const messagesContainer = document.querySelector("#chat-messages");
        if (!messagesContainer) {
            return;
        }

        const nextRoomId = String(roomId);
        if (messagesContainer.dataset.boundRoomId === nextRoomId) {
            return;
        }

        if (messagesContainer._devsyncScrollHandler) {
            messagesContainer.removeEventListener("scroll", messagesContainer._devsyncScrollHandler);
        }

        const handler = () => {
            window.devSyncNotifications.saveLastSeenMessage(roomId);
        };

        messagesContainer.dataset.boundRoomId = nextRoomId;
        messagesContainer._devsyncScrollHandler = handler;
        messagesContainer.addEventListener("scroll", handler, { passive: true });
    },

    restoreChatViewport: (roomId) => {
        const messagesContainer = document.querySelector("#chat-messages");
        if (!messagesContainer) {
            return;
        }

        const savedId = window.localStorage.getItem(`devsync:lastSeenMessage:${roomId}`);
        if (savedId) {
            const element = document.getElementById(savedId);
            if (element) {
                element.scrollIntoView({ behavior: "auto", block: "center" });
                return;
            }
        }

        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    },

    saveLastSeenMessage: (roomId) => {
        const messagesContainer = document.querySelector("#chat-messages");
        if (!messagesContainer) {
            return;
        }

        const containerRect = messagesContainer.getBoundingClientRect();
        const items = Array.from(messagesContainer.querySelectorAll("[id^='message-']"));
        if (items.length === 0) {
            return;
        }

        let lastSeenId = items[items.length - 1].id;
        for (let i = items.length - 1; i >= 0; i--) {
            const rect = items[i].getBoundingClientRect();
            if (rect.top < containerRect.bottom - 8) {
                lastSeenId = items[i].id;
                break;
            }
        }

        window.localStorage.setItem(`devsync:lastSeenMessage:${roomId}`, lastSeenId);
    }
};
