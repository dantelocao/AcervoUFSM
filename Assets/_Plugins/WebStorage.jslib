mergeInto(LibraryManager.library, {
  SetLocalStorage: function (keyPtr, jsonPtr) {
    var key  = UTF8ToString(keyPtr);
    var json = UTF8ToString(jsonPtr);
    try { localStorage.setItem(key, json); }
    catch (e) { console.error("SetLocalStorage error:", e); }
  },

  GetLocalStorage: function (keyPtr) {
    var key = UTF8ToString(keyPtr);
    try {
      var val = localStorage.getItem(key);
      if (val == null) return 0;
      var len = lengthBytesUTF8(val) + 1;
      var ptr = _malloc(len);
      stringToUTF8(val, ptr, len);
      return ptr;
    } catch (e) {
      console.error("GetLocalStorage error:", e);
      return 0;
    }
  },

  DownloadText: function (filenamePtr, contentPtr) {
    var filename = UTF8ToString(filenamePtr);
    var content  = UTF8ToString(contentPtr);
    try {
      var blob = new Blob([content], {type: 'application/json'});
      var a = document.createElement('a');
      a.href = URL.createObjectURL(blob);
      a.download = filename;
      a.style.display = 'none';
      document.body.appendChild(a);
      a.click();
      setTimeout(function(){ URL.revokeObjectURL(a.href); a.remove(); }, 0);
    } catch (e) {
      console.error("DownloadText error:", e);
    }
  }
});
