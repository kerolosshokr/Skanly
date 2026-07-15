// Skanly.Web/wwwroot/js/rtl-helpers.js
// Included in _Layout.cshtml for all pages

(function () {
    'use strict';

    // Detect RTL from <html dir>
    const isRtl = document.documentElement.dir === 'rtl';

    // Expose globally
    window.SkanlyRtl = {
        isRtl,
        isArabic: isRtl,
        dir: isRtl ? 'rtl' : 'ltr',

        /**
         * Flips left/right CSS property names for RTL.
         * e.g. skanlyRtl.side('left') → 'right' in Arabic
         */
        side(ltrSide) {
            if (!isRtl) return ltrSide;
            return ltrSide === 'left' ? 'right' : 'left';
        },

        /**
         * Returns correct text-align value for the current direction.
         */
        textAlign(ltrAlign = 'left') {
            if (!isRtl) return ltrAlign;
            return ltrAlign === 'left' ? 'right' : 'left';
        },

        /**
         * Flips a pixel offset for RTL.
         * Used in map overlays, tooltips, etc.
         */
        offset(containerWidth, elementWidth, ltrOffset) {
            if (!isRtl) return ltrOffset;
            return containerWidth - elementWidth - ltrOffset;
        }
    };

    // Auto-fix: reverse flex rows that need RTL flipping
    // (Bootstrap .flex-row stays LTR by default in RTL mode)
    if (isRtl) {
        // Google Maps requires LTR
        document.querySelectorAll('.gm-style').forEach(el => {
            el.dir = 'ltr';
        });

        // Chart.js canvases stay LTR
        document.querySelectorAll('canvas').forEach(el => {
            el.style.direction = 'ltr';
        });

        // Number inputs stay LTR
        document.querySelectorAll('input[type="number"]').forEach(el => {
            el.style.direction = 'ltr';
            el.style.textAlign = 'right';
        });

        // Phone inputs stay LTR
        document.querySelectorAll('input[type="tel"]').forEach(el => {
            el.dir = 'ltr';
        });
    }

})();