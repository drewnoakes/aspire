export function onExtensionLoad(themeName) {
    const iframe = document.getElementById('extension-frame');

    if (iframe == null) {
        // Page isn't loaded yet. Try again when it is.
        document.addEventListener('load', () => {
            console.log('postponing setting of theme, as page isn\'t loaded yet');
            onExtensionLoad(themeName);
        });
        return;
    }

    // Hide the UI until the theme is applied.
    iframe.style.display = 'none';

    // Listen for themeApplied and make the UI visible when it arrives.
    window.addEventListener('message', (event) => {
        if (event.data.type === 'themeApplied') {
            iframe.style.display = 'block';
        }
    });

    // Send the current theme to the extension.
    iframe.addEventListener('load', () => {
        iframe.contentWindow.postMessage({ type: 'theme', value: themeName }, '*');
    });
}

export function setTheme(themeName) {
    const iframe = document.getElementById('extension-frame');
    iframe.contentWindow.postMessage({ type: 'theme', value: themeName }, '*');
}
