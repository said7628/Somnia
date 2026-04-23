mergeInto(LibraryManager.library, {
  SomniaExitToMainSite: function () {
    try {
      if (window.parent && window.parent.SomniaBridge && typeof window.parent.SomniaBridge.exitToMainSite === 'function') {
        window.parent.SomniaBridge.exitToMainSite();
        return;
      }
    } catch (e) {
    }

    try {
      if (window.parent && typeof window.parent.postMessage === 'function') {
        window.parent.postMessage('somnia:exit', '*');
        return;
      }
    } catch (e) {
    }

    try {
      if (typeof window.postMessage === 'function') {
        window.postMessage({ type: 'SOMNIA_EXIT' }, '*');
        return;
      }
    } catch (e) {
    }

    try {
      if (typeof window.postMessage === 'function') {
        window.postMessage({ action: 'exit_game' }, '*');
      }
    } catch (e) {
    }
  }
});