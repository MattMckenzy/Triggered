
function initializeCodeEditor(element, modulesGridHelper, isHTML = false) {
    ace.require("ace/ext/language_tools");
    var codeEditor = ace.edit(element);
    codeEditor.setTheme("ace/theme/twilight");
    if (isHTML) {
        codeEditor.session.setMode("ace/mode/html");
    }
    else {
        codeEditor.session.setMode("ace/mode/csharp");
    }
    codeEditor.session.setUseWrapMode(true);
    codeEditor.setAutoScrollEditorIntoView(true);
    codeEditor.setFontSize("18px");

    codeEditor.setOption("enableBasicAutocompletion", true);
    codeEditor.setOption("enableSnippets", true);
    codeEditor.setOption("enableLiveAutocompletion", true);
     
    codeEditor.commands.addCommand({
        name: "dotCommand",
        bindKey: { win: ".", mac: "." },
        exec: function () {
            var pos = codeEditor.selection.getCursor();
            var session = codeEditor.session;

            var curLine = (session.getDocument().getLine(pos.row)).trim();
            var curTokens = curLine.slice(0, pos.column).split(/\s+/);
            var curCmd = curTokens[0];
            if (!curCmd) return;

            codeEditor.insert(".");
        }
    });

    var staticWordCompleter = {
        getCompletions: async function (editor, session, pos, prefix, callback) {

            var curLine = (session.getDocument().getLine(pos.row)).trim();
            var lineTokens = curLine.slice(0, pos.column).split(/\s+/);
            var currentToken = lineTokens[lineTokens.length - 1];
            var terms = currentToken.split(/\./);
            var membersJson = await modulesGridHelper.invokeMethodAsync('GetNetObjectMembers', terms[terms.length - 2]);
            var members = JSON.parse(membersJson);
            var autocompletes = members ?? [];

            callback(null, autocompletes.map(function (autocomplete) {
                return {
                    caption: autocomplete.Item1,
                    value: autocomplete.Item2,
                    meta: autocomplete.Item3,
                    score: 1000
                };
            }));
        }  
    }

    codeEditor.completers.push(staticWordCompleter);

    codeEditor.commands.on("afterExec", function (e) {
        if (e.command.name == "dotCommand" || (e.command.name == "insertstring" && /^[\w\.]$/.test(e.args))) {
            codeEditor.execCommand("startAutocomplete");
        }
    });

    const myObserver = new ResizeObserver(entries => {
        codeEditor.resize();
        codeEditor.renderer.updateFull();
    });

    myObserver.observe(element);

    var codeTimeout;
    codeEditor.on('input', function () { 
        modulesGridHelper.invokeMethodAsync('SetCode', codeEditor.getValue());

        if (codeTimeout)
            window.clearTimeout(codeTimeout);

        codeTimeout = window.setTimeout(modulesGridHelper.invokeMethodAsync, 2000, 'SetCode', codeEditor.getValue());
    });
}

function setCode(element, code) {
    var codeEditor = ace.edit(element);
    codeEditor.setValue(code);
}

function showModal(modalElement) {
    var modal = new bootstrap.Modal(modalElement);
    modal.show();
}

window.audio = {
    play: function (volume) {
        var audio = document.getElementById('player');
        if (audio != null) {
            var audioSource = document.getElementById('playerSource');
            if (audioSource != null) {
                audio.volume = Number(volume);

                var isPlaying = audio.currentTime > 0 && !audio.paused && !audio.ended
                    && audio.readyState > audio.HAVE_CURRENT_DATA;

                if (!isPlaying) {
                    audio.play();
                }

            }
        }
    },
    mute: function () {
        var audio = document.getElementById('player');
        audio.muted = !audio.muted;
    }
}

function setupTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    })    
}