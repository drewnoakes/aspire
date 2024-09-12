export function onExtensionLoad(themeName) {
    const iframe = document.getElementById('extension-frame');

    if (iframe == null) {
        // Page isn't loaded yet. Try again when it is.
        document.addEventListener('load', () => {
            onExtensionLoad(themeName);
        });
        return;
    }

    // Listen for messages. Subscribe before we start communicating, so we don't miss anything.
    window.addEventListener('message', (event) => {
        if (event.data.type === 'themeApplied') {
            // The extension has applied its theme and can be displayed.
            // This prevents flicker in the UI.
            iframe.style.display = 'block';

            const spinner = document.getElementById('extension-load-spinner');
            spinner.style.display = 'none';
        }
    });

    // Send the current theme to the extension.
    // Unfortunatley we cannot tell if the iframe has loaded, due to CORS.
    // Therefore we send a message immediately, and also again when the load
    // event fires.
    iframe.contentWindow.postMessage({ type: 'theme', value: themeName }, '*');
    iframe.addEventListener('load', () => {
        iframe.contentWindow.postMessage({ type: 'theme', value: themeName }, '*');
    });
}

export function setTheme(themeName) {
    const iframe = document.getElementById('extension-frame');
    iframe.contentWindow.postMessage({ type: 'theme', value: themeName }, '*');
}
