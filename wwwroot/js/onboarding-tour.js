/**
 * WORK TRACKER PRO — ONBOARDING SCREEN & INTERACTIVE WALKTHROUGH TOUR ENGINE
 * Author: Work Tracker Pro Team
 */

(function (window, document) {
    'use strict';

    const OnboardingTour = {
        userId: '',
        userName: '',
        isAdmin: false,
        currentSlide: 0,
        totalSlides: 5,
        currentStepIndex: 0,
        isActive: false,
        steps: [],

        // Initialize onboarding system
        init: function (config) {
            this.userId = config?.userId || 'guest';
            this.userName = config?.userName || 'Pengguna';
            this.isAdmin = config?.isAdmin || false;

            this.defineSteps();
            this.bindGlobalEvents();

            // Check if user is visiting for the first time or requested via query param (?tour=true)
            const urlParams = new URLSearchParams(window.location.search);
            const forceTour = urlParams.get('tour') === 'true' || urlParams.get('onboarding') === 'true';
            const hasSeen = localStorage.getItem('wtp_onboarding_dismissed_' + this.userId);

            if (forceTour) {
                setTimeout(() => {
                    this.showWelcomeModal();
                }, 500);
            } else if (!hasSeen && this.userId !== 'guest') {
                // Auto trigger welcome modal on first login after dashboard loads
                setTimeout(() => {
                    this.showWelcomeModal();
                }, 800);
            }
        },

        defineSteps: function () {
            this.steps = [
                {
                    target: '[data-tour="sidebar"]',
                    fallbackTarget: '#sidebar',
                    title: '🧭 Menu Navigasi Utama',
                    content: 'Akses cepat ke seluruh modul kerja: Dashboard, Manajemen Tugas (Kanban), Proyek, Kalender Kerja, Presensi GPS, Timesheet, Catatan & Dokumen, serta Anggota Tim.',
                    placement: 'right',
                    isSidebar: true
                },
                {
                    target: '[data-tour="search"]',
                    fallbackTarget: '#search-container',
                    title: '🔍 Pencarian Cepat Global',
                    content: 'Cari tugas, proyek, dan catatan secara instan hanya dengan mengetikkan kata kunci atau nama tugas langsung di kolom pencarian ini.',
                    placement: 'bottom'
                },
                {
                    target: '[data-tour="notif"]',
                    fallbackTarget: '#notifDropdownBtn',
                    title: '🔔 Pusat Pemberitahuan Tugas',
                    content: 'Dapatkan notifikasi cerdas untuk tugas yang mendekati jatuh tempo (Due), tugas yang sedang berjalan (Progress), dan pengingat jam kerja cut-off timesheet setiap tanggal 25.',
                    placement: 'bottom'
                },
                {
                    target: '[data-tour="theme"]',
                    fallbackTarget: '#themeDropdownBtn',
                    title: '🎨 16 Pilihan Tema Modern',
                    content: 'Sesuaikan antarmuka aplikasi sesuai preferensi Anda! Tersedia 16 tema pilihan yang mencakup mode terang (Light) dan mode gelap elegan (Dark Mode).',
                    placement: 'bottom'
                },
                {
                    target: '[data-tour="guide"]',
                    fallbackTarget: '#btnPanduanTop',
                    title: '📖 Buku Panduan & Cetak PDF',
                    content: 'Buka petunjuk lengkap operasional fitur, cari topik panduan secara cepat, atau cetak dokumen pedoman pengguna langsung ke format PDF.',
                    placement: 'bottom'
                },
                {
                    target: '[data-tour="profile"]',
                    fallbackTarget: '#userProfileDropdownBtn',
                    title: '👤 Profil & Keamanan Akun',
                    content: 'Kelola informasi profil pribadi, ganti avatar warna/foto, ubah kata sandi akun, atau keluar dari sesi aplikasi.',
                    placement: 'bottom'
                },
                {
                    target: '[data-tour="quick-actions"]',
                    fallbackTarget: '#dashboard-quick-actions',
                    title: '🚀 Aksi Cepat & Produktivitas',
                    content: 'Mulai buat tugas baru secara instan, catat ide di dokumen kerja, sinkronkan data secara real-time, dan lacak produktivitas harian Anda bersama tim!',
                    placement: 'bottom'
                }
            ];
        },

        bindGlobalEvents: function () {
            // Resize / scroll listener to re-align tooltip and spotlight
            const handleReposition = () => {
                if (this.isActive) {
                    this.positionCurrentStep();
                }
            };
            window.addEventListener('resize', handleReposition, { passive: true });
            window.addEventListener('scroll', handleReposition, { passive: true });

            // Keyboard navigation
            document.addEventListener('keydown', (e) => {
                if (this.isActive) {
                    if (e.key === 'ArrowRight' || e.key === 'Enter') {
                        e.preventDefault();
                        this.nextStep();
                    } else if (e.key === 'ArrowLeft') {
                        e.preventDefault();
                        this.prevStep();
                    } else if (e.key === 'Escape') {
                        e.preventDefault();
                        this.endTour();
                    }
                } else {
                    const welcomeModal = document.getElementById('onboardingWelcomeModal');
                    if (welcomeModal && welcomeModal.classList.contains('active') && e.key === 'Escape') {
                        this.closeWelcomeModal();
                    }
                }
            });
        },

        /* ── WELCOME MODAL LOGIC ── */
        showWelcomeModal: function () {
            const modal = document.getElementById('onboardingWelcomeModal');
            if (modal) {
                modal.classList.add('active');
                document.body.style.overflow = 'hidden';
                this.goToSlide(0);
            }
        },

        closeWelcomeModal: function (forceDismiss) {
            const modal = document.getElementById('onboardingWelcomeModal');
            if (modal) {
                modal.classList.remove('active');
                document.body.style.overflow = '';
            }

            const dontShowCheckbox = document.getElementById('chkDontShowOnboardingAgain');
            if (forceDismiss || (dontShowCheckbox && dontShowCheckbox.checked)) {
                localStorage.setItem('wtp_onboarding_dismissed_' + this.userId, 'true');
            }
        },

        goToSlide: function (index) {
            this.currentSlide = Math.max(0, Math.min(index, this.totalSlides - 1));
            
            // Hide all slides
            for (let i = 0; i < this.totalSlides; i++) {
                const slideEl = document.getElementById('onboardingSlide_' + i);
                const dotEl = document.getElementById('onboardingDot_' + i);
                if (slideEl) {
                    if (i === this.currentSlide) {
                        slideEl.classList.remove('hidden');
                        slideEl.classList.add('flex');
                    } else {
                        slideEl.classList.add('hidden');
                        slideEl.classList.remove('flex');
                    }
                }
                if (dotEl) {
                    if (i === this.currentSlide) {
                        dotEl.classList.add('w-8', 'bg-indigo-600');
                        dotEl.classList.remove('w-2.5', 'bg-slate-300');
                    } else {
                        dotEl.classList.remove('w-8', 'bg-indigo-600');
                        dotEl.classList.add('w-2.5', 'bg-slate-300');
                    }
                }
            }

            // Update footer buttons
            const btnPrev = document.getElementById('btnOnboardingPrev');
            const btnNext = document.getElementById('btnOnboardingNext');
            const btnStart = document.getElementById('btnOnboardingStartTour');

            if (btnPrev) {
                btnPrev.style.visibility = this.currentSlide === 0 ? 'hidden' : 'visible';
            }

            if (btnNext && btnStart) {
                if (this.currentSlide === this.totalSlides - 1) {
                    btnNext.classList.add('hidden');
                    btnStart.classList.remove('hidden');
                } else {
                    btnNext.classList.remove('hidden');
                    btnStart.classList.add('hidden');
                }
            }
        },

        nextSlide: function () {
            if (this.currentSlide < this.totalSlides - 1) {
                this.goToSlide(this.currentSlide + 1);
            }
        },

        prevSlide: function () {
            if (this.currentSlide > 0) {
                this.goToSlide(this.currentSlide - 1);
            }
        },

        startTourFromModal: function () {
            this.closeWelcomeModal(true);
            setTimeout(() => {
                this.startTour();
            }, 300);
        },

        /* ── INTERACTIVE SPOTLIGHT & TOOLTIP WALKTHROUGH ── */
        startTour: function () {
            this.isActive = true;
            this.currentStepIndex = 0;

            const overlay = document.getElementById('tourSpotlightOverlay');
            const backdrop = document.getElementById('tourSpotlightBackdrop');
            const tooltip = document.getElementById('tourTooltipContainer');

            if (overlay) overlay.classList.remove('hidden');
            if (backdrop) backdrop.classList.add('active');
            if (tooltip) tooltip.classList.add('active');

            this.goToStep(0);
        },

        endTour: function () {
            this.isActive = false;

            const overlay = document.getElementById('tourSpotlightOverlay');
            const backdrop = document.getElementById('tourSpotlightBackdrop');
            const tooltip = document.getElementById('tourTooltipContainer');
            const highlight = document.getElementById('tourTargetHighlight');

            if (overlay) overlay.classList.add('hidden');
            if (backdrop) backdrop.classList.remove('active');
            if (tooltip) {
                tooltip.classList.remove('active');
                tooltip.style.opacity = '0';
                tooltip.style.visibility = 'hidden';
            }
            if (highlight) highlight.style.display = 'none';

            // If mobile sidebar was opened during tour, close it
            if (window.innerWidth < 1024 && typeof window.closeMobileSidebar === 'function') {
                window.closeMobileSidebar();
            }

            // Save completed status
            localStorage.setItem('wtp_onboarding_dismissed_' + this.userId, 'true');

            // Show friendly completion toast
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'success',
                    title: 'Tur Selesai! 🎉',
                    text: 'Anda siap menggunakan Work Tracker Pro. Butuh bantuan kapan saja? Klik tombol "Panduan" atau "Tur Aplikasi" di header.',
                    confirmButtonText: 'Mulai Bekerja',
                    confirmButtonColor: '#6366F1',
                    timer: 4000,
                    timerProgressBar: true
                });
            }
        },

        goToStep: function (index) {
            if (index < 0 || index >= this.steps.length) {
                this.endTour();
                return;
            }

            this.currentStepIndex = index;
            const step = this.steps[index];

            // Resolve target element
            let targetEl = document.querySelector(step.target);
            if (!targetEl && step.fallbackTarget) {
                targetEl = document.querySelector(step.fallbackTarget);
            }

            // Handle mobile sidebar interaction
            if (step.isSidebar && window.innerWidth < 1024) {
                const sidebar = document.getElementById('sidebar');
                if (sidebar && sidebar.classList.contains('-translate-x-full')) {
                    if (typeof window.toggleMobileSidebar === 'function') {
                        window.toggleMobileSidebar();
                    }
                }
            } else if (!step.isSidebar && window.innerWidth < 1024) {
                const sidebar = document.getElementById('sidebar');
                if (sidebar && !sidebar.classList.contains('-translate-x-full')) {
                    if (typeof window.closeMobileSidebar === 'function') {
                        window.closeMobileSidebar();
                    }
                }
            }

            // Smooth scroll target into view if needed
            if (targetEl) {
                targetEl.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'center' });
            }

            // Render step details in tooltip
            setTimeout(() => {
                this.renderStepContent(step, index);
                this.positionCurrentStep();
            }, 150);
        },

        nextStep: function () {
            if (this.currentStepIndex < this.steps.length - 1) {
                this.goToStep(this.currentStepIndex + 1);
            } else {
                this.endTour();
            }
        },

        prevStep: function () {
            if (this.currentStepIndex > 0) {
                this.goToStep(this.currentStepIndex - 1);
            }
        },

        renderStepContent: function (step, index) {
            const titleEl = document.getElementById('tourTooltipTitle');
            const textEl = document.getElementById('tourTooltipText');
            const badgeEl = document.getElementById('tourStepBadge');
            const btnPrev = document.getElementById('btnTourPrev');
            const btnNext = document.getElementById('btnTourNext');
            const dotsContainer = document.getElementById('tourStepDots');

            if (titleEl) titleEl.innerText = step.title;
            if (textEl) textEl.innerText = step.content;
            if (badgeEl) badgeEl.innerText = `Langkah ${index + 1} dari ${this.steps.length}`;

            if (btnPrev) {
                btnPrev.style.visibility = index === 0 ? 'hidden' : 'visible';
            }

            if (btnNext) {
                if (index === this.steps.length - 1) {
                    btnNext.innerHTML = '<span>Selesai</span> <i class="fas fa-check-circle ml-1"></i>';
                    btnNext.className = 'px-4 py-1.5 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold transition-all shadow-sm flex items-center gap-1';
                } else {
                    btnNext.innerHTML = '<span>Lanjut</span> <i class="fas fa-chevron-right text-[10px] ml-1"></i>';
                    btnNext.className = 'px-4 py-1.5 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-bold transition-all shadow-sm flex items-center gap-1';
                }
            }

            if (dotsContainer) {
                dotsContainer.innerHTML = '';
                for (let i = 0; i < this.steps.length; i++) {
                    const dot = document.createElement('div');
                    dot.className = `tour-progress-dot ${i === index ? 'active' : ''}`;
                    dotsContainer.appendChild(dot);
                }
            }
        },

        positionCurrentStep: function () {
            if (!this.isActive) return;

            const step = this.steps[this.currentStepIndex];
            let targetEl = document.querySelector(step.target);
            if (!targetEl && step.fallbackTarget) {
                targetEl = document.querySelector(step.fallbackTarget);
            }

            const tooltip = document.getElementById('tourTooltipContainer');
            const highlight = document.getElementById('tourTargetHighlight');
            const maskPath = document.getElementById('tourSpotlightPath');

            if (!targetEl || !tooltip) {
                // If target not on current page, position tooltip centered
                if (tooltip) {
                    tooltip.style.top = '50%';
                    tooltip.style.left = '50%';
                    tooltip.style.transform = 'translate(-50%, -50%)';
                    tooltip.setAttribute('data-placement', 'none');
                }
                if (highlight) highlight.style.display = 'none';
                return;
            }

            const rect = targetEl.getBoundingClientRect();
            const padding = 6;
            const targetX = rect.left - padding;
            const targetY = rect.top - padding;
            const targetW = rect.width + padding * 2;
            const targetH = rect.height + padding * 2;

            // 1. Update Spotlight Highlight Ring
            if (highlight) {
                highlight.style.display = 'block';
                highlight.style.top = `${targetY}px`;
                highlight.style.left = `${targetX}px`;
                highlight.style.width = `${targetW}px`;
                highlight.style.height = `${targetH}px`;
            }

            // 2. Update SVG Spotlight Cutout Mask
            if (maskPath) {
                const vw = window.innerWidth;
                const vh = window.innerHeight;
                const r = 14; // corner radius
                const x = Math.max(0, targetX);
                const y = Math.max(0, targetY);
                const w = Math.min(vw, targetW);
                const h = Math.min(vh, targetH);

                // SVG Path cutout: full viewport box outer, inner cutout clockwise
                const d = `M 0 0 L ${vw} 0 L ${vw} ${vh} L 0 ${vh} Z ` +
                          `M ${x + r} ${y} ` +
                          `L ${x + w - r} ${y} Q ${x + w} ${y} ${x + w} ${y + r} ` +
                          `L ${x + w} ${y + h - r} Q ${x + w} ${y + h} ${x + w - r} ${y + h} ` +
                          `L ${x + r} ${y + h} Q ${x} ${y + h} ${x} ${y + h - r} ` +
                          `L ${x} ${y + r} Q ${x} ${y} ${x + r} ${y} Z`;
                maskPath.setAttribute('d', d);
            }

            // 3. Compute Smart Tooltip Position
            const tooltipRect = tooltip.getBoundingClientRect();
            const tooltipW = tooltipRect.width || 360;
            const tooltipH = tooltipRect.height || 220;
            const spacing = 16;
            const vw = window.innerWidth;
            const vh = window.innerHeight;

            let top = 0;
            let left = 0;
            let placement = step.placement || 'bottom';

            // Auto-adjust placement based on available viewport space
            if (placement === 'bottom' && (rect.bottom + tooltipH + spacing > vh) && rect.top > tooltipH + spacing) {
                placement = 'top';
            } else if (placement === 'top' && (rect.top - tooltipH - spacing < 0) && (rect.bottom + tooltipH + spacing < vh)) {
                placement = 'bottom';
            } else if (placement === 'right' && (rect.right + tooltipW + spacing > vw)) {
                placement = 'bottom';
            } else if (placement === 'left' && (rect.left - tooltipW - spacing < 0)) {
                placement = 'bottom';
            }

            if (placement === 'bottom') {
                top = rect.bottom + spacing;
                left = rect.left + (rect.width / 2) - (tooltipW / 2);
            } else if (placement === 'top') {
                top = rect.top - tooltipH - spacing;
                left = rect.left + (rect.width / 2) - (tooltipW / 2);
            } else if (placement === 'right') {
                top = rect.top + (rect.height / 2) - (tooltipH / 2);
                left = rect.right + spacing;
            } else if (placement === 'left') {
                top = rect.top + (rect.height / 2) - (tooltipH / 2);
                left = rect.left - tooltipW - spacing;
            }

            // Boundary constraints (Safe area inside viewport)
            const margin = 12;
            if (left < margin) left = margin;
            if (left + tooltipW > vw - margin) left = vw - tooltipW - margin;
            if (top < margin) top = margin;
            if (top + tooltipH > vh - margin) top = vh - tooltipH - margin;

            tooltip.style.top = `${top}px`;
            tooltip.style.left = `${left}px`;
            tooltip.style.transform = 'none';
            tooltip.setAttribute('data-placement', placement);

            // Align arrow pointer towards target element center
            const arrow = tooltip.querySelector('.tour-tooltip-arrow');
            if (arrow) {
                if (placement === 'bottom' || placement === 'top') {
                    const arrowLeft = Math.max(20, Math.min(tooltipW - 20, (rect.left + rect.width / 2) - left - 7));
                    arrow.style.left = `${arrowLeft}px`;
                    arrow.style.top = '';
                } else {
                    const arrowTop = Math.max(20, Math.min(tooltipH - 20, (rect.top + rect.height / 2) - top - 7));
                    arrow.style.top = `${arrowTop}px`;
                    arrow.style.left = '';
                }
            }
        }
    };

    window.OnboardingTour = OnboardingTour;

})(window, document);
