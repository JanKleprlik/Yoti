//defining csharp method
var processPartFile = Module.mono_bind_static_method('[Yoti.Wasm] Yoti.Shared.ViewModels.MainPageViewModel:ProcessEvent');

//raw data offset in audio obtained from FileReader().readAsDataURL(blob)
var audioMetadataOffset = 22;

//size of each datablock transfered from javascript to csharp
var uploadStepSize = 500000;


function recordAndUploadAudio(recordingLength, samplingRate, numChannels) {

    getMicrophone(async function (stream) {
        let recorder = RecordRTC(stream, {
            type: 'audio',
            mimeType: 'audio/wav',
            recorderType: StereoAudioRecorder,
            desiredSampRate: samplingRate,
            numberOfAudioChannels: numChannels

        });

        //start recording 
        recorder.startRecording();

        //sleep throughout recording
        const sleep = m => new Promise(r => setTimeout(r, m));
        await sleep(recordingLength * 1000);

        //stop recording and upload data to c#
        recorder.stopRecording(function(){ uploadDataToCsharp(recorder) });
    });

}

function uploadDataToCsharp(recorder) {
    let blob = recorder.getBlob();

    var reader = new FileReader();
    reader.readAsDataURL(blob);
    reader.onloadend = function () {
        var data = reader.result;

        if (!data.startsWith('data:audio/wav;base64,')) {
            abortJavaScript('Unsupported format.');
            return;
        }

        //start ... audio metadata of uploaded file
        let start = audioMetadataOffset;
        let step = uploadStepSize;
        let part = data.substring(start, start + step);
        while (part !== "") {
            processPartFile(part, false);
            start = start + step;
            part = data.substring(start, start + step);
        }

        //full file uploaded -> process it
        console.log('Done recording file');
        processPartFile("recording", true);
    }
}

function abortJavaScript(message) {
    console.log(message);
    alert(message);
    // proccess file with ture flag
    // error will be displayed to user
    processPartFile("error", true);
}

function getMicrophone(callback) {
    navigator.mediaDevices.getUserMedia({ audio: true })
        .then(callback)
        .catch(function (error) {
            console.log(error);
            abortJavaScript('Unable to access your microphone.');
        })

}