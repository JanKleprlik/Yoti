async function pickAndUploadAudioFile(limit) {
    console.log('Uploading file in javascript');
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = '.wav';

    //C# method to get audio binary data from js to c#
    var processPartFile = Module.mono_bind_static_method("[Yoti.Wasm] Yoti.Shared.ViewModels.MainPageViewModel:ProcessEvent");

    //on file selected
    input.oninput = e => {
        var file = e.target.files[0];
        //size cannot be bigger than limit
        if ((file.size / 1024 / 1024) > limit) {
            alert('File size exceeds size limit of ' + limit + ' MB');
            processPartFile(file.name, true);
        }
        else {
            var reader = new FileReader();
            reader.readAsDataURL(file);
            reader.onload = readerEvent => {
                //this is the binary uploaded content
                var data = readerEvent.target.result;

                if (!data.startsWith('data:audio/wav;base64,')) {
                    //abortJavaScript defined at Recorder.js
                    abortJavaScript('unsupported format.');
                    return;
                }

                //audioMetadataOffset defined at Recorder.js
                let start = audioMetadataOffset;
                //uploadStepSize defined at Recorder.js
                let step = uploadStepSize;
                let part = data.substring(start, start + step);
                while (part !== "") {
                    processPartFile(part, false);
                    start = start + step;
                    part = data.substring(start, start + step);
                }

                //full file uploaded -> process it
                console.log('Done uploading file');
                processPartFile(file.name, true);
            }
        };
    };

    //call click event manually
    input.click();
}