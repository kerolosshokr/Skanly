// Skanly.Web/wwwroot/js/chart-helpers.js
// ─────────────────────────────────────────────────────────────────────────
// SKANLY CHART HELPERS
// Provides theme-aware Chart.js defaults and a factory function.
// ─────────────────────────────────────────────────────────────────────────
(function () {
    'use strict';

    /* ── Theme-aware palette ─────────────────────────────────────────────── */
    const CHART_COLORS = {
        primary: '#6C63FF',
        success: '#10b981',
        warning: '#f59e0b',
        danger: '#ef4444',
        info: '#06b6d4',
        secondary: '#6b7280',
        purple: '#9c55ff',
    };

    const CHART_ALPHA = {
        fill15: (hex) => hex + '26',  // 15% opacity in hex
        fill30: (hex) => hex + '4D',  // 30%
        fill60: (hex) => hex + '99',  // 60%
    };

    /* ── Apply global Chart.js defaults based on current theme ──────────── */
    function applyChartTheme(dark) {
        if (typeof Chart === 'undefined') return;

        const textColor = dark ? '#94a3b8' : '#6b7280';
        const gridColor = dark ? 'rgba(255,255,255,.06)' : 'rgba(0,0,0,.05)';

        Object.assign(Chart.defaults, {
            color: textColor,
            'scale.grid.color': gridColor,
            'scale.ticks.color': textColor,
            'plugins.legend.labels.color': textColor,
            'plugins.tooltip.backgroundColor':
                dark ? '#1e2433' : '#1f2937',
            'plugins.tooltip.titleColor': '#f8fafc',
            'plugins.tooltip.bodyColor': '#cbd5e1',
            'plugins.tooltip.borderColor':
                dark ? 'rgba(255,255,255,.1)' : 'rgba(0,0,0,.1)',
            'plugins.tooltip.borderWidth': 1,
            'plugins.tooltip.cornerRadius': 8,
            'plugins.tooltip.padding': 10,
        });
    }

    /* ── Factory: create a chart and auto-update on theme change ─────────── */
    window.SkanlyChart = {
        colors: CHART_COLORS,
        alpha: CHART_ALPHA,

        /**
         * Create a Chart.js instance that automatically re-colours itself
         * when the user switches theme.
         */
        create(canvasId, config) {
            const canvas = document.getElementById(canvasId);
            if (!canvas) {
                console.warn(`SkanlyChart.create: canvas #${canvasId} not found`);
                return null;
            }

            // Apply current theme defaults before creating
            const isDark = window.SkanlyTheme?.isDark() ?? false;
            applyChartTheme(isDark);

            const chart = new Chart(canvas.getContext('2d'), config);

            // Re-colour on theme change
            document.addEventListener('skanly:themechange', function (e) {
                applyChartTheme(e.detail.theme === 'dark');
                try { chart.update('none'); } catch { /* chart destroyed */ }
            });

            return chart;
        },

        /**
         * Returns a gradient fill for line chart backgrounds.
         */
        gradient(canvas, colorHex, fromAlpha = 0.3, toAlpha = 0.0) {
            const ctx = canvas.getContext('2d');
            const gradient = ctx.createLinearGradient(
                0, 0, 0, canvas.offsetHeight);
            gradient.addColorStop(0, colorHex + Math.round(fromAlpha * 255)
                .toString(16).padStart(2, '0'));
            gradient.addColorStop(1, colorHex + Math.round(toAlpha * 255)
                .toString(16).padStart(2, '0'));
            return gradient;
        },

        /**
         * Common options shared by all line charts in the app.
         */
        lineOptions(extra = {}) {
            return {
                responsive: true,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: {
                        position: 'top',
                        labels: { boxWidth: 10, usePointStyle: true }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: { color: 'rgba(0,0,0,.05)' },
                        ticks: { maxTicksLimit: 6 }
                    },
                    x: {
                        grid: { display: false },
                        ticks: { maxTicksLimit: 12 }
                    }
                },
                ...extra
            };
        },

        /**
         * Common options for bar charts.
         */
        barOptions(extra = {}) {
            return {
                responsive: true,
                plugins: {
                    legend: { position: 'top', labels: { boxWidth: 10 } }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: { color: 'rgba(0,0,0,.05)' },
                        ticks: { maxTicksLimit: 6 }
                    },
                    x: { grid: { display: false } }
                },
                ...extra
            };
        },

        /**
         * Common options for doughnut charts.
         */
        doughnutOptions(extra = {}) {
            return {
                responsive: true,
                cutout: '65%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { boxWidth: 12, padding: 16 }
                    }
                },
                ...extra
            };
        }
    };

    /* ── Initialise on load ──────────────────────────────────────────────── */
    document.addEventListener('DOMContentLoaded', function () {
        const isDark = window.SkanlyTheme?.isDark() ?? false;
        applyChartTheme(isDark);
    });

})();