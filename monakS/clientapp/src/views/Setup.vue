<template>
  <div class="setup">
    <v-container>
      <v-stepper v-model="stepper">
        <v-stepper-header>
          <v-stepper-step step="1"></v-stepper-step>

          <v-divider></v-divider>

          <v-stepper-step step="2"></v-stepper-step>

          <v-divider></v-divider>

          <v-stepper-step step="3"></v-stepper-step>
        </v-stepper-header>

        <v-stepper-items>
          <v-stepper-content step="1">
            <v-tooltip top :open-on-hover="false">
              <template #activator="{ on }">
                <v-row align="center" justify="center">
                  <div class="text-h6 text-center">Add Camera</div>
                  <v-btn
                    text
                    @click="on.click"
                    @blur="on.blur"
                    retain-focus-on-click
                  >
                    <v-icon>mdi-help-circle-outline</v-icon>
                  </v-btn>
                </v-row>
              </template>
              <!-- // help ispy  -->
              <span>
                Enter a name and a <u>valid</u> rtsp stream url.
                <br />
                For a lot of models you can find settings
                <a href="https://www.ispyconnect.com/sources.aspx">here</a>.
              </span>
            </v-tooltip>

            <v-text-field
              label="Name"
              placeholder="Garage"
              v-model="camName"
            ></v-text-field>
            <v-text-field
              v-model="camStream"
              label="Stream"
              placeholder="rtsp://192.168.0.1:554/stream"
            ></v-text-field>
            <v-switch v-model="useCredentials" prepend-icon="mdi-lock">
            </v-switch>

            <v-text-field
              v-show="useCredentials"
              label="User"
              v-model="camUser"
            ></v-text-field>
            <v-text-field
              v-show="useCredentials"
              v-model="camPassword"
              :append-icon="showPassword ? 'mdi-eye' : 'mdi-eye-off'"
              :type="showPassword ? 'text' : 'password'"
              label="Password"
              counter
              @click:append="showPassword = !showPassword"
            ></v-text-field>

            <v-btn
              class="mt-8 mr-4"
              :color="error ? 'red' : 'green'"
              :loading="loading"
              :disabled="loading"
              @click="
                if (!updateMode) {
                  addCamera();
                } else {
                  updateCamera();
                }
              "
              v-text="updateMode ? 'Update' : 'Add'"
            >
            </v-btn>

            <v-btn class="mt-8" @click="cancel" :disabled="loading">
              Cancel
            </v-btn>

            <v-btn
              v-if="updateMode"
              absolute
              right
              class="mt-8"
              color="red"
              :loading="loading"
              @click="removeCam"
            >
              Delete
            </v-btn>
          </v-stepper-content>

          <v-stepper-content step="2">
            <v-tooltip top :open-on-hover="false">
              <template #activator="{ on }">
                <v-row align="center" justify="center">
                  <div class="text-h6 text-center">Object Detection</div>
                  <v-btn
                    text
                    @click="on.click"
                    @blur="on.blur"
                    retain-focus-on-click
                  >
                    <v-icon>mdi-help-circle-outline</v-icon>
                  </v-btn>
                </v-row>
              </template>
              <span>
                Enable or disable Object Detection.
                <br />
                It is trained for detecting people.
                <br />
                Be aware this will result in increased CPU usage.
              </span>
            </v-tooltip>
            <VideoPlayer
              v-if="shouldPlay && cam != null"
              @playing="playing"
              :cam="cam"
              :stream="stream"
              :isInteractive="false"
            />
            <v-switch
              v-model="enableObjectDetection"
              :prepend-icon="
                enableObjectDetection
                  ? 'mdi-motion-sensor'
                  : 'mdi-motion-sensor-off'
              "
              @click="updateCamera()"
            >
            </v-switch>
            <v-expansion-panels v-show="enableObjectDetection">
              <v-expansion-panel>
                <v-expansion-panel-header>Advanced</v-expansion-panel-header>
                <v-expansion-panel-content>
                  <v-switch
                    v-model="enableObjectDetectionRec"
                    prepend-icon="mdi-record-rec"
                  >
                  </v-switch>

                  <v-text-field
                    v-model="objectDetectionRecDuration"
                    label="Duration (s)"
                    type="number"
                    prepend-icon="mdi-clock-start"
                  ></v-text-field>

                  <v-text-field
                    v-model="objectDetectionRecBufferDuration"
                    placeholder="10s"
                    label="Timehsift (s)"
                    prepend-icon="mdi-autorenew"
                  ></v-text-field>
                </v-expansion-panel-content>
              </v-expansion-panel>
            </v-expansion-panels>

            <v-btn class="mt-8 mr-4" color="primary" @click="stepper = 3">
              <div v-if="motionSettingsUpdated">Continue</div>
              <div v-else>Skip</div>
            </v-btn>

            <v-btn class="mt-8" @click="cancel"> Cancel </v-btn>
          </v-stepper-content>

          <v-stepper-content step="3">
            <v-tooltip top :open-on-hover="false">
              <template #activator="{ on }">
                <v-row align="center" justify="center">
                  <div class="text-h6 text-center">Timer</div>
                  <v-btn
                    text
                    @click="on.click"
                    @blur="on.blur"
                    retain-focus-on-click
                  >
                    <v-icon>mdi-help-circle-outline</v-icon>
                  </v-btn>
                </v-row>
              </template>
              <span>
                Capture footage every day at a specific time interval.
              </span>
            </v-tooltip>
            <v-switch v-model="useTimer" prepend-icon="mdi-clock-outline">
            </v-switch>

            <v-dialog
              v-if="useTimer"
              ref="dialog1"
              v-model="dialogStartTime"
              :return-value.sync="startTime"
              persistent
              width="290px"
            >
              <template v-slot:activator="{ on, attrs }">
                <v-text-field
                  v-model="startTime"
                  label="Start"
                  prepend-icon="mdi-clock-start"
                  readonly
                  v-bind="attrs"
                  v-on="on"
                ></v-text-field>
              </template>
              <v-time-picker
                format="24hr"
                scrollable
                v-if="dialogStartTime"
                v-model="startTime"
                full-width
              >
                <v-spacer></v-spacer>
                <v-btn text color="primary" @click="dialogStartTime = false">
                  Cancel
                </v-btn>
                <v-btn
                  text
                  color="primary"
                  @click="$refs.dialog1.save(startTime)"
                >
                  OK
                </v-btn>
              </v-time-picker>
            </v-dialog>

            <v-dialog
              v-if="useTimer"
              ref="dialog2"
              v-model="dialogStopTime"
              :return-value.sync="stopTime"
              persistent
              width="290px"
            >
              <template v-slot:activator="{ on, attrs }">
                <v-text-field
                  v-model="stopTime"
                  label="Stop"
                  prepend-icon="mdi-clock-end"
                  readonly
                  v-bind="attrs"
                  v-on="on"
                ></v-text-field>
              </template>
              <v-time-picker
                format="24hr"
                scrollable
                v-if="dialogStopTime"
                v-model="stopTime"
                full-width
              >
                <v-spacer></v-spacer>
                <v-btn text color="primary" @click="dialogStopTime = false">
                  Cancel
                </v-btn>
                <v-btn
                  text
                  color="primary"
                  @click="$refs.dialog2.save(stopTime)"
                >
                  OK
                </v-btn>
              </v-time-picker>
            </v-dialog>

            <v-btn class="mt-8 mr-4" color="green" @click="$router.push('/')">
              Finish
            </v-btn>

            <v-btn class="mt-8" @click="cancel"> Cancel </v-btn>
          </v-stepper-content>
        </v-stepper-items>
      </v-stepper>
    </v-container>
  </div>
</template>

<script>
import VideoPlayer from "@/components/VideoPlayer.vue";
import gql from "graphql-tag";

export default {
  name: "Setup",
  components: {
    VideoPlayer
  },
  methods: {
    playing() {
      this.stepper = 2;
      this.loading = false;
    },
    async removeCam() {
      if (this.cam != null && this.updateMode && confirm("Ok ?")) {
        fetch(this.SERVER_URL + "camera", {
          method: "DELETE",
          body: JSON.stringify(this.cam),
          headers: {
            "Content-Type": "application/json"
          }
        });

        this.$router.push("/");
      }
    },
    async cancel() {
      if (!this.updateMode) {
        this.removeCam();
      }

      this.$router.push("/");
    },

    async updateCamera() {
      if (this.stepper == 2) {
        this.cam.isObjectDetectionEnabled = this.enableObjectDetection;
        this.motionSettingsUpdated = true;
      } else if (this.stepper == 1) {
        this.loading = true;
      }

      let result = await fetch(this.SERVER_URL + `camera/${this.cam.id}`, {
        method: "PUT",
        body: JSON.stringify({
          id: this.cam.id,
          name: this.camName,
          streamUrl: this.camStream,
          user: this.camUser,
          password: this.camPassword,
          isObjectDetectionEnabled: this.enableObjectDetection
        }),
        headers: {
          "Content-Type": "application/json"
        }
      });

      if (result.ok && this.stepper == 1) {
        let cam = await result.json();
        this.stream = await this.WEBRTC_CONNECT(cam);
        this.cam = cam;
        this.shouldPlay = true;
        this.error = false;
      } else {
        this.error = true;
      }
    },
    async addCamera() {
      this.loading = true;

      let result = await fetch(this.SERVER_URL + "camera", {
        method: "POST",
        body: JSON.stringify({
          name: this.camName,
          streamUrl: this.camStream,
          user: this.camUser,
          password: this.camPassword
        }),
        headers: {
          "Content-Type": "application/json"
        }
      });

      if (result.ok) {
        let cam = await result.json();
        this.stream = await this.WEBRTC_CONNECT(cam);
        this.cam = cam;
        this.error = false;
        this.shouldPlay = true;
      } else {
        this.loading = false;
        this.error = true;
      }
    }
  },
  mounted() {
    let cam = this.$route.params.cam;
    if (cam != undefined) {
      this.cam = cam;
      this.camName = cam.name;
      this.updateMode = true;
      this.camStream = cam.streamUrl;
      if (this.cam.user != "") {
        this.camUser = this.cam.user;
        this.useCredentials = true;
      }
    }
  },
  data: () => ({
    shouldPlay: false,
    updateMode: false,
    motionSettingsUpdated: false,
    dialogStartTime: false,
    dialogStopTime: false,
    startTime: null,
    stopTime: null,
    useTimer: false,
    cam: null,
    error: false,
    loading: false,
    showPassword: false,
    useCredentials: false,
    enableObjectDetection: false,
    enableObjectDetectionRec: true,
    objectDetectionRecDuration: 30,
    objectDetectionRecBufferDuration: 10,
    camPassword: "",
    camUser: "",
    camName: "",
    camStream: "",
    stepper: 1,
    stream: null
  })
};
</script>

<style scoped>
.v-tooltip__content {
  pointer-events: initial;
}
</style>