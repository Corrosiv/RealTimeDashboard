/**
 * WebSocket module - handles real-time communication with the server
 * Implements reconnection logic, replay protocol, and event subscription
 */

const WebSocketClient = {
    ws: null,
    url: null,
    reconnectAttempts: 0,
    maxReconnectAttempts: 10,
    reconnectDelay: 1000,
    maxReconnectDelay: 30000,
    lastSeenEventId: null,
    isConnected: false,
    isReconnecting: false,
    listeners: {},
    messageQueue: [],

    /**
     * Initialize WebSocket connection
     * Automatically determines protocol (ws:// or wss://) based on current page protocol
     */
    init: function() {
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        const host = window.location.host;
        this.url = `${protocol}//${host}/ws`;

        this.connect();
    },

    /**
     * Establish WebSocket connection
     */
    connect: function() {
        try {
            this.ws = new WebSocket(this.url);

            this.ws.onopen = () => this._onOpen();
            this.ws.onmessage = (event) => this._onMessage(event);
            this.ws.onerror = (event) => this._onError(event);
            this.ws.onclose = (event) => this._onClose(event);
        } catch (error) {
            console.error('WebSocket connection error:', error);
            this._scheduleReconnect();
        }
    },

    /**
     * Send message to server
     * @param {Object} message - Message object with 'type' and 'payload'
     */
    send: function(message) {
        if (this.isConnected) {
            try {
                this.ws.send(JSON.stringify(message));
            } catch (error) {
                console.error('Error sending WebSocket message:', error);
                this.messageQueue.push(message);
            }
        } else {
            this.messageQueue.push(message);
        }
    },

    /**
     * Subscribe to server events
     * @param {Object} options - { username, filters }
     */
    subscribe: function(options = {}) {
        const message = {
            type: 'Subscribe',
            payload: {
                username: options.username || API.getCurrentUsername(),
                filters: options.filters || {}
            }
        };
        this.send(message);
    },

    /**
     * Register event listener
     * @param {string} eventType - Event type to listen for
     * @param {Function} callback - Callback function
     */
    on: function(eventType, callback) {
        if (!this.listeners[eventType]) {
            this.listeners[eventType] = [];
        }
        this.listeners[eventType].push(callback);
    },

    /**
     * Unregister event listener
     * @param {string} eventType - Event type
     * @param {Function} callback - Callback function
     */
    off: function(eventType, callback) {
        if (this.listeners[eventType]) {
            this.listeners[eventType] = this.listeners[eventType].filter(cb => cb !== callback);
        }
    },

    /**
     * Emit local event
     * @param {string} eventType - Event type
     * @param {*} data - Event data
     */
    emit: function(eventType, data) {
        if (this.listeners[eventType]) {
            this.listeners[eventType].forEach(callback => {
                try {
                    callback(data);
                } catch (error) {
                    console.error(`Error in listener for ${eventType}:`, error);
                }
            });
        }
    },

    /**
     * WebSocket open handler
     */
    _onOpen: function() {
        console.log('WebSocket connected');
        this.isConnected = true;
        this.isReconnecting = false;
        this.reconnectAttempts = 0;
        this.reconnectDelay = 1000;

        this.emit('connect', {});

        // Send queued messages
        while (this.messageQueue.length > 0) {
            const message = this.messageQueue.shift();
            this.send(message);
        }

        // Resume with last seen event ID if available
        if (this.lastSeenEventId) {
            this.send({
                type: 'Resume',
                payload: { lastSeenEventId: this.lastSeenEventId }
            });
        } else {
            // Initial subscription
            this.subscribe();
        }
    },

    /**
     * WebSocket message handler
     */
    _onMessage: function(event) {
        try {
            const message = JSON.parse(event.data);

            // Update last seen event ID for replay on reconnect
            if (message.payload && message.payload.id) {
                this.lastSeenEventId = message.payload.id;
            }

            // Handle different message types
            switch (message.type) {
                case 'ReplayStarted':
                    this.emit('replayStarted', message.payload);
                    break;
                case 'ActivityEvent':
                    this.emit('activityEvent', message.payload);
                    break;
                case 'TransactionCreated':
                    this.emit('transactionCreated', message.payload);
                    break;
                case 'CsvUploaded':
                    this.emit('csvUploaded', message.payload);
                    break;
                case 'MetricsUpdated':
                    this.emit('metricsUpdated', message.payload);
                    break;
                case 'ReplayCompleted':
                    this.emit('replayCompleted', message.payload);
                    break;
                case 'ReplayError':
                    this.emit('replayError', message.payload);
                    break;
                case 'Error':
                    this.emit('error', message.payload);
                    break;
                default:
                    console.log('Unknown message type:', message.type);
            }
        } catch (error) {
            console.error('Error parsing WebSocket message:', error, event.data);
        }
    },

    /**
     * WebSocket error handler
     */
    _onError: function(event) {
        console.error('WebSocket error:', event);
        this.emit('error', { message: 'WebSocket connection error' });
    },

    /**
     * WebSocket close handler
     */
    _onClose: function(event) {
        console.log('WebSocket closed', event.code, event.reason);
        this.isConnected = false;
        this.emit('disconnect', { code: event.code, reason: event.reason });

        // Attempt to reconnect unless deliberately closed
        if (event.code !== 1000) {
            this._scheduleReconnect();
        }
    },

    /**
     * Schedule reconnection with exponential backoff
     */
    _scheduleReconnect: function() {
        if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            console.error('Max reconnection attempts reached');
            this.emit('connectionFailed', { message: 'Unable to reconnect after multiple attempts' });
            return;
        }

        this.isReconnecting = true;
        this.reconnectAttempts++;

        // Exponential backoff: 1s, 2s, 4s, 8s, etc., capped at 30s
        const delay = Math.min(this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1), this.maxReconnectDelay);

        console.log(`Reconnecting in ${delay}ms (attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts})`);
        this.emit('reconnecting', { attempt: this.reconnectAttempts, delay });

        setTimeout(() => {
            console.log('Attempting to reconnect...');
            this.connect();
        }, delay);
    }
};

// Make WebSocket client globally available
window.WebSocketClient = WebSocketClient;

// Auto-initialize on page load
window.addEventListener('DOMContentLoaded', () => {
    WebSocketClient.init();
});
