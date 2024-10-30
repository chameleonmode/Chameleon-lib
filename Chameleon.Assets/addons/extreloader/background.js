function reloadExtensionsUsingApi() {
    // find all unpacked extensions and reload them
    chrome.management.getAll(function (a) {
        var ext = {};
        for (var i = 0; i < a.length; i++) {
            ext = a[i];
            if ((ext.name !== 'Chromeleon Extension Reloader') &&  // don't reload yourself
                (ext.installType == "development") &&
                (ext.enabled == true) &&
                (ext.name != "Extensions Reloader")) {
                console.log(ext.name + " reloaded");
                (function (extensionId, extensionType) {
                    // disable
                    chrome.management.setEnabled(extensionId, false, function () {
                        // re-enable
                        chrome.management.setEnabled(extensionId, true, function () {
                            // re-launch packaged app
                            if (extensionType == "packaged_app") {
                                chrome.management.launchApp(extensionId);
                            }
                        });
                    });
                })(ext.id, ext.type);
            }
        }
    });
}
reloadExtensionsUsingApi();