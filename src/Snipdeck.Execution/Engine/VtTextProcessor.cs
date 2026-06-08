using System.Text;

namespace Snipdeck.Execution.Engine
{
    /// <summary>
    /// Turns a raw VT (ANSI) byte stream into a clean plain-text transcript by
    /// replaying it through a minimal virtual screen buffer: the cursor is tracked
    /// and CR / LF / cursor-movement / erase sequences are honoured, so spinner
    /// frames and progress bars (which overwrite the same cells) collapse to their
    /// final state, and colour / style (SGR) and other attributes are discarded.
    ///
    /// Headless and pure — no rendering, no I/O. It is the cleanliness contract for
    /// stored history: the output contains no escape codes, no carriage returns and
    /// no stacked frames. Full-screen TUI drawing on the alternate screen buffer is
    /// kept out of the transcript (a real terminal's scrollback wouldn't capture it
    /// either).
    ///
    /// It targets the common Windows ConPTY output repertoire, not a complete xterm;
    /// unrecognised sequences are skipped rather than emitted.
    /// </summary>
    public static class VtTextProcessor
    {
        private const int _tabWidth = 8;

        /// <summary>Processes a raw VT byte stream (decoded as UTF-8) into clean text.</summary>
        public static string Process(byte[] raw)
        {
            ArgumentNullException.ThrowIfNull(raw);
            return Process(raw.AsSpan());
        }

        /// <summary>Processes a raw VT byte span (decoded as UTF-8) into clean text.</summary>
        public static string Process(ReadOnlySpan<byte> raw) => Process(Encoding.UTF8.GetString(raw));

        /// <summary>Processes already-decoded VT text into clean text.</summary>
        public static string Process(string input)
        {
            ArgumentNullException.ThrowIfNull(input);
            var screen = new Screen();
            screen.Feed(input);
            return screen.ToCleanText();
        }

        private enum State
        {
            Normal,
            Escape,
            Csi,
            Osc,
            OscEsc,
            ConsumeOne,
        }

        private sealed class Screen
        {
            // The transcript buffer (normal screen). Alternate-buffer drawing goes to a
            // throwaway buffer so full-screen TUI frames never reach the transcript.
            private readonly List<List<char>> _main = [[]];
            private List<List<char>> _alt = [[]];
            private List<List<char>> _active;
            private bool _onAlt;

            private int _row;
            private int _col;
            private int _savedRow;
            private int _savedCol;
            private int _mainRow;
            private int _mainCol;

            private State _state = State.Normal;
            private readonly StringBuilder _csi = new();

            public Screen() => _active = _main;

            public void Feed(string input)
            {
                foreach (var ch in input)
                {
                    Step(ch);
                }
            }

            private void Step(char c)
            {
                switch (_state)
                {
                    case State.Normal:
                        StepNormal(c);
                        break;
                    case State.Escape:
                        StepEscape(c);
                        break;
                    case State.Csi:
                        StepCsi(c);
                        break;
                    case State.Osc:
                        if (c == '\a')
                        {
                            _state = State.Normal;
                        }
                        else if (c == '\u001b')
                        {
                            _state = State.OscEsc;
                        }

                        break;
                    case State.OscEsc:
                        // The '\' of an ST terminator (ESC \); anything else also ends the OSC.
                        _state = State.Normal;
                        break;
                    case State.ConsumeOne:
                        _state = State.Normal;
                        break;
                    default:
                        _state = State.Normal;
                        break;
                }
            }

            private void StepNormal(char c)
            {
                switch (c)
                {
                    case '\u001b':
                        _state = State.Escape;
                        break;
                    case '\r':
                        _col = 0;
                        break;
                    case '\n':
                        // Newline mode: advance a row and return to column 0. Correct for
                        // CRLF and the cleanest choice for bare LF transcripts.
                        _row++;
                        _col = 0;
                        break;
                    case '\t':
                        _col = ((_col / _tabWidth) + 1) * _tabWidth;
                        break;
                    case '\b':
                        _col = Math.Max(0, _col - 1);
                        break;
                    case '\a': // bell
                        break;
                    default:
                        if (!char.IsControl(c))
                        {
                            Put(c);
                        }

                        break;
                }
            }

            private void StepEscape(char c)
            {
                switch (c)
                {
                    case '[':
                        _ = _csi.Clear();
                        _state = State.Csi;
                        break;
                    case ']':
                        _state = State.Osc;
                        break;
                    case '(':
                    case ')':
                    case '*':
                    case '+':
                        _state = State.ConsumeOne; // charset designation; skip the next byte
                        break;
                    case 'M': // reverse index
                        _row = Math.Max(0, _row - 1);
                        _state = State.Normal;
                        break;
                    case 'D': // index
                        _row++;
                        _state = State.Normal;
                        break;
                    case 'E': // next line
                        _row++;
                        _col = 0;
                        _state = State.Normal;
                        break;
                    case '7': // save cursor (DECSC)
                        SaveCursor();
                        _state = State.Normal;
                        break;
                    case '8': // restore cursor (DECRC)
                        RestoreCursor();
                        _state = State.Normal;
                        break;
                    case 'c': // full reset (RIS)
                        ResetAll();
                        _state = State.Normal;
                        break;
                    default:
                        _state = State.Normal;
                        break;
                }
            }

            private void StepCsi(char c)
            {
                // Parameter / intermediate bytes accumulate until a final byte (0x40–0x7E).
                if (c is >= '@' and <= '~')
                {
                    DispatchCsi(c);
                    _state = State.Normal;
                }
                else
                {
                    _ = _csi.Append(c);
                }
            }

            private void DispatchCsi(char final)
            {
                var args = _csi.ToString();
                var isPrivate = args.StartsWith('?');
                if (isPrivate)
                {
                    HandlePrivateMode(final, args);
                    return;
                }

                switch (final)
                {
                    case 'A':
                        _row = Math.Max(0, _row - Param(args, 0, 1));
                        break;
                    case 'B':
                        _row += Param(args, 0, 1);
                        break;
                    case 'C':
                        _col += Param(args, 0, 1);
                        break;
                    case 'D':
                        _col = Math.Max(0, _col - Param(args, 0, 1));
                        break;
                    case 'E':
                        _row += Param(args, 0, 1);
                        _col = 0;
                        break;
                    case 'F':
                        _row = Math.Max(0, _row - Param(args, 0, 1));
                        _col = 0;
                        break;
                    case 'G':
                        _col = Math.Max(0, Param(args, 0, 1) - 1);
                        break;
                    case 'H':
                    case 'f':
                        _row = Math.Max(0, Param(args, 0, 1) - 1);
                        _col = Math.Max(0, Param(args, 1, 1) - 1);
                        break;
                    case 'J':
                        EraseDisplay(Param(args, 0, 0));
                        break;
                    case 'K':
                        EraseLine(Param(args, 0, 0));
                        break;
                    case 's':
                        SaveCursor();
                        break;
                    case 'u':
                        RestoreCursor();
                        break;
                    default:
                        // SGR ('m'), device queries, etc. — no effect on plain text.
                        break;
                }
            }

            private void HandlePrivateMode(char final, string args)
            {
                // Alternate screen buffer toggles. Drawing on the alt buffer is discarded
                // so full-screen TUIs don't pollute the transcript.
                var code = args.TrimStart('?');
                if (code is "1049" or "1047" or "47")
                {
                    if (final == 'h')
                    {
                        EnterAlt();
                    }
                    else if (final == 'l')
                    {
                        LeaveAlt();
                    }
                }

                // Other private modes (cursor visibility, bracketed paste, …) are ignored.
            }

            private void EnterAlt()
            {
                if (_onAlt)
                {
                    return;
                }

                _onAlt = true;
                _mainRow = _row;
                _mainCol = _col;
                _alt = [[]];
                _active = _alt;
                _row = 0;
                _col = 0;
            }

            private void LeaveAlt()
            {
                if (!_onAlt)
                {
                    return;
                }

                _onAlt = false;
                _active = _main;
                _row = _mainRow;
                _col = _mainCol;
            }

            private void Put(char ch)
            {
                EnsureRow(_row);
                var line = _active[_row];
                while (line.Count <= _col)
                {
                    line.Add(' ');
                }

                line[_col] = ch;
                _col++;
            }

            private void EnsureRow(int row)
            {
                while (_active.Count <= row)
                {
                    _active.Add([]);
                }
            }

            private void EraseLine(int mode)
            {
                EnsureRow(_row);
                var line = _active[_row];
                switch (mode)
                {
                    case 0: // cursor to end
                        if (line.Count > _col)
                        {
                            line.RemoveRange(_col, line.Count - _col);
                        }

                        break;
                    case 1: // start to cursor
                        for (var i = 0; i <= _col && i < line.Count; i++)
                        {
                            line[i] = ' ';
                        }

                        break;
                    case 2: // entire line
                        line.Clear();
                        break;
                    default:
                        break;
                }
            }

            private void EraseDisplay(int mode)
            {
                switch (mode)
                {
                    case 0: // cursor to end of screen
                        EraseLine(0);
                        if (_active.Count > _row + 1)
                        {
                            _active.RemoveRange(_row + 1, _active.Count - (_row + 1));
                        }

                        break;
                    case 1: // start of screen to cursor
                        for (var r = 0; r < _row && r < _active.Count; r++)
                        {
                            _active[r].Clear();
                        }

                        EraseLine(1);
                        break;
                    case 2: // entire screen
                    case 3: // entire screen + scrollback
                        foreach (var line in _active)
                        {
                            line.Clear();
                        }

                        break;
                    default:
                        break;
                }
            }

            private void SaveCursor()
            {
                _savedRow = _row;
                _savedCol = _col;
            }

            private void RestoreCursor()
            {
                _row = _savedRow;
                _col = _savedCol;
            }

            private void ResetAll()
            {
                _main.Clear();
                _main.Add([]);
                _active = _main;
                _onAlt = false;
                _row = _col = _savedRow = _savedCol = _mainRow = _mainCol = 0;
            }

            public string ToCleanText()
            {
                // Always serialise the main (transcript) buffer, even if the stream ended
                // while still on the alternate screen.
                var sb = new StringBuilder();
                foreach (var line in _main)
                {
                    var text = new string([.. line]).TrimEnd();
                    _ = sb.Append(text).Append('\n');
                }

                // Drop trailing blank lines (e.g. left by an erased final progress frame).
                var result = sb.ToString().TrimEnd('\n');
                return result;
            }
        }

        private static int Param(string args, int index, int fallback)
        {
            var parts = args.Split(';');
            return index < parts.Length
                && int.TryParse(parts[index], out var value)
                && value > 0
                ? value
                : fallback;
        }
    }
}
