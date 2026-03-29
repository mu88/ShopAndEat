// Resizable splitter between chat messages and input area
(function () {
    function initSplitter() {
        const splitter = document.getElementById('chatSplitter');
        const inputArea = document.getElementById('chatInputArea');
        if (!splitter || !inputArea) {
            return;
        }

        // Restore saved height
        const savedHeight = localStorage.getItem('chatInputHeight');
        if (savedHeight) {
            inputArea.style.height = savedHeight;
            inputArea.style.flexShrink = '0';
            inputArea.style.flexGrow = '0';
        }

        let startY = 0;
        let startHeight = 0;

        function onPointerDown(e) {
            startY = e.clientY;
            startHeight = inputArea.getBoundingClientRect().height;
            splitter.classList.add('dragging');
            document.body.style.cursor = 'row-resize';
            document.body.style.userSelect = 'none';
            document.addEventListener('pointermove', onPointerMove);
            document.addEventListener('pointerup', onPointerUp);
            e.preventDefault();
        }

        function onPointerMove(e) {
            const delta = startY - e.clientY;
            const newHeight = Math.max(120, startHeight + delta);
            inputArea.style.height = newHeight + 'px';
            inputArea.style.flexShrink = '0';
            inputArea.style.flexGrow = '0';
        }

        function onPointerUp() {
            splitter.classList.remove('dragging');
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
            document.removeEventListener('pointermove', onPointerMove);
            document.removeEventListener('pointerup', onPointerUp);
            localStorage.setItem('chatInputHeight', inputArea.style.height);
        }

        splitter.addEventListener('pointerdown', onPointerDown);
    }

    // Initialize when DOM is ready, and re-initialize after Blazor renders
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSplitter);
    } else {
        initSplitter();
    }

    // Re-init on Blazor enhanced navigation
    const observer = new MutationObserver(function () {
        if (document.getElementById('chatSplitter') && !document.getElementById('chatSplitter')._initialized) {
            document.getElementById('chatSplitter')._initialized = true;
            initSplitter();
        }
    });
    observer.observe(document.body, { childList: true, subtree: true });
})();
