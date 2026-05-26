window.tgTodoAppState = {
    getSelectedGroup: function () {
        const id = sessionStorage.getItem('tgtodo_groupId');
        const name = sessionStorage.getItem('tgtodo_groupName');
        if (!id) return null;
        return { id: id, name: name || '' };
    },
    setSelectedGroup: function (id, name) {
        if (!id) {
            sessionStorage.removeItem('tgtodo_groupId');
            sessionStorage.removeItem('tgtodo_groupName');
            return;
        }
        sessionStorage.setItem('tgtodo_groupId', id);
        sessionStorage.setItem('tgtodo_groupName', name || '');
    }
};
