/**
 * API module - handles all HTTP communication with the backend
 * Separates API logic from UI rendering for maintainability and future React migration
 */

const API = {
    baseUrl: `${window.location.protocol}//${window.location.host}/api`,

    /**
     * Upload CSV file to the server
     * @param {File} file - The CSV file to upload
     * @returns {Promise} Response with processedCount, errors, and activityMessage
     */
    uploadCsv: async function(file) {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('username', this.getCurrentUsername());

        try {
            const response = await fetch(`${this.baseUrl}/upload/csv`, {
                method: 'POST',
                body: formData
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Upload failed');
            }

            return await response.json();
        } catch (error) {
            throw error;
        }
    },

    /**
     * Get paginated list of transactions
     * @param {Object} options - Query options { limit, cursor, search, categoryId }
     * @returns {Promise} Response with data array, nextCursor, totalCount, count
     */
    getTransactions: async function(options = {}) {
        const params = new URLSearchParams();
        if (options.limit) params.append('limit', options.limit);
        if (options.cursor) params.append('cursor', options.cursor);
        if (options.search) params.append('search', options.search);
        if (options.categoryId) params.append('categoryId', options.categoryId);

        try {
            const response = await fetch(`${this.baseUrl}/transactions?${params.toString()}`);
            if (!response.ok) throw new Error('Failed to fetch transactions');
            return await response.json();
        } catch (error) {
            console.error('Error fetching transactions:', error);
            throw error;
        }
    },

    /**
     * Create a new transaction (for manual entry)
     * @param {Object} transaction - Transaction data { timestamp, amount, currency, description, categoryId, createdBy }
     * @returns {Promise} Created transaction object
     */
    createTransaction: async function(transaction) {
        try {
            const response = await fetch(`${this.baseUrl}/transactions`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    ...transaction,
                    createdBy: transaction.createdBy || this.getCurrentUsername()
                })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Failed to create transaction');
            }

            return await response.json();
        } catch (error) {
            console.error('Error creating transaction:', error);
            throw error;
        }
    },

    /**
     * Get paginated activity feed with optional filters
     * @param {Object} options - { limit, cursor, createdBy, eventType, since, until, resourceId }
     * @returns {Promise} Response with entries array, nextCursor, hasMore
     */
    getActivityFeed: async function(options = {}) {
        const params = new URLSearchParams();
        if (options.limit) params.append('limit', options.limit);
        if (options.cursor) params.append('cursor', options.cursor);
        if (options.createdBy) params.append('createdBy', options.createdBy);
        if (options.eventType) params.append('eventType', options.eventType);
        if (options.since) params.append('since', options.since);
        if (options.until) params.append('until', options.until);
        if (options.resourceId) params.append('resourceId', options.resourceId);

        try {
            const response = await fetch(`${this.baseUrl}/activityfeed?${params.toString()}`);
            if (!response.ok) throw new Error('Failed to fetch activity feed');
            return await response.json();
        } catch (error) {
            console.error('Error fetching activity feed:', error);
            throw error;
        }
    },

    /**
     * Get current username from sessionStorage or generate one
     * @returns {string} Username
     */
    getCurrentUsername: function() {
        let username = sessionStorage.getItem('username');
        if (!username) {
            username = `User_${Math.random().toString(36).substring(7)}`;
            sessionStorage.setItem('username', username);
        }
        return username;
    },

    /**
     * Set username in session
     * @param {string} username - New username
     */
    setUsername: function(username) {
        sessionStorage.setItem('username', username);
    }
};

// Make API globally available
window.API = API;
