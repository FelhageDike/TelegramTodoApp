window.tgTodoTelegram = {
    enableGestures: function () {
        const tg = window.Telegram?.WebApp;
        if (!tg) return;
        try {
            tg.ready();
            tg.expand();
            if (typeof tg.disableVerticalSwipes === 'function') {
                tg.disableVerticalSwipes();
            }
        } catch (e) { /* ignore */ }
    },
    getInitData: function () {
        const tg = window.Telegram?.WebApp;
        if (!tg) return null;
        this.enableGestures();
        return tg.initData || null;
    },
    getTheme: function () {
        if (window.tgTodoTheme?.getTelegramColorScheme) {
            var scheme = window.tgTodoTheme.getTelegramColorScheme();
            if (scheme) return scheme;
        }
        var tg = window.Telegram?.WebApp;
        if (tg) {
            try { tg.ready(); } catch (e) { /* ignore */ }
            if (tg.colorScheme) return tg.colorScheme;
        }
        try {
            return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        } catch (e) {
            return 'light';
        }
    },
    getThemeParams: function () {
        const tg = window.Telegram?.WebApp;
        if (!tg?.themeParams) return null;
        const p = tg.themeParams;
        return {
            colorScheme: tg.colorScheme || 'light',
            bgColor: p.bg_color || null,
            secondaryBgColor: p.secondary_bg_color || null,
            textColor: p.text_color || null,
            hintColor: p.hint_color || null,
            linkColor: p.link_color || null,
            buttonColor: p.button_color || null,
            buttonTextColor: p.button_text_color || null
        };
    },
    onThemeChanged: function (dotNetRef) {
        const tg = window.Telegram?.WebApp;
        if (!tg?.onEvent) return;
        tg.onEvent('themeChanged', function () {
            dotNetRef.invokeMethodAsync('OnTelegramThemeChanged');
        });
    },
    getUser: function () {
        const u = window.Telegram?.WebApp?.initDataUnsafe?.user;
        if (!u) return null;
        return {
            firstName: u.first_name || '',
            lastName: u.last_name || '',
            photoUrl: u.photo_url || null,
            username: u.username || null
        };
    }
};
