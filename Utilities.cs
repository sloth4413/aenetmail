﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AE.Net.Mail
{
    internal static class Utilities
    {
        #region Fields

        internal static readonly Encoding _defaultEncoding = Encoding.UTF8;

        //stolen from http://stackoverflow.com/questions/3355407/validate-string-is-base64-format-using-regex
        private const char Base64Padding = '=';

        private static readonly CultureInfo _enUsCulture = new CultureInfo("en-US");

        private static readonly HashSet<char> Base64Characters = new HashSet<char>() {
                        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
                        'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f',
                        'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
                        'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/'
                };

        private static string[] _encodings = new string[] {
    "IBM037", "IBM437", "IBM500", "ASMO-708", "DOS-720", "ibm737",
    "ibm775", "ibm850", "ibm852", "IBM855", "ibm857", "IBM00858",
    "IBM860", "ibm861", "DOS-862", "IBM863", "IBM864", "IBM865",
    "cp866", "ibm869", "IBM870", "windows-874", "cp875",
    "shift_jis", "gb2312", "ks_c_5601-1987", "big5", "IBM1026",
    "IBM01047", "IBM01140", "IBM01141", "IBM01142", "IBM01143",
    "IBM01144", "IBM01145", "IBM01146", "IBM01147", "IBM01148",
    "IBM01149", "utf-16", "utf-16BE", "windows-1250",
    "windows-1251", "Windows-1252", "windows-1253", "windows-1254",
    "windows-1255", "windows-1256", "windows-1257", "windows-1258",
    "Johab", "macintosh", "x-mac-japanese", "x-mac-chinesetrad",
    "x-mac-korean", "x-mac-arabic", "x-mac-hebrew", "x-mac-greek",
    "x-mac-cyrillic", "x-mac-chinesesimp", "x-mac-romanian",
    "x-mac-ukrainian", "x-mac-thai", "x-mac-ce", "x-mac-icelandic",
    "x-mac-turkish", "x-mac-croatian", "utf-32", "utf-32BE",
    "x-Chinese-CNS", "x-cp20001", "x-Chinese-Eten", "x-cp20003",
    "x-cp20004", "x-cp20005", "x-IA5", "x-IA5-German",
    "x-IA5-Swedish", "x-IA5-Norwegian", "us-ascii", "x-cp20261",
    "x-cp20269", "IBM273", "IBM277", "IBM278", "IBM280", "IBM284",
    "IBM285", "IBM290", "IBM297", "IBM420", "IBM423", "IBM424",
    "x-EBCDIC-KoreanExtended", "IBM-Thai", "koi8-r", "IBM871",
    "IBM880", "IBM905", "IBM00924", "EUC-JP", "x-cp20936",
    "x-cp20949", "cp1025", "koi8-u", "iso-8859-1", "iso-8859-2",
    "iso-8859-3", "iso-8859-4", "iso-8859-5", "iso-8859-6",
    "iso-8859-7", "iso-8859-8", "iso-8859-9", "iso-8859-13",
    "iso-8859-15", "x-Europa", "iso-8859-8-i", "iso-2022-jp",
    "csISO2022JP", "iso-2022-jp", "iso-2022-kr", "x-cp50227",
    "euc-jp", "EUC-CN", "euc-kr", "hz-gb-2312", "GB18030",
    "x-iscii-de", "x-iscii-be", "x-iscii-ta", "x-iscii-te",
    "x-iscii-as", "x-iscii-or", "x-iscii-ka", "x-iscii-ma",
    "x-iscii-gu", "x-iscii-pa", "utf-7", "utf-8"
};

        private static Regex rxNegativeHours = new Regex(@"(?<=\s)\-(?=\d{1,2}\:)", RegexOptions.Compiled);

        private static Regex rxTimeZoneColon = new Regex(@"\s+(\+|\-)(\d{1,2})\D(\d{2})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        //Mon, 28 Feb 2005 19:26:34 -0500 (EST)
        private static Regex rxTimeZoneMinutes = new Regex(@"([\+\-]?\d{1,2})(\d{2})$", RegexOptions.Compiled);

        private static Regex rxTimeZoneName = new Regex(@"\s+\([a-z]+\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #endregion

        #region Methods

        public static string DecodeQuotedPrintable(string value, Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = _defaultEncoding;
            }

            if (value.IndexOf('_') > -1 && value.IndexOf(' ') == -1)
                value = value.Replace('_', ' ');

            var data = System.Text.Encoding.ASCII.GetBytes(value);
            var eq = Convert.ToByte('=');
            var n = 0;

            for (int i = 0; i < data.Length; i++)
            {
                var b = data[i];

                if ((b == eq) && ((i + 2) < data.Length))
                {
                    byte b1 = data[i + 1], b2 = data[i + 2];
                    if (b1 == 10 || b1 == 13)
                    {
                        i++;
                        if (b2 == 10 || b2 == 13)
                        {
                            i++;
                        }
                        continue;
                    }

                    if (byte.TryParse(value.Substring(i + 1, 2), NumberStyles.HexNumber, null, out b))
                    {
                        data[n] = (byte)b;
                        n++;
                        i += 2;
                    }
                    else
                    {
                        data[i] = eq;
                        n++;
                    }
                }
                else
                {
                    data[n] = b;
                    n++;
                }
            }

            value = encoding.GetString(data, 0, n);
            return value;
        }

        //Mon, 28 Feb 2005 19:26:34 -0500 (EST)
        //search can be strict because the format has already been normalized
        public static string NormalizeDate(string value)
        {
            value = rxTimeZoneName.Replace(value, string.Empty);
            value = rxTimeZoneColon.Replace(value, match => " " + match.Groups[1].Value + match.Groups[2].Value.PadLeft(2, '0') + match.Groups[3].Value);
            value = rxNegativeHours.Replace(value, string.Empty);
            var minutes = rxTimeZoneMinutes.Match(value);
            if (minutes.Groups[2].Value.ToInt() > 60)
            { //even if there's no match, the value = 0
                value = value.Substring(0, minutes.Index) + minutes.Groups[1].Value + "00";
            }
            return value;
        }

        //http://www.opensourcejavaphp.net/csharp/openpopdotnet/HeaderFieldParser.cs.html
        /// Parse a character set into an encoding.
        /// </summary>
        /// <param name="characterSet">The character set to parse</param>
        /// <param name="@default">The character set to default to if it can't be parsed</param>
        /// <returns>An encoding which corresponds to the character set</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="characterSet"/> is <see langword="null"/></exception>
        public static Encoding ParseCharsetToEncoding(string characterSet, Encoding @default)
        {
            if (string.IsNullOrEmpty(characterSet))
                return @default ?? _defaultEncoding;

            string charSetUpper = characterSet.ToUpperInvariant();
            if (charSetUpper.Contains("WINDOWS") || charSetUpper.Contains("CP"))
            {
                // It seems the character set contains an codepage value, which we should use to parse the encoding
                charSetUpper = charSetUpper.Replace("CP", ""); // Remove cp
                charSetUpper = charSetUpper.Replace("WINDOWS", ""); // Remove windows
                charSetUpper = charSetUpper.Replace("-", ""); // Remove - which could be used as cp-1554

                // Now we hope the only thing left in the characterSet is numbers.
                int codepageNumber = int.Parse(charSetUpper, System.Globalization.CultureInfo.InvariantCulture);

                return GetEncodings().Where(x => x.CodePage == codepageNumber)
                    .FirstOrDefault() ?? @default ?? _defaultEncoding;
            }

            // It seems there is no codepage value in the characterSet. It must be a named encoding
            return GetEncodings().Where(x => x.EncodingName.Is(characterSet))
                .FirstOrDefault() ?? @default ?? _defaultEncoding;
        }

        public static dynamic ParseImapHeader(string data)
        {
            dynamic values = new NameValueCollection();
            string name = null;
            int nump = 0;
            var temp = new StringBuilder();
            if (data != null)
                foreach (var c in data)
                {
                    if (c == ' ')
                    {
                        if (name == null)
                        {
                            name = temp.ToString();
                            temp.Clear();
                        }
                        else if (nump == 0)
                        {
                            values[name] = temp.ToString();
                            name = null;
                            temp.Clear();
                        }
                        else
                            temp.Append(c);
                    }
                    else if (c == '(')
                    {
                        if (nump > 0)
                            temp.Append(c);
                        nump++;
                    }
                    else if (c == ')')
                    {
                        nump--;
                        if (nump > 0)
                            temp.Append(c);
                    }
                    else
                        temp.Append(c);
                }

            if (name != null)
                values[name] = temp.ToString();

            return values;
        }

        internal static void CopyStream(Stream a, Stream b, int maxLength, int bufferSize = 8192)
        {
            int read;
            var buffer = new byte[bufferSize];
            while (maxLength > 0)
            {
                read = Math.Min(bufferSize, maxLength);
                read = a.Read(buffer, 0, read);
                if (read == 0) return;
                maxLength -= read;
                b.Write(buffer, 0, read);
            }
        }

        internal static string DecodeBase64(string data, Encoding encoding = null)
        {
            if (!IsValidBase64String(ref data))
            {
                return data;
            }
            var bytes = Convert.FromBase64String(data);
            return (encoding ?? Utilities._defaultEncoding).GetString(bytes);
        }

        internal static string DecodeWords(string encodedWords, Encoding @default = null)
        {
            if (string.IsNullOrEmpty(encodedWords))
                return string.Empty;

            string decodedWords = encodedWords;

            // Notice that RFC2231 redefines the BNF to
            // encoded-word := "=?" charset ["*" language] "?" encoded-text "?="
            // but no usage of this BNF have been spotted yet. It is here to
            // ease debugging if such a case is discovered.

            // This is the regex that should fit the BNF
            // RFC Says that NO WHITESPACE is allowed in this encoding, but there are examples
            // where whitespace is there, and therefore this regex allows for such.
            const string strRegEx = @"\=\?(?<Charset>\S+?)\?(?<Encoding>\w)\?(?<Content>.+?)\?\=";
            // \w	Matches any word character including underscore. Equivalent to "[A-Za-z0-9_]".
            // \S	Matches any nonwhite space character. Equivalent to "[^ \f\n\r\t\v]".
            // +?   non-gready equivalent to +
            // (?<NAME>REGEX) is a named group with name NAME and regular expression REGEX

            var matches = Regex.Matches(encodedWords, strRegEx);
            foreach (Match match in matches)
            {
                // If this match was not a success, we should not use it
                if (!match.Success)
                    continue;

                string fullMatchValue = match.Value;

                string encodedText = match.Groups["Content"].Value;
                string encoding = match.Groups["Encoding"].Value;
                string charset = match.Groups["Charset"].Value;

                // Get the encoding which corrosponds to the character set
                Encoding charsetEncoding = ParseCharsetToEncoding(charset, @default);

                // Store decoded text here when done
                string decodedText;

                // Encoding may also be written in lowercase
                switch (encoding.ToUpperInvariant())
                {
                    // RFC:
                    // The "B" encoding is identical to the "BASE64"
                    // encoding defined by RFC 2045.
                    // http://tools.ietf.org/html/rfc2045#section-6.8
                    case "B":
                        decodedText = DecodeBase64(encodedText, charsetEncoding);
                        break;

                    // RFC:
                    // The "Q" encoding is similar to the "Quoted-Printable" content-
                    // transfer-encoding defined in RFC 2045.
                    // There are more details to this. Please check
                    // http://tools.ietf.org/html/rfc2047#section-4.2
                    //
                    case "Q":
                        decodedText = DecodeQuotedPrintable(encodedText, charsetEncoding);
                        break;

                    default:
                        throw new ArgumentException("The encoding " + encoding + " was not recognized");
                }

                // Repalce our encoded value with our decoded value
                decodedWords = decodedWords.Replace(fullMatchValue, decodedText);
            }

            return decodedWords;
        }

        internal static bool EndsWithWhiteSpace(this string line)
        {
            if (string.IsNullOrEmpty(line))
                return false;
            var chr = line[line.Length - 1];
            return IsWhiteSpace(chr);
        }

        internal static void Fire<T>(this EventHandler<T> events, object sender, T args) where T : EventArgs
        {
            if (events == null)
                return;
            events(sender, args);
        }

        internal static VT Get<KT, VT>(this IDictionary<KT, VT> dictionary, KT key, VT defaultValue = default(VT))
        {
            if (dictionary == null)
                return defaultValue;
            VT value;
            if (dictionary.TryGetValue(key, out value))
                return value;
            return defaultValue;
        }

        internal static IEnumerable<Encoding> GetEncodings()
        {
            return _encodings.Select(x => Encoding.GetEncoding(x)).Where(x => x != null);
        }

        internal static string GetRFC2060Date(this DateTime date)
        {
            return date.ToString("dd-MMM-yyyy HH:mm:ss zz", _enUsCulture);
        }

        internal static string GetRFC2822Date(this DateTime date)
        {
            return date.ToString("ddd, d MMM yyyy HH:mm:ss zz", _enUsCulture);
        }

        internal static bool Is(this string input, string other)
        {
            return string.Equals(input, other, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsValidBase64String(ref string param, bool strictPadding = false)
        {
            if (param == null)
            {
                // null string is not Base64
                return false;
            }

            // replace optional CR and LF characters
            param = param.Replace("\r", String.Empty).Replace("\n", String.Empty);

            var lengthWPadding = param.Length;
            var missingPaddingLength = lengthWPadding % 4;
            if (missingPaddingLength != 0)
            {
                // Base64 string length should be multiple of 4
                if (strictPadding)
                {
                    return false;
                }
                else
                {
                    //add the minimum necessary padding
                    if (missingPaddingLength > 2)
                        missingPaddingLength = missingPaddingLength % 2;
                    param += new string(Base64Padding, missingPaddingLength);
                    lengthWPadding += missingPaddingLength;
                }
            }

            if (lengthWPadding == 0)
            {
                // Base64 string should not be empty
                return false;
            }

            // replace pad chacters
            var paramWOPadding = param.TrimEnd(Base64Padding);
            var lengthWOPadding = paramWOPadding.Length;

            if ((lengthWPadding - lengthWOPadding) > 2)
            {
                // there should be no more than 2 pad characters
                return false;
            }

            foreach (char c in paramWOPadding)
            {
                if (!Base64Characters.Contains(c))
                {
                    // string contains non-Base64 character
                    return false;
                }
            }

            // nothing invalid found
            return true;
        }

        internal static bool IsWhiteSpace(this char chr)
        {
            return chr == ' ' || chr == '\t' || chr == '\n' || chr == '\r';
        }

        internal static string NotEmpty(this string input, params string[] others)
        {
            if (!string.IsNullOrEmpty(input))
                return input;
            foreach (var item in others)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    return item;
                }
            }
            return string.Empty;
        }

        internal static string QuoteString(this string value)
        {
            return "\"" + value
                                            .Replace("\\", "\\\\")
                                            .Replace("\r", "\\r")
                                            .Replace("\n", "\\n")
                                            .Replace("\"", "\\\"") + "\"";
        }

        internal static byte[] Read(this Stream stream, int len)
        {
            var data = new byte[len];
            int read, pos = 0;
            while (pos < len && (read = stream.Read(data, pos, len - pos)) > 0)
            {
                pos += read;
            }
            return data;
        }

        internal static string ReadLine(this Stream stream, ref int maxLength, Encoding encoding, char? termChar, int ReadTimeout = 10000)
        {
            if (stream.CanTimeout)
                stream.ReadTimeout = ReadTimeout;

            var maxLengthSpecified = maxLength > 0;
            int i;
            byte b = 0, b0;
            var read = false;
            using (var mem = new MemoryStream())
            {
                while (true)
                {
                    b0 = b;
                    i = stream.ReadByte();
                    if (i == -1) break;
                    else read = true;

                    b = (byte)i;
                    if (maxLengthSpecified) maxLength--;

                    if (maxLengthSpecified && mem.Length == 1 && b == termChar && b0 == termChar)
                    {
                        maxLength++;
                        continue;
                    }

                    if (b == 10 || b == 13)
                    {
                        if (mem.Length == 0 && b == 10)
                        {
                            continue;
                        }
                        else break;
                    }

                    mem.WriteByte(b);
                    if (maxLengthSpecified && maxLength == 0)
                        break;
                }

                if (mem.Length == 0 && !read) return null;
                return encoding.GetString(mem.ToArray());
            }
        }

        internal static string ReadToEnd(this Stream stream, int maxLength, Encoding encoding)
        {
            if (stream.CanTimeout)
                stream.ReadTimeout = 10000;

            int read = 1;
            byte[] buffer = new byte[8192];
            using (var mem = new MemoryStream())
            {
                do
                {
                    var length = maxLength == 0 ? buffer.Length : Math.Min(maxLength - (int)mem.Length, buffer.Length);
                    read = stream.Read(buffer, 0, length);
                    mem.Write(buffer, 0, read);
                    if (maxLength > 0 && mem.Length == maxLength) break;
                } while (read > 0);

                return encoding.GetString(mem.ToArray());
            }
        }

        internal static void Set<KT, VT>(this IDictionary<KT, VT> dictionary, KT key, VT value)
        {
            if (!dictionary.ContainsKey(key))
                lock (dictionary)
                    if (!dictionary.ContainsKey(key))
                    {
                        dictionary.Add(key, value);
                        return;
                    }

            dictionary[key] = value;
        }

        internal static bool StartsWithWhiteSpace(this string line)
        {
            if (string.IsNullOrEmpty(line))
                return false;
            var chr = line[0];
            return IsWhiteSpace(chr);
        }

        internal static MailAddress ToEmailAddress(this string input)
        {
            try
            {
                return new MailAddress(input);
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static int ToInt(this string input)
        {
            int result;
            if (int.TryParse(input, out result))
            {
                return result;
            }
            else
            {
                return 0;
            }
        }

        internal static DateTime? ToNullDate(this string input)
        {
            DateTime result;
            input = NormalizeDate(input);
            if (DateTime.TryParse(input, _enUsCulture, DateTimeStyles.None, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        internal static string TrimEndOnce(this string line)
        {
            var result = line;

            if (result.EndsWithWhiteSpace())
                result = result.Substring(0, result.Length - 1);

            return result;
        }

        internal static string TrimStartOnce(this string line)
        {
            var result = line;

            if (result.StartsWithWhiteSpace())
                result = result.Substring(1, result.Length - 1);

            return result;
        }

        internal static void TryDispose<T>(ref T obj) where T : class, IDisposable
        {
            try
            {
                if (obj != null)
                    obj.Dispose();
            }
            catch (Exception) { }
            obj = null;
        }

        #endregion

        /*
		private static Dictionary<string, string> _TimeZoneAbbreviations = @"
ACDT +10:30
ACST +09:30
ACT +08
ADT -03
AEDT +11
AEST +10
AFT +04:30
AKDT -08
AKST -09
AMST +05
AMT +04
ART -03
AWDT +09
AWST +08
AZOST -01
AZT +04
BDT +08
BIOT +06
BIT -12
BOT -04
BRT -03
BTT +06
CAT +02
CCT +06:30
CDT -05
CEDT +02
CEST +02
CET +01
CHADT +13:45
CHAST +12:45
CIST -08
CKT -10
CLST -03
CLT -04
COST -04
COT -05
CST -06
CT +08
CVT -01
CXT +07
CHST +10
DFT +01
EAST -06
EAT +03
EDT -04
EEDT +03
EEST +03
EET +02
EST -05
FJT +12
FKST -03
FKT -04
GALT -06
GET +04
GFT -03
GILT +12
GIT -09
GMT
GYT -04
HADT -09
HAST -10
HKT +08
HMT +05
HST -10
ICT +07
IDT +03
IRKT +08
IRST +03:30
JST +09
KRAT +07
KST +09
LHST +10:30
LINT +14
MAGT +11
MDT -06
MIT -09:30
MSD +04
MSK +03
MST -07
MUT +04
MYT +08
NDT -02:30
NFT +11:30
NPT +05:45
NST -03:30
NT -03:30
NZDT +13
NZST +12
OMST +06
PDT -07
PETT +12
PHOT +13
PKT +05
PST -08
RET +04
SAMT +04
SAST +02
SBT +11
SCT +04
SGT +08
SLT +05:30
TAHT -10
THA +07
UYST -02
UYT -03
VET -04:30
VLAT +10
WAT +01
WEDT +01
WEST +01
WET
WST +08
YAKT +09
YEKT +05"
				.Trim().Split('\n').Select(line => line.Trim().Split(' ').Select(col => col.Trim()).Take(2).ToArray())
				.Where(x => x.Length == 2).ToDictionary(x => x[0], x => x[1]);

		internal static System.DateTime? ToNullDate(this string input, string format = null, DateTimeKind kind = DateTimeKind.Unspecified) {
				if (string.IsNullOrEmpty(input)) return null;
				if (input.Contains("T")) {
						foreach (var x in _TimeZoneAbbreviations) {
								input = input.Replace(x.Key, x.Value);
						}
				}

				System.DateTime num;
				if ((format != null && DateTime.TryParseExact(input, format, null, System.Globalization.DateTimeStyles.None, out num))
						|| (System.DateTime.TryParse(input, out  num))) {
						return DateTime.SpecifyKind(num, kind == DateTimeKind.Unspecified && input.Contains('Z') ? DateTimeKind.Utc : kind);
				} else {
						return null;
				}
		}
		 */
    }
}