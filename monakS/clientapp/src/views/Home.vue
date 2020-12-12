<template>
  <div class="home">
    <v-btn
      class="addBtn"
      @click="$router.push('setup')"
      v-show="cameras != null && cameras.length == 0"
      color="primary"
    >
      Add Camera
      <v-icon> mdi-camera-plus </v-icon>
    </v-btn>

    <v-dialog
      v-model="showDialog"
      fullscreen
      hide-overlay
      transition="dialog-bottom-transition"
    >
      <v-card>
        <v-toolbar style="position: fixed; z-index: 1000" width="100vw">
          <v-btn icon dark @click="showDialog = false">
            <v-icon>mdi-close</v-icon>
          </v-btn>
          <v-toolbar-title> Captures </v-toolbar-title>
          <v-spacer></v-spacer>
        </v-toolbar>
        <CaptureOverview
          v-if="dialogCam != null"
          :cam="dialogCam"
        ></CaptureOverview>
      </v-card>
    </v-dialog>
    <div v-for="(cam, i) in cameras" :key="i">
      <VideoPlayer
        @show-files="showFiles(cam)"
        :cam="cam"
        :stream="streams[cam.id]"
      />
    </div>
  </div>
</template>

<script>
import VideoPlayer from "@/components/VideoPlayer.vue";
import CaptureOverview from "@/components/CaptureOverview.vue";

import gql from "graphql-tag";

export default {
  name: "Home",
  components: {
    VideoPlayer,
    CaptureOverview,
  },
  apollo: {
    $subscribe: {
      onCamerasChanged: {
        query: gql`
          subscription {
            onCamerasChanged {
              name
              id
              isObjectDetectionEnabled
            }
          }
        `,
        async result({ data }) {
          this.cameras = data.onCamerasChanged;
          this.streams = [];
          for (const cam of this.cameras) {
            let stream = await this.WEBRTC_CONNECT(cam);

            if (this.ADAPTER.browserDetails.browser == "safari") {
              setTimeout(() => {
                this.$set(this.streams, cam.id, stream);
              }, 100);
            } else {
              this.$set(this.streams, cam.id, stream);
            }
          }
        },
      },
    },
  },
  methods: {
    showFiles(cam) {
      this.dialogCam = cam;
      this.showDialog = true;
    },
  },
  data: () => ({
    showDialog: false,
    dialogCam: null,
    streams: [],
    cameras: null,
  }),
};
</script>

<style scoped>
.addBtn {
  position: absolute;
  user-select: none;
  top: 50%;
  left: 50%;
  transform: translate(-50%, 0%);
}

/* v-dialog overflow fix */
/* .v-dialog {
  overflow: hidden;
}

.v-dialog .v-card {
  overflow: scroll;
  height: 100%;
} */
</style>