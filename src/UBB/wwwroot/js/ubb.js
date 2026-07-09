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
