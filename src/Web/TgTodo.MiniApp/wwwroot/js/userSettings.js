window.tgTodoSettings = {
    defaults: function () {
        return {
            primaryColor: '#5B7FFF',
            themeMode: 'system',
            useTelegramColors: true,
            timeZoneId: null
        };
    },
    normalizeThemeMode: function (mode) {
        if (mode === 1 || mode === '1') return 'light';
        if (mode === 2 || mode === '2') return 'dark';
        if (mode === 0 || mode === '0') return 'system';
        if (typeof mode === 'string') {
            var m = mode.toLowerCase();
            if (m === 'light' || m === 'dark' || m === 'system') return m;
        }
        return 'system';
    },
    load: function () {
        try {
            var raw = localStorage.getItem('tgtodo_settings');
            var parsed = raw ? JSON.parse(raw) : {};
            var merged = Object.assign(this.defaults(), parsed);
            merged.themeMode = this.normalizeThemeMode(merged.themeMode);
            return merged;
        } catch (e) {
            return this.defaults();
        }
    },
    loadJson: function () {
        return localStorage.getItem('tgtodo_settings') || JSON.stringify(this.defaults());
    },
    save: function (settings) {
        var merged = Object.assign(this.defaults(), settings || {});
        localStorage.setItem('tgtodo_settings', JSON.stringify(merged));
        if (merged.primaryColor) {
            localStorage.setItem('tgtodo_primaryColor', merged.primaryColor);
        }
    },
    saveJson: function (json) {
        localStorage.setItem('tgtodo_settings', json);
        try {
            var s = JSON.parse(json);
            if (s.primaryColor) {
                localStorage.setItem('tgtodo_primaryColor', s.primaryColor);
            }
        } catch (e) { /* ignore */ }
    }
};
