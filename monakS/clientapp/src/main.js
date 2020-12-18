import Vue from 'vue'
import App from './App.vue'
import router from './router'
import vuetify from './plugins/vuetify';
import adapter from 'webrtc-adapter';
import { HubConnectionBuilder } from "@microsoft/signalr";
import { createProvider } from './vue-apollo'

Vue.config.productionTip = false

let SERVER_URL = process.env.NODE_ENV == "development" ? "http://127.0.0.1:5000/" : "/";

Vue.prototype.SERVER_URL = SERVER_URL;

let SIGNAL_R = new HubConnectionBuilder()
  .withUrl(SERVER_URL + "msg")
  .build();

Vue.prototype.SIGNAL_R = SIGNAL_R;

Vue.prototype.ADAPTER = adapter;

Vue.prototype.WEBRTC_CONNECT = async (cam) => {
  return new Promise(async (resolve, reject) => {
    let pc = new RTCPeerConnection();

    pc.ontrack = async (t) => {
      t.track.onunmute = () => {
        let stream = t.streams[0];
        resolve(stream);
      };
    };

    pc.onicecandidate = async (e) => {
      if (e.candidate) {
        await SIGNAL_R.invoke("setCandidate", session_id, e.candidate);
      }
    };

    let session_id = await SIGNAL_R.invoke("getSessionId", cam);

    let offer_sdp = await SIGNAL_R.invoke("getOffer", session_id);

    await pc.setRemoteDescription({
      type: "offer",
      sdp: offer_sdp,
    });

    let answer = await pc.createAnswer();
    await pc.setLocalDescription(answer);

    let answer_sdp = pc.localDescription.sdp;
    await SIGNAL_R.invoke("setAnswer", session_id, answer_sdp);
  });
}

Vue.directive("hold", {
  bind: function (el, binding) {
    if (typeof binding.value === "function") {
      let ms = binding.modifiers ? Object.keys(binding.modifiers)[0] : 500;
      var manager = new Hammer.Manager(el);
      var Press = new Hammer.Press({ time: ms });
      manager.add(Press);
      manager.on('press', binding.value);
    }
  }
});

async function main() {
  await SIGNAL_R.stop();
  await SIGNAL_R.start();

  new Vue({
    router,
    vuetify,
    apolloProvider: createProvider(),
    render: h => h(App)
  }).$mount('#app')
}

main();
