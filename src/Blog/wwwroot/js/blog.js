// Small helpers for the Blazor app: code highlighting and PWA update handling.
window.blog = {
    highlight: function (element) {
        if (!window.hljs || !element) return;
        element.querySelectorAll('pre code').forEach(function (block) {
            if (!block.dataset.highlighted) window.hljs.highlightElement(block);
        });
    }
};

(function () {
    if (!('serviceWorker' in navigator)) return;

    var reloading = false;
    navigator.serviceWorker.addEventListener('controllerchange', function () {
        if (reloading) return;
        reloading = true;
        window.location.reload();
    });

    navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' }).then(function (registration) {
        // A new version is waiting when this page was opened.
        if (registration.waiting && navigator.serviceWorker.controller) showUpdateBanner(registration.waiting);

        // A new version was found while the page is open.
        registration.addEventListener('updatefound', function () {
            var worker = registration.installing;
            if (!worker) return;
            worker.addEventListener('statechange', function () {
                if (worker.state === 'installed' && navigator.serviceWorker.controller) showUpdateBanner(worker);
            });
        });

        // Installed apps can stay open for a long time: check for a new version every hour.
        setInterval(function () { registration.update(); }, 60 * 60 * 1000);
    });

    function showUpdateBanner(worker) {
        if (document.getElementById('update-banner')) return;
        var banner = document.createElement('div');
        banner.id = 'update-banner';
        banner.className = 'update-banner';
        banner.setAttribute('role', 'status');
        banner.innerHTML = '<span>A new version of the blog is available.</span>';
        var button = document.createElement('button');
        button.className = 'button';
        button.textContent = 'Reload';
        button.addEventListener('click', function () {
            button.disabled = true;
            worker.postMessage({ type: 'SKIP_WAITING' });
        });
        banner.appendChild(button);
        document.body.appendChild(banner);
    }
}());
