(function(e){function t(t){for(var a,o,s=t[0],c=t[1],l=t[2],p=0,d=[];p<s.length;p++)o=s[p],Object.prototype.hasOwnProperty.call(r,o)&&r[o]&&d.push(r[o][0]),r[o]=0;for(a in c)Object.prototype.hasOwnProperty.call(c,a)&&(e[a]=c[a]);u&&u(t);while(d.length)d.shift()();return i.push.apply(i,l||[]),n()}function n(){for(var e,t=0;t<i.length;t++){for(var n=i[t],a=!0,s=1;s<n.length;s++){var c=n[s];0!==r[c]&&(a=!1)}a&&(i.splice(t--,1),e=o(o.s=n[0]))}return e}var a={},r={app:0},i=[];function o(t){if(a[t])return a[t].exports;var n=a[t]={i:t,l:!1,exports:{}};return e[t].call(n.exports,n,n.exports,o),n.l=!0,n.exports}o.m=e,o.c=a,o.d=function(e,t,n){o.o(e,t)||Object.defineProperty(e,t,{enumerable:!0,get:n})},o.r=function(e){"undefined"!==typeof Symbol&&Symbol.toStringTag&&Object.defineProperty(e,Symbol.toStringTag,{value:"Module"}),Object.defineProperty(e,"__esModule",{value:!0})},o.t=function(e,t){if(1&t&&(e=o(e)),8&t)return e;if(4&t&&"object"===typeof e&&e&&e.__esModule)return e;var n=Object.create(null);if(o.r(n),Object.defineProperty(n,"default",{enumerable:!0,value:e}),2&t&&"string"!=typeof e)for(var a in e)o.d(n,a,function(t){return e[t]}.bind(null,a));return n},o.n=function(e){var t=e&&e.__esModule?function(){return e["default"]}:function(){return e};return o.d(t,"a",t),t},o.o=function(e,t){return Object.prototype.hasOwnProperty.call(e,t)},o.p="/";var s=window["webpackJsonp"]=window["webpackJsonp"]||[],c=s.push.bind(s);s.push=t,s=s.slice();for(var l=0;l<s.length;l++)t(s[l]);var u=c;i.push([0,"chunk-vendors"]),n()})({0:function(e,t,n){e.exports=n("56d7")},"034f":function(e,t,n){"use strict";n("85ec")},"4e89":function(e,t,n){},5491:function(e,t,n){"use strict";n("7ff4")},"56d7":function(e,t,n){"use strict";n.r(t);n("b64b"),n("d3b7"),n("ac1f"),n("5319"),n("96cf");var a=n("1da1"),r=(n("e260"),n("e6cf"),n("cca6"),n("a79d"),n("2b0e")),i=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("v-app",{},[n("v-main",[n("router-view")],1)],1)},o=[],s=n("e87a"),c={name:"App",mounted:function(){var e=this;return Object(a["a"])(regeneratorRuntime.mark((function t(){return regeneratorRuntime.wrap((function(t){while(1)switch(t.prev=t.next){case 0:console.log("production"),console.log(e.SERVER_URL);case 2:case"end":return t.stop()}}),t)})))()},data:function(){return{}}},l=c,u=(n("034f"),n("2877")),p=n("6544"),d=n.n(p),m=n("7496"),v=n("f6c4"),f=Object(u["a"])(l,i,o,!1,null,null,null),h=f.exports;d()(f,{VApp:m["a"],VMain:v["a"]});var b=n("8c4f"),g=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("div",{staticClass:"home"},[n("v-btn",{directives:[{name:"show",rawName:"v-show",value:null!=e.cameras&&0==e.cameras.length,expression:"cameras != null && cameras.length == 0"}],staticClass:"addBtn",attrs:{color:"primary"},on:{click:function(t){return e.$router.push("setup")}}},[e._v(" Add Camera "),n("v-icon",[e._v(" mdi-camera-plus ")])],1),n("v-dialog",{attrs:{fullscreen:"","hide-overlay":"",transition:"dialog-bottom-transition"},model:{value:e.showDialog,callback:function(t){e.showDialog=t},expression:"showDialog"}},[n("v-card",[n("v-toolbar",{staticStyle:{position:"fixed","z-index":"1000"},attrs:{width:"100vw"}},[n("v-btn",{attrs:{icon:"",dark:""},on:{click:function(t){e.showDialog=!1}}},[n("v-icon",[e._v("mdi-close")])],1),n("v-toolbar-title",[e._v(" Captures ")]),n("v-spacer")],1),null!=e.dialogCam?n("CaptureOverview",{attrs:{cam:e.dialogCam}}):e._e()],1)],1),e._l(e.cameras,(function(t,a){return n("div",{key:a},[n("VideoPlayer",{attrs:{cam:t,stream:e.streams[t.id]},on:{"show-files":function(n){return e.showFiles(t)}}})],1)}))],2)},w=[],y=(n("3ca3"),n("ddb0"),n("b85c")),x=n("8785"),C=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("div",{staticStyle:{position:"relative"}},[n("div",{directives:[{name:"show",rawName:"v-show",value:e.overlay,expression:"overlay"}],staticClass:"headline font-weight-bold overlay",staticStyle:{top:"5%",left:"50%"}},[e._v(" "+e._s(e.cam.name)+" ")]),n("span",{directives:[{name:"show",rawName:"v-show",value:e.cam.isObjectDetectionEnabled,expression:"cam.isObjectDetectionEnabled"}],staticClass:"overlay",staticStyle:{top:"5%",right:"0px"}},[n("v-icon",{attrs:{color:"orange"}},[e._v("mdi-motion-sensor")])],1),n("DetectionCanvas",{attrs:{cam:e.cam}}),n("span",{directives:[{name:"show",rawName:"v-show",value:e.motionCaptureActive||e.timerCaptureActive||e.manualCaptureActive,expression:"motionCaptureActive || timerCaptureActive || manualCaptureActive"}],staticClass:"overlay",staticStyle:{top:"5%",right:"40px"}},[n("v-icon",{attrs:{color:e.manualCaptureActive?"red":"orange"}},[e._v("mdi-record-circle")])],1),n("v-btn",e._b({directives:[{name:"show",rawName:"v-show",value:e.overlay,expression:"overlay"}],staticStyle:{bottom:"20px"},attrs:{color:e.manualCaptureActive?"red":e.captureActive?"orange":"green",dark:"",fab:"",absolute:"",left:""},on:{click:function(t){return e.toggleCapture(e.cam)}}},"v-btn",e.btnSize,!1),[n("v-icon",[e._v("mdi-camera")])],1),n("v-btn",{directives:[{name:"show",rawName:"v-show",value:e.overlay,expression:"overlay"}],staticStyle:{bottom:"20px",left:"90px"},attrs:{color:"primary",dark:"",fab:"",absolute:"",left:""},on:{click:function(t){return e.$emit("show-files")}}},[n("v-icon",[e._v("mdi-folder-search")])],1),n("v-overlay",{attrs:{absolute:"",value:e.loading}},[n("v-progress-circular",{attrs:{indeterminate:"",size:"64"}})],1),n("video",{directives:[{name:"hold",rawName:"v-hold.500",value:function(){e.isInteractive&&(e.overlay=!e.overlay)},expression:"\n      () => {\n        if (isInteractive) overlay = !overlay;\n      }\n    ",modifiers:{500:!0}},{name:"hold",rawName:"v-hold.1500",value:function(){e.longPress()},expression:"\n      () => {\n        longPress();\n      }\n    ",modifiers:{1500:!0}}],ref:"player",attrs:{autoplay:"",muted:"",playsinline:""},domProps:{muted:!0,srcObject:e.stream},on:{loadeddata:e.playing}})],1)},k=[],_=(n("4de4"),n("45fc"),n("b0c0"),n("ade3")),S=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("div",{staticStyle:{"pointer-events":"none"}},[n("div",{ref:"container",staticClass:"overlay"},[n("transition",{attrs:{name:"fade"}},[n("canvas",{directives:[{name:"show",rawName:"v-show",value:e.visible,expression:"visible"}],ref:"canvas",staticClass:"overlay"})])],1)])},O=[],T={name:"DetectionCanvas",props:{cam:Object},methods:{detectionResult:function(e){var t=this,n=e.cam,a=e.summary;if(n.id==this.cam.id){var r=this.$refs.canvas,i=this.$refs.container,o=r.getContext("2d");r.width=i.clientWidth,r.height=i.clientHeight,this.lastMsg=e,setTimeout((function(){t.lastMsg==e&&(t.visible=!1)}),4e3),o.clearRect(0,0,r.width,r.height),o.stroke();var s,c=a.detections.filter((function(e){return"person"==e.label})),l=Object(y["a"])(c);try{for(l.s();!(s=l.n()).done;){var u=s.value;console.log("detected");var p=r.width*u.left,d=r.height*u.top,m=r.width*u.right-p,v=r.height*u.bottom-d;o.lineWidth=4,o.strokeStyle="#E6007E",o.strokeRect(p,d,m,v),o.strokeStyle="black",o.fillStyle="#E6007E",o.font="bold 16px monospace",o.strokeText(u.label.toUpperCase(),p,d),o.fillText(u.label.toUpperCase(),p,d),o.stroke()}}catch(f){l.e(f)}finally{l.f()}this.visible=!0}}},mounted:function(){this.SIGNAL_R.on("OnDetectionResult",this.detectionResult)},beforeDestroy:function(){this.SIGNAL_R.off("OnDetectionResult",this.detectionResult)},data:function(){return{visible:!1,lastMsg:{}}}},j=T,R=(n("5491"),Object(u["a"])(j,S,O,!1,null,"01cef76e",null)),D=R.exports,V=n("9530"),A=n.n(V);function E(){var e=Object(x["a"])(["\n          subscription($id: Int!) {\n            onActiveCapturesChanged(\n              where: { isActive: { eq: true }, cam: { id: { eq: $id } } }\n            ) {\n              isActive\n              trigger\n              name\n              cam {\n                id\n              }\n            }\n          }\n        "]);return E=function(){return e},e}function P(){var e=Object(x["a"])(["\n        query($id: Int!) {\n          captures(\n            where: { isActive: { eq: true }, cam: { id: { eq: $id } } }\n          ) {\n            isActive\n            trigger\n            name\n            cam {\n              id\n            }\n          }\n        }\n      "]);return P=function(){return e},e}var N={name:"VideoPlayer",props:{stream:MediaStream,isInteractive:{default:!0,type:Boolean},cam:Object},components:{DetectionCanvas:D},apollo:{captures:{query:A()(P()),subscribeToMore:{document:A()(E()),variables:function(){return{id:this.cam.id}},updateQuery:function(e,t){var n=this,a=t.subscriptionData,r=a.data.onActiveCapturesChanged;return r=r.filter((function(e){return e.cam.id==n.cam.id})),{captures:r}}},variables:function(){return{id:this.cam.id}},result:function(e){var t=e.data.captures;t&&(this.motionCaptureActive=t.some((function(e){return"MOTION"==e.trigger})),this.manualCaptureActive=t.some((function(e){return"MANUAL"==e.trigger})),this.timerCaptureActive=t.some((function(e){return"TIMER"==e.trigger})))}}},computed:{btnSize:function(){var e={xs:"default",sm:"default",lg:"large",xl:"x-large"}[this.$vuetify.breakpoint.name];return e?Object(_["a"])({},e,!0):{}},captureActive:function(){return this.motionCaptureActive||this.timerCaptureActive||this.manualCaptureActive}},methods:{longPress:function(){console.log(1)},playing:function(){this.$emit("playing"),this.loading=!1},toggleCapture:function(){this.manualCaptureActive?this.SIGNAL_R.invoke("stopCapture",this.cam):this.SIGNAL_R.invoke("startCapture",this.cam)},captureStarted:function(e){var t=e.info;t.cam.id==this.cam.id&&("motion"==t.trigger?this.motionCaptureActive=!0:"manual"==t.trigger?this.manualCaptureActive=!0:"timer"==t.trigger&&(this.timerCaptureActive=!0))},captureResult:function(e){var t=e.info;t.cam.id==this.cam.id&&("motion"==t.trigger?this.motionCaptureActive=!1:"manual"==t.trigger?this.manualCaptureActive=!1:"timer"==t.trigger&&(this.timerCaptureActive=!1))}},data:function(){return{loading:!0,overlay:!0,motionCaptureActive:!1,manualCaptureActive:!1,timerCaptureActive:!1}}},$=N,I=(n("d215"),n("8336")),U=n("132d"),L=n("a797"),M=n("490a"),q=Object(u["a"])($,C,k,!1,null,"bf31a898",null),B=q.exports;d()(q,{VBtn:I["a"],VIcon:U["a"],VOverlay:L["a"],VProgressCircular:M["a"]});var H=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("div",[n("video",{ref:"player",staticClass:"mt-5",attrs:{controls:"",muted:""},domProps:{muted:!0}}),n("v-list",{staticClass:"captureList",attrs:{"three-line":""}},[e._l(e.captures,(function(t,a){return[n("div",{key:a},[n("v-divider"),n("v-list-item",[n("v-btn",{staticClass:"mr-5",attrs:{color:"primary"},on:{click:function(n){return e.play(t)}}},[e.active!=t?n("v-icon",[e._v("mdi-play")]):e.active!=t||e.playing?n("v-icon",[e._v("mdi-pause")]):n("v-icon",[e._v("mdi-play-pause")])],1),n("v-list-item-content",[n("v-list-item-title",{domProps:{innerHTML:e._s(t.name)}}),n("v-list-item-subtitle",[n("v-expansion-panels",[n("v-expansion-panel",[n("v-expansion-panel-header",[e._v(e._s(t.trigger))]),n("v-expansion-panel-content",[e._v(" Start: "+e._s(t)+" ")])],1)],1)],1)],1)],1)],1)]}))],2)],1)},G=[];function z(){var e=Object(x["a"])(["\n          subscription($id: Int!) {\n            onActiveCapturesChanged(where: { cam: { id: { eq: $id } } }) {\n              isActive\n              trigger\n              name\n              start\n              end\n              cam {\n                id\n              }\n            }\n          }\n        "]);return z=function(){return e},e}function F(){var e=Object(x["a"])(["\n        query($id: Int!) {\n          captures(\n            where: { cam: { id: { eq: $id } } }\n            order: { isActive: DESC }\n          ) {\n            isActive\n            trigger\n            name\n            start\n            end\n            cam {\n              id\n            }\n          }\n        }\n      "]);return F=function(){return e},e}var J={name:"CaptureOVerview",props:{cam:Object},apollo:{captures:{query:A()(F()),subscribeToMore:{document:A()(z()),variables:function(){return{id:this.cam.id}},updateQuery:function(e,t){var n=this,a=t.subscriptionData,r=a.data.onActiveCapturesChanged;return r=r.filter((function(e){return e.cam.id==n.cam.id})),{captures:r}}},variables:function(){return{id:this.cam.id}},result:function(e){e.data.captures}}},computed:{},methods:{play:function(e){var t=this.$refs.player,n=this.SERVER_URL+"captures/"+e.name;this.active!=e&&(t.src=n,this.active=e,this.activeSrc=n,this.playing=!1),this.playing?(t.pause(),this.playing=!1):(t.play(),this.playing=!0)}},data:function(){return{active:null,activeSrc:"",playing:!1}}},W=J,K=(n("e378"),n("ce7e")),Q=n("cd55"),Y=n("49e2"),X=n("c865"),Z=n("0393"),ee=n("8860"),te=n("da13"),ne=n("5d23"),ae=Object(u["a"])(W,H,G,!1,null,"feab597c",null),re=ae.exports;function ie(){var e=Object(x["a"])(["\n          subscription {\n            onCamerasChanged {\n              name\n              id\n              isObjectDetectionEnabled\n            }\n          }\n        "]);return ie=function(){return e},e}d()(ae,{VBtn:I["a"],VDivider:K["a"],VExpansionPanel:Q["a"],VExpansionPanelContent:Y["a"],VExpansionPanelHeader:X["a"],VExpansionPanels:Z["a"],VIcon:U["a"],VList:ee["a"],VListItem:te["a"],VListItemContent:ne["a"],VListItemSubtitle:ne["b"],VListItemTitle:ne["c"]});var oe={name:"Home",components:{VideoPlayer:B,CaptureOverview:re},apollo:{$subscribe:{onCamerasChanged:{query:A()(ie()),result:function(e){var t=this;return Object(a["a"])(regeneratorRuntime.mark((function n(){var a,r,i,o;return regeneratorRuntime.wrap((function(n){while(1)switch(n.prev=n.next){case 0:a=e.data,t.cameras=a.onCamerasChanged,t.streams=[],r=Object(y["a"])(t.cameras),n.prev=4,o=regeneratorRuntime.mark((function e(){var n,a;return regeneratorRuntime.wrap((function(e){while(1)switch(e.prev=e.next){case 0:return n=i.value,e.next=3,t.WEBRTC_CONNECT(n);case 3:a=e.sent,"safari"==t.ADAPTER.browserDetails.browser?setTimeout((function(){t.$set(t.streams,n.id,a)}),100):t.$set(t.streams,n.id,a);case 5:case"end":return e.stop()}}),e)})),r.s();case 7:if((i=r.n()).done){n.next=11;break}return n.delegateYield(o(),"t0",9);case 9:n.next=7;break;case 11:n.next=16;break;case 13:n.prev=13,n.t1=n["catch"](4),r.e(n.t1);case 16:return n.prev=16,r.f(),n.finish(16);case 19:case"end":return n.stop()}}),n,null,[[4,13,16,19]])})))()}}}},methods:{showFiles:function(e){this.dialogCam=e,this.showDialog=!0}},data:function(){return{showDialog:!1,dialogCam:null,streams:[],cameras:null}}},se=oe,ce=(n("9f08"),n("b0af")),le=n("169a"),ue=n("2fa4"),pe=n("71d9"),de=n("2a7f"),me=Object(u["a"])(se,g,w,!1,null,"edd7cc48",null),ve=me.exports;d()(me,{VBtn:I["a"],VCard:ce["a"],VDialog:le["a"],VIcon:U["a"],VSpacer:ue["a"],VToolbar:pe["a"],VToolbarTitle:de["a"]});var fe=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("div",{staticClass:"setup"},[n("v-container",[n("v-stepper",{model:{value:e.stepper,callback:function(t){e.stepper=t},expression:"stepper"}},[n("v-stepper-header",[n("v-stepper-step",{attrs:{step:"1"}}),n("v-divider"),n("v-stepper-step",{attrs:{step:"2"}}),n("v-divider"),n("v-stepper-step",{attrs:{step:"3"}})],1),n("v-stepper-items",[n("v-stepper-content",{attrs:{step:"1"}},[n("v-tooltip",{attrs:{top:"","open-on-hover":!1},scopedSlots:e._u([{key:"activator",fn:function(t){var a=t.on;return[n("v-row",{attrs:{align:"center",justify:"center"}},[n("div",{staticClass:"text-h6 text-center"},[e._v("Add Camera")]),n("v-btn",{attrs:{text:"","retain-focus-on-click":""},on:{click:a.click,blur:a.blur}},[n("v-icon",[e._v("mdi-help-circle-outline")])],1)],1)]}}])},[n("span",[e._v(" Enter a name and a "),n("u",[e._v("valid")]),e._v(" rtsp stream url. "),n("br"),e._v(" For a lot of models you can find settings "),n("a",{attrs:{href:"https://www.ispyconnect.com/sources.aspx"}},[e._v("here")]),e._v(". ")])]),n("v-text-field",{attrs:{label:"Name",placeholder:"Garage"},model:{value:e.camName,callback:function(t){e.camName=t},expression:"camName"}}),n("v-text-field",{attrs:{label:"Stream",placeholder:"rtsp://192.168.0.1:554/stream"},model:{value:e.camStream,callback:function(t){e.camStream=t},expression:"camStream"}}),n("v-switch",{attrs:{"prepend-icon":"mdi-lock"},model:{value:e.useCredentials,callback:function(t){e.useCredentials=t},expression:"useCredentials"}}),n("v-text-field",{directives:[{name:"show",rawName:"v-show",value:e.useCredentials,expression:"useCredentials"}],attrs:{label:"User"},model:{value:e.camUser,callback:function(t){e.camUser=t},expression:"camUser"}}),n("v-text-field",{directives:[{name:"show",rawName:"v-show",value:e.useCredentials,expression:"useCredentials"}],attrs:{"append-icon":e.showPassword?"mdi-eye":"mdi-eye-off",type:e.showPassword?"text":"password",label:"Password",counter:""},on:{"click:append":function(t){e.showPassword=!e.showPassword}},model:{value:e.camPassword,callback:function(t){e.camPassword=t},expression:"camPassword"}}),n("v-btn",{staticClass:"mt-8 mr-4",attrs:{color:e.error?"red":"green",loading:e.loading},on:{click:e.addCamera}},[e._v(" Save ")]),n("v-btn",{staticClass:"mt-8",attrs:{disabled:e.loading},on:{click:e.cancel}},[e._v(" Cancel ")])],1),n("v-stepper-content",{attrs:{step:"2"}},[n("v-tooltip",{attrs:{top:"","open-on-hover":!1},scopedSlots:e._u([{key:"activator",fn:function(t){var a=t.on;return[n("v-row",{attrs:{align:"center",justify:"center"}},[n("div",{staticClass:"text-h6 text-center"},[e._v("Object Detection")]),n("v-btn",{attrs:{text:"","retain-focus-on-click":""},on:{click:a.click,blur:a.blur}},[n("v-icon",[e._v("mdi-help-circle-outline")])],1)],1)]}}])},[n("span",[e._v(" Enable or disable Object Detection. "),n("br"),e._v(" It is trained for detecting people. "),n("br"),e._v(" Be aware this will result in increased CPU usage. ")])]),null!=e.cam?n("VideoPlayer",{attrs:{cam:e.cam,stream:e.stream,isInteractive:!1},on:{playing:e.playing}}):e._e(),n("v-switch",{attrs:{"prepend-icon":e.enableObjectDetection?"mdi-motion-sensor":"mdi-motion-sensor-off"},on:{click:function(t){return e.updateCamera()}},model:{value:e.enableObjectDetection,callback:function(t){e.enableObjectDetection=t},expression:"enableObjectDetection"}}),n("v-expansion-panels",{directives:[{name:"show",rawName:"v-show",value:e.enableObjectDetection,expression:"enableObjectDetection"}]},[n("v-expansion-panel",[n("v-expansion-panel-header",[e._v("Advanced")]),n("v-expansion-panel-content",[n("v-switch",{attrs:{"prepend-icon":"mdi-record-rec"},model:{value:e.enableObjectDetectionRec,callback:function(t){e.enableObjectDetectionRec=t},expression:"enableObjectDetectionRec"}}),n("v-text-field",{attrs:{label:"Duration (s)",type:"number","prepend-icon":"mdi-clock-start"},model:{value:e.objectDetectionRecDuration,callback:function(t){e.objectDetectionRecDuration=t},expression:"objectDetectionRecDuration"}}),n("v-text-field",{attrs:{placeholder:"10s",label:"Timehsift (s)","prepend-icon":"mdi-autorenew"},model:{value:e.objectDetectionRecBufferDuration,callback:function(t){e.objectDetectionRecBufferDuration=t},expression:"objectDetectionRecBufferDuration"}})],1)],1)],1),n("v-btn",{staticClass:"mt-8 mr-4",attrs:{color:"primary"},on:{click:function(t){e.stepper=3}}},[e.motionSettingsUpdated?n("div",[e._v("Continue")]):n("div",[e._v("Skip")])]),n("v-btn",{staticClass:"mt-8",on:{click:e.cancel}},[e._v(" Cancel ")])],1),n("v-stepper-content",{attrs:{step:"3"}},[n("v-tooltip",{attrs:{top:"","open-on-hover":!1},scopedSlots:e._u([{key:"activator",fn:function(t){var a=t.on;return[n("v-row",{attrs:{align:"center",justify:"center"}},[n("div",{staticClass:"text-h6 text-center"},[e._v("Timer")]),n("v-btn",{attrs:{text:"","retain-focus-on-click":""},on:{click:a.click,blur:a.blur}},[n("v-icon",[e._v("mdi-help-circle-outline")])],1)],1)]}}])},[n("span",[e._v(" Capture footage every day at a specific time interval. ")])]),n("v-switch",{attrs:{"prepend-icon":"mdi-clock-outline"},model:{value:e.useTimer,callback:function(t){e.useTimer=t},expression:"useTimer"}}),e.useTimer?n("v-dialog",{ref:"dialog1",attrs:{"return-value":e.startTime,persistent:"",width:"290px"},on:{"update:returnValue":function(t){e.startTime=t},"update:return-value":function(t){e.startTime=t}},scopedSlots:e._u([{key:"activator",fn:function(t){var a=t.on,r=t.attrs;return[n("v-text-field",e._g(e._b({attrs:{label:"Start","prepend-icon":"mdi-clock-start",readonly:""},model:{value:e.startTime,callback:function(t){e.startTime=t},expression:"startTime"}},"v-text-field",r,!1),a))]}}],null,!1,3841312212),model:{value:e.dialogStartTime,callback:function(t){e.dialogStartTime=t},expression:"dialogStartTime"}},[e.dialogStartTime?n("v-time-picker",{attrs:{format:"24hr",scrollable:"","full-width":""},model:{value:e.startTime,callback:function(t){e.startTime=t},expression:"startTime"}},[n("v-spacer"),n("v-btn",{attrs:{text:"",color:"primary"},on:{click:function(t){e.dialogStartTime=!1}}},[e._v(" Cancel ")]),n("v-btn",{attrs:{text:"",color:"primary"},on:{click:function(t){return e.$refs.dialog1.save(e.startTime)}}},[e._v(" OK ")])],1):e._e()],1):e._e(),e.useTimer?n("v-dialog",{ref:"dialog2",attrs:{"return-value":e.stopTime,persistent:"",width:"290px"},on:{"update:returnValue":function(t){e.stopTime=t},"update:return-value":function(t){e.stopTime=t}},scopedSlots:e._u([{key:"activator",fn:function(t){var a=t.on,r=t.attrs;return[n("v-text-field",e._g(e._b({attrs:{label:"Stop","prepend-icon":"mdi-clock-end",readonly:""},model:{value:e.stopTime,callback:function(t){e.stopTime=t},expression:"stopTime"}},"v-text-field",r,!1),a))]}}],null,!1,775256475),model:{value:e.dialogStopTime,callback:function(t){e.dialogStopTime=t},expression:"dialogStopTime"}},[e.dialogStopTime?n("v-time-picker",{attrs:{format:"24hr",scrollable:"","full-width":""},model:{value:e.stopTime,callback:function(t){e.stopTime=t},expression:"stopTime"}},[n("v-spacer"),n("v-btn",{attrs:{text:"",color:"primary"},on:{click:function(t){e.dialogStopTime=!1}}},[e._v(" Cancel ")]),n("v-btn",{attrs:{text:"",color:"primary"},on:{click:function(t){return e.$refs.dialog2.save(e.stopTime)}}},[e._v(" OK ")])],1):e._e()],1):e._e(),n("v-btn",{staticClass:"mt-8 mr-4",attrs:{color:"green"},on:{click:function(t){return e.$router.push("/")}}},[e._v(" Finish ")]),n("v-btn",{staticClass:"mt-8",on:{click:e.cancel}},[e._v(" Cancel ")])],1)],1)],1)],1)],1)},he=[],be={name:"Setup",components:{VideoPlayer:B},methods:{playing:function(){this.stepper=2,this.loading=!1},cancel:function(){var e=this;return Object(a["a"])(regeneratorRuntime.mark((function t(){return regeneratorRuntime.wrap((function(t){while(1)switch(t.prev=t.next){case 0:null!=e.cam&&fetch(e.SERVER_URL+"camera",{method:"DELETE",body:JSON.stringify(e.cam),headers:{"Content-Type":"application/json"}}),e.$router.push("/");case 2:case"end":return t.stop()}}),t)})))()},updateCamera:function(){var e=this;return Object(a["a"])(regeneratorRuntime.mark((function t(){var n;return regeneratorRuntime.wrap((function(t){while(1)switch(t.prev=t.next){case 0:return e.cam.isObjectDetectionEnabled=e.enableObjectDetection,e.motionSettingsUpdated=!0,t.next=4,fetch(e.SERVER_URL+"camera/".concat(e.cam.id),{method:"PUT",body:JSON.stringify(e.cam),headers:{"Content-Type":"application/json"}});case 4:return n=t.sent,t.t0=console,t.next=8,n.json();case 8:t.t1=t.sent,t.t0.log.call(t.t0,t.t1);case 10:case"end":return t.stop()}}),t)})))()},addCamera:function(){var e=this;return Object(a["a"])(regeneratorRuntime.mark((function t(){var n,a;return regeneratorRuntime.wrap((function(t){while(1)switch(t.prev=t.next){case 0:return e.loading=!0,t.next=3,fetch(e.SERVER_URL+"camera",{method:"POST",body:JSON.stringify({name:e.camName,streamUrl:e.camStream,user:e.camUser,password:e.camPassword}),headers:{"Content-Type":"application/json"}});case 3:if(n=t.sent,!n.ok){t.next=15;break}return t.next=7,n.json();case 7:return a=t.sent,t.next=10,e.WEBRTC_CONNECT(a);case 10:e.stream=t.sent,e.cam=a,e.error=!1,t.next=17;break;case 15:e.loading=!1,e.error=!0;case 17:case"end":return t.stop()}}),t)})))()}},data:function(){return{motionSettingsUpdated:!1,dialogStartTime:!1,dialogStopTime:!1,startTime:null,stopTime:null,useTimer:!1,cam:null,error:!1,loading:!1,showPassword:!1,useCredentials:!1,enableObjectDetection:!1,enableObjectDetectionRec:!0,objectDetectionRecDuration:30,objectDetectionRecBufferDuration:10,camPassword:"",camUser:"",camName:"",camStream:"",stepper:1,stream:null}}},ge=be,we=(n("5f93"),n("a523")),ye=n("0fd9"),xe=n("7e85"),Ce=n("e516"),ke=n("9c54"),_e=n("56a4"),Se=n("b73d"),Oe=n("8654"),Te=n("c964"),je=n("3a2f"),Re=Object(u["a"])(ge,fe,he,!1,null,"e6d3185a",null),De=Re.exports;d()(Re,{VBtn:I["a"],VContainer:we["a"],VDialog:le["a"],VDivider:K["a"],VExpansionPanel:Q["a"],VExpansionPanelContent:Y["a"],VExpansionPanelHeader:X["a"],VExpansionPanels:Z["a"],VIcon:U["a"],VRow:ye["a"],VSpacer:ue["a"],VStepper:xe["a"],VStepperContent:Ce["a"],VStepperHeader:ke["a"],VStepperItems:ke["b"],VStepperStep:_e["a"],VSwitch:Se["a"],VTextField:Oe["a"],VTimePicker:Te["a"],VTooltip:je["a"]}),r["a"].use(b["a"]);var Ve=[{path:"/",name:"Home",component:ve},{path:"/setup",name:"Setup",component:De}],Ae=new b["a"]({routes:Ve}),Ee=Ae,Pe=n("f309");r["a"].use(Pe["a"]);var Ne=new Pe["a"]({theme:{dark:!0}}),$e=n("d093"),Ie=(n("99af"),n("5530")),Ue=n("522d"),Le=n("efe7");r["a"].use(Ue["a"]);var Me="apollo-token",qe=window.location.hostname,Be=window.location.port,He="".concat(qe,":").concat(Be,"/"),Ge="http://"+He+"graphql",ze="ws://"+He+"graphql",Fe=Ge,Je={httpEndpoint:Fe,wsEndpoint:ze,tokenName:Me,persisting:!1,websocketsOnly:!1,ssr:!1};function We(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:{},t=Object(Le["createApolloClient"])(Object(Ie["a"])(Object(Ie["a"])({},Je),e)),n=t.apolloClient,a=t.wsClient;n.wsClient=a;var r=new Ue["a"]({defaultClient:n,defaultOptions:{$query:{}},errorHandler:function(e){console.log("%cError","background: red; color: white; padding: 2px 4px; border-radius: 3px; font-weight: bold;",e.message)}});return r}r["a"].config.productionTip=!1;var Ke="/";r["a"].prototype.SERVER_URL=Ke;var Qe=(new s["a"]).withUrl(Ke+"msg").build();function Ye(){return Xe.apply(this,arguments)}function Xe(){return Xe=Object(a["a"])(regeneratorRuntime.mark((function e(){return regeneratorRuntime.wrap((function(e){while(1)switch(e.prev=e.next){case 0:return e.next=2,Qe.stop();case 2:return e.next=4,Qe.start();case 4:new r["a"]({router:Ee,vuetify:Ne,apolloProvider:We(),render:function(e){return e(h)}}).$mount("#app");case 5:case"end":return e.stop()}}),e)}))),Xe.apply(this,arguments)}r["a"].prototype.SIGNAL_R=Qe,r["a"].prototype.ADAPTER=$e["a"],r["a"].prototype.WEBRTC_CONNECT=function(){var e=Object(a["a"])(regeneratorRuntime.mark((function e(t){return regeneratorRuntime.wrap((function(e){while(1)switch(e.prev=e.next){case 0:return e.abrupt("return",new Promise(function(){var e=Object(a["a"])(regeneratorRuntime.mark((function e(n,r){var i,o,s,c,l;return regeneratorRuntime.wrap((function(e){while(1)switch(e.prev=e.next){case 0:return i=new RTCPeerConnection,i.ontrack=function(){var e=Object(a["a"])(regeneratorRuntime.mark((function e(t){var a;return regeneratorRuntime.wrap((function(e){while(1)switch(e.prev=e.next){case 0:a=t.streams[0],n(a);case 2:case"end":return e.stop()}}),e)})));return function(t){return e.apply(this,arguments)}}(),i.onicecandidate=function(){var e=Object(a["a"])(regeneratorRuntime.mark((function e(t){return regeneratorRuntime.wrap((function(e){while(1)switch(e.prev=e.next){case 0:if(!t.candidate){e.next=3;break}return e.next=3,Qe.invoke("setCandidate",o,t.candidate);case 3:case"end":return e.stop()}}),e)})));return function(t){return e.apply(this,arguments)}}(),e.next=5,Qe.invoke("getSessionId",t);case 5:return o=e.sent,e.next=8,Qe.invoke("getOffer",o);case 8:return s=e.sent,e.next=11,i.setRemoteDescription({type:"offer",sdp:s});case 11:return e.next=13,i.createAnswer();case 13:return c=e.sent,e.next=16,i.setLocalDescription(c);case 16:return l=i.localDescription.sdp,l=l.replace("a=fmtp:102 profile-level-id=42e01f;level-asymmetry-allowed=1;packetization-mode=1\r\n",""),e.next=20,Qe.invoke("setAnswer",o,l);case 20:case"end":return e.stop()}}),e)})));return function(t,n){return e.apply(this,arguments)}}()));case 1:case"end":return e.stop()}}),e)})));return function(t){return e.apply(this,arguments)}}(),r["a"].directive("hold",{bind:function(e,t){if("function"===typeof t.value){var n=t.modifiers?Object.keys(t.modifiers)[0]:500,a=new Hammer.Manager(e),r=new Hammer.Press({time:n});a.add(r),a.on("press",t.value)}}}),Ye()},"5f93":function(e,t,n){"use strict";n("8cf5")},"7ff4":function(e,t,n){},"85ec":function(e,t,n){},"8cf5":function(e,t,n){},"9cf9":function(e,t,n){},"9f08":function(e,t,n){"use strict";n("a429")},a429:function(e,t,n){},d215:function(e,t,n){"use strict";n("4e89")},e378:function(e,t,n){"use strict";n("9cf9")}});
//# sourceMappingURL=app.c6f605b8.js.map