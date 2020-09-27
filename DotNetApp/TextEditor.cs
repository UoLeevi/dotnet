using System;
using System.Text.RegularExpressions;

namespace DotNetApp
{
    public class TextEditor : TextEditor<TextEditor>
    {
        public enum SearchScope
        {
            EntireText,
            AfterCaret,
            BeforeCaret
        }
    }

    public abstract class TextEditor<TTextEditor>
        where TTextEditor : TextEditor<TTextEditor>, new()
    {
        public string Text { get; set; }

        private long caretIndex;
        public long CaretIndex
        {
            get
            {
                if (HasSelection)
                {
                    return SelectionStart + SelectionLength;
                }

                if (caretIndex > Text.Length)
                {
                    return Text.Length;
                }

                return caretIndex;
            }
            set
            {
                Index = value;
            }
        }

        private long index;
        public long Index
        {
            get => index;
            set
            {
                if (value >= 0 && value <= Text.Length)
                {
                    caretIndex = value;
                }

                index = Math.Max(value, -1);
                ResetSelection();
            }
        }

        private long selectionStart;
        public long SelectionStart
        {
            get => Math.Min(Math.Max(selectionStart, 0), Text.Length);
            set
            {
                selectionStart = Math.Min(Math.Max(value, 0), Text.Length);
                selectionLength = 0;
            }
        }

        private long selectionLength;
        public long SelectionLength
        {
            get => Math.Min(Math.Max(selectionLength, 0), Text.Length - SelectionStart);
            set
            {
                if (value <= 0)
                {
                    selectionLength = 0;
                    selectionStart = 0;
                    return;
                }

                selectionLength = Math.Min(Math.Max(value, 0), Text.Length - SelectionStart);
            }
        }

        public bool HasSelection => SelectionLength > 0;

        public long LineStartIndex
        {
            get
            {
                int position = Text.LastIndexOf("\r\n", (int)CaretIndex);
                return position == -1 ? 0 : position + 2;
            }
        }

        public long LineEndIndex
        {
            get
            {
                int position = Text.IndexOf("\r\n", (int)CaretIndex);
                return position == -1 ? Text.Length : position;
            }
        }

        public ReadOnlySpan<char> CurrentLineText
        {
            get
            {
                int start = (int)LineStartIndex;
                int length = (int)LineEndIndex - start;
                return Text.AsSpan().Slice(start, length);
            }
        }

        public bool IsWhiteSpaceLine => CurrentLineText.IsWhiteSpace();

        public Match Match { get; private set; }

        public override string ToString() => Text;

        public static TTextEditor FromString(string text)
        {
            return new TTextEditor()
            {
                Text = text
            };
        }

        public virtual TTextEditor Select(Regex pattern, TextEditor.SearchScope scope = TextEditor.SearchScope.EntireText)
        {            
            switch (scope)
            {
                case TextEditor.SearchScope.EntireText:
                    Match = pattern.Match(Text);
                    break;

                case TextEditor.SearchScope.AfterCaret:
                    Match = pattern.Match(Text, (int)CaretIndex);
                    break;

                case TextEditor.SearchScope.BeforeCaret:
                    Match = pattern.Match(Text, 0, (int)CaretIndex);
                    break;
            }

            SelectionStart = Match.Index;
            SelectionLength = Match.Length;
            return (TTextEditor)this;
        }

        public virtual TTextEditor Select(string pattern, TextEditor.SearchScope scope = TextEditor.SearchScope.EntireText)
        {
            return Select(new Regex(pattern), scope);
        }

        public virtual TTextEditor ResetSelection()
        {
            SelectionLength = 0;
            Match = null;
            return (TTextEditor)this;
        }

        public virtual TTextEditor MoveToStart()
        {
            CaretIndex = 0;
            return (TTextEditor)this;
        }

        public virtual TTextEditor MoveToEnd()
        {
            CaretIndex = Text.Length;
            return (TTextEditor)this;
        }

        public virtual TTextEditor MoveToLineEnd()
        {
            CaretIndex = LineEndIndex;
            return (TTextEditor)this;
        }

        public virtual TTextEditor MoveToLineStart()
        {
            CaretIndex = LineStartIndex;
            return (TTextEditor)this;
        }

        public virtual TTextEditor MoveToPattern(Regex pattern, TextEditor.SearchScope scope = TextEditor.SearchScope.EntireText)
        {
            Select(pattern, scope);
            CaretIndex = SelectionStart;
            return (TTextEditor)this;
        }

        public virtual TTextEditor MoveToPattern(string pattern, TextEditor.SearchScope scope = TextEditor.SearchScope.EntireText)
        {
            return MoveToPattern(new Regex(pattern), scope);
        }

        public virtual TTextEditor MoveToNextEmptyLine()
        {
            do
            {
                MoveToLineEnd();
                CaretIndex += 2;
            } while (!IsWhiteSpaceLine);

            if (CaretIndex == Text.Length)
            {
                Write("\r\n");
            }

            return (TTextEditor)this;
        }

        public virtual TTextEditor Write(string value)
        {
            if (HasSelection)
            {
                int start = (int)SelectionStart;
                int length = (int)SelectionLength;
                CaretIndex = start;
                Text = Text.Remove(start, length);
                return Write(value);                
            }
            else
            {
                Text = Text.Insert((int)CaretIndex, value);
            }

            CaretIndex += value.Length;

            return (TTextEditor)this;
        }

        public virtual TTextEditor WriteLine(string value = "")
        {
            return Write(value + "\r\n");
        }

        public virtual TTextEditor WriteIndentedLines(string indentationString, params string[] lines)
        {
            foreach (string line in lines)
            {
                WriteLine(line == string.Empty ? line : $"{indentationString}{line}");
            }

            return (TTextEditor)this;
        }

        public virtual TTextEditor WriteLines(params string[] lines)
        {
            foreach (string line in lines)
            {
                WriteLine(line);
            }

            return (TTextEditor)this;
        }

        public virtual TTextEditor ReplaceLine(string value)
        {
            RemoveLine();
            return WriteLine(value);
        }

        public virtual TTextEditor RemoveLine()
        {
            MoveToLineStart();
            int start = (int)CaretIndex;
            int end = Math.Max((int)LineEndIndex + 2, Text.Length);
            Text = Text.Remove(start, end - start);
            return (TTextEditor)this;
        }

        public virtual TTextEditor Do(Action action)
        {
            action();
            return (TTextEditor)this;
        }
    }
}
