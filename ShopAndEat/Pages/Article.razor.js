const scrollBehavior = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth';

export function focusEditor() {
    const input = document.querySelector('.article-item-editing .article-form-name');
    if (!input) {
        return;
    }

    input.focus({ preventScroll: true });
    input.select();
    input.scrollIntoView({ block: 'center', behavior: scrollBehavior() });
}

export function revealArticle(articleId) {
    const row = document.querySelector(`[data-testid="article-row-${articleId}"]`);
    if (!row) {
        return;
    }

    row.scrollIntoView({ block: 'nearest', behavior: scrollBehavior() });
    row.classList.remove('article-item-highlight');
    row.classList.add('article-item-highlight');
    row.addEventListener('animationend', () => row.classList.remove('article-item-highlight'), { once: true });
}
