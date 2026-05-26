window.tgTodoTime = {
    getDeviceTimeZone: function () {
        try {
            return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
        } catch (e) {
            return 'UTC';
        }
    },
    getEffectiveTimeZone: function (timeZoneId) {
        if (timeZoneId && String(timeZoneId).trim()) return String(timeZoneId).trim();
        return this.getDeviceTimeZone();
    },
    getToday: function (timeZoneId) {
        var tz = this.getEffectiveTimeZone(timeZoneId);
        try {
            return new Date().toLocaleDateString('en-CA', { timeZone: tz });
        } catch (e) {
            return new Date().toLocaleDateString('en-CA');
        }
    }
};
