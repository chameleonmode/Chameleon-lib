{
  const noiseLevels = {
    micro: 0.0000001,
    mini: 0.0000002,
    low: 0.0000003,
    medium: 0.0000004,
    bold: 0.0000005,
    high: 0.0000006,
    ultra: 0.0000007,
    super: 0.0000008,
    max: 0.0000009,
  };

  const analyzerLevels = {
    micro: 0.1,
    mini: 0.2,
    low: 0.3,
    medium: 0.4,
    bold: 0.5,
    high: 0.6,
    ultra: 0.7,
    super: 0.8,
    max: 0.9,
  };

  const context = {
    BUFFER: null,
    getChannelData: function (e) {
      e.prototype.getChannelData = new Proxy(e.prototype.getChannelData, {
        apply(target, self, args) {
          const results_1 = Reflect.apply(target, self, args);
          //
          if (context.BUFFER !== results_1) {
            context.BUFFER = results_1;
            //
            for (let i = 0; i < results_1.length; i += 100) {
              if (settings.randomAudioSpoofing === true) {
                let index = Math.floor(Math.random() * i);
                results_1[index] = results_1[index] + Math.random() * noiseLevels[settings.noiseLevel];
              }else{
                let index = Math.floor(i);
                results_1[index] = results_1[index] + noiseLevels[settings.noiseLevel];
              }
            }
          }
          //
          return results_1;
        },
      });
    },
    createAnalyser: function (e) {
      e.prototype.__proto__.createAnalyser = new Proxy(
        e.prototype.__proto__.createAnalyser,
        {
          apply(target, self, args) {
            const results_2 = Reflect.apply(target, self, args);
            //
            results_2.__proto__.getFloatFrequencyData = new Proxy(
              results_2.__proto__.getFloatFrequencyData,
              {
                apply(target, self, args) {
                  const results_3 = Reflect.apply(target, self, args);
                  //
                  for (let i = 0; i < arguments[0].length; i += 100) {
                    if (settings.randomAudioSpoofing === true) {
                    let index = Math.floor(Math.random() * i);
                    arguments[0][index] = arguments[0][index] + Math.random() * analyzerLevels[settings.noiseLevel];
                    } else {
                      let index = Math.floor(i);
                      arguments[0][index] = arguments[0][index] + analyzerLevels[settings.noiseLevel];
                    }
                  }
                  //
                  return results_3;
                },
              }
            );
            //
            return results_2;
          },
        }
      );
    },
  };

  {
    const mkey = "cffjcbnflngjpnjenjogeaojacooflng-sandboxed-audio";
    document.documentElement.setAttribute(mkey, "");
    //
    window.addEventListener(
      "message",
      async function (e) {
        if (e.data && e.data.key === mkey) {
          e.preventDefault();
          e.stopPropagation();
          if (settings.audioSpoofing === false || settings.enabled === false) {
            return;
          }
          //
          context.getChannelData(AudioBuffer);
          context.createAnalyser(AudioContext);
          context.createAnalyser(OfflineAudioContext);
          //
          if (e.source) {
            if (e.source.AudioBuffer) {
              if (e.source.AudioBuffer.prototype) {
                if (e.source.AudioBuffer.prototype.getChannelData) {
                  e.source.AudioBuffer.prototype.getChannelData =
                    AudioBuffer.prototype.getChannelData;
                }
              }
            }
            //
            if (e.source.AudioContext) {
              if (e.source.AudioContext.prototype) {
                if (e.source.AudioContext.prototype.__proto__) {
                  if (
                    e.source.AudioContext.prototype.__proto__.createAnalyser
                  ) {
                    e.source.AudioContext.prototype.__proto__.createAnalyser =
                      AudioContext.prototype.__proto__.createAnalyser;
                  }
                }
              }
            }
            //
            if (e.source.OfflineAudioContext) {
              if (e.source.OfflineAudioContext.prototype) {
                if (e.source.OfflineAudioContext.prototype.__proto__) {
                  if (
                    e.source.OfflineAudioContext.prototype.__proto__
                      .createAnalyser
                  ) {
                    e.source.OfflineAudioContext.prototype.__proto__.createAnalyser =
                      OfflineAudioContext.prototype.__proto__.createAnalyser;
                  }
                }
              }
            }
            //
            if (e.source.OfflineAudioContext) {
              if (e.source.OfflineAudioContext.prototype) {
                if (e.source.OfflineAudioContext.prototype.__proto__) {
                  if (
                    e.source.OfflineAudioContext.prototype.__proto__
                      .getChannelData
                  ) {
                    e.source.OfflineAudioContext.prototype.__proto__.getChannelData =
                      OfflineAudioContext.prototype.__proto__.getChannelData;
                  }
                }
              }
            }
          }
        }
      },
      false
    );
  }
}
