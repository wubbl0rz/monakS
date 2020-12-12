<template>
  <v-app v-cloak>
    <v-main>
      <router-view></router-view>
    </v-main>
  </v-app>
</template>

<script>
import { HubConnectionBuilder } from "@microsoft/signalr";

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
