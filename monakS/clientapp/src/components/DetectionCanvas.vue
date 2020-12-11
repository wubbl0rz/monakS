<template>
  <div style="pointer-events: none">
    <transition name="fade">
      <canvas class="overlay" ref="canvas"></canvas>
    </transition>
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
      console.log(msg);
      let cam = msg.cam;
      let summary = msg.summary;

      if (cam.id != this.cam.id) {
        return;
      }

      let el = this.$refs.canvas;
      var ctx = el.getContext("2d");
      el.width = el.clientWidth;
      el.height = el.clientHeight;

      this.visible = true;

      this.lastMsg = msg;

      setTimeout(() => {
        if (this.lastMsg == msg) {
          ctx.clearRect(0, 0, el.width, el.height);
          ctx.stroke();
          this.visible = false;
        }
      }, 2000);

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

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.5s;
}

.fade-enter,
.fade-leave-to {
  opacity: 0;
}
</style>
