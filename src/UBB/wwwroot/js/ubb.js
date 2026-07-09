// UBB helpers — extracted from index.html so the CSP can omit 'unsafe-inline'.
window.ubb = {
    initPopovers: function () {
        if (!window.bootstrap) return;
        document.querySelectorAll('[data-bs-toggle="popover"]').forEach(function (el) {
            bootstrap.Popover.getOrCreateInstance(el, {
                trigger: 'hover focus',
                delay: { show: 100, hide: 600 }
            });
        });
    },
    getHash: function () {
        return window.location.hash;
    },
    showToast: function (message) {
        const toastContainer = document.getElementById('toast-container') || (() => {
            const container = document.createElement('div');
            container.id = 'toast-container';
            container.style.cssText = 'position:fixed;top:20px;right:20px;z-index:10000;';
            document.body.appendChild(container);
            return container;
        })();

        const toastEl = document.createElement('div');
        toastEl.className = 'toast';
        toastEl.setAttribute('role', 'alert');

        // Build via textContent (not innerHTML) so the message can never inject markup (A03).
        const body = document.createElement('div');
        body.className = 'toast-body bg-success text-white';
        body.textContent = message;
        toastEl.appendChild(body);

        toastContainer.appendChild(toastEl);

        const toast = new bootstrap.Toast(toastEl, { delay: 2000 });
        toast.show();

        setTimeout(() => toastEl.remove(), 2500);
    }
};

// TD-16: cold-load watchdog — if the Blazor WASM runtime has not replaced the
// loading spinner within 30 seconds, stop spinning forever and offer a reload.
// Built with createElement/textContent only (no innerHTML — A03).
(function () {
    var TIMEOUT_MS = 30000;
    setTimeout(function () {
        var spinner = document.querySelector('#app .spinner-border');
        if (!spinner) return; // Blazor booted — nothing to do

        var app = document.getElementById('app');
        if (!app) return;

        var wrapper = document.createElement('div');
        wrapper.className = 'd-flex justify-content-center align-items-center';
        wrapper.style.height = '100vh';

        var box = document.createElement('div');
        box.className = 'text-center';
        box.setAttribute('role', 'alert');

        var heading = document.createElement('p');
        heading.className = 'h5 text-danger';
        heading.textContent = 'The simulator is taking too long to load.';

        var detail = document.createElement('p');
        detail.className = 'text-muted';
        detail.textContent = 'Your connection may be slow, or the download may have failed.';

        var button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn btn-primary';
        button.textContent = 'Reload';
        button.addEventListener('click', function () { location.reload(); });

        box.appendChild(heading);
        box.appendChild(detail);
        box.appendChild(button);
        wrapper.appendChild(box);

        app.replaceChildren(wrapper);
    }, TIMEOUT_MS);
})();
