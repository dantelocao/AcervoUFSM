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
  },

  // ===== NOVO: abre o seletor de arquivos e envia o conteúdo .json para o C# =====
  OpenFilePicker: function (goNamePtr, cbNamePtr) {
    try {
      var goName = UTF8ToString(goNamePtr);
      var cbName = UTF8ToString(cbNamePtr);

      var input = document.createElement('input');
      input.type = 'file';
      input.accept = '.json,application/json';

      input.onchange = function (e) {
        var file = e.target.files && e.target.files[0];
        if (!file) {
          // Nada selecionado: notifica o C# com string vazia
          try { SendMessage(goName, cbName, ""); } catch (_) {}
          return;
        }

        var reader = new FileReader();
        reader.onload = function (evt) {
          try {
            var json = evt.target.result || "";
            SendMessage(goName, cbName, json);
          } catch (e2) {
            console.error("FileReader onload error:", e2);
            try { SendMessage(goName, cbName, ""); } catch (_) {}
          }
        };
        reader.onerror = function (err) {
          console.error("FileReader error:", err);
          try { SendMessage(goName, cbName, ""); } catch (_) {}
        };
        reader.readAsText(file);
      };

      input.style.display = 'none';
      document.body.appendChild(input);
      input.click();
      setTimeout(function(){ input.remove(); }, 0);
    } catch (e) {
      console.error("OpenFilePicker error:", e);
      // Em caso de erro, ainda chamamos o callback para não deixar o lado C# pendurado
      try {
        var goNameFallback = UTF8ToString(goNamePtr);
        var cbNameFallback = UTF8ToString(cbNamePtr);
        SendMessage(goNameFallback, cbNameFallback, "");
      } catch (_) {}
    }
  }
});
