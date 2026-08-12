// ═══════════════════════════════════════════════════════════════════════
//  Client-side helpers for the auth pages. Runs in pure JS so it works
//  with static SSR pages (Login, Register, ResetPassword, etc.).
// ═══════════════════════════════════════════════════════════════════════

(function () {
    'use strict';

    // ── Show/hide password toggle ──────────────────────────────────────
    // Any button with [data-toggle-password="<input-id>"] flips that
    // input between type=password and type=text and swaps the icon.
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-toggle-password]');
        if (!btn) return;
        e.preventDefault();

        const targetId = btn.getAttribute('data-toggle-password');
        const input = document.getElementById(targetId);
        if (!input) return;

        const showing = input.type === 'text';
        input.type = showing ? 'password' : 'text';
        btn.setAttribute('aria-label', showing ? 'Mostrar contraseña' : 'Ocultar contraseña');
        btn.classList.toggle('is-showing', !showing);
    });

    // ── Password strength meter ────────────────────────────────────────
    // Any input with [data-strength-meter] gets scored on every input
    // event. The score (0-4) drives a sibling .gf-strength-bar element.
    function scorePassword(pw) {
        if (!pw) return 0;
        let score = 0;
        if (pw.length >= 8)  score++;
        if (pw.length >= 12) score++;
        if (/[a-z]/.test(pw) && /[A-Z]/.test(pw)) score++;
        if (/\d/.test(pw))                        score++;
        if (/[^A-Za-z0-9]/.test(pw))              score++;
        return Math.min(4, score);
    }

    const LABELS = ['', 'Muy débil', 'Débil', 'Aceptable', 'Fuerte', 'Excelente'];
    const CLASSES = ['gf-strength-0', 'gf-strength-1', 'gf-strength-2', 'gf-strength-3', 'gf-strength-4'];

    function updateMeter(input) {
        const container = input.closest('.gf-strength-wrap');
        if (!container) return;

        const bar = container.querySelector('.gf-strength-bar');
        const label = container.querySelector('.gf-strength-label');
        if (!bar || !label) return;

        const score = scorePassword(input.value);
        CLASSES.forEach(c => bar.classList.remove(c));
        bar.classList.add(CLASSES[score]);

        label.textContent = input.value ? LABELS[score + 1] || '' : '';
    }

    document.addEventListener('input', function (e) {
        const input = e.target;
        if (input.matches && input.matches('[data-strength-meter]')) {
            updateMeter(input);
        }
    });
})();
