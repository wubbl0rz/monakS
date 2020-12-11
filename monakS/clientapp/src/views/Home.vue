<template>
  <div class="home">
    <v-btn
      @click="$router.push('setup')"
      v-show="cameras != null && cameras.length == 0"
      color="primary"
    >
      Add Camera
      <v-icon> mdi-camera-plus </v-icon>
    </v-btn>
    <div v-for="(cam, i) in cameras" :key="i">
      <VideoPlayer :cam="cam" :stream="streams[cam.id]" />
    </div>
  </div>
</template>

<script>
import VideoPlayer from "@/components/VideoPlayer.vue";
import gql from "graphql-tag";

export default {
  name: "Home",
  components: {
    VideoPlayer,
  },
  apollo: {
    $subscribe: {
      cameras: {
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
            await this.connect(cam);
          }
        },
      },
    },
  },
  methods: {
    async connect(cam, i) {
      let pc = new RTCPeerConnection();

      pc.ontrack = async (t) => {
        let stream = t.streams[0];
        this.$set(this.streams, cam.id, stream);
      };

      pc.onicecandidate = async (e) => {
        if (e.candidate) {
          await this.SIGNAL_R.invoke("setCandidate", session_id, e.candidate);
        }
      };

      let session_id = await this.SIGNAL_R.invoke("getSessionId", cam);

      let offer_sdp = await this.SIGNAL_R.invoke("getOffer", session_id);

      await pc.setRemoteDescription({
        type: "offer",
        sdp: offer_sdp,
      });

      let answer = await pc.createAnswer();
      await pc.setLocalDescription(answer);

      let answer_sdp = pc.localDescription.sdp;
      answer_sdp = answer_sdp.replace(
        "a=fmtp:102 profile-level-id=42e01f;level-asymmetry-allowed=1;packetization-mode=1\r\n",
        ""
      );
      await this.SIGNAL_R.invoke("setAnswer", session_id, answer_sdp);
    },
  },
  data: () => ({
    streams: [],
    cameras: [],
  }),
};
</script>

<style scoped>
.v-btn {
  position: absolute;
  user-select: none;
  top: 50%;
  left: 50%;
  transform: translate(-50%, 0%);
}
</style>