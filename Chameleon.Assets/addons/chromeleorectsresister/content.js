const ikey = "clientrects-defender-sandboxed-frame";

if (document.documentElement.getAttribute(ikey) === null) {
  parent.postMessage(ikey, '*');
  window.top.postMessage(ikey, '*');
} else {
  document.documentElement.removeAttribute(ikey);
}