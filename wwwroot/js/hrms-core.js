/**
 * HRMS Core Client Utilities — Sober & Robust
 */
const HRMS = (() => {
    let currentStream = null;

    // Live clock in topbar
    function initClock() {
        const clockEl = document.getElementById('hrmsLiveClock');
        if (!clockEl) return;

        function update() {
            const now = new Date();
            const dateStr = now.toLocaleDateString('en-GB', {
                weekday: 'short',
                day: '2-digit',
                month: 'short',
                year: 'numeric'
            });
            const timeStr = now.toLocaleTimeString('en-US', {
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: true
            });
            clockEl.innerHTML = `<i class="bi bi-clock me-1 text-teal"></i> ${dateStr} &bull; ${timeStr}`;
        }

        update();
        setInterval(update, 1000);
    }

    // Sidebar collapse & expand (Single unified toggle)
    function initSidebarToggle() {
        const burgerBtn = document.getElementById('btnTopbarBurger');
        const sidebar = document.getElementById('hrmsSidebar');
        const backdrop = document.getElementById('hrmsSidebarBackdrop');
        const mainContent = document.querySelector('.hrms-main');

        if (!sidebar) return;

        // Restore saved desktop collapse state
        const isCollapsed = localStorage.getItem('hrms_sidebar_collapsed') === 'true';
        if (isCollapsed && window.innerWidth >= 992) {
            sidebar.classList.add('collapsed');
            if (mainContent) mainContent.classList.add('sidebar-collapsed');
        }

        function toggleSidebar() {
            if (window.innerWidth >= 992) {
                // Desktop: icon collapse mode
                sidebar.classList.toggle('collapsed');
                if (mainContent) mainContent.classList.toggle('sidebar-collapsed');
                const state = sidebar.classList.contains('collapsed');
                localStorage.setItem('hrms_sidebar_collapsed', state);
            } else {
                // Mobile: off-canvas drawer
                sidebar.classList.toggle('show');
                if (backdrop) backdrop.classList.toggle('show');
            }
        }

        if (burgerBtn) burgerBtn.addEventListener('click', toggleSidebar);
        
        const closeMobileBtn = document.getElementById('btnSidebarCloseMobile');
        if (closeMobileBtn) {
            closeMobileBtn.addEventListener('click', () => {
                sidebar.classList.remove('show');
                if (backdrop) backdrop.classList.remove('show');
            });
        }

        if (backdrop) backdrop.addEventListener('click', () => {
            sidebar.classList.remove('show');
            backdrop.classList.remove('show');
        });

        // Auto-close sidebar on mobile when navigating
        document.querySelectorAll('.hrms-nav-link').forEach(link => {
            link.addEventListener('click', () => {
                if (window.innerWidth < 992) {
                    sidebar.classList.remove('show');
                    if (backdrop) backdrop.classList.remove('show');
                }
            });
        });
    }

    // Robust camera helper
    async function startCamera(videoElementId, errorCallback) {
        const video = document.getElementById(videoElementId);
        if (!video) return null;

        stopCamera();

        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            const msg = 'Camera access is not supported by your browser or requires a secure connection (localhost / HTTPS).';
            console.error(msg);
            if (errorCallback) errorCallback(msg);
            return null;
        }

        // Configure element attributes to prevent browser auto-play blocks
        video.muted = true;
        video.playsInline = true;
        video.setAttribute('playsinline', '');
        video.setAttribute('muted', '');
        video.setAttribute('autoplay', '');

        try {
            // First attempt: ideal 640x480
            let stream = null;
            try {
                stream = await navigator.mediaDevices.getUserMedia({
                    video: {
                        width: { ideal: 640 },
                        height: { ideal: 480 }
                    },
                    audio: false
                });
            } catch (err) {
                console.warn('Initial camera constraints failed, attempting fallback to basic video...', err);
                // Fallback attempt: basic video
                stream = await navigator.mediaDevices.getUserMedia({
                    video: true,
                    audio: false
                });
            }

            currentStream = stream;
            video.srcObject = stream;

            await new Promise((resolve) => {
                if (video.readyState >= 2) {
                    resolve();
                } else {
                    video.onloadedmetadata = () => resolve();
                    // Fallback timeout in case onloadedmetadata doesn't fire
                    setTimeout(resolve, 800);
                }
            });

            await video.play().catch(playErr => {
                console.warn('video.play() warning:', playErr);
            });

            return currentStream;
        } catch (err) {
            console.error('Camera access error:', err);
            let friendlyMsg = err.message || 'Camera access failed.';
            if (err.name === 'NotAllowedError' || err.name === 'PermissionDeniedError') {
                friendlyMsg = 'Camera permission was denied. Please allow camera access in your browser address bar.';
            } else if (err.name === 'NotFoundError' || err.name === 'DevicesNotFoundError') {
                friendlyMsg = 'No camera device detected on this computer.';
            } else if (err.name === 'NotReadableError' || err.name === 'TrackStartError') {
                friendlyMsg = 'Camera is currently locked by another application (e.g. Teams, Zoom, or another tab).';
            }
            if (errorCallback) {
                errorCallback(friendlyMsg);
            }
            return null;
        }
    }

    function stopCamera() {
        if (currentStream) {
            currentStream.getTracks().forEach(track => {
                try { track.stop(); } catch (e) { }
            });
            currentStream = null;
        }
    }

    function captureFrameAsBase64(videoElementId) {
        const video = document.getElementById(videoElementId);
        if (!video) return null;

        const w = (video.videoWidth && video.videoWidth > 0) ? video.videoWidth : (video.clientWidth || 640);
        const h = (video.videoHeight && video.videoHeight > 0) ? video.videoHeight : (video.clientHeight || 480);

        if (w <= 0 || h <= 0) {
            console.warn('Cannot capture frame: video dimensions invalid');
            return null;
        }

        const canvas = document.createElement('canvas');
        canvas.width = w;
        canvas.height = h;
        const ctx = canvas.getContext('2d');

        // Draw natural frame unmirrored
        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
        return canvas.toDataURL('image/jpeg', 0.90);
    }

    // Helper to read any File object as Base64 Data URL
    function fileToBase64(file) {
        return new Promise((resolve, reject) => {
            if (!file) {
                resolve(null);
                return;
            }
            const reader = new FileReader();
            reader.onload = (e) => resolve(e.target.result);
            reader.onerror = (e) => reject(e);
            reader.readAsDataURL(file);
        });
    }

    // PWA Service Worker Registration
    function registerServiceWorker() {
        if ('serviceWorker' in navigator) {
            window.addEventListener('load', () => {
                navigator.serviceWorker.register('/service-worker.js')
                    .then(reg => {
                        console.log('HRMS ServiceWorker registered with scope:', reg.scope);
                    })
                    .catch(err => {
                        console.warn('ServiceWorker registration error:', err);
                    });
            });
        }
    }

    // Toast Notification helper
    function showNotification(message, type = 'info') {
        const container = document.getElementById('hrmsToastContainer');
        if (!container) return;

        const toastEl = document.createElement('div');
        const bgClass = type === 'success' ? 'bg-success text-white' :
                        type === 'error' ? 'bg-danger text-white' :
                        type === 'warning' ? 'bg-warning text-dark' : 'bg-primary text-white';

        toastEl.className = `toast align-items-center ${bgClass} border-0 show shadow-sm`;
        toastEl.setAttribute('role', 'alert');
        toastEl.innerHTML = `
            <div class="d-flex">
                <div class="toast-body fw-semibold py-2">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        `;

        container.appendChild(toastEl);
        setTimeout(() => {
            toastEl.remove();
        }, 5000);
    }

    // Auto initialize on DOM ready
    document.addEventListener('DOMContentLoaded', () => {
        initClock();
        initSidebarToggle();
        registerServiceWorker();
    });

    return {
        startCamera,
        stopCamera,
        captureFrameAsBase64,
        fileToBase64,
        showNotification
    };
})();
