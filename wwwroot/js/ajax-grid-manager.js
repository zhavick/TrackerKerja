/**
 * Work Tracker Pro - AJAX Table Grid & Pagination Manager
 * Enables zero-reload pagination, sorting, search/filter, and page-size changes
 * with full CSS/theme preservation and browser history pushState integration.
 */
(function (window, $) {
    'use strict';

    const AjaxGridManager = {
        defaultSelector: '#ajaxGridContainer',
        activeRequests: {},

        /**
         * Initialize global listeners for AJAX table grids
         */
        init: function () {
            const self = this;

            // 1. Intercept pagination links inside any ajax-grid container
            $(document).on('click', '[data-ajax-grid="true"] a, #ajaxGridContainer a', function (e) {
                const $link = $(this);
                const href = $link.attr('href');

                // Skip non-navigational links
                if (!href || href === '#' || href.startsWith('javascript:') || href.startsWith('mailto:') || href.startsWith('tel:')) {
                    return;
                }

                // Skip target="_blank", downloads, exports, or explicit no-ajax links
                if ($link.attr('target') === '_blank' || 
                    $link.attr('download') !== undefined || 
                    $link.hasClass('no-ajax') || 
                    $link.attr('data-no-ajax') === 'true' ||
                    href.includes('/Export') || 
                    href.includes('/Detail') || 
                    href.includes('/Edit') || 
                    href.includes('/Create') ||
                    href.includes('/Delete') ||
                    href.includes('/Kanban')) {
                    return;
                }

                // Check if the link is a pagination or sorting link for the current page
                const currentPath = window.location.pathname.toLowerCase();
                const linkUrl = new URL(href, window.location.origin);

                if (linkUrl.pathname.toLowerCase() === currentPath || 
                    (currentPath === '/' && linkUrl.pathname.toLowerCase() === '/task')) {
                    e.preventDefault();
                    const containerSelector = $link.closest('[data-ajax-grid="true"], #ajaxGridContainer').attr('id') 
                        ? '#' + $link.closest('[data-ajax-grid="true"], #ajaxGridContainer').attr('id') 
                        : self.defaultSelector;
                    
                    self.loadUrl(linkUrl.href, containerSelector, true);
                }
            });

            // 2. Intercept filter and search forms
            $(document).on('submit', 'form[data-ajax-grid="true"], form[data-ajax-filter="true"]', function (e) {
                e.preventDefault();
                const $form = $(this);
                const action = $form.attr('action') || window.location.pathname;
                const formUrl = new URL(action, window.location.origin);
                const formData = new FormData(this);

                for (const [key, value] of formData.entries()) {
                    if (value !== null && value !== undefined && value !== '') {
                        formUrl.searchParams.set(key, value);
                    }
                }
                // Reset page to 1 when applying new filters
                formUrl.searchParams.set('page', '1');

                const targetContainer = $form.attr('data-target-grid') || self.defaultSelector;
                self.loadUrl(formUrl.href, targetContainer, true);
            });

            // 3. Handle browser Back/Forward navigation (popstate)
            window.addEventListener('popstate', function (event) {
                self.loadUrl(window.location.href, self.defaultSelector, false);
            });

            // 4. Initial lifecycle hook on page load
            self.rebindComponents($(self.defaultSelector));
        },

        /**
         * Load given URL and hot-swap grid table container
         */
        loadUrl: function (url, containerSelector, pushHistory) {
            const self = this;
            const selector = containerSelector || self.defaultSelector;
            const $container = $(selector);

            if (!$container.length) {
                // Fallback to normal navigation if container not present
                window.location.href = url;
                return;
            }

            // Abort previous pending request for this container if any
            if (self.activeRequests[selector]) {
                self.activeRequests[selector].abort();
            }

            // Show loading state
            self.showLoading($container);

            const xhr = $.ajax({
                url: url,
                type: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'X-Ajax-Grid': 'true'
                },
                success: function (data, status, req) {
                    try {
                        const $parsed = $('<div>').append($.parseHTML(data, document, true));
                        let $newContent = $parsed.find(selector);

                        if (!$newContent.length) {
                            $newContent = $parsed.filter(selector);
                        }

                        if ($newContent.length) {
                            // Smoothly replace the inner content
                            $container.html($newContent.html());
                            
                            // Re-apply any attributes from new container if modified
                            $container.removeClass('is-loading');
                            $container.addClass('ajax-grid-fade-enter');
                            setTimeout(function () {
                                $container.removeClass('ajax-grid-fade-enter');
                            }, 300);

                            // Update secondary elements if present (e.g. counters or title badges)
                            self.syncSecondaryElements($parsed);

                            // Update browser URL
                            if (pushHistory !== false && window.location.href !== url) {
                                window.history.pushState({ ajaxGrid: true, url: url }, '', url);
                            }

                            // Re-bind interactive components inside new DOM
                            self.rebindComponents($container);

                            // Trigger custom events
                            $(document).trigger('ajaxGridUpdated', [url, selector]);
                            document.dispatchEvent(new CustomEvent('ajaxGridUpdated', { detail: { url: url, selector: selector } }));
                        } else {
                            // Fallback to full reload if response cannot be parsed
                            window.location.href = url;
                        }
                    } catch (err) {
                        console.error('Error during AJAX grid DOM swap:', err);
                        window.location.href = url;
                    }
                },
                error: function (xhr, status, error) {
                    if (status !== 'abort') {
                        console.warn('AJAX grid request failed, falling back to full navigation:', error);
                        window.location.href = url;
                    }
                },
                complete: function () {
                    self.hideLoading($container);
                    delete self.activeRequests[selector];
                }
            });

            self.activeRequests[selector] = xhr;
        },

        /**
         * Update specific query parameters and reload grid
         */
        updateQueryParam: function (params, containerSelector) {
            const currentUrl = new URL(window.location.href);
            Object.keys(params).forEach(function (key) {
                if (params[key] === null || params[key] === undefined || params[key] === '') {
                    currentUrl.searchParams.delete(key);
                } else {
                    currentUrl.searchParams.set(key, params[key]);
                }
            });
            this.loadUrl(currentUrl.href, containerSelector || this.defaultSelector, true);
        },

        /**
         * Show subtle loading overlay
         */
        showLoading: function ($container) {
            $container.addClass('is-loading');
            let $overlay = $container.find('.ajax-grid-loading-overlay');
            if (!$overlay.length) {
                $overlay = $(`
                    <div class="ajax-grid-loading-overlay">
                        <div class="ajax-grid-spinner mb-3"></div>
                        <span class="text-xs font-bold text-slate-600 dark:text-slate-300">Memuat data...</span>
                    </div>
                `);
                $container.append($overlay);
            }
        },

        /**
         * Hide loading overlay
         */
        hideLoading: function ($container) {
            $container.removeClass('is-loading');
            $container.find('.ajax-grid-loading-overlay').remove();
        },

        /**
         * Sync secondary counters or filter state across the page
         */
        syncSecondaryElements: function ($parsed) {
            ['#taskCountBadge', '#totalItemsBadge', '#selectedCountBadge', '#taskSummaryCards'].forEach(function (sel) {
                const $source = $parsed.find(sel);
                const $target = $(sel);
                if ($source.length && $target.length) {
                    $target.html($source.html());
                }
            });
        },

        /**
         * Re-initialize interactive components (Select2, tooltips, checkboxes, timers)
         */
        rebindComponents: function ($context) {
            // 1. Re-sync bulk selection toolbar if function exists
            if (typeof window.updateBulkActionBar === 'function') {
                window.updateBulkActionBar();
            }

            // 2. Re-initialize Select2 if present
            if (typeof $.fn.select2 === 'function') {
                $context.find('.select2').each(function () {
                    if (!$(this).hasClass('select2-hidden-accessible')) {
                        $(this).select2({
                            width: '100%',
                            theme: 'classic'
                        });
                    }
                });
            }

            // 3. Re-bind quick tooltips or popovers
            if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
                $context.find('[data-bs-toggle="tooltip"]').each(function () {
                    new bootstrap.Tooltip(this);
                });
            }

            // 4. Fire custom rebind event for page-specific handlers
            $(document).trigger('ajaxGrid:rebound', [$context]);
        }
    };

    // Global expose
    window.AjaxGridManager = AjaxGridManager;

    // Auto-init on DOM ready
    $(function () {
        AjaxGridManager.init();
    });

})(window, window.jQuery || window.$);
