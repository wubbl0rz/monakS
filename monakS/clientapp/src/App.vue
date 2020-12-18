<template>
  <v-app v-cloak>
    <v-main>
      <router-view></router-view>
    </v-main>
  </v-app>
</template>

<script>
import { HubConnectionBuilder } from "@microsoft/signalr";

var is_touch = "ontouchstart" in document.documentElement;
var hidden, visibilityChange;
if (typeof document.hidden !== "undefined") {
  hidden = "hidden";
  visibilityChange = "visibilitychange";
} else if (typeof document.msHidden !== "undefined") {
  hidden = "msHidden";
  visibilityChange = "msvisibilitychange";
} else if (typeof document.webkitHidden !== "undefined") {
  hidden = "webkitHidden";
  visibilityChange = "webkitvisibilitychange";
}

var marker = false;

var clearId = setInterval(() => {
  let state = document[hidden];
  if (state) {
    console.log(document[hidden]);
    marker = true;
  }
  if (marker && is_touch && !state) {
    clearInterval(clearId);
    location.reload();
  }
}, 300);

export default {
  name: "App",

  async mounted() {
    console.log(process.env.NODE_ENV);
    console.log(this.SERVER_URL);
  },

  data: () => ({}),
};
</script>

<style>
html::-webkit-scrollbar {
  width: 0;
  height: 0;
}

*::-webkit-scrollbar {
  width: 0;
  height: 0;
}

[v-cloak] {
  display: none;
}
</style>
