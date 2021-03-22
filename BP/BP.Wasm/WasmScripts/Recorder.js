function record_and_recognize() {
    var RecognizeMethod = Module.mono_bind_static_method("[BP.Wasm] BP.Shared.Views.MainPage:Recognize");

    var audioCtx = new AudioContext({ sampleRate: 48000 });
    var microphone_stream;

    // get microphone access
    if (navigator.getUserMedia) {

        navigator.mediaDevices.getUserMedia({ audio: true })
            .then(function (stream) {
                start_recording(stream);
            })
            .catch(function (err) {
                alert('Error capturing audio: ' + err);
            });

    } else { alert('getUserMedia not supported in this browser.'); }

    function start_recording(stream) {
        var streamTrack = stream.getAudioTracks()[0];
        streamTrack.applyConstraints({
            channelCount: 1,
            sampleRate: 48000,
            sampleSize: 16
        });
        console.log(streamTrack);
        console.log(streamTrack.getSettings());


        const options = { mimeType: 'audio/webm;codecs=pcm' };
        const recordedChunks = [];
        const mediaRecorder = new MediaRecorder(stream, options);
        var shouldStop = false;
        var stopped = false;

        mediaRecorder.ondataavailable = function (e) {
            if (e.data.size > 0) {
                console.log('data available');
                recordedChunks.push(e.data);
                shouldStop = true;
            }

            //send data to c#
            if (shouldStop === true && stopped === true) {
                var reader = new FileReader();
                reader.readAsDataURL(new Blob(recordedChunks));

                reader.onloadend = function () {
                    var base64data = reader.result;
                    RecognizeMethod(base64data);
                }
            }

            //stop recording and give one more round to process last data collected
            if (shouldStop === true && stopped === false) {
                console.log('stopping recording');
                mediaRecorder.stop();
                stopped = true;
            }

        }

        mediaRecorder.start(2000);
        console.log('start recording');
    }

}