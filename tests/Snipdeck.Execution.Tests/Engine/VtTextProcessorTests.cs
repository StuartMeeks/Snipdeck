using Snipdeck.Execution.Engine;

namespace Snipdeck.Execution.Tests.Engine
{
    public class VtTextProcessorTests
    {
        private const string _esc = "\u001b";
        private const string _bel = "\a";

        [Fact]
        public void Plain_text_passes_through()
        {
            Assert.Equal("hello world", VtTextProcessor.Process("hello world"));
        }

        [Fact]
        public void Crlf_and_lf_both_produce_newlines()
        {
            Assert.Equal("one\ntwo", VtTextProcessor.Process("one\r\ntwo"));
            Assert.Equal("one\ntwo", VtTextProcessor.Process("one\ntwo"));
        }

        [Fact]
        public void Trailing_whitespace_and_blank_lines_are_trimmed()
        {
            Assert.Equal("a\nb", VtTextProcessor.Process("a   \r\nb\t\r\n\r\n\r\n"));
        }

        [Fact]
        public void Sgr_colour_codes_are_stripped()
        {
            var input = $"{_esc}[31mError:{_esc}[0m something{_esc}[1;32m ok{_esc}[0m";
            Assert.Equal("Error: something ok", VtTextProcessor.Process(input));
        }

        [Fact]
        public void Carriage_return_spinner_frames_collapse_to_the_final_frame()
        {
            // Hide cursor, cycle spinner frames over the same line via \r, erase the line,
            // then print the final coloured result. Mirrors a Spectre.Console status spinner.
            var input =
                $"{_esc}[?25l" +
                "\r⠋ Working..." +
                "\r⠙ Working..." +
                "\r⠹ Working..." +
                $"\r{_esc}[2K{_esc}[32m✓{_esc}[0m Done" +
                $"\n{_esc}[?25h";

            Assert.Equal("✓ Done", VtTextProcessor.Process(input));
        }

        [Fact]
        public void Single_line_progress_bar_redrawn_with_cursor_up_keeps_only_final_state()
        {
            var input =
                "Progress: 0%\n" +
                $"{_esc}[1A\rProgress: 50%\n" +
                $"{_esc}[1A\rProgress: 100%\n";

            Assert.Equal("Progress: 100%", VtTextProcessor.Process(input));
        }

        [Fact]
        public void Multi_line_progress_bar_redrawn_with_cursor_up_n_keeps_only_final_state()
        {
            var input =
                "line A v1\nline B v1\n" +
                $"{_esc}[2Aline A v2\nline B v2\n";

            Assert.Equal("line A v2\nline B v2", VtTextProcessor.Process(input));
        }

        [Fact]
        public void Erase_in_display_clears_redrawn_interactive_prompt()
        {
            // A multi-select prompt: first render, then cursor home + erase-display + redraw
            // with the pointer moved. Only the final render survives — no stacked frames.
            var first = "Select items:\n> Apple\n  Banana\n  Cherry";
            var redraw = $"{_esc}[3A\r{_esc}[JSelect items:\n  Apple\n> Banana\n  Cherry";
            var result = VtTextProcessor.Process(first + redraw);

            Assert.Equal("Select items:\n  Apple\n> Banana\n  Cherry", result);

            // The header appears exactly once: the first render was fully overwritten.
            var occurrences = result.Split("Select items:").Length - 1;
            Assert.Equal(1, occurrences);
        }

        [Fact]
        public void Alternate_screen_buffer_drawing_is_excluded_from_the_transcript()
        {
            var input = $"before\n{_esc}[?1049hFULLSCREEN GARBAGE{_esc}[?1049lafter\n";
            var result = VtTextProcessor.Process(input);

            Assert.Equal("before\nafter", result);
            Assert.DoesNotContain("FULLSCREEN", result, StringComparison.Ordinal);
        }

        [Fact]
        public void Osc_title_sequences_are_stripped_with_both_bel_and_st_terminators()
        {
            Assert.Equal("Hello", VtTextProcessor.Process($"{_esc}]0;My Title{_bel}Hello"));
            Assert.Equal("World", VtTextProcessor.Process($"{_esc}]0;T{_esc}\\World"));
        }

        [Fact]
        public void Backspace_moves_the_cursor_back_one_column()
        {
            Assert.Equal("aXc", VtTextProcessor.Process("abc\b\bX"));
        }

        [Fact]
        public void Tab_advances_to_the_next_eight_column_stop()
        {
            Assert.Equal("a       b", VtTextProcessor.Process("a\tb"));
        }

        [Fact]
        public void Output_contains_no_escape_codes_or_carriage_returns()
        {
            var input =
                $"{_esc}[?25l\r⠋ Loading{_esc}[2K\r{_esc}[33m50%{_esc}[0m" +
                $"\r{_esc}[2K{_esc}[32mDone{_esc}[0m\n{_esc}[?25h";
            var result = VtTextProcessor.Process(input);

            Assert.DoesNotContain('\u001b', result);
            Assert.DoesNotContain('\r', result);
            Assert.Equal("Done", result);
        }

        [Fact]
        public void Empty_input_produces_empty_output()
        {
            Assert.Equal(string.Empty, VtTextProcessor.Process(string.Empty));
        }

        [Fact]
        public void Utf8_bytes_decode_before_processing()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("café ✓");
            Assert.Equal("café ✓", VtTextProcessor.Process(bytes));
        }
    }
}
