<template>
  <div style="position: relative">
    <div
      style="top: 5%; left: 50%"
      v-show="overlay"
      class="headline font-weight-bold overlay"
    >
      {{ cam.name }}
    </div>

    <span
      v-show="cam.isObjectDetectionEnabled"
      class="overlay"
      style="top: 5%; right: 0px"
    >
      <v-icon color="orange">mdi-motion-sensor</v-icon>
    </span>

    <DetectionCanvas :cam="cam" />

    <span
      v-show="motionCaptureActive || timerCaptureActive || manualCaptureActive"
      class="overlay"
      style="top: 5%; right: 40px"
    >
      <v-icon :color="manualCaptureActive ? 'red' : 'orange'"
        >mdi-record-circle</v-icon
      >
    </span>

    <v-btn
      v-show="overlay"
      :color="manualCaptureActive ? 'red' : captureActive ? 'orange' : 'green'"
      dark
      fab
      absolute
      left
      v-bind="btnSize"
      style="bottom: 20px"
      @click="toggleCapture(cam)"
    >
      <v-icon>mdi-camera</v-icon>
    </v-btn>

    <v-btn
      v-show="overlay"
      color="primary"
      dark
      fab
      absolute
      left
      style="bottom: 20px; left: 90px"
      @click="$emit('show-files')"
    >
      <v-icon>mdi-folder-search</v-icon>
    </v-btn>

    <v-overlay absolute :value="loading">
      <v-progress-circular indeterminate size="64"></v-progress-circular>
    </v-overlay>

    <video
      v-hold.500="
        () => {
          if (isInteractive) overlay = !overlay;
        }
      "
      v-hold.1500="
        () => {
          longPress();
        }
      "
      @loadeddata="playing"
      ref="player"
      autoplay
      muted
      playsinline
      :src-object.prop.camel="stream"
    ></video>
  </div>
</template>

<script>
import DetectionCanvas from "@/components/DetectionCanvas.vue";
import gql from "graphql-tag";

export default {
  name: "VideoPlayer",
  props: {
    stream: MediaStream,
    isInteractive: {
      default: true,
      type: Boolean,
    },
    cam: Object,
  },
  components: {
    DetectionCanvas,
  },

  apollo: {
    captures: {
      query: gql`
        query($id: Int!) {
          captures(
            where: { isActive: { eq: true }, cam: { id: { eq: $id } } }
          ) {
            isActive
            trigger
            name
            cam {
              id
            }
          }
        }
      `,
      subscribeToMore: {
        document: gql`
          subscription($id: Int!) {
            onActiveCapturesChanged(
              where: { isActive: { eq: true }, cam: { id: { eq: $id } } }
            ) {
              isActive
              trigger
              name
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
        if (!captures) return;

        this.motionCaptureActive = captures.some((c) => c.trigger == "MOTION");
        this.manualCaptureActive = captures.some((c) => c.trigger == "MANUAL");
        this.timerCaptureActive = captures.some((c) => c.trigger == "TIMER");
      },
    },
  },
  computed: {
    btnSize() {
      const size = {
        xs: "default",
        sm: "default",
        lg: "large",
        xl: "x-large",
      }[this.$vuetify.breakpoint.name];
      return size ? { [size]: true } : {};
    },
    captureActive() {
      return (
        this.motionCaptureActive ||
        this.timerCaptureActive ||
        this.manualCaptureActive
      );
    },
  },
  methods: {
    longPress() {
      console.log(1);
    },
    playing() {
      this.$emit("playing");
      this.loading = false;
    },
    toggleCapture() {
      if (this.manualCaptureActive) {
        this.SIGNAL_R.invoke("stopCapture", this.cam);
      } else {
        this.SIGNAL_R.invoke("startCapture", this.cam);
      }
    },
    captureStarted(msg) {
      let capture = msg.info;
      if (capture.cam.id == this.cam.id) {
        if (capture.trigger == "motion") {
          this.motionCaptureActive = true;
        } else if (capture.trigger == "manual") {
          this.manualCaptureActive = true;
        } else if (capture.trigger == "timer") {
          this.timerCaptureActive = true;
        }
      }
    },
    captureResult(msg) {
      let capture = msg.info;
      if (capture.cam.id == this.cam.id) {
        if (capture.trigger == "motion") {
          this.motionCaptureActive = false;
        } else if (capture.trigger == "manual") {
          this.manualCaptureActive = false;
        } else if (capture.trigger == "timer") {
          this.timerCaptureActive = false;
        }
      }
    },
  },
  data: () => ({
    loading: true,
    overlay: true,
    motionCaptureActive: false,
    manualCaptureActive: false,
    timerCaptureActive: false,
  }),
};
</script>

<style scoped>
video {
  width: 100%;
  max-height: 100vh;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.5s;
}

.fade-enter,
.fade-leave-to {
  opacity: 0;
}

.overlay {
  position: absolute;
  user-select: none;
  text-shadow: 0 0 4px black, 0 0 4px black, 0 0 4px black, 0 0 4px black;
  transform: translate(-50%, 0%);
}
</style>
