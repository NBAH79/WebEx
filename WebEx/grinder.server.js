// Grinder Pages v1.0a
// 2023 Pavlovsky Ivan 

var server = server || (function () {
    return {
        init: function (Args) {
            return new Server(Args[0], Args[1]);
        },
    };
}());

const args = {
    command: 0, ref: 1, parent: 2, tag: 3, class: 4, style: 5, attr: 6, event: 7, inner: 8
}

class Server {
    static websocket;
    static queue = [];
    static waitForConnection = () => {
        return new Promise((resolve, reject) => {
            Server.queue.push({ resolve, reject });
        });
    }

    static async SEND(data, retries = 0) {
        if (data == null) return;
        try {
            Server.websocket.send(data);
        }
        catch (error) {
            if (retries < 4 && error.name === "InvalidStateError") {
                await waitForConnection(); //промиз закидывает в очередь
                SEND(data, retries + 1);
            }
            else {
                throw error;
            }
        }
    }


    

    constructor(YYY, KEY) {
        Server.queue = [];
        Server.websocket = Server.websocket || new WebSocket(YYY);
        Server.websocket.binaryType = "arraybuffer";
        Server.websocket.onopen = (e) => {
            writeToScreen("CONNECTED");
            this.doSendString(`${KEY}|${location}`); //надо дать знать серверу кто спрашивает и что
        };

        Server.websocket.onclose = (e) => {
            writeToScreen("DISCONNECTED");
        };

        Server.websocket.onerror = (e) => {
            writeToScreen(`<span style="color:red;">ERROR:</span> ${e.data}`);
        };

        Server.websocket.onmessage = (e) => {
            writeToScreen(`<span>SOME DATA:(${e})</span>`);
            if (e.data instanceof ArrayBuffer) {
                //var buffer = e.data;
                writeToScreen(`<span>RESPONSE DATA:(${e.data.length})</span>`);
            }
            else {
                //example
                //var jsonObject = JSON.parse(event.data);
                //var username = jsonObject.name;
                //var message = jsonObject.message;
                var array = e.data.split('|');
                var _command = array[args.command];
                if (_command) writeToScreen(`>>>>>> COMMAND:(${_command}) (${array}) `);
                switch (array.length) {
                    case 0: break; //ошибка
                    case 1:
                        writeToScreen(`<span>RESPONSE STRING:(${e.data})</span>`);
                        break;
                        if (_command == 'UPD') {
                            writeToScreen(`>>> UPDATE 1:(${array})`);
                            //var _object = array[1];
                            Server.SEND(e.data);
                            break;
                        }
                    case 2:
                        //тут будет swich
                        if (_command == 'URL') {
                            writeToScreen(`>>> URL:(${array})`);
                            var _location = array[1];
                            setLocation(_location);
                            document.body.innerHTML = "";
                            Server.SEND(e.data);
                            break;
                        }
                        if (_command == 'UPD') {
                            writeToScreen(`>>> UPDATE 2:(${array})`);
                            //var _object = array[1];
                            Server.SEND(e.data);
                            break;
                        }
                        if (_command == 'REL') { //page release с таймером
                            //var _object = array[1];
                            writeToScreen(`>>> RELEASE:(${array})`);
                            Server.SEND(e.data);
                            break;
                        }
                        
                        break;
                    default:
                        writeToScreen(`<span>PARSE STRING:(${array})</span>`);
                        //this.Parse(array);
                        this.Parse(array);
                        break;
                }
            };
        };

        Server.websocket.addEventListener('open', () => {
            Server.queue.forEach(r => r.resolve())
        });
    }

    readDataFile(e) {
        var file = e.target.files[0];
        if (!file) return;
        var reader = new FileReader();
        reader.onload = function () { this.SendData(new Int8Array(reader.result)); };
        reader.readAsArrayBuffer(file);
    }

    readBase64File(e) {
        var file = e.target.files[0];
        if (!file) return;
        var reader = new FileReader();
        reader.onload = function (e) { this.doSendData(e.target.result); };
        reader.readAsDataURL(file);
    }

    readTextFile(e) {
        var file = e.target.files[0];
        if (!file) return;
        var reader = new FileReader();
        reader.onload = function (e) { this.doSendData(e.target.result); };
        reader.readAsText(file);
    }

    

    Parse(array) {
        var _ref = array[args.ref];
        var _parent = array[args.parent];
        var _tag = (array.length > args.tag) ? array[args.tag] : "";
        var _class = (array.length > args.class) ? array[args.class] : "";
        var _style = (array.length > args.style) ? array[args.style] : "";
        var _attr = (array.length > args.attr) ? array[args.attr] : "";
        var _event = (array.length > args.event) ? array[args.event] : "";
        var _inner = (array.length > args.inner) ? array[args.inner] : "";

        var _classes = (_class) ? _class.split(' ') : null;
        var _styles = (_style) ? _style.split(';') : null;
        var _attributes = (_attr) ? _attr.split(';') : null;
        var _events = (_event) ? _event.split(';') : null;

        //если ref пустой то body

        var elements = document.querySelectorAll(`[ref="${_ref}"]`);
        if (elements.length == 0) {
            writeToScreen(`NEW ELEMENT:(${_ref})`);
            var parents = document.querySelectorAll(`[ref="${_parent}"]`);
            var newelem = document.createElement(_tag); 
            if (_class) newelem.className = _class;
            _styles?.forEach((s) => {
                var val = s.split(':');
                if (val.length == 2) newelem.style[val[0]] = val[1];
            });
            
            _attributes?.forEach((a) => {
                var val = a.split(':');
                if (val.length == 1) newelem.setAttribute(val[0]);
                if (val.length == 2) newelem.setAttribute(val[0], val[1]);
            });
            _events?.forEach((e) => {
                var x = e.split(':');
                var flags = (x.length > 1) ? parseInt(x[1]) : 0;
                //newelem.setAttribute("on" + x[0], function (__e) { Server.SEND(LsonEvent(_ref, flags, __e), 0); });
                newelem.addEventListener(x[0], function (__e) { Server.SEND(LsonEvent(_ref, flags, __e), 0); })
            })
            if (_inner) newelem.innerHTML = _inner;
            newelem.setAttribute("ref", _ref);
            if (parents.length>0) parents[0].appendChild(newelem); //можно организовать множество, но не нужно
            else document.body.appendChild(newelem);
        }
        else {
            writeToScreen(`FOUND ELEMENT:(${array[0]}:${array.length})`);
            var elem = elements[0];//можно организовать изменение множества, но не нужно, берем нулевой
            //не надо этого делать потому что евенты не меняются с работой страницы
            if (array.length < 2) { //удаление
                _timeout = parseInt(_parent);
                if (_timeout == NaN) elem.remove();
                else setTimeout(function () { elem.remove(); }, parseInt(_timeout)); return;
            }
            if (_class) elem.className = _class;
            _styles?.forEach((s) => {
                var val = s.split(':');
                if (val.length == 2) elem.style[val[0]] = val[1];
            });
            _attributes?.forEach((a) => {
                var val = a.split(':');
                //бывают аттрибуты без значения, надо как то их организовать
                if (val.length == 1) elem.removeAttribute(val[0]); //а надо ли это? типа если значения больше нет то он не нужен?
                if (val.length == 2) elem.setAttribute(val[0], val[1]);
            });
         
            //removeElementListener не работает без параметра который некуда записать
            //addElementListener добавляет еще и еще - проверено
            //можно вставить инлайн скрипты, но выглядит уродски
            //а копирование элемента убивает анимацию
            //а почему не сделали очистку событий - просто не надо это делать
            // если смысл элемента поменялся, надо заменять весь элемент а не его евенты!
            // но теоретически добавить евенты можно, или поменять
            if (_events) {
                writeToScreen(`EVENT REPLACE:(${_events})`);
                elem.replaceWith(elem.cloneNode(true)); //true иначе не сохранятся подобьекты, добавленные скриптом
                elem.setAttribute("ref", _ref);
                _events?.forEach((e) => {
                    var x = e.split(':');
                    var flags = (x.length > 1) ? parseInt(x[1]) : 0;
                    //elem.removeEventListener(x[0]);
                    //elem.setAttribute("on" + x[0], function (__e) { Server.SEND(LsonEvent(_ref, flags, __e), 0); });
                    elem.addEventListener(x[0], function (__e) { Server.SEND(LsonEvent(_ref, flags, __e), 0); })
                })
            }
            
            if (_inner) elem.innerHTML = _inner;
        }
        return;
    }

    doSendString(message) {
        if (message.length < 1000) writeToScreen(`SENT: ${message}`);
        else writeToScreen(`SENT TEXT: ${message.length}`);
        Server.SEND(message).then(); //this.websocket.
    };


    


    doSendData(data) {
        //var array = new Int8Array(data.length+4);
        //for (let n=0;n<data.length;n++){{ array[n+4]=data[n];}};
        Server.SEND(this,data); //this.websocket.
        writeToScreen(`SENT: (${data.length})`);
    };
};


function LsonEvent(obj, flags, event) {
    var ret = `${obj}|${event.type}|${flags}|`; //первая палка откуда то уже есть
    switch (event.type) {
        case 'click':
            if (flags == 0 || flags==NaN) break;
            if ((flags & 1) > 0) ret += `offsetX:${event.offsetX};offsetY:${event.offsetY};`;
            if ((flags & 2) > 0) ret += `pageX:${event.pageX};pageY:${event.pageY};`;

            if ((flags & 0x10) > 0) ret += `shiftKey:${event.shiftKey};`;
            if ((flags & 0x20) > 0) ret += `altKey:${event.altKey};`;
            if ((flags & 0x40) > 0) ret += `ctrlKey:${event.ctrlKey};`;
            if ((flags & 0x80) > 0) ret += `metaKey:${event.metaKey};`;
            if ((flags & 0x100) > 0) ret += `timeStamp:${event.timeStamp};`;
            break;
        default: ret += stringify(e);
    }
    writeToScreen(`SENT:(${ret})`);
    return ret;
}

function stringifyEvent(e) {
    const obj = {};
    for (let k in e) {
        obj[k] = e[k];
    }
    return JSON.stringify(obj, (k, v) => {
        if (v instanceof Node) return 'Node';
        if (v instanceof Window) return 'Window';
        return v;
    }, ' ');
}

function writeToScreen(message) {
    console.info(message);
    //output.insertAdjacentHTML("afterbegin", `<p>${message}</p>`);
};

function addEvent(type, handler) {
    if (this.addEventListener)
        this.addEventListener(type, handler, false);
    else {
        this.attachEvent("on" + type,
            function (event) {
                return handler.call(this, event);
            });
    }
}
//const interval = setinterval(function ping() {
//    wss.clients.foreach(function each(ws) {
//        if (ws.isalive === false) {
//            console.log("i am terminating");
//            return ws.terminate();
//        }
//        ws.isalive = false;
//        ws.ping(self.noop);
//    });
//}, 30000);

//async function downloadfromstream(filename, contentstreamreference) {{
// const arraybuffer = await contentstreamreference.arraybuffer();
// const blob = new blob([arraybuffer]);
// const url = url.createobjecturl(blob);
// const anchorelement = document.createelement(""a"");
//  anchorelement.href = url;
//  anchorelement.download = filename ?? """";
//  anchorelement.click();
//  anchorelement.remove();
//  url.revokeobjecturl(url);
//}};

function setLocation(curLoc, anchor) {
    try {
        history.pushState(null, null, curLoc);
        // return;
    } catch (e) { }
    if (anchor) location.hash = '#' + anchor;
}

function setDocument(curLoc) {
    fetch(curLoc, {
        headers: {
            'Accept': 'application/json'
        }
    })
        .then(response => response.text())
        .then(text => setDocument(text))
}

function setDocument(text) {
    document.open();
    document.write(text);
    document.close();
}