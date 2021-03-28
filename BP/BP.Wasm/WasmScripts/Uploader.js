async function pick_and_upload_file_by_parts(limit, initial_start) {
    console.log('Uploading file in javascript');
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = '.wav';
    //on file selected
    input.oninput = e => {

        var file = e.target.files[0];
        //size cannot be bigger than 50  Mbs
        if ((file.size / 1024 / 1024) > limit) {
            alert('File size exceeds size limit of ' + limit + ' Mb');
        }
        else {
            var reader = new FileReader();
            reader.readAsDataURL(file);
            reader.onload = readerEvent => {
                //this is the binary uploaded content
                var data = readerEvent.target.result;
                //invoke C# method to get audio binary data
                var processPartFile = Module.mono_bind_static_method("[BP.Wasm] BP.Shared.Views.MainPage:ProcessEvent");

                if (!data.startsWith('data:audio/wav;base64,')) {
                    console.log('Unsupported format.');
                    alert('Unsupported format.');
                    return;
                }

                //start ... audio metadata of uploaded file
                let start = initial_start;
                let step = 500000;
                let part = data.substring(start, start + step);
                while (part !== "") {
                    processPartFile(part, false);
                    start = start + step;
                    part = data.substring(start, start + step);
                }

                //full file uploaded -> process it
                console.log('Done uploading file');
                processPartFile("", true);
            }
        };
    };

    //on canceled
    input.oncancel = e => {
        htmlElement.dispatchEvent(new Event("CancelEvent"));
    }
    input.click();
}