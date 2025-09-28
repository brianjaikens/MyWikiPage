// JavaScript extracted from Index.cshtml

// Config modal handlers
document.getElementById('openConfig').addEventListener('click', () => {
    var modal = new bootstrap.Modal(document.getElementById('configModal'));
    modal.show();
});

document.getElementById('saveConfig').addEventListener('click', () => {
    var modal = bootstrap.Modal.getInstance(document.getElementById('configModal'));
    modal.hide();
});

// Connect to SSE
(function() {
    var evtSource = new EventSource('/sse/logs');
    var log = document.getElementById('log');
    var pagesFoundEl = document.getElementById('pagesFound');
    evtSource.onmessage = function(e) {
        if (!log) return;
        log.innerText += (log.innerText ? '\n' : '') + e.data;
        log.scrollTop = log.scrollHeight;

        // Update pages found count if message contains 'Pages found:'
        try {
            if (e.data && e.data.indexOf('Pages found:') !== -1) {
                var parts = e.data.split(':');
                var val = parts[parts.length-1].trim();
                if (pagesFoundEl) pagesFoundEl.innerText = val;
            }
        } catch (err) { }
    };
    evtSource.onerror = function(e) {
        console.error('SSE error', e);
        evtSource.close();
    };
})();

// Form submit for background grab
document.getElementById('grabForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    var form = e.target;
    var data = new FormData(form);

    var response;
    try {
        response = await fetch('', { method: 'POST', body: data, headers: { 'Accept': 'application/json' } });
    } catch (err) {
        console.error('Network error:', err);
        alert('Network error while submitting form');
        return;
    }

    var result = null;
    if (!response.ok) {
        // Try to read text and show status
        var txt = await response.text().catch(() => '');
        console.error('Server returned non-OK status', response.status, txt);
        alert('Server error: ' + response.status + (txt ? '\n' + txt : ''));
        return;
    }

    try {
        // Some servers may return an empty body; guard against that
        var contentType = response.headers.get('content-type');
        if (contentType && contentType.indexOf('application/json') !== -1) {
            result = await response.json();
        } else {
            // attempt to parse; if empty, fallback
            var text = await response.text();
            if (text && text.trim().length > 0) {
                try { result = JSON.parse(text); } catch { result = null; }
            }
        }
    } catch (err) {
        console.error('Failed to parse JSON response', err);
    }

    var status = document.getElementById('status');
    if (status) status.style.display = 'block';

    if (result && result.success) {
        if (status) {
            status.className = 'alert alert-success';
            status.innerText = result.message || 'Job queued';
            if (result.folder) {
                status.innerText += '\nSaved pages will be placed in: ' + result.folder;
            }
        }
    } else {
        if (status) {
            status.className = 'alert alert-danger';
            status.innerText = (result && result.message) ? ('Failed to enqueue: ' + result.message) : 'Failed to enqueue: Unknown';
        }
    }
});

// Discover Site Size
document.getElementById('discoverBtn').addEventListener('click', async () => {
    var form = document.getElementById('grabForm');
    var data = new FormData(form);
    data.append('DiscoverOnly', 'true');

    var status = document.getElementById('status');
    if (status) {
        status.style.display = 'block';
        status.className = 'alert alert-info';
        status.innerText = 'Starting discovery...';
    }

    try {
        var response = await fetch('/discover', { method: 'POST', body: data, headers: { 'Accept': 'application/json' } });
        if (!response.ok) {
            var txt = await response.text().catch(() => '');
            if (status) { status.className = 'alert alert-danger'; status.innerText = 'Discovery failed: ' + response.status + (txt ? '\n' + txt : ''); }
            return;
        }

        var result = await response.json().catch(() => null);
        if (result && result.success) {
            if (status) { status.className = 'alert alert-success'; status.innerText = 'Discovery completed. Pages found: ' + (result.pagesFound ?? 'unknown'); }
            var pagesFoundEl = document.getElementById('pagesFound');
            if (pagesFoundEl && result.pagesFound !== undefined) pagesFoundEl.innerText = result.pagesFound;
        } else {
            if (status) { status.className = 'alert alert-danger'; status.innerText = (result && result.message) ? ('Discovery failed: ' + result.message) : 'Discovery failed'; }
        }
    } catch (err) {
        console.error('Discovery error', err);
        if (status) { status.className = 'alert alert-danger'; status.innerText = 'Discovery error: ' + err.message; }
    }
});

// Quick actions
document.getElementById('quickRefresh').addEventListener('click', async () => {
    // trigger a refresh action on the page
    window.location.reload();
});

document.getElementById('downloadMd').addEventListener('click', () => {
    alert('Download functionality not implemented yet');
});
