// Skanly.Web/wwwroot/js/theme-manager.js
// ─────────────────────────────────────────────────────────────────────────
// SKANLY THEME MANAGER
// Handles theme detection, persistence, switching, and system sync.
// Loaded as a module so it does not pollute the global namespace beyond
// the intentional window.SkanlyTheme export.
// ─────────────────────────────────────────────────────────────────────────
(function () {
    'use strict';

    /* ── Constants ─────────────────────────────────────────────────────── */
    const STORAGE_KEY = 'skanly-theme';      // localStorage key
    const THEME_LIGHT = 'light';
    const THEME_DARK = 'dark';
    const THEME_SYSTEM = 'system';            // follow OS preference
    const HTML_EL = document.documentElement;
    const THEME_ATTR = 'data-bs-theme';

    /* ── Detect system preference ───────────────────────────────────────── */
    const prefersDark = () =>
        window.matchMedia('(prefers-color-scheme: dark)').matches;

    /* ── Resolve "system" to an actual theme ─────────────────────────────── */
    function resolveTheme(preference) {
        if (preference === THEME_SYSTEM)
            return prefersDark() ? THEME_DARK : THEME_LIGHT;
        return preference === THEME_DARK ? THEME_DARK : THEME_LIGHT;
    }

    /* ── Read persisted preference ─────────────────────────────────────── */
    function getPreference() {
        try {
            return localStorage.getItem(STORAGE_KEY) || THEME_SYSTEM;
        } catch {
            return THEME_SYSTEM;
        }
    }

    /* ── Persist preference ─────────────────────────────────────────────── */
    function savePreference(pref) {
        try {
            localStorage.setItem(STORAGE_KEY, pref);
        } catch { /* private/incognito mode */ }
    }

    /* ── Apply theme to DOM ─────────────────────────────────────────────── */
    function applyTheme(theme) {
        // Apply to <html> — Bootstrap reads this attribute
        HTML_EL.setAttribute(THEME_ATTR, theme);

        // Reflect on <meta name="color-scheme"> for browser chrome
        let meta = document.querySelector('meta[name="color-scheme"]');
        if (!meta) {
            meta = document.createElement('meta');
            meta.name = 'color-scheme';
            document.head.appendChild(meta);
        }
        meta.content = theme === THEME_DARK ? 'dark' : 'light';

        // Update all toggle buttons across the page
        syncToggleButtons(theme);

        // Re-color Chart.js charts (if any are rendered)
        syncCharts(theme);

        // Fire a custom event so other scripts can react
        document.dispatchEvent(new CustomEvent('skanly:themechange', {
            detail: { theme }
        }));
    }

    /* ── Sync all toggle button icons ──────────────────────────────────── */
    function syncToggleButtons(theme) {
        document.querySelectorAll('[data-skanly-theme-toggle]')
            .forEach(btn => {
                const icon = btn.querySelector('[data-theme-icon]');
                const label = btn.querySelector('[data-theme-label]');

                if (icon) {
                    icon.className = theme === THEME_DARK
                        ? 'fas fa-sun'
                        : 'fas fa-moon';
                }

                if (label) {
                    label.textContent = theme === THEME_DARK
                        ? 'Light Mode'
                        : 'Dark Mode';
                }

                btn.setAttribute('aria-label',
                    theme === THEME_DARK
                        ? 'Switch to light mode'
                        : 'Switch to dark mode');
            });

        // Sync the theme-selector radio/select inputs
        document.querySelectorAll('[data-theme-option]')
            .forEach(el => {
                const pref = getPreference();
                if (el.tagName === 'INPUT' && el.type === 'radio') {
                    el.checked = el.value === pref;
                }
                if (el.tagName === 'OPTION') {
                    el.selected = el.value === pref;
                }
            });
    }

    /* ── Update Chart.js color scheme ────────────────────────────────────── */
    function syncCharts(theme) {
        if (typeof Chart === 'undefined') return;

        const textColor = theme === THEME_DARK ? '#94a3b8' : '#6b7280';
        const gridColor = theme === THEME_DARK
            ? 'rgba(255,255,255,.06)'
            : 'rgba(0,0,0,.05)';

        Chart.defaults.color = textColor;
        Chart.defaults.scale.grid.color = gridColor;
        Chart.defaults.scale.ticks.color = textColor;
        Chart.defaults.plugins.legend.labels.color = textColor;
        Chart.defaults.plugins.tooltip.backgroundColor =
            theme === THEME_DARK ? '#1e2433' : '#1f2937';
        Chart.defaults.plugins.tooltip.titleColor = '#f1f5f9';
        Chart.defaults.plugins.tooltip.bodyColor = '#cbd5e1';
        Chart.defaults.plugins.tooltip.borderColor =
            theme === THEME_DARK
                ? 'rgba(255,255,255,.1)'
                : 'rgba(0,0,0,.1)';
        Chart.defaults.plugins.tooltip.borderWidth = 1;

        // Re-render existing charts
        Object.values(Chart.instances || {}).forEach(chart => {
            try { chart.update('none'); } catch { /* skip */ }
        });
    }

    /* ── Toggle between light and dark ──────────────────────────────────── */
    function toggle() {
        const current = HTML_EL.getAttribute(THEME_ATTR) || THEME_LIGHT;
        const next = current === THEME_DARK ? THEME_LIGHT : THEME_DARK;
        savePreference(next);
        applyTheme(next);
        return next;
    }

    /* ── Set a specific preference ───────────────────────────────────────── */
    function setPreference(pref) {
        // pref can be "light" | "dark" | "system"
        savePreference(pref);
        applyTheme(resolveTheme(pref));
    }

    /* ── Initialise on DOM ready ─────────────────────────────────────────── */
    function init() {
        const pref = getPreference();
        const theme = resolveTheme(pref);
        applyTheme(theme);

        // Watch system preference changes (if user chose "system")
        window.matchMedia('(prefers-color-scheme: dark)')
            .addEventListener('change', () => {
                if (getPreference() === THEME_SYSTEM) {
                    applyTheme(resolveTheme(THEME_SYSTEM));
                }
            });

        // Wire all toggle buttons
        document.addEventListener('click', function (e) {
            const btn = e.target.closest('[data-skanly-theme-toggle]');
            if (btn) {
                e.preventDefault();
                toggle();
            }
        });

        // Wire theme selector (three-way: light / dark / system)
        document.addEventListener('change', function (e) {
            const el = e.target.closest('[data-theme-option]');
            if (el) {
                setPreference(el.value);
            }
        });
    }

    /* ── Public API ─────────────────────────────────────────────────────── */
    window.SkanlyTheme = {
        toggle,
        setPreference,
        getPreference,
        getCurrentTheme: () =>
            HTML_EL.getAttribute(THEME_ATTR) || THEME_LIGHT,
        isDark: () =>
            HTML_EL.getAttribute(THEME_ATTR) === THEME_DARK,
        LIGHT: THEME_LIGHT,
        DARK: THEME_DARK,
        SYSTEM: THEME_SYSTEM,
    };

    /* ── Boot ────────────────────────────────────────────────────────────── */
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();