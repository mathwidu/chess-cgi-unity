mergeInto(LibraryManager.library, {
  ChessWebPublishState: function (json) {
    var panel = document.getElementById('qa-state');
    if (panel) panel.textContent = UTF8ToString(json);
  }
});
