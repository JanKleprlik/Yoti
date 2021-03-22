
function loadFile(){
    console.log('calling javascript');
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = '.wav';
    input.onchange = e => {
        var file = e.target.files[0];
        //size in MBs cannot be bigger than 5
        if ((file.size / 1024 / 1024) > 5) {
            alert('File size exceeds 5 MB');
        }
        else {
            var reader = new FileReader();
            reader.readAsDataURL(file);
            reader.onload = readerEvent => {
                //this is the binary uploaded content
                var content = readerEvent.target.result;
                //invoke C# method to get audio binary data
                var selectFile = Module.mono_bind_static_method("[BP.Wasm] BP.Shared.Views.MainPage:SelectFile");
                selectFile(content);
            }
        };
    };
    input.click();
}

function test2() {
    var testMethod = Module.mono_bind_static_method("[BP.Wasm] BP.Shared.Views.MainPage:Test");
    testMethod("HI");
}


function test() {

    var audioCtx = new AudioContext({ sampleRate: 48000 })

    // check if recording is supported
    if (!navigator.getUserMedia)
        navigator.getUserMedia = navigator.getUserMedia || navigator.webkitGetUserMedia ||
            navigator.mozGetUserMedia || navigator.msGetUserMedia;

    // get microphone access
    if (navigator.getUserMedia) {

        navigator.getUserMedia({ audio: true },
            function (stream) {
                start_microphone(stream);
            },
            function (e) {
                alert('Error capturing audio.');
            }
        );

    } else { alert('getUserMedia not supported in this browser.'); }

    //audioCtx.createBuffer(1, audioCtx.sampleRate * 3, audioCtx.sampleRate);

    var testMethod = Module.mono_bind_static_method("[BP.Wasm] BP.Shared.Views.MainPage:Test");
    testMethod("HI");
}

function recordAudio() {


    var audioContext = new AudioContext();
    console.log('audio is starting up ...');
    console.log(audioContext.sampleRate);

    // var BUFF_SIZE_RENDERER = 16384;

    var audioInput = null,
        microphone_stream = null,
        gain_node = null,
        script_processor_analysis_node = null,
        analyser_node = null;

    if (!navigator.getUserMedia)
        navigator.getUserMedia = navigator.getUserMedia || navigator.webkitGetUserMedia ||
            navigator.mozGetUserMedia || navigator.msGetUserMedia;

    if (navigator.getUserMedia) {

        navigator.mediaDevices.getUserMedia({ audio: true })
            .then(function (stream) {
                start_microphone(stream)
            })
            .catch(function (err) {
                alert('Error capturing audio: ' + err)
            });
        );

    } else { alert('getUserMedia not supported in this browser.'); }


    function indexOfMax(arr) {
        if (arr.length === 0) {
            return -1;
        }

        var max = arr[0];
        var maxIndex = 0;

        for (var i = 1; i < arr.length; i++) {
            if (arr[i] > max) {
                maxIndex = i;
                max = arr[i];
            }
        }

        return maxIndex;
    }

    function start_microphone(stream) {

        //gain_node = audioContext.createGain();
        //gain_node.connect(audioContext.destination);

        microphone_stream = audioContext.createMediaStreamSource(stream);
        microphone_stream.connect(gain_node);

        // --- setup FFT

        script_processor_analysis_node = audioContext.createScriptProcessor(2048, 1, 1);
        script_processor_analysis_node.connect(gain_node);

        analyser_node = audioContext.createAnalyser();
        analyser_node.smoothingTimeConstant = 0;
        analyser_node.fftSize = 2048;

        microphone_stream.connect(analyser_node);

        analyser_node.connect(script_processor_analysis_node);

        var buffer_length = analyser_node.frequencyBinCount;

        var array_freq_domain = new Uint8Array(buffer_length);
        var array_time_domain = new Uint8Array(buffer_length);
        console.log(array_freq_domain);
        console.log('buffer_length ' + buffer_length);

        script_processor_analysis_node.onaudioprocess = function () {

            // get the average for the first channel
            analyser_node.getByteFrequencyData(array_freq_domain);
            analyser_node.getByteTimeDomainData(array_time_domain);

            // draw the spectrogram
            if (microphone_stream.playbackState == microphone_stream.PLAYING_STATE) {
                console.log(48000 * indexOfMax(array_freq_domain) / 2048);
            }
        };
    }



}