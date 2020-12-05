import Vue from 'vue'
import App from './App.vue'
import router from './router'
import vuetify from './plugins/vuetify';
import { HubConnectionBuilder } from "@microsoft/signalr";
import { createProvider } from './vue-apollo'

Vue.config.productionTip = false

let SERVER_URL = process.env.NODE_ENV == "development" ? "http://127.0.0.1:5000/" : "/";

Vue.prototype.SERVER_URL = SERVER_URL;

let SIGNAL_R = new HubConnectionBuilder()
  .withUrl(SERVER_URL + "msg")
  .build();

Vue.prototype.SIGNAL_R = SIGNAL_R;

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
