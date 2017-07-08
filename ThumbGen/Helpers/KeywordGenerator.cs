using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace ThumbGen
{
    internal static class KeywordGenerator
    {
        private static string NOISE_FILTER = @"(([\(\{\[]|\b)((576|720|1080)[pi]|dir(ectors )?cut|dvd([r59]|rip|scr(eener)?)|(avc)?hd|wmv|ntsc|pal|mpeg|dsr|r[1-5]|bd[59]|dts|ac3|blu(-)?ray|[hp]dtv|stv|hddvd|xvid|bdrip|divx|x264|dxva|axxo|stg|fxg|flawl3ss|metis|(?i)FEST[Ii]VAL|L[iI]M[iI]TED|[WF]S|PROPER|REPACK|RER[Ii]P|REAL|RETA[Ii]L|EXTENDED|REMASTERED|UNRATED|CHRONO|THEATR[Ii]CAL|DC|SE|UNCUT|[Ii]NTERNAL|[DS]UBBED)([\]\)\}]|\b)(-[^\s]+$)?)";
        //private static string SERIES_FILTER = @"[s\._]([0-9]+)\.?_?\s?[e\._]([0-9]+)*";
        private static string[] SERIES_FILTER = new string[] { "([0-9]+)x([0-9]+)", @"[Ss]([0-9]+)[\.\-]?[EeDd]([0-9]+)" };
        //private static string SERIES_FILTER_2 = "([0-9])([0-9]{2})";


        // group 2 = season, group 3 = episode (may have leading zeroes)
        //private static string SEASON_EPISODE_FILTER = @"([s\._]([0-9]+)?\.?_?\s?)?[e\._]([0-9]+)*";

        private static string CD_FILTER = @"cd[\ ]?([0-9]+)"; // group 1 = cd number


        private static Dictionary<string, string> m_Replacements;

        static KeywordGenerator()
        {
            m_Replacements = new Dictionary<string, string>();
            m_Replacements.Add(@"(?<!\b\p{L})\.", " ");  // Replace dots that are not part of acronyms with spaces             
            m_Replacements.Add(@"_", " ");  // Replace underscores with spaces             
            m_Replacements.Add(@"tt\d{7}", ""); // Removes imdb numbers 
        }

        public static string GetKeywords(string input, out string year)
        {
            string _keywords = input;

            // apply user defined black list
            _keywords = ApplyBlacklist(_keywords);

            if (!FileManager.Configuration.Options.SwitchOffInternalNoiseRemover)
            {
                // Execute configured string replacements             
                foreach (KeyValuePair<string, string> replacement in m_Replacements)
                    _keywords = Regex.Replace(_keywords, replacement.Key, replacement.Value);
                // Remove noise characters/words
                _keywords = RemoveNoise(_keywords);
                // Remove series/episodes 
                _keywords = RemoveSeriesEpisodesNoise(_keywords);
            }
            // Detect year in a title string             
            int iYear = 0;
            // if the remover is switched off, return the current keywords and just process the year
            string _tmp = ExtractYearFromTitle(_keywords, false, out iYear);
            _keywords = FileManager.Configuration.Options.SwitchOffInternalNoiseRemover ? _keywords : _tmp;
            if (iYear != 0)
            {
                year = iYear.ToString();
            }
            else
            {
                year = null;
            }

            return _keywords.Trim().ToLowerInvariant();
        }

        private static string ApplyBlacklist(string input)
        {
            try
            {
                if (FileManager.Configuration.Options.UseBlacklist && !string.IsNullOrEmpty(FileManager.Configuration.Options.Blacklist))
                {
                    string _blist = FileManager.Configuration.Options.Blacklist.Trim();
                    if (!string.IsNullOrEmpty(_blist))
                    {
                        _blist = _blist.ToLowerInvariant();
                        string[] _words = _blist.Split(',');
                        foreach (string _word in _words)
                        {
                            if (!string.IsNullOrEmpty(_word))
                            {
                                input = Regex.Replace(input, _word, string.Empty, RegexOptions.IgnoreCase);
                                //input = input.Replace(_word, string.Empty);
                            }
                        }
                    }
                }
            }
            catch { }

            return input;
        }

        // Filters "noise" from the input string
        public static string RemoveNoise(string input)
        {
            Regex expr = new Regex(NOISE_FILTER, RegexOptions.IgnoreCase);
            string denoisedTitle = expr.Replace(input, "");
            denoisedTitle = denoisedTitle.Trim();
            return denoisedTitle;
        }

        // filter out the tv shows
        private static string RemoveSeriesEpisodesNoise(string input)
        {
            string denoisedTitle = input;
            foreach (string _s in SERIES_FILTER)
            {
                Regex expr = new Regex(_s, RegexOptions.IgnoreCase);
                denoisedTitle = expr.Replace(denoisedTitle, "");
                denoisedTitle = denoisedTitle.Trim();
            }
            return denoisedTitle;
        }

        public static string ExtractYearFromTitle(string input, bool allowEmptyResult, out int year)
        {
            string rtn = input;
            year = 0;
            // if there is a four digit number that looks like a year, parse it out             
            Regex expr = new Regex(@"^(.*)[\[\(]?(19\d{2}|20\d{2})[\]\)]?($|.+)");
            Match match = expr.Match(rtn);
            if (match.Success)
            {
                rtn = match.Groups[1].Value.TrimEnd('(', '['); // leading title string                 
                year = int.Parse(match.Groups[2].Value);
                if (rtn.Trim() == string.Empty)
                    rtn = match.Groups[3].Value.TrimEnd('(', '['); // trailing title string             
            }
            // If the title becomes 0 length, undo this method's processing.             
            if (rtn.Trim().Length == 0 && !allowEmptyResult)
            {
                rtn = input;
                year = 0;
                return rtn;
            }
            else
            {
                return rtn.Trim();
            }
        }

        private static string PrepareSeasonEpisode(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                input = input.Trim();
                //if (!string.IsNullOrEmpty(input))
                //{
                //    input = input.TrimStart('0');
                //}
                if (!string.IsNullOrEmpty(input))
                {
                    int _n;
                    if (!Int32.TryParse(input, out _n))
                    {
                        input = null;
                    }
                    else
                    {
                        input = _n.ToString();
                    }
                }
            }
            return input;
        }

        public static string ExtractCDNumber(string input)
        {
            string _cd = null;
            Regex _filter = new Regex(CD_FILTER, RegexOptions.IgnoreCase);
            if (_filter.IsMatch(input))
            {
                foreach (Match _match in _filter.Matches(input))
                {
                    _cd = _match.Groups[1].Value;

                    if (!string.IsNullOrEmpty(_cd))
                    {
                        // prepare cd number
                        _cd = PrepareSeasonEpisode(_cd);
                    }

                    if (!string.IsNullOrEmpty(_cd))
                    {
                        break;
                    }
                }
            }
            return _cd;
        }

    }

    public enum EpisodeType
    {
        AiredOrder,
        DVDOrder,
        Absolute
    }

    public abstract class BaseSeriesFilter
    {
        public abstract string Filter { get; }
        public virtual EpisodeType Type { get { return EpisodeType.AiredOrder; } }
        public virtual bool HasSeason { get { return true; } }
    }

    public class XFilter : BaseSeriesFilter
    {
        public override string Filter { get { return "([0-9]+)x([0-9]+)"; } }
    }

    public class SEAiredFilter : BaseSeriesFilter
    {
        public override string Filter { get { return @"[Ss]([0-9]+)[\.\-]?[Ee]([0-9]+)"; } }
    }

    public class SEDVDFilter : BaseSeriesFilter
    {
        public override string Filter { get { return @"[Ss]([0-9]+)[\.\-]?[Dd]([0-9]+)"; } }
        public override EpisodeType Type { get { return EpisodeType.DVDOrder; } }
    }

    public class AbsoluteFilter : BaseSeriesFilter
    {
        public override string Filter { get { return @"()([0-9]{2,3})"; } }
        public override EpisodeType Type { get { return EpisodeType.Absolute; } }
        public override bool HasSeason { get { return false; } }
    }

    public class ShortFilter : BaseSeriesFilter
    {
        public override string Filter { get { return @"([0-9]{1,2})([0-9]{2})"; } }
    }

    public class EpisodeData
    {
        //private static string[] SeasonEpisodeFilters = new string[] { "([0-9]+)x([0-9]+)", @"[Ss]([0-9]+)[\.\-]?[Ee]([0-9]+)", @"[Ss]([0-9]+)[\.\-]?[Dd]([0-9]+)" };
        // group 1 = season, group 2 = episode
        //private static string SEASON_EPISODE_FILTER_2 = "([0-9])([0-9]{2})";

        public string Season { get; private set; }
        public string Episode { get; private set; }
        public EpisodeType Type { get; private set; }
        public string FileNameWithoutExt { get; private set; }

        public static bool IsEpisodeFile(string filePath)
        {
            return new EpisodeData(filePath).ExtractData();
        }

        private static List<BaseSeriesFilter> m_Filters = new List<BaseSeriesFilter>();

        public EpisodeData(string filePath)
        {
            m_Filters.Clear();

            // add filters here based on user's settings
            UserOptions.TVShowsFilters _o = FileManager.Configuration.Options.TVShowsFiltersOptions;
            if (_o.UseX)
            {
                m_Filters.Add(new XFilter());
            }
            if (_o.UseAired || _o.UseDVD)
            {
                m_Filters.Add(new SEAiredFilter());
                m_Filters.Add(new SEDVDFilter());
            }
            
            if (_o.UseAbsolute)
            {
                m_Filters.Add(new AbsoluteFilter());
            }
            if (_o.UseShort)
            {
                m_Filters.Add(new ShortFilter());
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                FileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            }
        }

        private static string PrepareSeasonEpisode(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                input = input.Trim();
                if (!string.IsNullOrEmpty(input))
                {
                    int _n;
                    if (!Int32.TryParse(input, out _n))
                    {
                        input = null;
                    }
                    else
                    {
                        input = _n.ToString();
                    }
                }
            }
            return input;
        }

        private bool ExtractData()
        {
            bool _result = false;
            Season = null;
            Episode = null;
            Type = EpisodeType.AiredOrder;

            if (!string.IsNullOrEmpty(FileNameWithoutExt))
            {
                foreach (BaseSeriesFilter _filter in m_Filters)
                {
                    Regex _reg = new Regex(_filter.Filter, RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(FileNameWithoutExt))
                    {
                        foreach (Match _match in _reg.Matches(FileNameWithoutExt))
                        {
                            // Season (Group 1)
                            string _season = _match.Groups[1].Value;
                            if (!string.IsNullOrEmpty(_season))
                            {
                                // prepare season
                                Season = PrepareSeasonEpisode(_season);
                            }
                            // Episode (Group 2)
                            string _episode = _match.Groups[2].Value;
                            if (!string.IsNullOrEmpty(_episode))
                            {
                                Episode = PrepareSeasonEpisode(_episode);
                            }
                            // if we have both Season and Episode then return
                            if (!string.IsNullOrEmpty(Episode) && ((_filter.HasSeason && !string.IsNullOrEmpty(Season)) || !_filter.HasSeason))
                            {
                                Type = _filter.Type;
                                return true;
                            }
                        }
                    }
                }
            }

            return _result;
        }

        public static EpisodeData GetEpisodeData(string filePath)
        {
            EpisodeData _result = new EpisodeData(filePath);

            _result.ExtractData();

            return _result;
        }
    }
}
