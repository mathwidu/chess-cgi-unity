// A channel owns one persistent single-threaded WASM Worker. Cancellation kills
// it: UCI has no request IDs and a late bestmove must never enter another game.
mergeInto(LibraryManager.library, {
  $ChessStockfish: {
    nextHandle: 0,
    channels: {},
    create: function () {
      var handle = ++ChessStockfish.nextHandle;
      ChessStockfish.channels[handle] = { worker: null, request: null, queue: [], phase: "idle", generation: 0 };
      return handle;
    },
    terminate: function (channel) {
      channel.generation++;
      if (channel.worker) {
        channel.worker.onmessage = null;
        channel.worker.onerror = null;
        channel.worker.onmessageerror = null;
        channel.worker.terminate();
      }
      channel.worker = null;
      channel.request = null;
      channel.phase = "idle";
    },
    emit: function (channel, type, value) {
      if (channel.request) channel.queue.push(JSON.stringify({
        requestId: channel.request.id, type: type, value: value || ""
      }));
    },
    fail: function (channel, message) {
      ChessStockfish.emit(channel, "error", message);
      ChessStockfish.terminate(channel);
    },
    send: function (channel, command) { channel.worker.postMessage(command); },
    prepare: function (channel) {
      channel.phase = "preparing";
      ChessStockfish.send(channel, "setoption name Skill Level value " + channel.request.skill);
      ChessStockfish.send(channel, "isready");
    },
    receive: function (channel, line) {
      if (typeof line !== "string") return;
      line = line.trim();
      if (channel.phase === "initializing" && line === "uciok") {
        ChessStockfish.send(channel, "setoption name Hash value 16");
        ChessStockfish.send(channel, "setoption name Ponder value false");
        ChessStockfish.send(channel, "setoption name UCI_LimitStrength value false");
        ChessStockfish.send(channel, "ucinewgame");
        ChessStockfish.prepare(channel);
      } else if (channel.phase === "preparing" && line === "readyok") {
        channel.phase = "searching";
        ChessStockfish.emit(channel, "searching");
        var request = channel.request;
        ChessStockfish.send(channel, "position fen " + request.fen + (request.moves ? " moves " + request.moves : ""));
        ChessStockfish.send(channel, "go movetime " + request.moveTime);
      } else if (channel.phase === "searching" && /^bestmove(?:\s|$)/.test(line)) {
        var fields = line.split(/\s+/);
        ChessStockfish.emit(channel, "bestmove", fields[1] || "");
        channel.request = null;
        channel.phase = "idle";
      } else if (/^(?:Aborted|abort\(|Error|ERROR|RuntimeError)/.test(line)) {
        ChessStockfish.fail(channel, "O motor web encerrou inesperadamente. Tente novamente.");
      }
    },
    begin: function (handle, id, fen, moves, skill, moveTime) {
      var channel = ChessStockfish.channels[handle];
      if (!channel) return;
      if (channel.request) ChessStockfish.terminate(channel);
      channel.queue = [];
      channel.request = { id: id, fen: fen, moves: moves, skill: skill, moveTime: moveTime };
      try {
        if (!channel.worker) {
          // Resolve against the hosting page, not Unity's Build/ framework URL.
          var assetBase = typeof window !== 'undefined' && window.CiCoChessRuntime && window.CiCoChessRuntime.assetBaseUrl;
          var url = new URL("stockfish/stockfish-18-lite-single.js", assetBase || document.baseURI);
          var worker = new Worker(url.href);
          channel.worker = worker;
          var generation = channel.generation;
          worker.onmessage = function (event) {
            if (channel.generation !== generation || channel.worker !== worker) return;
            try { ChessStockfish.receive(channel, event.data); }
            catch (error) { ChessStockfish.fail(channel, "Falha na comunicacao com a IA web. Tente novamente."); }
          };
          worker.onerror = function (event) {
            if (channel.generation !== generation || channel.worker !== worker) return;
            if (event.preventDefault) event.preventDefault();
            ChessStockfish.fail(channel, "Nao foi possivel carregar a IA web. Confira os arquivos Stockfish e tente novamente.");
          };
          worker.onmessageerror = function () {
            if (channel.generation !== generation || channel.worker !== worker) return;
            ChessStockfish.fail(channel, "A IA web enviou uma resposta invalida. Tente novamente.");
          };
          channel.phase = "initializing";
          ChessStockfish.send(channel, "uci");
        } else ChessStockfish.prepare(channel);
      } catch (error) {
        ChessStockfish.fail(channel, "Nao foi possivel iniciar a IA web. Confira o suporte a WebAssembly e Web Workers.");
      }
    }
  },
  ChessStockfish_Create__deps: ["$ChessStockfish"],
  ChessStockfish_Create: function () { return ChessStockfish.create(); },
  ChessStockfish_Begin__deps: ["$ChessStockfish"],
  ChessStockfish_Begin: function (handle, id, fen, moves, skill, moveTime) {
    ChessStockfish.begin(handle, id, UTF8ToString(fen), UTF8ToString(moves), skill, moveTime);
  },
  ChessStockfish_TakeMessage__deps: ["$ChessStockfish"],
  ChessStockfish_TakeMessage: function (handle) {
    var channel = ChessStockfish.channels[handle];
    if (!channel || channel.queue.length === 0) return 0;
    var json = channel.queue.shift();
    var size = lengthBytesUTF8(json) + 1;
    var buffer = _malloc(size);
    stringToUTF8(json, buffer, size);
    // IL2CPP frees returned strings allocated with Emscripten malloc.
    return buffer;
  },
  ChessStockfish_Cancel__deps: ["$ChessStockfish"],
  ChessStockfish_Cancel: function (handle) {
    var channel = ChessStockfish.channels[handle];
    if (!channel) return;
    ChessStockfish.terminate(channel);
    channel.queue = [];
  },
  ChessStockfish_Dispose__deps: ["$ChessStockfish"],
  ChessStockfish_Dispose: function (handle) {
    var channel = ChessStockfish.channels[handle];
    if (!channel) return;
    ChessStockfish.terminate(channel);
    delete ChessStockfish.channels[handle];
  }
});
