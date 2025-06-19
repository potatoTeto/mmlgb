namespace MMLGB
{
    public class Parser
    {
        private List<Lexer.Token> tokens;
        private Song song, song_root;
        private int position;
        private bool in_macro;
        private Lexer.Token next;

        private static readonly string[] CHANNEL_NAMES = { "A", "B", "C", "D" };
        private static readonly string[] NOTE_NAMES = { "c", "cs", "d", "ds", "e", "f", "fs", "g", "gs", "a", "as", "b" };
        private static readonly int[] WAVE_VOLUMES = { 0, 96, 64, 32 };

        private const int BEAT_STEPS = 48;
        private const int BAR_STEPS = 4 * BEAT_STEPS;
        private const int TIMA_SPEED = 4096;

        public Parser(List<Lexer.Token> tokens)
        {
            this.tokens = tokens;
            song = new Song();
            song_root = song;
            position = 0;
            in_macro = false;
            next = tokens[0];
        }

        public Song Parse()
        {
            while (next.Type != Lexer.TokenType.EOF)
            {
                if (next.Type == Lexer.TokenType.CHANNEL)
                {
                    ParseChannel();
                }
                else if (next.Type == Lexer.TokenType.MACRO)
                {
                    ParseDefinition();
                }
                else if (next.Type == Lexer.TokenType.NEWLINE)
                {
                    Eat();
                }
                else
                {
                    throw new ParserException($"Unexpected token {next}.", next);
                }
            }

            for (int i = 0; i < 4; ++i)
            {
                song.AddData(i, (int)Song.CMD.T_EOF);
            }

            return song;
        }

        private void ParseDefinition()
        {
            if (next.Data == "@wave")
            {
                ParseWaveData();
            }
            else if (next.Data == "@@")
            {
                ParseMacroData();
            }
            else
            {
                throw new ParserException($"Unexpected token {next}.", next);
            }
        }

        private void ParseWaveData()
        {
            Eat();

            if (next.Type != Lexer.TokenType.NUMBER)
                throw new ParserException("Expected wave data id.", next);

            int id = ParseInt(next.Data);
            if (id < 0)
                throw new ParserException("Invalid wave data id. Must be nonnegative integer.", next);
            Eat();

            Eat(Lexer.TokenType.ASSIGN, "=");
            Eat(Lexer.TokenType.LCURLY, "{");

            List<int> samples = new List<int>(32);
            for (int i = 0; i < 32; ++i)
            {
                while (next.Type == Lexer.TokenType.NEWLINE) Eat();
                if (next.Type != Lexer.TokenType.NUMBER)
                    throw new ParserException("Invalid wave sample. Expected number.", next);

                int sample = ParseInt(next.Data);
                Eat();
                if (sample < 0 || sample > 15)
                    throw new ParserException($"Invalid wave sample {sample}. Expected 0-15.", next);

                samples.Add(sample);

                while (next.Type == Lexer.TokenType.NEWLINE) Eat();
            }

            Eat(Lexer.TokenType.RCURLY, "}");
            Eat(Lexer.TokenType.NEWLINE, "Line break");

            song.AddWaveData(id, samples);
        }

        private void ParseMacroData()
        {
            Eat();

            if (next.Type != Lexer.TokenType.NUMBER)
                throw new ParserException("Expected macro id.", next);

            int id = ParseInt(next.Data);
            if (id < 0)
                throw new ParserException("Invalid macro id. Must be nonnegative integer.", next);
            Eat();

            Eat(Lexer.TokenType.ASSIGN, "=");
            Eat(Lexer.TokenType.LCURLY, "{");

            List<Lexer.Token> macro = new List<Lexer.Token>();
            while (next.Type != Lexer.TokenType.RCURLY)
            {
                while (next.Type == Lexer.TokenType.NEWLINE) Eat();

                macro.Add(new Lexer.Token(next));
                Eat();

                while (next.Type == Lexer.TokenType.NEWLINE) Eat();
            }
            macro.Add(new Lexer.Token(Lexer.TokenType.NEWLINE, "\n"));

            Eat(Lexer.TokenType.RCURLY, "}");
            Eat(Lexer.TokenType.NEWLINE, "Line break");

            // Backup parser state
            var tokensBak = tokens;
            var songBak = song;
            var positionBak = position;
            var nextBak = next;

            // Temporary song just for macro parsing
            Song tempSong = new Song();
            song = tempSong;
            tokens = macro;
            position = 0;
            in_macro = true;
            next = tokens[0];

            // Parse macro commands into tempSong
            bool[] active = { true, false, false, false };
            ParseCommands(active);

            // Extract macro data from channel 0
            List<int> macroData = new List<int>(tempSong.GetChannel(0));

            // Restore previous parser state
            tokens = tokensBak;
            song = songBak;
            position = positionBak;
            next = nextBak;
            in_macro = false;

            // Store macro data in the real song
            song.AddMacroData(id, macroData);
        }

        private void ParseChannel()
        {
            bool[] active = new bool[4];
            while (next.Type == Lexer.TokenType.CHANNEL)
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (next.Data == CHANNEL_NAMES[i])
                    {
                        active[i] = true;
                        break;
                    }
                }
                Eat();
            }

            ParseCommands(active);
        }

        private int ParseLength(bool required)
        {
            int length = 0;
            if (next.Type == Lexer.TokenType.NUMBER)
            {
                length = ParseInt(next.Data);
                if (length < 1 || length > BAR_STEPS)
                    throw new ParserException($"Invalid note length {length}. Expected 1-{BAR_STEPS}.", next);
                Eat();

                if ((BAR_STEPS / length) * length != BAR_STEPS)
                    throw new ParserException($"Invalid note length {length}. Not enough precision.", next);

                length = BAR_STEPS / length;

                int dot = length / 2;
                while (next.Type == Lexer.TokenType.DOT)
                {
                    if (dot <= 0)
                        throw new ParserException("Too many dots in length. Not enough precision.", next);
                    Eat();
                    length += dot;
                    dot /= 2;
                }
            }
            else if (next.Type == Lexer.TokenType.ASSIGN)
            {
                Eat();

                length = ParseInt(next.Data);
                if (length < 1 || length > 255)
                    throw new ParserException($"Invalid note frame length {length}. Expected 1-255.", next);
                Eat();
            }
            else if (required)
            {
                throw new ParserException("Expected note length.", next);
            }

            if (next.Type == Lexer.TokenType.TIE)
            {
                Eat();
                length += ParseLength(true);
            }

            return length;
        }

        private void Eat(Lexer.TokenType expected, string message)
        {
            if (next.Type != expected)
                throw new ParserException($"Found token {next.Data}. Expected {message}.", next);

            Eat();
        }

        private void Eat()
        {
            position++;
            if (position >= tokens.Count)
                throw new ParserException("End of file reached.", next);

            next = tokens[position];
        }

        private static int ParseInt(string s)
        {
            if (s.StartsWith("-"))
                return -ParseInt(s.Substring(1));
            else if (s.StartsWith("0x"))
                return Convert.ToInt32(s.Substring(2), 16);
            else if (s.StartsWith("0b"))
                return Convert.ToInt32(s.Substring(2), 2);
            else
                return int.Parse(s);
        }


        private void ParseCommand(bool[] active)
        {
            switch (next.Data)
            {
                case "r":
                    Eat();
                    int rLength = ParseLength(false);
                    if (rLength == 0)
                    {
                        song.AddData(active, (int)Song.CMD.T_REST);
                    }
                    else
                    {
                        song.AddData(active, (int)Song.CMD.T_REST | 0x80);
                        song.AddData(active, rLength);
                        for (int i = 0; i < rLength / 255; ++i)
                        {
                            song.AddData(active, (int)Song.CMD.T_WAIT | 0x80);
                            song.AddData(active, 255);
                        }
                    }
                    break;

                case "w":
                    Eat();
                    int wLength = ParseLength(false);
                    if (wLength == 0)
                    {
                        song.AddData(active, (int)Song.CMD.T_WAIT);
                    }
                    else
                    {
                        song.AddData(active, (int)Song.CMD.T_WAIT | 0x80);
                        song.AddData(active, wLength);
                        for (int i = 0; i < wLength / 255; ++i)
                        {
                            song.AddData(active, (int)Song.CMD.T_WAIT | 0x80);
                            song.AddData(active, 255);
                        }
                    }
                    break;

                case "o":
                    Eat();
                    if (next.Type != Lexer.TokenType.NUMBER)
                        throw new ParserException("Expected number after octave command.", next);
                    int octave = ParseInt(next.Data);
                    Eat();
                    song.AddData(active, (int)Song.CMD.T_OCTAVE);
                    song.AddData(active, octave);
                    break;

                case "<":
                    Eat();
                    song.AddData(active, (int)Song.CMD.T_OCT_DOWN);
                    break;

                case ">":
                    Eat();
                    song.AddData(active, (int)Song.CMD.T_OCT_UP);
                    break;

                case "l":
                    Eat();
                    var lengthToken = next;
                    int length = ParseLength(true);
                    if (length > 255)
                        throw new ParserException("Length overflow. Lengths more than 255 frames not allowed for l command.", lengthToken);
                    song.AddData(active, (int)Song.CMD.T_LENGTH);
                    song.AddData(active, length);
                    break;

                case "v":
                    Eat();
                    if (next.Type != Lexer.TokenType.NUMBER)
                        throw new ParserException("Invalid volume. Expected number.", next);
                    int volume = ParseInt(next.Data);
                    if (active[2] && (volume < 0 || volume > 3))
                        throw new ParserException("Invalid volume for wave channel. Expected 0-3.", next);
                    if (volume < 0 || volume > 15)
                        throw new ParserException("Invalid volume value. Expected 0-15.", next);
                    Eat();
                    for (int i = 0; i < 4; ++i)
                    {
                        if (active[i])
                        {
                            song.AddData(i, (int)Song.CMD.T_VOL);
                            song.AddData(i, i == 2 ? WAVE_VOLUMES[volume] : volume << 4);
                        }
                    }
                    break;

                case "t":
                    Eat();
                    if (next.Type != Lexer.TokenType.NUMBER)
                        throw new ParserException("Invalid tempo command. Expected number.", next);
                    int bpm = ParseInt(next.Data);
                    Eat();
                    float ups = (float)bpm / 60.0f * BEAT_STEPS;
                    int mod = (int)Math.Round((float)TIMA_SPEED / ups);
                    song.AddData(active, (int)Song.CMD.T_TEMPO);
                    song.AddData(active, 255 - mod);
                    break;

                case "y":
                    Eat();
                    bool neg = false;
                    if (next.Type == Lexer.TokenType.DASH)
                    {
                        Eat();
                        neg = true;
                    }
                    if (next.Type != Lexer.TokenType.NUMBER)
                        throw new ParserException("Invalid panning command. Expected number.", next);
                    int pan = ParseInt(next.Data);
                    Eat();
                    int val = pan == 0 ? 17 : (neg ? 16 : 1);
                    song.AddData(active, (int)Song.CMD.T_PAN);
                    song.AddData(active, val);
                    break;

                case "L":
                    Eat();
                    song.AddData(active, (int)Song.CMD.T_LOOP);
                    break;

                default:
                    throw new ParserException($"Unknown command '{next.Data}'.", next);
            }
        }

        private void ParseCommands(bool[] active)
        {
            while (next.Type != Lexer.TokenType.NEWLINE)
            {
                switch (next.Type)
                {
                    case Lexer.TokenType.NOTE:
                        {
                            int firstNote = (int)Song.CMD.T_C;
                            int lastNote = (int)Song.CMD.T_B;
                            int note = 0;

                            for (int i = 0; i < 12; ++i)
                            {
                                if (next.Data == NOTE_NAMES[i])
                                {
                                    note = firstNote + i;
                                    break;
                                }
                            }
                            Eat();

                            if (next.Type == Lexer.TokenType.SHARP)
                            {
                                note++;
                                Eat();
                            }
                            else if (next.Type == Lexer.TokenType.DASH)
                            {
                                note--;
                                Eat();
                            }

                            if (note == firstNote - 1) note = lastNote;
                            else if (note == lastNote + 1) note = firstNote;

                            int length = ParseLength(false);

                            if (length == 0)
                            {
                                song.AddData(active, note);
                            }
                            else
                            {
                                song.AddData(active, note | 0x80);
                                song.AddData(active, length % 255);
                                for (int i = 0; i < length / 255; ++i)
                                {
                                    song.AddData(active, (int)Song.CMD.T_WAIT | 0x80);
                                    song.AddData(active, 255);
                                }
                            }
                            break;
                        }

                    case Lexer.TokenType.COMMAND:
                        ParseCommand(active);
                        break;

                    case Lexer.TokenType.TIE:
                        {
                            Eat();
                            int length = ParseLength(false);
                            if (length == 0)
                            {
                                song.AddData(active, (int)Song.CMD.T_WAIT);
                            }
                            else
                            {
                                song.AddData(active, (int)Song.CMD.T_WAIT | 0x80);
                                song.AddData(active, length);
                            }
                            break;
                        }

                    case Lexer.TokenType.MACRO:
                        ParseMacroCommand(active);
                        break;

                    case Lexer.TokenType.LBRACKET:
                        Eat();
                        song.AddData(active, (int)Song.CMD.T_REP_START);
                        break;

                    case Lexer.TokenType.RBRACKET:
                        Eat();
                        if (next.Type != Lexer.TokenType.NUMBER)
                            throw new ParserException("Expected repetition count.", next);
                        int reps = ParseInt(next.Data);
                        if (reps < 2)
                            throw new ParserException("Invalid repetition count. Must be >= 2.", next);
                        Eat();
                        song.AddData(active, (int)Song.CMD.T_REP_END);
                        song.AddData(active, reps);
                        break;

                    case Lexer.TokenType.EOF:
                        return;

                    default:
                        throw new ParserException($"Unexpected token {next}.", next);
                }
            }
        }

        private void ParseMacroCommand(bool[] active)
        {
            switch (next.Data)
            {
                case "@wave":
                    Eat();
                    if (next.Type != Lexer.TokenType.NUMBER)
                        throw new ParserException("Expected wave data id.", next);
                    int waveId = ParseInt(next.Data);
                    Eat();
                    int? id = song.GetWaveIndex(waveId);
                    if (!id.HasValue)
                        throw new ParserException($"Wave \"{next.Data}\" not defined.", next);
                    song.AddData(active, (int)Song.CMD.T_WAVE);
                    song.AddData(active, id.Value);
                    break;

                case "@ve":
                    Eat();
                    bool increasing = true;
                    if (next.Type == Lexer.TokenType.DASH)
                    {
                        Eat();
                        increasing = false;
                    }
                    if (next.Type != Lexer.TokenType.NUMBER)
                        throw new ParserException("Invalid volume envelope. Expected number.", next);
                    int envelope = ParseInt(next.Data);
                    Eat();
                    if (envelope > 7)
                        throw new ParserException("Invalid volume envelope. Expected values from -7 to 7.", next);
                    if (increasing) envelope |= (1 << 3);
                    song.AddData(active, (int)Song.CMD.T_ENV);
                    song.AddData(active, envelope);
                    break;

                case "@wd":
                    Eat();
                    if (next.Type != Lexer.TokenType.NUMBER)
                        throw new ParserException("Invalid wave duty. Expected number.", next);
                    int duty = ParseInt(next.Data);
                    if (duty < 0 || duty > 3)
                        throw new ParserException("Invalid wave duty. Expected values 0-3.", next);
                    Eat();
                    song.AddData(active, (int)Song.CMD.T_WAVEDUTY);
                    song.AddData(active, duty << 6);
                    break;




                case "@p":
                    if (active[2])
                        throw new ParserException("@p only allowed in channel 1, 2 and 4.", next);
                    Eat();

                    if (next.Type != Lexer.TokenType.NUMBER)
                        throw new ParserException("Expected speed after @p macro.", next);

                    int speed = ParseInt(next.Data);
                    if (speed < 0 || speed > 128)
                        throw new ParserException("Invalid portamento speed. Expected 0-128.", next);
                    Eat();

                    song.AddData(active, (int)Song.CMD.T_PORTAMENTO);
                    song.AddData(active, speed);
                    break;

                case "@po":
                    if (active[3])
                        throw new ParserException("@po only allowed in channel 1, 2 and 3.", next);
                    Eat();

                    bool negative = false;
                    if (next.Type == Lexer.TokenType.DASH)
                    {
                        negative = true;
                        Eat();
                    }

                    if (next.Type != Lexer.TokenType.NUMBER)
                        throw new ParserException("Expected number after @po macro.", next);

                    int offset = ParseInt(next.Data);
                    if (offset < 0 || offset > 127)
                        throw new ParserException("Invalid pitch offset. Expected values 0-127.", next);
                    Eat();

                    if (negative) offset = -offset;

                    song.AddData(active, (int)Song.CMD.T_PITCH_OFFSET);
                    song.AddData(active, offset + 128);
                    break;

                case "@v":
                    if (active[2] || active[3])
                        throw new ParserException("@v only allowed in channel 1 and 2.", next);
                    Eat();

                    if (next.Type != Lexer.TokenType.NUMBER)
                        throw new ParserException("Expected vibrato speed after @v macro.", next);

                    int vibratoSpeed = ParseInt(next.Data);
                    if (vibratoSpeed < 0 || vibratoSpeed > 15)
                        throw new ParserException("Invalid vibrato speed. Expected values 0-15.", next);
                    Eat();

                    Eat(Lexer.TokenType.COMMA, "comma");

                    if (next.Type != Lexer.TokenType.NUMBER)
                        throw new ParserException("Expected vibrato depth after @v macro.", next);

                    int vibratoDepth = ParseInt(next.Data);
                    if (vibratoDepth < 0 || vibratoDepth > 4)
                        throw new ParserException("Invalid vibrato depth. Expected values 0-4.", next);
                    Eat();

                    int delay = 0;
                    if (next.Type == Lexer.TokenType.COMMA)
                    {
                        Eat();
                        delay = ParseLength(true);
                    }

                    if (delay > 0)
                    {
                        song.AddData(active, (int)Song.CMD.T_VIBRATO_DELAY);
                        song.AddData(active, vibratoSpeed | (vibratoDepth << 4));
                        song.AddData(active, delay);
                    }
                    else
                    {
                        song.AddData(active, (int)Song.CMD.T_VIBRATO);
                        song.AddData(active, vibratoSpeed | (vibratoDepth << 4));
                    }
                    break;

                case "@ns":
                    if (active[0] || active[1] || active[2])
                        throw new ParserException("@ns only allowed in channel 4.", next);
                    Eat();

                    if (next.Type != Lexer.TokenType.NUMBER)
                        throw new ParserException("Expected 0 or 1 after @ns macro.", next);

                    int state = ParseInt(next.Data);
                    if (state < 0 || state > 1)
                        throw new ParserException("Expected 0 or 1 after @ns macro.", next);
                    Eat();

                    song.AddData(active, (int)Song.CMD.T_NOISE_STEP);
                    song.AddData(active, state);
                    break;

                case "@@":
                    Eat();

                    if (next.Type != Lexer.TokenType.NUMBER)
                        throw new ParserException("Expected macro id.", next);

                    int macroId = ParseInt(next.Data);
                    if (macroId == null)
                        throw new ParserException($"Macro @@{macroId} not found.", next);
                    Eat();

                    if (in_macro)
                    {
                        List<int> macroData = song_root.GetMacroData(macroId);
                        song.AddData(active, macroData);
                    }
                    else
                    {
                        song.AddData(active, (int)Song.CMD.T_MACRO);
                        song.AddData(active, song_root.GetMacroIndex(macroId));
                    }
                    break;

                default:
                    throw new ParserException($"Unknown macro command {next.Data}.", next);
            }
        }
    }
}

