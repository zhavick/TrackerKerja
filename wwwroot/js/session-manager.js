/**
 * TrackerKerja - Modern Inactivity Session Manager
 * Automatically logs out user after 1 Hour (60 minutes) of inactivity for security.
 * Displays a friendly 5-minute countdown warning at 55 minutes, with option to extend session.
 */
(function () {
    'use strict';

    const IDLE_TIMEOUT_MS = 60 * 60 * 1000;       // 60 minutes = 1 Hour
    const WARNING_TIMEOUT_MS = 55 * 60 * 1000;    // 55 minutes (5 min before timeout)
    const THROTTLE_ACTIVITY_MS = 3000;            // Throttle activity listener to reduce CPU usage

    let warningTimer = null;
    let logoutTimer = null;
    let warningModalActive = false;
    let countdownInterval = null;
    let lastActivityTime = Date.now();

    function init() {
        // Only run if user is authenticated (indicated by existence of userProfileDropdownBtn or sidebar user)
        const hasAuthProfile = document.getElementById('userProfileDropdownBtn') || document.querySelector('form[action*="Logout"]');
        if (!hasAuthProfile) {
            return;
        }

        resetTimers();
        bindActivityEvents();
    }

    function resetTimers() {
        if (warningModalActive) {
            return; // Wait for user interaction on active warning dialog
        }

        clearTimeout(warningTimer);
        clearTimeout(logoutTimer);

        warningTimer = setTimeout(showTimeoutWarning, WARNING_TIMEOUT_MS);
        logoutTimer = setTimeout(performAutoLogout, IDLE_TIMEOUT_MS);
    }

    function onUserActivity() {
        const now = Date.now();
        if (now - lastActivityTime < THROTTLE_ACTIVITY_MS) {
            return;
        }
        lastActivityTime = now;
        resetTimers();
    }

    function bindActivityEvents() {
        const events = ['mousedown', 'mousemove', 'keydown', 'scroll', 'touchstart', 'click'];
        events.forEach(function (evt) {
            window.addEventListener(evt, onUserActivity, { passive: true });
        });
    }

    function showTimeoutWarning() {
        warningModalActive = true;
        let secondsLeft = 300; // 5 minutes

        if (typeof Swal === 'function') {
            Swal.fire({
                title: 'Peringatan Keamanan Sesi',
                html: `
                    <div class="text-left text-xs sm:text-sm text-slate-600 space-y-3">
                        <p>Sesi Anda akan berakhir dalam <strong class="text-rose-600 font-mono text-base sm:text-lg" id="session-countdown-display">05:00</strong> karena tidak ada aktivitas selama 55 menit.</p>
                        <div class="p-3 bg-amber-50 rounded-xl border border-amber-200 text-amber-800 text-xs">
                            <i class="fas fa-shield-alt text-amber-600 mr-1.5"></i>
                            Demi keamanan akun Anda, sistem secara otomatis mengakhiri sesi yang tidak aktif setelah <strong>1 jam</strong>.
                        </div>
                        <p class="text-slate-500">Apakah Anda ingin tetap masuk dan melanjutkan pekerjaan?</p>
                    </div>
                `,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#4F46E5',
                cancelButtonColor: '#EF4444',
                confirmButtonText: '<i class="fas fa-redo mr-1.5"></i>Tetap Masuk (Perpanjang Sesi)',
                cancelButtonText: '<i class="fas fa-sign-out-alt mr-1.5"></i>Keluar Sekarang',
                allowOutsideClick: false,
                allowEscapeKey: false,
                customClass: {
                    popup: 'rounded-3xl shadow-2xl border border-slate-200'
                },
                didOpen: function () {
                    countdownInterval = setInterval(function () {
                        secondsLeft--;
                        const mins = Math.floor(secondsLeft / 60);
                        const secs = secondsLeft % 60;
                        const formatted = `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
                        const displayEl = document.getElementById('session-countdown-display');
                        if (displayEl) {
                            displayEl.innerText = formatted;
                        }

                        if (secondsLeft <= 0) {
                            clearInterval(countdownInterval);
                            performAutoLogout();
                        }
                    }, 1000);
                },
                willClose: function () {
                    if (countdownInterval) {
                        clearInterval(countdownInterval);
                    }
                }
            }).then(function (result) {
                if (result.isConfirmed) {
                    extendSession();
                } else if (result.dismiss === Swal.DismissReason.cancel) {
                    performAutoLogout();
                }
            });
        } else {
            // Fallback if Swal is not loaded
            const stay = confirm('Sesi Anda akan berakhir karena tidak ada aktivitas selama 1 jam. Klik OK untuk tetap masuk.');
            if (stay) {
                extendSession();
            } else {
                performAutoLogout();
            }
        }
    }

    function extendSession() {
        fetch('/Account/KeepAlive', { method: 'GET', credentials: 'same-origin' })
            .then(function (res) {
                if (res.ok) {
                    warningModalActive = false;
                    resetTimers();
                    if (typeof Swal === 'function') {
                        Swal.fire({
                            title: 'Sesi Diperpanjang',
                            text: 'Sesi Anda telah berhasil diperpanjang untuk 1 jam ke depan.',
                            icon: 'success',
                            timer: 2000,
                            showConfirmButton: false,
                            customClass: { popup: 'rounded-2xl' }
                        });
                    }
                } else {
                    performAutoLogout();
                }
            })
            .catch(function () {
                warningModalActive = false;
                resetTimers();
            });
    }

    function performAutoLogout() {
        if (countdownInterval) {
            clearInterval(countdownInterval);
        }
        window.location.href = '/Account/Logout?reason=timeout';
    }

    // Initialize on DOM Ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
