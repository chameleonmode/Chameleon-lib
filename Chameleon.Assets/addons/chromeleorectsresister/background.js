chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
    if (changeInfo.status === 'loading' && /^http/.test(tab.url)) {

            chrome.scripting.executeScript({
                target: { allFrames: true, tabId: chrome.devtools.inspectedWindow.tabId },
                files: ["injected.js"],
            });
     
    }
});
