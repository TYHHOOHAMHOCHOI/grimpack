using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Project05.CombatCore
{
    /// <summary>
    /// Minimal engine-free recursive-descent JSON parser.
    /// Avoids UnityEngine.JsonUtility and floating-point types.
    /// </summary>
    internal sealed class SimpleJsonParser
    {
        private readonly string _json;
        private int _pos;

        private SimpleJsonParser(string json)
        {
            _json = json;
            _pos = 0;
        }

        public static object Parse(string json)
        {
            if (json == null) throw new FormatException("JSON input is null.");
            var parser = new SimpleJsonParser(json);
            object value = parser.ParseValue();
            parser.SkipWhitespace();
            if (parser._pos < parser._json.Length)
            {
                throw new FormatException("Unexpected trailing characters at position " + parser._pos);
            }
            return value;
        }

        private object ParseValue()
        {
            SkipWhitespace();
            if (_pos >= _json.Length) throw new FormatException("Unexpected end of JSON.");

            char c = _json[_pos];
            switch (c)
            {
                case '{': return ParseObject();
                case '[': return ParseArray();
                case '"': return ParseString();
                case 't': return ParseLiteral("true", true);
                case 'f': return ParseLiteral("false", false);
                case 'n': return ParseLiteral("null", null);
                default:
                    if (c == '-' || (c >= '0' && c <= '9')) return ParseInteger();
                    throw new FormatException("Unexpected character '" + c + "' at position " + _pos);
            }
        }

        private Dictionary<string, object> ParseObject()
        {
            Expect('{');
            var dict = new Dictionary<string, object>(StringComparer.Ordinal);
            SkipWhitespace();
            if (Peek() == '}')
            {
                _pos++;
                return dict;
            }

            while (true)
            {
                SkipWhitespace();
                if (Peek() != '"') throw new FormatException("Expected string key at position " + _pos);
                string key = ParseString();
                SkipWhitespace();
                Expect(':');
                object val = ParseValue();
                dict[key] = val;
                SkipWhitespace();
                char next = Peek();
                if (next == ',')
                {
                    _pos++;
                    continue;
                }
                if (next == '}')
                {
                    _pos++;
                    return dict;
                }
                throw new FormatException("Expected ',' or '}' in object at position " + _pos);
            }
        }

        private List<object> ParseArray()
        {
            Expect('[');
            var list = new List<object>();
            SkipWhitespace();
            if (Peek() == ']')
            {
                _pos++;
                return list;
            }

            while (true)
            {
                list.Add(ParseValue());
                SkipWhitespace();
                char next = Peek();
                if (next == ',')
                {
                    _pos++;
                    continue;
                }
                if (next == ']')
                {
                    _pos++;
                    return list;
                }
                throw new FormatException("Expected ',' or ']' in array at position " + _pos);
            }
        }

        private string ParseString()
        {
            Expect('"');
            var sb = new StringBuilder();
            while (_pos < _json.Length)
            {
                char c = _json[_pos++];
                if (c == '"') return sb.ToString();
                if (c == '\\')
                {
                    if (_pos >= _json.Length) throw new FormatException("Unterminated escape sequence.");
                    char esc = _json[_pos++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_pos + 4 > _json.Length) throw new FormatException("Incomplete unicode escape.");
                            string hex = _json.Substring(_pos, 4);
                            _pos += 4;
                            if (!ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort code))
                            {
                                throw new FormatException("Invalid unicode escape: \\u" + hex);
                            }
                            sb.Append((char)code);
                            break;
                        default:
                            throw new FormatException("Invalid escape character '\\" + esc + "'.");
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            throw new FormatException("Unterminated string literal.");
        }

        private long ParseInteger()
        {
            int start = _pos;
            if (_json[_pos] == '-') _pos++;
            int digitsStart = _pos;
            while (_pos < _json.Length && _json[_pos] >= '0' && _json[_pos] <= '9')
            {
                _pos++;
            }

            if (_pos == digitsStart)
            {
                throw new FormatException("Invalid number at position " + start);
            }

            if (_pos < _json.Length && (_json[_pos] == '.' || _json[_pos] == 'e' || _json[_pos] == 'E'))
            {
                throw new FormatException("Non-integer JSON number is not supported in CombatCore at position " + start);
            }

            string token = _json.Substring(start, _pos - start);
            if (!long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
            {
                throw new FormatException("Integer out of Int64 range: " + token);
            }
            return value;
        }

        private object ParseLiteral(string literal, object result)
        {
            if (_pos + literal.Length > _json.Length ||
                !string.Equals(_json.Substring(_pos, literal.Length), literal, StringComparison.Ordinal))
            {
                throw new FormatException("Invalid literal at position " + _pos);
            }
            _pos += literal.Length;
            return result;
        }

        private void SkipWhitespace()
        {
            while (_pos < _json.Length)
            {
                char c = _json[_pos];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') _pos++;
                else break;
            }
        }

        private char Peek()
        {
            if (_pos >= _json.Length) throw new FormatException("Unexpected end of JSON.");
            return _json[_pos];
        }

        private void Expect(char expected)
        {
            if (_pos >= _json.Length || _json[_pos] != expected)
            {
                throw new FormatException("Expected '" + expected + "' at position " + _pos);
            }
            _pos++;
        }
    }
}
