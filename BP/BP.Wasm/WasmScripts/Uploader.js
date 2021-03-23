function pick_and_upload_file_by_parts() {
    console.log('calling javascript');
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = '.wav';
    input.onchange = e => {
        var file = e.target.files[0];
        //size cannot be bigger than 50  Mbs
        if ((file.size / 1024 / 1024) > 50) {
            alert('File size exceeds 50 Mb');
        }
        else {
            var reader = new FileReader();
            reader.readAsDataURL(file);
            reader.onload = readerEvent => {
                //this is the binary uploaded content
                var data = readerEvent.target.result;
                //invoke C# method to get audio binary data
                //var processPartFile = Module.mono_bind_static_method("[BP.Wasm] BP.Shared.Views.MainPage:ProcessUploadedPart");
                var processPartFile = Module.mono_bind_static_method("[BP.Wasm] BP.Shared.Views.MainPage:SelectFile");
                let start = 100;
                let step = 100000;
                let part = data.substring(start, start + step);
                while (part !== "") {
                    console.log(start);

                    processPartFile(part);
                    start = start + step;
                    part = data.substring(start, start + step);
                }

                var processFullFile = Module.mono_bind_static_method("[BP.Wasm] BP.Shared.Views.MainPage:ProcessFullSong");
                processFullFile();
            }
        };
    };
    input.click();
}