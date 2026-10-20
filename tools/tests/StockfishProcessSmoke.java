import com.mathwidu.chesscgi.StockfishProcess;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.concurrent.TimeUnit;

// Host JVM smoke of the exact Java transport shipped in the APK. The argument
// is the host Stockfish executable, not the Android engine (which needs Android).
public final class StockfishProcessSmoke {
    private static String waitFor(StockfishProcess session, String prefix) throws Exception {
        long deadline = System.nanoTime() + TimeUnit.SECONDS.toNanos(5);
        while (System.nanoTime() < deadline) {
            String line = session.readLine(50);
            if (line != null && line.startsWith(prefix)) return line;
        }
        throw new AssertionError("Missing UCI reply: " + prefix);
    }
    private static void initialize(StockfishProcess session) throws Exception {
        session.send("uci"); waitFor(session, "uciok");
        session.send("setoption name Threads value 1");
        session.send("setoption name Hash value 16");
        session.send("setoption name Ponder value false");
        session.send("setoption name Skill Level value 20");
        session.send("isready"); waitFor(session, "readyok");
    }
    private static void require(boolean ok, String message) {
        if (!ok) throw new AssertionError(message);
    }
    public static void main(String[] args) throws Exception {
        if (args.length != 1) throw new IllegalArgumentException("Expected host engine path");
        try (StockfishProcess session = StockfishProcess.open(args[0])) {
            initialize(session);
            session.send("position startpos");
            session.send("go movetime 50");
            require(waitFor(session, "bestmove ").matches("bestmove [a-h][1-8][a-h][1-8].*"), "Opening move");
            session.send("position startpos moves f2f3 e7e5 g2g4");
            session.send("go movetime 150");
            require(waitFor(session, "bestmove ").startsWith("bestmove d8h4"), "Fool's mate");
            session.send("position fen 8/7P/8/8/8/2k5/1r6/K7 w - - 0 1");
            session.send("go movetime 150");
            require(waitFor(session, "bestmove ").matches("bestmove h7h8[qrbn].*"), "Promotion");
            try { session.send("uci\nquit"); throw new AssertionError("Multiline accepted"); }
            catch (IOException expected) { }
        }
        for (int i = 0; i < 3; i++) {
            StockfishProcess session = StockfishProcess.open(args[0]);
            initialize(session);
            session.send("position startpos"); session.send("go movetime 10000");
            require(session.readLine(30) == null, "Polling should time out before bestmove");
            long start = System.nanoTime();
            session.close(); session.close();
            require(System.nanoTime() - start < TimeUnit.SECONDS.toNanos(1), "Shutdown blocked");
            try { session.readLine(50); throw new AssertionError("Closed session still readable"); }
            catch (IOException expected) { }
        }
        try (StockfishProcess session = StockfishProcess.open("/usr/bin/true")) {
            try { waitFor(session, "uciok"); throw new AssertionError("EOF treated as timeout"); }
            catch (IOException expected) { }
        }
        try { StockfishProcess.open("/missing/chess-engine"); throw new AssertionError("Missing executable accepted"); }
        catch (IOException expected) { }
        Path blocked = Files.createTempFile("stockfish-blocked-stdin-", ".sh");
        try {
            Files.writeString(blocked, "#!/bin/sh\nexec /bin/sleep 60\n");
            require(blocked.toFile().setExecutable(true), "Cannot create host fixture");
            try (StockfishProcess session = StockfishProcess.open(blocked.toString())) {
                long start = System.nanoTime();
                session.send("position fen " + "x".repeat(1024 * 1024));
                require(System.nanoTime() - start < TimeUnit.SECONDS.toNanos(1), "Send blocked on stdin");
                require(session.readLine(50) == null, "Silent child did not time out");
                start = System.nanoTime();
                session.close();
                require(System.nanoTime() - start < TimeUnit.SECONDS.toNanos(1), "Blocked stdin prevented shutdown");
            }
        } finally { Files.deleteIfExists(blocked); }
        require(ProcessHandle.current().descendants().noneMatch(ProcessHandle::isAlive), "Child engine leaked");
        System.out.println("STOCKFISH_JAVA_SMOKE passed: handshake, reuse, opening, mate, promotion, polling, cancel/reopen x3, EOF, invalid command, missing file, blocked stdin, no child leak");
    }
}
