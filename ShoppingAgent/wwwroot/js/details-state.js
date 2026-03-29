// Preserves <details> open/closed state across Blazor re-renders.
// Blazor replaces innerHTML on each StateHasChanged(), which resets all <details> to closed.
(function () {
    const openDetails = new Set();
    let isRestoring = false;

    document.addEventListener('toggle', function (e) {
        if (isRestoring || e.target.tagName !== 'DETAILS') return;
        const key = getKey(e.target);
        if (!key) return;
        if (e.target.open) {
            openDetails.add(key);
        } else {
            openDetails.delete(key);
        }
    }, true);

    window.restoreDetailsStates = function () {
        if (openDetails.size === 0) return;
        isRestoring = true;
        document.querySelectorAll('#chatMessages details').forEach(function (d) {
            var key = getKey(d);
            if (key && openDetails.has(key)) {
                d.open = true;
            }
        });
        isRestoring = false;
    };

    window.clearDetailsStates = function () {
        openDetails.clear();
    };

    function getKey(details) {
        var summary = details.querySelector(':scope > summary');
        return summary ? summary.textContent.trim() : null;
    }
})();
