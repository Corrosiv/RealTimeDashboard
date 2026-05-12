/**
 * Main application module - orchestrates UI rendering, API calls, and WebSocket events
 * Implements the demo scenario: CSV upload → transactions appear → activity feed updates
 */

const App = {
    state: {
        transactions: [],
        transactionsCursor: null,
        transactionsHasMore: false,
        activityFeed: [],
        activityFeedCursor: null,
        activityFeedHasMore: false,
        categories: new Set(),
        metrics: {
            transactionCount: 0,
            totalBalance: 0,
            categoryTotals: {}
        },
        currentFilter: {
            search: '',
            categoryId: null
        }
    },

    init: function() {
        this._attachEventListeners();
        this._setupWebSocketListeners();
        this._loadInitialData();
    },

    /**
     * Attach DOM event listeners
     */
    _attachEventListeners: function() {
        // Upload
        document.getElementById('uploadBtn').addEventListener('click', () => this._handleUpload());
        document.getElementById('csvFile').addEventListener('change', () => this._clearUploadStatus());

        // Filters
        document.getElementById('filterSearch').addEventListener('input', (e) => {
            this.state.currentFilter.search = e.target.value;
            this.state.transactionsCursor = null;
            this.state.transactions = [];
            this._loadTransactions();
        });

        document.getElementById('filterCategory').addEventListener('change', (e) => {
            this.state.currentFilter.categoryId = e.target.value ? parseInt(e.target.value) : null;
            this.state.transactionsCursor = null;
            this.state.transactions = [];
            this._loadTransactions();
        });

        // Pagination
        document.getElementById('loadMoreBtn').addEventListener('click', () => this._loadMoreTransactions());
        document.getElementById('loadMoreActivityBtn').addEventListener('click', () => this._loadMoreActivity());

        // Modal
        document.getElementById('messageClose').addEventListener('click', () => this._closeModal());
        document.getElementById('messageOkBtn').addEventListener('click', () => this._closeModal());
        document.getElementById('messageModal').addEventListener('click', (e) => {
            if (e.target.id === 'messageModal') this._closeModal();
        });
    },

    /**
     * Setup WebSocket event listeners
     */
    _setupWebSocketListeners: function() {
        WebSocketClient.on('connect', () => this._updateConnectionStatus('connected'));
        WebSocketClient.on('disconnect', () => this._updateConnectionStatus('disconnected'));
        WebSocketClient.on('reconnecting', () => this._updateConnectionStatus('reconnecting'));
        WebSocketClient.on('connectionFailed', () => this._updateConnectionStatus('disconnected'));

        WebSocketClient.on('activityEvent', (event) => this._handleActivityEvent(event));
        WebSocketClient.on('transactionCreated', (transaction) => this._handleTransactionCreated(transaction));
        WebSocketClient.on('replayError', (error) => this._showError('Replay Error', `${error.code}: ${error.message}`));
        WebSocketClient.on('error', (error) => this._showError('Connection Error', error.message));
    },

    /**
     * Load initial data on app start
     */
    _loadInitialData: function() {
        this._loadTransactions();
        this._loadActivityFeed();
    },

    /**
     * Handle CSV upload
     */
    _handleUpload: async function() {
        const fileInput = document.getElementById('csvFile');
        const file = fileInput.files[0];

        if (!file) {
            this._showUploadStatus('Please select a CSV file', 'error');
            return;
        }

        this._showUploadStatus('Uploading...', 'progress');
        document.getElementById('uploadBtn').disabled = true;

        try {
            const result = await API.uploadCsv(file);

            // Build error summary if any
            let message = `✓ Successfully processed ${result.processedCount} transactions`;
            if (result.errors && result.errors.length > 0) {
                message += `\n\nErrors (${result.errors.length}):\n`;
                result.errors.slice(0, 5).forEach(err => {
                    message += `• Row ${err.rowNumber}: ${err.message}\n`;
                });
                if (result.errors.length > 5) {
                    message += `... and ${result.errors.length - 5} more errors`;
                }
            }

            this._showUploadStatus(`✓ ${message}`, 'success');
            fileInput.value = '';

            // Auto-refresh transactions and activity feed
            this.state.transactionsCursor = null;
            this.state.transactions = [];
            this.state.activityFeedCursor = null;
            this.state.activityFeed = [];

            this._loadTransactions();
            this._loadActivityFeed();
        } catch (error) {
            this._showUploadStatus(`✗ Upload failed: ${error.message}`, 'error');
        } finally {
            document.getElementById('uploadBtn').disabled = false;
        }
    },

    /**
     * Load transactions from API
     */
    _loadTransactions: async function() {
        try {
            const options = {
                limit: 20,
                search: this.state.currentFilter.search || undefined,
                categoryId: this.state.currentFilter.categoryId || undefined
            };

            if (this.state.transactionsCursor) {
                options.cursor = this.state.transactionsCursor;
            }

            const result = await API.getTransactions(options);

            // If first page, replace; otherwise append
            if (!this.state.transactionsCursor) {
                this.state.transactions = result.data || [];
            } else {
                this.state.transactions = [...this.state.transactions, ...(result.data || [])];
            }

            this.state.transactionsCursor = result.nextCursor;
            this.state.transactionsHasMore = !!result.nextCursor;

            // Extract categories
            if (result.data) {
                result.data.forEach(t => {
                    if (t.categoryId) this.state.categories.add(t.categoryId);
                });
            }

            this._renderTransactions();
            this._updateCategoryFilter();
            this._updateMetrics(result);
        } catch (error) {
            console.error('Error loading transactions:', error);
        }
    },

    /**
     * Load more transactions (pagination)
     */
    _loadMoreTransactions: function() {
        if (this.state.transactionsHasMore) {
            this._loadTransactions();
        }
    },

    /**
     * Load activity feed from API
     */
    _loadActivityFeed: async function() {
        try {
            const options = {
                limit: 20
            };

            if (this.state.activityFeedCursor) {
                options.cursor = this.state.activityFeedCursor;
            }

            const result = await API.getActivityFeed(options);

            // If first page, replace; otherwise prepend new items
            if (!this.state.activityFeedCursor) {
                this.state.activityFeed = result.entries || [];
            } else {
                this.state.activityFeed = [...(result.entries || []), ...this.state.activityFeed];
            }

            this.state.activityFeedCursor = result.nextCursor;
            this.state.activityFeedHasMore = result.hasMore;

            this._renderActivityFeed();
        } catch (error) {
            console.error('Error loading activity feed:', error);
        }
    },

    /**
     * Load more activity feed items (pagination)
     */
    _loadMoreActivity: function() {
        if (this.state.activityFeedHasMore) {
            this._loadActivityFeed();
        }
    },

    /**
     * Handle new activity event from WebSocket
     */
    _handleActivityEvent: function(event) {
        // Prepend new event to feed
        this.state.activityFeed.unshift(event);
        this._renderActivityFeed();
    },

    /**
     * Handle new transaction from WebSocket
     */
    _handleTransactionCreated: function(transaction) {
        // Prepend new transaction to list (if on first page)
        if (!this.state.transactionsCursor) {
            this.state.transactions.unshift(transaction);
            this._renderTransactions();
        }
    },

    /**
     * Render transactions list
     */
    _renderTransactions: function() {
        const container = document.getElementById('transactionsList');
        const loadMoreBtn = document.getElementById('loadMoreBtn');

        if (this.state.transactions.length === 0) {
            container.innerHTML = '<div class="empty-state"><div class="empty-state-icon">📄</div><div class="empty-state-text">No transactions yet. Upload a CSV or create one manually.</div></div>';
            loadMoreBtn.style.display = 'none';
            return;
        }

        // Render header row
        let html = '<div class="transaction-row">';
        html += '<div>Timestamp</div>';
        html += '<div>Amount</div>';
        html += '<div>Description</div>';
        html += '<div>Category</div>';
        html += '<div>Created By</div>';
        html += '</div>';

        // Render data rows
        this.state.transactions.forEach(t => {
            const amount = parseFloat(t.amount);
            const amountClass = amount >= 0 ? 'positive' : 'negative';
            const amountFormatted = new Intl.NumberFormat('en-US', {
                style: 'currency',
                currency: t.currency || 'USD'
            }).format(amount);

            const timestamp = new Date(t.timestamp).toLocaleString();

            html += '<div class="transaction-row">';
            html += `<div class="transaction-timestamp">${timestamp}</div>`;
            html += `<div class="transaction-amount ${amountClass}">${amountFormatted}</div>`;
            html += `<div class="transaction-description">${this._escapeHtml(t.description || '')}</div>`;
            html += `<div class="transaction-category">${t.categoryId || 'N/A'}</div>`;
            html += `<div class="transaction-createdby">${this._escapeHtml(t.createdBy || 'Unknown')}</div>`;
            html += '</div>';
        });

        container.innerHTML = html;
        loadMoreBtn.style.display = this.state.transactionsHasMore ? 'block' : 'none';
    },

    /**
     * Render activity feed
     */
    _renderActivityFeed: function() {
        const container = document.getElementById('activityFeed');
        const loadMoreBtn = document.getElementById('loadMoreActivityBtn');

        if (this.state.activityFeed.length === 0) {
            container.innerHTML = '<div class="empty-state"><div class="empty-state-icon">📊</div><div class="empty-state-text">No activity yet.</div></div>';
            loadMoreBtn.style.display = 'none';
            return;
        }

        let html = '';
        this.state.activityFeed.forEach(event => {
            const timestamp = new Date(event.occurredAtUtc).toLocaleString();
            html += '<div class="activity-item">';
            html += `<div class="activity-timestamp">${timestamp}</div>`;
            html += `<div class="activity-type">${this._escapeHtml(event.eventType)}</div>`;
            html += `<div class="activity-message">${this._escapeHtml(event.message)}</div>`;
            html += `<div class="activity-meta">by <strong>${this._escapeHtml(event.createdBy)}</strong></div>`;
            html += '</div>';
        });

        container.innerHTML = html;
        loadMoreBtn.style.display = this.state.activityFeedHasMore ? 'block' : 'none';
    },

    /**
     * Update metrics display
     */
    _updateMetrics: function(transactionsResult) {
        // Calculate metrics
        let totalBalance = 0;
        const categoryTotals = {};

        this.state.transactions.forEach(t => {
            totalBalance += parseFloat(t.amount);
            const catId = t.categoryId || 'Other';
            categoryTotals[catId] = (categoryTotals[catId] || 0) + parseFloat(t.amount);
        });

        this.state.metrics.transactionCount = transactionsResult.totalCount || 0;
        this.state.metrics.totalBalance = totalBalance;
        this.state.metrics.categoryTotals = categoryTotals;

        // Render metrics
        document.getElementById('metricTransactions').textContent = this.state.metrics.transactionCount;
        document.getElementById('metricBalance').textContent = new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD'
        }).format(totalBalance);

        const categoryHtml = Object.entries(categoryTotals)
            .sort((a, b) => Math.abs(b[1]) - Math.abs(a[1]))
            .slice(0, 5)
            .map(([cat, amount]) => `
                <div class="metric-table-row">
                    <span class="metric-category-name">Category ${cat}</span>
                    <span class="metric-category-amount">${new Intl.NumberFormat('en-US', {
                        style: 'currency',
                        currency: 'USD'
                    }).format(amount)}</span>
                </div>
            `)
            .join('');

        document.getElementById('metricCategories').innerHTML = categoryHtml || '<div style="color: #999;">No data</div>';
    },

    /**
     * Update category filter dropdown
     */
    _updateCategoryFilter: function() {
        const select = document.getElementById('filterCategory');
        const currentValue = select.value;

        let html = '<option value="">All Categories</option>';
        Array.from(this.state.categories).sort().forEach(catId => {
            html += `<option value="${catId}">Category ${catId}</option>`;
        });

        select.innerHTML = html;
        select.value = currentValue;
    },

    /**
     * Update connection status indicator
     */
    _updateConnectionStatus: function(status) {
        const statusEl = document.getElementById('connectionStatus');
        const dot = statusEl.querySelector('.status-dot');
        const text = statusEl.querySelector('.status-text');

        dot.className = 'status-dot ' + status;
        text.textContent = {
            'connected': 'Connected ✓',
            'disconnected': 'Disconnected ✗',
            'reconnecting': 'Reconnecting…'
        }[status] || 'Unknown';
    },

    /**
     * Show/hide upload status message
     */
    _showUploadStatus: function(message, type) {
        const statusEl = document.getElementById('uploadStatus');
        statusEl.className = `upload-status show ${type}`;
        statusEl.textContent = message;

        if (type === 'success') {
            setTimeout(() => {
                statusEl.classList.remove('show');
            }, 5000);
        }
    },

    /**
     * Clear upload status
     */
    _clearUploadStatus: function() {
        const statusEl = document.getElementById('uploadStatus');
        statusEl.classList.remove('show');
    },

    /**
     * Show error modal
     */
    _showError: function(title, message) {
        document.getElementById('messageTitle').textContent = title;
        document.getElementById('messageBody').textContent = message;
        document.getElementById('messageModal').style.display = 'flex';
    },

    /**
     * Close modal
     */
    _closeModal: function() {
        document.getElementById('messageModal').style.display = 'none';
    },

    /**
     * Escape HTML special characters
     */
    _escapeHtml: function(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
};

// Initialize app on DOM ready
window.addEventListener('DOMContentLoaded', () => {
    App.init();
});
