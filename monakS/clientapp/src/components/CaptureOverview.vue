<template>
  <div>
    <video
      @stalled="stop"
      @ended="stop"
      playsinline
      muted
      class="mt-5"
      ref="player"
      @progress="progress"
    ></video>

    <div class="captureList">
      <v-row
        v-show="captures.length == 0"
        class="mt-2"
        style="height: 40px"
        align="center"
        justify="center"
      >
        <div class="text-h6 text-center">
          Captured camera footage will be listed here.
        </div>
      </v-row>

      <v-list three-line>
        <template v-for="(capture, i) in captures">
          <div :key="i">
            <v-divider></v-divider>
            <v-list-item>
              <v-btn @click="play(capture)" class="mr-5" color="primary">
                <v-icon v-if="active != capture">mdi-play</v-icon>
                <v-icon v-else-if="active == capture && !playing"
                  >mdi-play-pause</v-icon
                >
                <v-icon v-else>mdi-pause</v-icon>
              </v-btn>

              <v-list-item-content>
                <v-list-item-title>
                  {{ new Date(capture.start).toLocaleString() }}
                  (
                  {{
                    Math.round(
                      (new Date(capture.end) - new Date(capture.start)) / 1000
                    )
                  }}
                  s )
                </v-list-item-title>
                <v-list-item-subtitle>
                  <div>
                    {{ capture.trigger }}
                  </div>
                </v-list-item-subtitle>
              </v-list-item-content>
            </v-list-item>
          </div>
        </template>
      </v-list>
    </div>
  </div>
</template>

<script>
import gql from "graphql-tag";

export default {
  name: "CaptureOverview",
  props: {
    cam: Object,
  },
  apollo: {
    $subscribe: {
      onActiveCapturesChanged: {
        query: gql`
          subscription($id: Int!) {
            onActiveCapturesChanged(where: { cam: { id: { eq: $id } } }) {
              isActive
              trigger
              name
              start
              end
              cam {
                id
              }
            }
          }
        `,
        async result({ data }) {
          this.captures = data.onActiveCapturesChanged;
        },
        variables() {
          return {
            id: this.cam.id,
          };
        },
      },
    },
  },
  computed: {},
  methods: {
    progress(e) {
      console.log(e);
    },
    stop(e) {
      this.active = null;
      this.playing = false;
    },
    play(capture) {
      let player = this.$refs.player;
      let src = this.SERVER_URL + "captures/" + capture.name;

      if (this.active != capture) {
        player.src = src;
        this.active = capture;
        this.activeSrc = src;
        this.playing = false;
      }

      if (this.playing) {
        player.pause();
        this.playing = false;
      } else {
        player.play();
        this.playing = true;
      }
    },
  },
  data: () => ({
    active: null,
    activeSrc: "",
    playing: false,
    captures: [],
  }),
};
</script>

<style scoped>
video {
  background: black;
  padding-top: 36px;
  z-index: 1;
  position: fixed;
  width: 100%;
  height: 35vh;
}

video:focus {
  outline: none;
}

.captureList {
  padding-top: calc(35vh + 20px);
}
</style>
