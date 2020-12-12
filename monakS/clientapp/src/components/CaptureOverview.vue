<template>
  <div>
    <video controls muted class="mt-5" ref="player"></video>

    <v-list class="captureList" three-line>
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
              <v-list-item-title v-html="capture.name"></v-list-item-title>
              <v-list-item-subtitle>
                <v-expansion-panels>
                  <v-expansion-panel>
                    <v-expansion-panel-header>{{
                      capture.trigger
                    }}</v-expansion-panel-header>
                    <v-expansion-panel-content>
                      Start: {{ capture }}
                    </v-expansion-panel-content>
                  </v-expansion-panel>
                </v-expansion-panels>
              </v-list-item-subtitle>
            </v-list-item-content>
          </v-list-item>
        </div>
      </template>
    </v-list>
  </div>
</template>

<script>
import gql from "graphql-tag";

export default {
  name: "CaptureOVerview",
  props: {
    cam: Object,
  },
  apollo: {
    captures: {
      query: gql`
        query($id: Int!) {
          captures(
            where: { cam: { id: { eq: $id } } }
            order: { isActive: DESC }
          ) {
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
      subscribeToMore: {
        document: gql`
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
        variables() {
          return {
            id: this.cam.id,
          };
        },
        updateQuery: function (previousResult, { subscriptionData }) {
          let captures = subscriptionData.data.onActiveCapturesChanged;
          captures = captures.filter((c) => c.cam.id == this.cam.id);

          return {
            captures: captures,
          };
        },
      },
      variables() {
        return {
          id: this.cam.id,
        };
      },
      result(data) {
        let captures = data.data.captures;
      },
    },
  },
  computed: {},
  methods: {
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
  height: 40vh;
}

video:focus {
  outline: none;
}

.captureList {
  padding-top: calc(40vh + 20px);
}
</style>
