package com.mathwidu.chesscgi;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.Closeable;
import java.io.File;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.nio.charset.StandardCharsets;
import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

/** UCI transport only. The executable and its NNUE networks ship inside the APK. */
public final class StockfishProcess implements Closeable {
    private final Process process;
    private final BufferedWriter input;
    private final ArrayBlockingQueue<String> commands = new ArrayBlockingQueue<>(32);
    private final ArrayBlockingQueue<String> output = new ArrayBlockingQueue<>(128);
    private final AtomicBoolean closed = new AtomicBoolean();
    private final Thread reader;
    private final Thread writer;
    private volatile boolean ended;
    private volatile IOException failure;

    public static StockfishProcess open(String executable) throws IOException {
        return new StockfishProcess(executable);
    }

    private StockfishProcess(String executable) throws IOException {
        File file = new File(executable);
        if (!file.isFile() || !file.canExecute())
            throw new IOException("Stockfish is not installed as an executable native library.");
        // No shell, chmod, writable-directory copy, download or System.loadLibrary.
        process = new ProcessBuilder(file.getAbsolutePath())
            .directory(file.getParentFile()).redirectErrorStream(true).start();
        input = new BufferedWriter(new OutputStreamWriter(process.getOutputStream(), StandardCharsets.UTF_8));
        reader = new Thread(this::readOutput, "Chess-Stockfish-output");
        reader.setDaemon(true);
        writer = new Thread(this::writeInput, "Chess-Stockfish-input");
        writer.setDaemon(true);
        reader.start();
        writer.start();
    }

    public void send(String command) throws IOException {
        if (closed.get()) throw new IOException("Stockfish session is closed.");
        if (failure != null) throw failure;
        if (command == null || command.indexOf('\n') >= 0 || command.indexOf('\r') >= 0)
            throw new IOException("A UCI command must be one line.");
        // Never block a JNI caller on a full stdin pipe: it must remain able to
        // observe cancellation and kill even a child that stopped reading input.
        if (!commands.offer(command)) throw new IOException("Stockfish command queue overflow.");
    }

    private void writeInput() {
        try {
            while (!closed.get()) {
                String command = commands.poll(100, TimeUnit.MILLISECONDS);
                if (command == null) continue;
                input.write(command);
                input.newLine();
                input.flush();
            }
        } catch (IOException exception) {
            if (!closed.get()) {
                failure = exception;
                process.destroyForcibly();
            }
        } catch (InterruptedException exception) {
            Thread.currentThread().interrupt();
        }
    }

    // A null return is a polling timeout. Process exit and protocol overflow throw.
    public String readLine(int timeoutMilliseconds) throws IOException, InterruptedException {
        if (timeoutMilliseconds < 1 || timeoutMilliseconds > 1000)
            throw new IllegalArgumentException("Bounded polling is required.");
        if (closed.get()) throw new IOException("Stockfish session is closed.");
        if (failure != null) throw failure;
        String line = output.poll(timeoutMilliseconds, TimeUnit.MILLISECONDS);
        if (failure != null) throw failure;
        if (line == null && ended) throw new IOException("Stockfish exited before replying.");
        return line;
    }

    private void readOutput() {
        try (BufferedReader stream = new BufferedReader(
                new InputStreamReader(process.getInputStream(), StandardCharsets.UTF_8))) {
            String line;
            while (!closed.get() && (line = stream.readLine()) != null) {
                // Search telemetry is not consumed by gameplay; keep all control replies.
                if (line.startsWith("info ")) continue;
                if (!output.offer(line)) {
                    failure = new IOException("Stockfish output queue overflow.");
                    process.destroyForcibly();
                    break;
                }
            }
        } catch (IOException exception) {
            if (!closed.get()) failure = exception;
        } finally { ended = true; }
    }

    // Called by the C# search/cleanup worker, never synchronously during Unity pause.
    @Override public void close() {
        if (!closed.compareAndSet(false, true)) return;
        // Destroy before closing streams: readLine may be blocked in the reader thread.
        process.destroy();
        try {
            if (!process.waitFor(200, TimeUnit.MILLISECONDS)) {
                process.destroyForcibly();
                process.waitFor(200, TimeUnit.MILLISECONDS);
            }
            reader.interrupt();
            writer.interrupt();
            reader.join(100);
            writer.join(100);
        } catch (InterruptedException exception) {
            process.destroyForcibly();
            Thread.currentThread().interrupt();
        } finally {
            try { input.close(); } catch (IOException ignored) { }
            try { process.getInputStream().close(); } catch (IOException ignored) { }
            output.clear();
            commands.clear();
        }
    }
}
