
///// Inject the `injected.js` script file into the page context
//chrome.runtime.sendMessage({ action: "getInjectionStatus" }, (shouldInject) => {
//        chrome.scripting.executeScript({
//            target: { allFrames: true, tabId: chrome.devtools.inspectedWindow.tabId },
//            files: ["injected.js"],
//        });
//});