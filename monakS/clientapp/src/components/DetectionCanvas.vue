<template>
  <div style="pointer-events: none">
    <div ref="container" class="overlay">
      <transition name="fade">
        <canvas v-show="visible" class="overlay" ref="canvas"></canvas>
      </transition>
    </div>
  </div>
</template>

<script>
export default {
  name: "DetectionCanvas",
  props: {
    cam: Object,
  },
  methods: {
    detectionResult(msg) {
      let cam = msg.cam;
      let summary = msg.summary;

      if (cam.id != this.cam.id) {
        return;
      }

      let el = this.$refs.canvas;
      let container = this.$refs.container;
      var ctx = el.getContext("2d");
      el.width = container.clientWidth;
      el.height = container.clientHeight;

      this.lastMsg = msg;

      setTimeout(() => {
        if (this.lastMsg == msg) {
          this.visible = false;
        }
      }, 4000);

      ctx.clearRect(0, 0, el.width, el.height);
      ctx.stroke();

      let detections = summary.detections.filter((d) => d.label == "person");

      for (const pos of detections) {
        console.log("detected");

        var startLeft = el.width * pos.left;
        var startTop = el.height * pos.top;
        var width = el.width * pos.right - startLeft;
        var height = el.height * pos.bottom - startTop;

        ctx.lineWidth = 4;
        ctx.strokeStyle = "#E6007E";

        ctx.strokeRect(startLeft, startTop, width, height);

        ctx.strokeStyle = "black";
        ctx.fillStyle = "#E6007E";
        ctx.font = "bold 16px monospace";

        ctx.strokeText(pos.label.toUpperCase(), startLeft, startTop);
        ctx.fillText(pos.label.toUpperCase(), startLeft, startTop);
        ctx.stroke();
      }

      this.visible = true;
    },
  },
  mounted() {
    this.SIGNAL_R.on("OnDetectionResult", this.detectionResult);
  },
  beforeDestroy() {
    this.SIGNAL_R.off("OnDetectionResult", this.detectionResult);
  },
  data: () => ({
    visible: false,
    lastMsg: {},
  }),
};
</script>

<style scoped>
.overlay {
  position: absolute;
  width: 100%;
  height: 100%;
  top: 0;
  left: 0;
  z-index: 5;
}

.fade-enter-active {
  transition: opacity 0.5s;
}
.fade-leave-active {
  transition: opacity 2s;
}

.fade-enter,
.fade-leave-to {
  opacity: 0;
}
</style>
