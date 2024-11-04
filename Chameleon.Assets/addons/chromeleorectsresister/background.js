var app = {};

app.error = function () {
  return chrome.runtime.lastError;
};

app.name = function () {
  return chrome.runtime.getManifest().name;
};

app.notifications = {
  "create": function (e, callback) {
    if (chrome.notifications) {
      chrome.notifications.create(app.notifications.id, {
        "type": e.type ? e.type : "basic",
        "message": e.message ? e.message : '',
        "title": e.title ? e.title : "Notifications",
        "iconUrl": e.iconUrl ? chrome.runtime.getURL(e.iconUrl) : chrome.runtime.getURL("data/icons/64.png")
      }, function (e) {
        if (callback) callback(e);
      });
    }
  }
};

app.popup = {
  "port": null,
  "message": {},
  "receive": function (id, callback) {
    if (id) {
      app.popup.message[id] = callback;
    }
  },
  "send": function (id, data) {
    if (id) {
      chrome.runtime.sendMessage({"data": data, "method": id, "path": "background-to-popup"}, app.error);
    }
  },
  "post": function (id, data) {
    if (id) {
      if (app.popup.port) {
        app.popup.port.postMessage({"data": data, "method": id, "path": "background-to-popup"});
      }
    }
  }
};

app.contextmenu = {
  "create": function (options, callback) {
    if (chrome.contextMenus) {
      chrome.contextMenus.create(options, function (e) {
        if (callback) callback(e);
      });
    }
  },
  "update": function (id, options, callback) {
    if (chrome.contextMenus) {
      chrome.contextMenus.update(id, options, function (e) {
        if (callback) callback(e);
      });
    }
  },
  "on": {
    "clicked": function (callback) {
      if (chrome.contextMenus) {
        chrome.contextMenus.onClicked.addListener(function (info, tab) {
          app.storage.load(function () {
            callback(info, tab);
          });
        });
      }
    }
  }
};

app.tab = {
  "query": {
    "index": function (callback) {
      chrome.tabs.query({"active": true, "currentWindow": true}, function (tabs) {
        let tmp = chrome.runtime.lastError;
        if (tabs && tabs.length) {
          callback(tabs[0].index);
        } else callback(undefined);
      });
    }
  },
  "open": function (url, index, active, callback) {
    let properties = {
      "url": url, 
      "active": active !== undefined ? active : true
    };
    /*  */
    if (index !== undefined) {
      if (typeof index === "number") {
        properties.index = index + 1;
      }
    }
    /*  */
    chrome.tabs.create(properties, function (tab) {
      if (callback) callback(tab);
    }); 
  }
};

app.storage = {
  "local": {},
  "read": function (id) {
    return app.storage.local[id];
  },
  "update": function (callback) {
    if (app.session) app.session.load();
    /*  */
    chrome.storage.local.get(null, function (e) {
      app.storage.local = e;
      if (callback) {
        callback("update");
      }
    });
  },
  "write": function (id, data, callback) {
    let tmp = {};
    tmp[id] = data;
    app.storage.local[id] = data;
    /*  */
    chrome.storage.local.set(tmp, function (e) {
      if (callback) {
        callback(e);
      }
    });
  },
  "load": function (callback) {
    const keys = Object.keys(app.storage.local);
    if (keys && keys.length) {
      if (callback) {
        callback("cache");
      }
    } else {
      app.storage.update(function () {
        if (callback) callback("disk");
      });
    }
  } 
};

app.on = {
  "management": function (callback) {
    chrome.management.getSelf(callback);
  },
  "uninstalled": function (url) {
    chrome.runtime.setUninstallURL(url, function () {});
  },
  "installed": function (callback) {
    chrome.runtime.onInstalled.addListener(function (e) {
      app.storage.load(function () {
        callback(e);
      });
    });
  },
  "startup": function (callback) {
    chrome.runtime.onStartup.addListener(function (e) {
      app.storage.load(function () {
        callback(e);
      });
    });
  },
  "connect": function (callback) {
    chrome.runtime.onConnect.addListener(function (e) {
      app.storage.load(function () {
        if (callback) callback(e);
      });
    });
  },
  "storage": function (callback) {
    chrome.storage.onChanged.addListener(function (changes, namespace) {
      app.storage.update(function () {
        if (callback) {
          callback(changes, namespace);
        }
      });
    });
  },
  "message": function (callback) {
    chrome.runtime.onMessage.addListener(function (request, sender, sendResponse) {
      app.storage.load(function () {
        callback(request, sender, sendResponse);
      });
      /*  */
      return true;
    });
  }
};

app.page = {
  "port": null,
  "message": {},
  "sender": {
    "port": {}
  },
  "receive": function (id, callback) {
    if (id) {
      app.page.message[id] = callback;
    }
  },
  "post": function (id, data, tabId) {
    if (id) {
      if (tabId) {
        if (app.page.sender.port[tabId]) {
          app.page.sender.port[tabId].postMessage({"data": data, "method": id, "path": "background-to-page"});
        }
      } else if (app.page.port) {
        app.page.port.postMessage({"data": data, "method": id, "path": "background-to-page"});
      }
    }
  },
  "send": function (id, data, tabId, frameId) {
    if (id) {
      chrome.tabs.query({}, function (tabs) {
        let tmp = chrome.runtime.lastError;
        if (tabs && tabs.length) {
          let message = {
            "method": id, 
            "data": data ? data : {}, 
            "path": "background-to-page"
          };
          /*  */
          tabs.forEach(function (tab) {
            if (tab) {
              message.data.tabId = tab.id;
              message.data.top = tab.url ? tab.url : '';
              message.data.title = tab.title ? tab.title : '';
              /*  */
              if (tabId !== null && tabId !== undefined) {
                if (tabId === tab.id) {
                  if (frameId !== null && frameId !== undefined) {
                    chrome.tabs.sendMessage(tab.id, message, {"frameId": frameId}, app.error);
                  } else {
                    chrome.tabs.sendMessage(tab.id, message, app.error);
                  }
                }
              } else {
                chrome.tabs.sendMessage(tab.id, message, app.error);
              }
            }
          });
        }
      });
    }
  }
};


//
app.version = function () {return chrome.runtime.getManifest().version};
app.homepage = function () {return chrome.runtime.getManifest().homepage_url};

// if (!navigator.webdriver) {
//   app.on.uninstalled(app.homepage() + "?v=" + app.version() + "&type=uninstall");
//   app.on.installed(function (e) {
//     app.on.management(function (result) {
//       if (result.installType === "normal") {
//         app.tab.query.index(function (index) {
//           let previous = e.previousVersion !== undefined && e.previousVersion !== app.version();
//           let doupdate = previous && parseInt((Date.now() - config.welcome.lastupdate) / (24 * 3600 * 1000)) > 45;
//           if (e.reason === "install" || (e.reason === "update" && doupdate)) {
//             let parameter = (e.previousVersion ? "&p=" + e.previousVersion : '') + "&type=" + e.reason;
//             let url = app.homepage() + "?v=" + app.version() + parameter;
//             app.tab.open(url, index, e.reason === "install");
//             config.welcome.lastupdate = Date.now();
//           }
//         });
//       }
//     });
//   });
// }

app.on.message(function (request, sender) {
  if (request) {
    if (request.path === "popup-to-background") {
      for (let id in app.popup.message) {
        if (app.popup.message[id]) {
          if ((typeof app.popup.message[id]) === "function") {
            if (id === request.method) {
              app.popup.message[id](request.data);
            }
          }
        }
      }
    }
    /*  */
    if (request.path === "page-to-background") {
      for (let id in app.page.message) {
        if (app.page.message[id]) {
          if ((typeof app.page.message[id]) === "function") {
            if (id === request.method) {
              let a = request.data || {};
              if (sender) {
                a.frameId = sender.frameId;
                /*  */
                if (sender.tab) {
                  if (a.tabId === undefined) a.tabId = sender.tab.id;
                  if (a.title === undefined) a.title = sender.tab.title ? sender.tab.title : '';
                  if (a.top === undefined) a.top = sender.tab.url ? sender.tab.url : (sender.url ? sender.url : '');
                }
              }
              /*  */
              app.page.message[id](a);
            }
          }
        }
      }
    }
  }
});

app.on.connect(function (port) {
  if (port) {
    if (port.name) {
      if (port.name in app) {
        app[port.name].port = port;
      }
    }
    /*  */
    port.onDisconnect.addListener(function (e) {
      app.storage.load(function () {
        if (e) {
          if (e.name) {
            if (e.name in app) {
              app[e.name].port = null;
            }
          }
        }
      });
    });
    /*  */
    port.onMessage.addListener(function (e) {
      app.storage.load(function () {
        if (e) {
          if (e.path) {
            if (e.port) {
              if (e.port in app) {
                if (e.path === (e.port + "-to-background")) {
                  for (let id in app[e.port].message) {
                    if (app[e.port].message[id]) {
                      if ((typeof app[e.port].message[id]) === "function") {
                        if (id === e.method) {
                          app[e.port].message[id](e.data);
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      });
    });
  }
});



var core = {
    "start": function () {
      core.load();
    },
    "install": function () {
      core.load();
    },
    "load": function () {
      app.contextmenu.create({
        "type": "normal",
        "id": "test.page",
        "contexts": ["action"],
        "title": "What is my Fingerprint?"
      }, app.error);
      /*  */
      app.contextmenu.create({
        "type": "checkbox",
        "contexts": ["action"],
        "id": "notification.checkbox",
        "checked": config.notification.show,
        "title": "Show Desktop Notifications"
      }, app.error);
    },
    "action": {
      "storage": function (changes, namespace) {
        if ("notification" in changes) {
          app.contextmenu.update("notification.checkbox", {
            "checked": config.notification.show,
          }, app.error);
        }
      },
      "contextmenu": function (e) {
        if (e.menuItemId === "test.page") {
          app.tab.open(config.test.page);
        } else {
          config.notification.show = !config.notification.show;
        }
      },
      "popup": {
        "load": function () {
          app.popup.send("storage", {
            "notifications": config.notification.show
          });
        },
        "notifications": function () {
          config.notification.show = !config.notification.show;
          app.popup.send("storage", {
            "notifications": config.notification.show
          });
        },
        "fingerprint": function (e) {
          const message = "\nA fingerprinting attempt is detected!\nYour browser is reporting a fake value.";
          /*  */
          if (config.notification.show) {
            if (config.notification.timeout) clearTimeout(config.notification.timeout);
            config.notification.timeout = setTimeout(function () {
              app.notifications.create({
                "type": "basic",
                "title": app.name(),
                "message": e.host + message
              });
            }, 1000);
          }
        }
      }
    }
  };
  
  app.contextmenu.on.clicked(core.action.contextmenu);
  app.page.receive("fingerprint", core.action.popup.fingerprint);
  
  app.popup.receive("load", core.action.popup.load);
  app.popup.receive("notifications", core.action.popup.notifications);
  app.popup.receive("support", function () {app.tab.open(app.homepage())});
  app.popup.receive("fingerprint", function () {app.tab.open(config.test.page)});
  app.popup.receive("donation", function () {app.tab.open(app.homepage() + "?reason=support")});
  
  app.on.startup(core.start);
  app.on.installed(core.install);
  app.on.storage(core.action.storage);



//
var config = {};

config.test = {"page": "https://webbrowsertools.com/clientrects-fingerprint/"};

config.welcome = {
  set lastupdate (val) {app.storage.write("lastupdate", val)},
  get lastupdate () {return app.storage.read("lastupdate") !== undefined ? app.storage.read("lastupdate") : 0}
};

config.notification = {
  "timeout": null,
  set show (val) {app.storage.write("notification", val)},
  get show () {return app.storage.read("notification") !== undefined ? app.storage.read("notification") : false}
};