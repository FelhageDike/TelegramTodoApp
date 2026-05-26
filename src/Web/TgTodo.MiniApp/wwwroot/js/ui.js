window.tgTodoUi = {
    scrollToCenter: function (container, selector) {
        if (!container) return;
        var el = typeof selector === 'string'
            ? container.querySelector(selector)
            : selector;
        if (!el) return;
        var left = el.offsetLeft - (container.clientWidth - el.offsetWidth) / 2;
        container.scrollTo({ left: Math.max(0, left), behavior: 'smooth' });
    },

    scrollCalDayIntoView: function (isoDate) {
        var strip = document.querySelector('.week-calendar-strip');
        if (!strip) return;
        var el = strip.querySelector('[data-date="' + isoDate + '"]');
        this.scrollToCenter(strip, el);
    },

    scrollKanbanToDay: function (day) {
        var board = document.querySelector('.kanban-board');
        if (!board) return;
        var col = board.querySelector('.kanban-col[data-day="' + day + '"]');
        this.scrollToCenter(board, col);
    },

    initHorizontalScroll: function () {
        document.querySelectorAll('.h-scroll').forEach(function (el) {
            if (el.dataset.scrollInit === '1') return;
            el.dataset.scrollInit = '1';

            var startX = 0;
            var startY = 0;
            var moved = false;

            el.addEventListener('touchstart', function (e) {
                if (!e.touches.length) return;
                startX = e.touches[0].clientX;
                startY = e.touches[0].clientY;
                moved = false;
            }, { passive: true });

            el.addEventListener('touchmove', function (e) {
                if (!e.touches.length) return;
                var dx = Math.abs(e.touches[0].clientX - startX);
                var dy = Math.abs(e.touches[0].clientY - startY);
                if (dx > 10 && dx > dy * 1.2) moved = true;
            }, { passive: true });

            el.addEventListener('click', function (e) {
                if (moved) {
                    e.preventDefault();
                    e.stopImmediatePropagation();
                    moved = false;
                }
            }, true);
        });
    }
};
