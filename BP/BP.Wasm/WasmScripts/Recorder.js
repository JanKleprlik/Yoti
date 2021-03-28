function record_and_recognize(recordingLength) {
    var RecognizeMethod = Module.mono_bind_static_method("[BP.Wasm] BP.Shared.Views.MainPage:ProcessEvent ");

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
            sampleSize: 16,
            noiseSuppression: false,
            latency: 0.1
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
                recordedChunks.push(e.data);
                shouldStop = true;
            }

            //send data to c#
            if (shouldStop === true && stopped === true) {
                var reader = new FileReader();
                reader.readAsDataURL(new Blob(recordedChunks));

                reader.onloadend = function () {
                    var base64data = reader.result;
                    RecognizeMethod(base64data, false); //send data
                    RecognizeMethod(base64data, true); //process data
                }
            }

            //stop recording and give one more round to process last data collected
            if (shouldStop === true && stopped === false) {
                mediaRecorder.stop();
                stopped = true;
                console.log('stopped recording');
            }

        }

        mediaRecorder.start(recordingLength);
        console.log('start recording');
    }

}