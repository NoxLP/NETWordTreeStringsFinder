using NETWordTreeStringsFinder.StringFinderBase;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NETWordTreeStringsFinder
{
    public sealed class DateTimeStringFinder : aStringFinderBase
    {
        #region fields
        private static readonly char[] _DatePartChar = new char[3] { 'd', 'M', 'y' };
        private static readonly char[] _Separators = new char[4] { '.', '/', ' ', '-' };
        private static readonly string _DateSeparators = @"(\.|\/|\ |\-)";
        private static readonly string _DigitNumbers1 = @"[1-9]";
        private static readonly string _DigitNumbers2 = @"\d{2}";
        private static readonly string _DigitNumbers4 = @"\d{4}";

        private static DateTimeFormatInfo _DtFInfo;
        private static string _AbbreviatedMonths;
        private static string _Months;
        private static string _RegexString;
        #endregion

        #region properties
        public static string[] DateFormats { get; private set; }
        #endregion

        #region init
        private static async Task BuildDTFInfoAsync()
        {
            if (_DtFInfo == null)
            {
                var current = CultureInfo.CurrentCulture;
                _DtFInfo = CultureInfo.CreateSpecificCulture(CultureInfo.CurrentCulture.Name).DateTimeFormat;
                if (current.Name.Equals("es") || current.Name.Contains("es-"))
                {
                    _DtFInfo.AbbreviatedMonthNames = _DtFInfo.AbbreviatedMonthGenitiveNames = new string[13]
                    {
                "ene",
                "feb",
                "mar",
                "abr",
                "may",
                "jun",
                "jul",
                "ago",
                "sep",
                "oct",
                "nov",
                "dic",
                ""
                    };
                }
                _Months = $"({string.Join("|", _DtFInfo.MonthNames.Take(_DtFInfo.MonthNames.Length - 1))})";
                _AbbreviatedMonths = $"({string.Join("|", _DtFInfo.AbbreviatedMonthNames.Take(_DtFInfo.MonthNames.Length - 1))})";
            }
        }
        private static bool AllCharsAreOneDatePart(string source)
        {
            DatePart? previous = (DatePart)Array.IndexOf(_DatePartChar, source[0]);
            if (!previous.HasValue)
                return false;

            DatePart? current = null;
            foreach (char c in source)
            {
                current = (DatePart)Array.IndexOf(_DatePartChar, c);
                if (!current.HasValue || !previous.Equals(current))
                    return false;
            }

            return true;
        }
        private static void AddToRegex(string part, string format, ref List<string> formatToRegex, bool addSeparator)
        {
            if (!_DatePartChar.Contains(part[0]))
            {
                SetLastException(
                    new ArgumentException($"DateTime string format {format} is wrong: chars for date parts must be the following(case sensitive): 'd' for day, 'M' for month, 'y' for year"));
                throw LastException;
            }
            int count = part.Length;
            DatePart? dPart = (DatePart)Array.IndexOf(_DatePartChar, part[0]);

            switch (dPart)
            {
                case DatePart.Day:
                    if (count == 1)
                        formatToRegex.Add(_DigitNumbers1);
                    else if (count == 2)
                        formatToRegex.Add(_DigitNumbers2);
                    else
                    {
                        SetLastException(new ArgumentException($"DateTime string format {format} is wrong: day must have ONLY 1 or 2 'd' chars"));
                        throw LastException;
                    }
                    break;
                case DatePart.Month:
                    if (count == 2)
                        formatToRegex.Add(_DigitNumbers2);
                    else if (count == 3)
                        formatToRegex.Add(_AbbreviatedMonths);
                    else if (count == 4)
                        formatToRegex.Add(_Months);
                    else
                    {
                        SetLastException(new ArgumentException($"DateTime string format {format} is wrong: month must have ONLY 2, 3 or 4 'M' chars"));
                        throw LastException;
                    }
                    break;
                case DatePart.Year:
                    if (count == 2)
                        formatToRegex.Add(_DigitNumbers2);
                    else if (count == 4)
                        formatToRegex.Add(_DigitNumbers4);
                    else
                    {
                        SetLastException(new ArgumentException($"DateTime string format {format} is wrong: month must have ONLY 2 or 4 'y' chars"));
                        throw LastException;
                    }
                    break;
            }
            if(addSeparator)
                formatToRegex.Add(_DateSeparators);
        }
        private static async Task BuildRegexAsync()
        {
            HashSet<string> regexParts = new HashSet<string>();

            int formatCount, i;
            bool addSeparator;
            List<string> formatToRegex = new List<string>();
            string[] split = null;
            foreach (var format in DateFormats)
            {
                formatToRegex.Clear();
                split = null;
                foreach (var separator in _Separators)
                {
                    if (!format.Contains(separator))
                        continue;

                    split = format.Split(separator);
                    formatCount = split.Length;
                    i = 1;                    
                    foreach (var part in split)
                    {
                        addSeparator = i < formatCount;
                        AddToRegex(part, format, ref formatToRegex, addSeparator);
                        i++;
                    }
                }
                if (split == null)
                {
                    if (format.Length > 4 || !AllCharsAreOneDatePart(format))
                    {
                        SetLastException(new ArgumentException(
$"DateTime string format { format } is wrong: formats must have one of these separators: '.', '/', ' ', '-' ; or be one only date part, f.i. {"MM"}, {"d"} without spaces or other symbols"));
                        throw LastException;
                    }

                    AddToRegex(format, format, ref formatToRegex, false);
                }
                regexParts.Add($"({string.Join("",formatToRegex)})");
            }

            _RegexString = $"({string.Join("|", regexParts.ToArray())})";
        }
        /// <summary>
        /// Constructor estático con cultura y formatinfo en español
        /// </summary>
        /// <param name="dateFormatsFile">One DateTime string format per line txt file</param>
        public static async Task InitAsync(FileInfo dateFormatsFile)
        {
            DateFormats = File.ReadLines(dateFormatsFile.FullName).ToArray();
            var d = BuildDTFInfoAsync();
            var r = BuildRegexAsync();
            await d;
            await r;
            SetLastException(null);
        }
        #endregion

        private async Task<Tuple<string, List<DateTime>>> GetDatesInAsync(string match)
        {
            bool hasDate = false;
            DateTime fecha;
            HashSet<DateTime> current = new HashSet<DateTime>();
            foreach (var format in DateFormats)
            {
                hasDate = DateTime.TryParseExact(match, format, CultureInfo.CurrentCulture, DateTimeStyles.None, out fecha);
                //hasDate = DateTime.TryParseExact(match.ToString(), DateFormat, null, DateTimeStyles.None, out fecha);
                if (hasDate)
                {
                    current.Add(fecha);
                }
            }

            if (current.Count == 0)
                return null;
            return new Tuple<string, List<DateTime>>(match, current.ToList());
        }
        public async Task<Tuple<string, List<DateTime>>[]> GetDatesInTargetStringAsync(string targetString)
        {
            var msg = !string.IsNullOrEmpty(LastErrorMsg);
            var ok = !await GetIfStaticDataIsOKAsync();
            if (!string.IsNullOrEmpty(LastErrorMsg) || !await GetIfStaticDataIsOKAsync())
            {
                SetLastException(new ArgumentException($"Error in static data initialization: {LastErrorMsg}"));
                throw LastException;
            }

            targetString = targetString.ToLower();
            var dateRegex = new Regex(_RegexString, RegexOptions.IgnoreCase);
            var matchCollection = dateRegex.Matches(targetString);
            Task<Tuple<string, List<DateTime>>>[] tasks = new Task<Tuple<string, List<DateTime>>>[matchCollection.Count];
            for (int i = 0; i < matchCollection.Count; i++)
            {
                if (!matchCollection[i].Success || string.IsNullOrEmpty(matchCollection[i].Value))
                    continue;

                tasks[i] = GetDatesInAsync(matchCollection[i].Value);
            }
            
            var matches = await Task.WhenAll(tasks.Where(t => t != null && t.Result != null).ToArray());

            if (matches.Length > 0)
                return matches;

            return null;
        }

        protected async override Task<bool> GetIfStaticDataIsOKAsync()
        {
            if (!string.IsNullOrEmpty(_RegexString))
                return true;
            return false;
        }
    }
}
