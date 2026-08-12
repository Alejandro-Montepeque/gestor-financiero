// ═══════════════════════════════════════════════════════════════════════
//  Client-side helper for triggering file downloads from Blazor Server.
//  Called via JSInterop: await JS.InvokeVoidAsync("gfDownload", ...).
// ═══════════════════════════════════════════════════════════════════════

window.gfDownload = function (filename, mimeType, content) {
    // UTF-8 BOM lets Excel open the CSV with the right encoding.
    const bom = '﻿';
    const blob = new Blob([bom + content], { type: mimeType + ';charset=utf-8' });
    const url = URL.createObjectURL(blob);

    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);

    // Free the object URL on next tick so the click has time to fire.
    setTimeout(() => URL.revokeObjectURL(url), 100);
};
