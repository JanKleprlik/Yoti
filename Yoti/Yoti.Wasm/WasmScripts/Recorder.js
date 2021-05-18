function record_and_recognize(recordingLength) {
    var processPartFile = Module.mono_bind_static_method("[Yoti.Wasm] Yoti.Shared.ViewModels.MainPageViewModel:ProcessEvent");


    navigator.mediaDevices.getUserMedia({
        audio: true
    }).then(async function (stream) {
        let recorder = RecordRTC(stream, {
            type: 'audio',
            mimeType: 'audio/wav',
            recorderType: StereoAudioRecorder,
            sampleRate: 48000,
            desiredSampRate: 48000,
            numberOfAudioChannels: 1

        });
        recorder.startRecording();

        const sleep = m => new Promise(r => setTimeout(r, m));
        await sleep(recordingLength * 1000);

        recorder.stopRecording(function () {
            let blob = recorder.getBlob();

            var reader = new FileReader();
            reader.readAsDataURL(blob);
            reader.onloadend = function () {
                var data = reader.result;

                if (!data.startsWith('data:audio/wav;base64,')) {
                    console.log('Unsupported format.');
                    alert('Unsupported format.');
                    return;
                }

                //start ... audio metadata of uploaded file
                let start = 0;
                let step = 500000;
                let part = data.substring(start, start + step);
                console.log(part);
                while (part !== "") {
                    processPartFile(part, false);
                    start = start + step;
                    part = data.substring(start, start + step);
                }

                //full file uploaded -> process it
                console.log('Done recording file');
                processPartFile("recording", true);
            }

        });
    });

}