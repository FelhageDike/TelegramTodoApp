window.tgTodoTheme = {
    _darkPalette: {
        primaryColor: '#5B7FFF',
        background: '#0f172a',
        surface: '#1e293b',
        text: '#f1f5f9',
        textMuted: '#94a3b8'
    },
    _lightPalette: {
        primaryColor: '#5B7FFF',
        background: '#f3f4f8',
        surface: '#ffffff',
        text: '#1e293b',
        textMuted: '#64748b'
    },

    getPrimaryColor: function () {
        var s = window.tgTodoSettings?.load?.() || {};
        return s.primaryColor || localStorage.getItem('tgtodo_primaryColor') || '#5B7FFF';
    },

    setPrimaryColor: function (color) {
        if (!color) return;
        var s = window.tgTodoSettings.load();
        s.primaryColor = color;
        window.tgTodoSettings.save(s);
    },

    _parseRgb: function (color) {
        if (!color) return null;
        var c = String(color).trim();
        if (c.charAt(0) === '#') {
            var h = c.slice(1);
            if (h.length === 3) {
                h = h[0] + h[0] + h[1] + h[1] + h[2] + h[2];
            }
            if (h.length !== 6) return null;
            return {
                r: parseInt(h.slice(0, 2), 16),
                g: parseInt(h.slice(2, 4), 16),
                b: parseInt(h.slice(4, 6), 16)
            };
        }
        return null;
    },

    _isDarkColor: function (color) {
        var rgb = this._parseRgb(color);
        if (!rgb) return false;
        var l = (0.299 * rgb.r + 0.587 * rgb.g + 0.114 * rgb.b) / 255;
        return l < 0.45;
    },

    _getTelegram: function () {
        var tg = window.Telegram?.WebApp;
        if (!tg) return null;
        try {
            tg.ready();
            tg.expand();
        } catch (e) { /* ignore */ }
        return tg;
    },

    getTelegramColorScheme: function () {
        var tg = this._getTelegram();
        if (!tg) return null;
        if (tg.colorScheme === 'dark' || tg.colorScheme === 'light') {
            return tg.colorScheme;
        }
        var bg = tg.themeParams?.bg_color || tg.themeParams?.secondary_bg_color;
        if (bg) {
            return this._isDarkColor(bg) ? 'dark' : 'light';
        }
        return null;
    },

    _normalizeMode: function (mode) {
        if (window.tgTodoSettings?.normalizeThemeMode) {
            return window.tgTodoSettings.normalizeThemeMode(mode);
        }
        if (mode === 1 || mode === '1') return 'light';
        if (mode === 2 || mode === '2') return 'dark';
        if (typeof mode === 'string') return mode.toLowerCase();
        return 'system';
    },

    resolveIsDark: function (forcedMode) {
        var mode = forcedMode
            ? this._normalizeMode(forcedMode)
            : this._normalizeMode((window.tgTodoSettings?.load?.() || {}).themeMode);

        if (mode === 'dark') return true;
        if (mode === 'light') return false;

        var tgScheme = this.getTelegramColorScheme();
        if (tgScheme === 'dark') return true;
        if (tgScheme === 'light') return false;

        try {
            return window.matchMedia('(prefers-color-scheme: dark)').matches;
        } catch (e) {
            return false;
        }
    },

    resolvePalette: function (isDark, forcedMode) {
        var base = isDark ? this._darkPalette : this._lightPalette;
        var palette = Object.assign({}, base);
        palette.primaryColor = this.getPrimaryColor();

        var s = window.tgTodoSettings?.load?.() || {};
        var mode = forcedMode ? this._normalizeMode(forcedMode) : this._normalizeMode(s.themeMode);
        var tg = this._getTelegram();
        if (s.useTelegramColors === true && mode === 'system' && tg?.themeParams) {
            var p = tg.themeParams;
            if (p.button_color) palette.primaryColor = p.button_color;
            if (p.bg_color) palette.background = p.bg_color;
            if (p.secondary_bg_color) palette.surface = p.secondary_bg_color;
            if (p.text_color) palette.text = p.text_color;
            if (p.hint_color) palette.textMuted = p.hint_color;
        }

        return palette;
    },

    applyTheme: function (opts) {
        var isDark = !!opts.isDark;
        var root = document.documentElement;
        var primary = opts.primaryColor || this.getPrimaryColor();
        var background = opts.background || (isDark ? '#0f172a' : '#f3f4f8');
        var surface = opts.surface || (isDark ? '#1e293b' : '#ffffff');
        var text = opts.text || (isDark ? '#f1f5f9' : '#1e293b');
        var textMuted = opts.textMuted || (isDark ? '#94a3b8' : '#64748b');

        root.style.setProperty('--tgtodo-primary', primary);
        root.style.setProperty('--tgtodo-bg', background);
        root.style.setProperty('--tgtodo-surface', surface);
        root.style.setProperty('--tgtodo-text', text);
        root.style.setProperty('--tgtodo-text-muted', textMuted);

        document.body.style.background = background;
        document.body.style.color = text;

        var app = document.getElementById('app');
        if (app) app.style.background = background;

        var targets = '.app-shell, .app-main, .mud-layout, .mud-main-content, .home-calendar, .home-tasks, .home-block';
        document.querySelectorAll(targets).forEach(function (el) {
            el.style.backgroundColor = background;
            el.style.color = text;
        });

        if (isDark) {
            document.body.classList.add('tgtodo-dark');
            document.documentElement.classList.add('tgtodo-dark');
            document.body.classList.remove('tgtodo-light');
            document.documentElement.classList.remove('tgtodo-light');
        } else {
            document.body.classList.remove('tgtodo-dark');
            document.documentElement.classList.remove('tgtodo-dark');
            document.body.classList.add('tgtodo-light');
            document.documentElement.classList.add('tgtodo-light');
        }

        var tg = this._getTelegram();
        if (tg?.setHeaderColor && tg?.setBackgroundColor) {
            try {
                tg.setHeaderColor(background);
                tg.setBackgroundColor(background);
            } catch (e) { /* ignore */ }
        }
    },

    bootstrap: function () {
        var s = window.tgTodoSettings?.load?.() || {};
        var mode = this._normalizeMode(s.themeMode);
        var isDark = this.resolveIsDark(mode);
        var palette = this.resolvePalette(isDark, mode);
        this.applyTheme({
            isDark: isDark,
            primaryColor: palette.primaryColor,
            background: palette.background,
            surface: palette.surface,
            text: palette.text,
            textMuted: palette.textMuted,
            themeMode: mode
        });
        return isDark;
    },

    applyPrimaryColor: function (color) {
        this.applyTheme({
            primaryColor: color || this.getPrimaryColor(),
            isDark: this.resolveIsDark(),
            background: document.documentElement.style.getPropertyValue('--tgtodo-bg'),
            surface: document.documentElement.style.getPropertyValue('--tgtodo-surface'),
            text: document.documentElement.style.getPropertyValue('--tgtodo-text'),
            textMuted: document.documentElement.style.getPropertyValue('--tgtodo-text-muted')
        });
    }
};
