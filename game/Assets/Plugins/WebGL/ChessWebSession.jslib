mergeInto(LibraryManager.library, {
  $ChessSessionStorage: { observed: {} },
  ChessSession_Read__deps: ["$ChessSessionStorage"],
  ChessSession_Read: function (key) {
    var result;
    try {
      var name = UTF8ToString(key);
      var value = window.localStorage.getItem(name);
      // Remember the raw value, including null versus an existing empty string.
      // A successful storage read establishes the comparison baseline even when
      // the value is too large or invalid for the C# save schema.
      ChessSessionStorage.observed[name] = { value: value };
      // Bound the bridge allocation as well as the C# parser. An empty value is
      // an existing invalid save, so it must not be mistaken for a missing key.
      var exists = value !== null;
      result = value !== null && value.length > 65536
        ? { ok: false, hasSave: true, reason: "save-too-large", value: "" }
        : { ok: true, hasSave: exists, reason: "", value: value || "" };
    } catch (error) {
      result = { ok: false, hasSave: false, reason: "storage-unavailable", value: "" };
    }
    var json = JSON.stringify(result);
    var size = lengthBytesUTF8(json) + 1;
    var buffer = _malloc(size);
    stringToUTF8(json, buffer, size);
    return buffer;
  },
  ChessSession_Write__deps: ["$ChessSessionStorage"],
  ChessSession_Write: function (key, value) {
    try {
      var name = UTF8ToString(key);
      var next = UTF8ToString(value);
      var storage = window.localStorage;
      var current = storage.getItem(name);
      if (current === next) {
        ChessSessionStorage.observed[name] = { value: current };
        return 0;
      }
      if (!Object.prototype.hasOwnProperty.call(ChessSessionStorage.observed, name) ||
          current !== ChessSessionStorage.observed[name].value) return 3;
      // setItem is atomic: a quota error preserves the previous complete save.
      // Do not advance the baseline until the write actually succeeds.
      storage.setItem(name, next);
      ChessSessionStorage.observed[name] = { value: next };
      return 0;
    } catch (error) {
      return error && (error.name === "QuotaExceededError" || error.name === "NS_ERROR_DOM_QUOTA_REACHED") ? 1 : 2;
    }
  },
  ChessSession_Notify: function (name, ok, hasSave, reason) {
    var eventName = UTF8ToString(name);
    var detail = { ok: ok === 1, reason: UTF8ToString(reason) };
    if (eventName !== "cicochess:resume") detail.hasSave = hasSave === 1;
    window.dispatchEvent(new CustomEvent(eventName, { detail: detail }));
  }
});
