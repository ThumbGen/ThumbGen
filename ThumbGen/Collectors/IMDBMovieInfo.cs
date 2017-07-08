using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Globalization;

namespace ThumbGen
{
    public class MovieCertification
    {
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string Certification { get; set; }

        public MovieCertification(string code, string name, string certif)
        {
            CountryCode = code;
            CountryName = name;
            Certification = certif;
        }
    }

    public class IMDBMovieInfoCacheItem
    {
        public MovieInfo MovieInfo { get; set; }
        public string CountryCode { get; set; }

        public IMDBMovieInfoCacheItem()
            : this("us", null)
        {

        }

        public IMDBMovieInfoCacheItem(string countryCode, MovieInfo info)
        {
            MovieInfo = info;
            CountryCode = countryCode;
        }
    }

    internal class IMDBMovieInfo
    {
        public string Language { get; private set; }

        private const string ResultsRegexPattern = @"result_text""> <a href=""/title/(?<Id>tt\d{7})/\?ref_=fn_tt_tt_\d+"" >(?<Title>[^<]+)</a>.*? \((?<Year>\d{4})";
        
        private IMDBCountryFactory m_CountryFactory;

        private string GetTitle(string page)
        {
            string _res = BaseCollector.GetItem(page, @"<title>(?<Title>[^\(]+)", "Title").Trim().Trim(new char[] { '"' }).Replace("IMDb - ", "");
            if (!string.IsNullOrEmpty(_res))
            {
                return _res;
            }
            else
            {
                Match match = new Regex(@"<title>(.*?)\s\(\d{4}", RegexOptions.Multiline | RegexOptions.IgnoreCase).Match(page);
                if (match.Success)
                {
                    return HttpUtility.HtmlDecode(match.Groups[1].Value.Trim()).Trim(new char[] { '"' });
                }
                return string.Empty;
            }
        }

        private string GetOriginalTitle(string page)
        {
            string _res = BaseCollector.GetItem(page, "class=\"title-extra\">(?<OriginalTitle>[^<^\\(]+)", "OriginalTitle").Trim().Trim(new char[] { '"' });
            if (!string.IsNullOrEmpty(_res))
            {
                return _res;
            }
            else
            {
                Match match = new Regex("class=\"title-extra\">(?<OriginalTitle>[^<^\\(]+)", RegexOptions.Singleline | RegexOptions.IgnoreCase).Match(page);
                if (match.Success)
                {
                    return HttpUtility.HtmlDecode(match.Groups["OriginalTitle"].Value.Trim()).Trim(new char[] { '"' });
                }
                return string.Empty;
            }
        }

        private string GetYear(string page)
        {
            string _res = BaseCollector.GetItem(page, @"<title>[^\(]+\((?<Year>\d{4})|<title>[^\(]+\(.*?(?<Year>\d{4})", "Year");
            if (!string.IsNullOrEmpty(_res))
            {
                return _res;
            }
            else
            {
                Match match = new Regex(@"<title>.*?\s\((\d{4})", RegexOptions.Multiline | RegexOptions.IgnoreCase).Match(page);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                return string.Empty;
            }
        }

        private string GetRunTime(string page)
        {
            string _res = BaseCollector.GetItem(page, "(?<Runtime>\\d{1,3})\\smin", "Runtime");
            if (!string.IsNullOrEmpty(_res))
            {
                return _res;
            }
            else
            {
                Match match = new Regex(@"(\d{1,3})\smin", RegexOptions.Multiline | RegexOptions.IgnoreCase).Match(page);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                return string.Empty;
            }
        }

        private string GetRating(string page)
        {
            string _res = BaseCollector.GetItem(page, @"itemprop=""ratingValue"">(?<Rating>[\d\.]+)</span></strong><span class=""mellow"">/<span itemprop=""bestRating"">10", "Rating");
            if (!string.IsNullOrEmpty(_res))
            {
                return _res;
            }
            else
            {
                Match match = new Regex(@"<b>(\d.*?)/10</b>", RegexOptions.Multiline | RegexOptions.IgnoreCase).Match(page);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                return string.Empty;
            }
        }

        private string GetReleaseDateFast(string page)
        {
            string _res = BaseCollector.GetItem(page, m_CountryFactory.RE_ReleaseDateFast + @"</h4>.*?(?<ReleaseDate>[^\(]*)", "ReleaseDate");
            if (!string.IsNullOrEmpty(_res))
            {
                return _res;
            }
            else
            {
                Match match = new Regex(m_CountryFactory.RE_ReleaseDateFast + @"</h5>.*\n.*\n([^\(]*)", RegexOptions.Multiline | RegexOptions.IgnoreCase).Match(page);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                return string.Empty;
            }
        }

        private string GetReleaseDate(string imdbid)
        {
            string _page = Helpers.GetPage(string.Format("{0}/title/{1}/releaseinfo", m_CountryFactory.TargetHost, imdbid), null, m_CountryFactory.GetEncoding, "", true, false, m_CountryFactory.Language);
            if (!string.IsNullOrEmpty(_page))
            {
                Match match = new Regex("<a href=\"/date/[0-9\\-]*/\">([^<]*)</a> <a href=\"/year/\\d{4}/\">(\\d{4})</a>", RegexOptions.Multiline | RegexOptions.IgnoreCase).Match(_page);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim() + " " + match.Groups[2].Value.Trim();
                }
            }
            return string.Empty;
        }

        private string GetTagline(string page)
        {
            string _res = BaseCollector.GetItem(page, "Taglines:</h4>.*?(?<Tagline>[^<]+)<", "Tagline").Trim().Replace("\r", "").Replace("\n", "").Trim();
            if (!string.IsNullOrEmpty(_res))
            {
                return _res;
            }
            else
            {
                Match match = new Regex("Tagline:</h5>.*?info-content\">(.*?)<", RegexOptions.Singleline | RegexOptions.IgnoreCase).Match(page);
                if (match.Success)
                {
                    return HttpUtility.HtmlDecode(match.Groups[1].Value.Replace("\r", "").Replace("\n", "").Trim());
                }
                return string.Empty;
            }
        }

        private string GetMetascore(string page)
        {
            string _res = BaseCollector.GetItem(page, @"Metascore: <a href=""criticreviews\?ref_=tt_ov_rt"" title=""(?<Metascore>\d+)\s", "Metascore", RegexOptions.Singleline).Trim();
            if (!string.IsNullOrEmpty(_res))
            {
                return _res;
            }
            return string.Empty;
        }

        private string GetMPAA(string page)
        {
            string _res = BaseCollector.GetItem(page, "contentRating\">(?<MPAA>[^<]+)<", "MPAA").Trim().Replace("\r", "").Replace("\n", "").Trim();
            if (!string.IsNullOrEmpty(_res))
            {
                return _res;
            }
            else
            {
                Match match = new Regex("MPAA</a>:</h5>[\\r\\n]*<div class=\"info-content\">([^<]+)</div", RegexOptions.Singleline | RegexOptions.IgnoreCase).Match(page);
                if (match.Success)
                {
                    return HttpUtility.HtmlDecode(match.Groups[1].Value.Replace("\r", "").Replace("\n", "").Trim());
                }
                return string.Empty;
            }
        }

        private string GetTrailerLink(string page)
        {
            string _res = BaseCollector.GetItem(page, @"<a href=""(?<Trailer>/video/imdb/vi\d+/\?ref_=tt_ov_vi)"" class=""btn2 btn2_text_on large title-trailer", "Trailer");
            if (!string.IsNullOrEmpty(_res))
            {
                return _res;
            }
            else
            {
                return string.Empty;
            }
        }

        private List<string> GetCompanies(string page)
        {
            List<string> list = BaseCollector.GetItems(page, @"href=""/company/co\d+\?ref_=tt_dt_co"" itemprop='url'>(<span class=""itemprop"" itemprop=""name"">)?(?<Company>[^<]+)</", "Company", RegexOptions.Singleline);
            if (list.Count == 0)
            {
                // old layout
                list = new List<string>();
                Regex regex = new Regex(string.Format("<h5>{0}</h5>.*?info-content\">(.*?)</div>", m_CountryFactory.RE_Companies), RegexOptions.Multiline | RegexOptions.IgnoreCase);
                Match match = regex.Match(page.Replace("\n", ""));
                if (match.Success)
                {
                    regex = new Regex("<a href=\"/company.*?>(.*?)</a>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    foreach (Match match2 in regex.Matches(match.Groups[1].Value))
                    {
                        list.Add(HttpUtility.HtmlDecode(match2.Groups[1].Value));
                    }
                }
            }
            return list;
        }

        private string GetPlot(string page)
        {
            string _res = BaseCollector.GetItem(page, m_CountryFactory.RE_Plot + "</h2>.*?<p>(?<Plot>[^<]+)", "Plot", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!string.IsNullOrEmpty(_res))
            {
                return _res;
            }
            else
            {
                Match match = new Regex(string.Format("<h5>{0}</h5>.*?info-content\">(.*?)</div>", m_CountryFactory.RE_Plot), RegexOptions.Multiline | RegexOptions.IgnoreCase).Match(page.Replace("\n", ""));
                if (match.Success)
                {
                    string _result = HttpUtility.HtmlDecode(Regex.Replace(match.Groups[1].Value, "<a class.*?</a>", "")).Trim().TrimEnd(new char[] { '|' }).TrimEnd(new char[0]);
                    _result = Regex.Replace(_result, "<a href=\"/title.*?</a>", "");
                    int _start = _result.LastIndexOf(".");
                    if (_start >= 0)
                    {
                        try
                        {
                            _result = _result.Substring(0, _start + 1);
                        }
                        catch { }
                    }
                    return _result;
                }

                return string.Empty;
            }
        }

        private string GetOverview(string imdbid)
        {
            string _result = null;

            if (!string.IsNullOrEmpty(imdbid))
            {
                string _summaryPage = Helpers.GetPage(string.Format("{0}/title/{1}/plotsummary", m_CountryFactory.TargetHost, imdbid), null, m_CountryFactory.GetEncoding, "", true, false, m_CountryFactory.Language);
                if (!string.IsNullOrEmpty(_summaryPage))
                {
                    Match match = new Regex(m_CountryFactory.RE_Overview, RegexOptions.Singleline | RegexOptions.IgnoreCase).Match(_summaryPage);
                    if (match.Success)
                    {
                        _result = HttpUtility.HtmlDecode(match.Groups["Plot"].Value.Trim().Replace("<br/>", "").Replace("<br>", ""));
                    }
                }
            }

            return _result;
        }

        private List<string> GetGenres(string page)
        {
            // new layout
            List<string> _result = BaseCollector.GetItems(page, @"<a href=""/genre/[^\?]+\?ref_=tt_ov_inf"" >(<span class=""itemprop"" itemprop=""genre"">)?(?<Genre>[^<]+)</", "Genre", RegexOptions.Singleline);
            if (_result.Count != 0)
            {
                return _result;
            }
            else
            {
                // old layout
                List<string> list = new List<string>();
                MatchCollection matchs = new Regex("<a href=\"/Sections/Genres/[^/]*/\">([^\"]*)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase).Matches(page);
                if (matchs.Count != 0)
                {
                    foreach (Match match in matchs)
                    {
                        list.Add(HttpUtility.HtmlDecode(match.Groups[1].Value.Trim()));
                    }
                }
                else
                {
                    matchs = new Regex(m_CountryFactory.RE_Genres, RegexOptions.Singleline | RegexOptions.IgnoreCase).Matches(page);

                    foreach (Match match in matchs)
                    {
                        string[] _s = match.Groups[1].Value.Split('|');
                        foreach (string _item in _s)
                        {
                            list.Add(HttpUtility.HtmlDecode(_item.Trim().Trim('\r').Trim()));
                        }
                    }
                }
                return list;
            }
        }

        private List<string> GetDirectors(string page)
        {
            // new layout
            List<string> list = new List<string>();
            //Regex regex = new Regex(string.Format("{0}(.*?)</div>", m_CountryFactory.RE_Directors), RegexOptions.Singleline | RegexOptions.IgnoreCase);
            //Match match = regex.Match(page.Replace("\n", ""));
            //if (match.Success)
            //{
                list = BaseCollector.GetItems(page, @"href=""/name/nm\d+/\?ref_=tt_ov_\wr"" itemprop='url'>(<span class=""itemprop"" itemprop=""name"">)?(?<Director>[^<]+)</", "Director");
            //}
            if (list.Count != 0)
            {
                return list.Take(1).ToList();
            }
            else
            {
                list = BaseCollector.GetItems(page, "itemprop=\"director\".*?>(?<Director>[^<]+)</a", "Director", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (list.Count != 0)
                {
                    return list;
                }
                else
                {
                    List<string> list2 = new List<string>();
                    Regex regex2 = new Regex(string.Format("<h5>{0}</h5>.*?info-content\">(.*?)</div>", m_CountryFactory.RE_Directors), RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    Match match2 = regex2.Match(page.Replace("\n", ""));
                    if (match2.Success)
                    {
                        Regex regex3 = new Regex("<a\\shref=\"/name/nm\\d{7}/\".*?\">(.*?)</a>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                        foreach (Match match22 in regex3.Matches(match2.Groups[2].Value))
                        {
                            list2.Add(HttpUtility.HtmlDecode(match22.Groups[1].Value));
                        }
                    }
                    return list2;
                }
            }
        }

        private List<string> GetActors(string page)
        {
            List<string> _res = BaseCollector.GetItems(page, @"href=""/name/nm[0-9]+/\?ref_=tt_cl_t\d+"" itemprop='[^']+'>\s*(<span class=""itemprop"" itemprop=""name"">)?(?<Actor>[^<]+)</", "Actor", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (_res.Count != 0)
            {
                return _res;
            }
            else
            {
                MatchCollection matchs = new Regex("<td class=\"nm\"><a href=\"(.*?)\">(.*?)</a>", RegexOptions.Multiline | RegexOptions.IgnoreCase).Matches(page);
                List<string> list = new List<string>();
                foreach (Match match in matchs)
                {
                    list.Add(HttpUtility.HtmlDecode(match.Groups[2].Value.Trim()));
                }
                return list;
            }
        }

        private string GetCertification(string countryCode, string page, string imdbid)
        {
            string _result = string.Empty;

            if (!string.IsNullOrEmpty(countryCode))
            {
                List<MovieCertification> _certifList = GetCertifications(page, imdbid);
                foreach (MovieCertification _cer in _certifList)
                {
                    if (string.Compare(countryCode, _cer.CountryCode, true) == 0)
                    {
                        _result = _cer.Certification;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(_result) && string.Compare(countryCode, "us", true) != 0)
            {
                _result = GetCertification("us", page, imdbid);
            }

            return _result;
        }

        private List<MovieCertification> DoGetCertifs(string page)
        {
            List<MovieCertification> list = new List<MovieCertification>();
            Regex regex = new Regex("<h5>Certification:</h5>.*?info-content\">(.*?)</div>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Match match = regex.Match(page.Replace("\n", ""));
            if (match.Success)
            {
                regex = new Regex("certificates=(?<CountryCode>[a-z]*)(:|\\|)[^\"]*\">(?<CountryName>[^:]*):(?<Certification>[^<]*)<", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                foreach (Match match2 in regex.Matches(match.Groups[1].Value))
                {
                    list.Add(new MovieCertification(HttpUtility.HtmlDecode(match2.Groups["CountryCode"].Value),
                                                    HttpUtility.HtmlDecode(match2.Groups["CountryName"].Value),
                                                    HttpUtility.HtmlDecode(match2.Groups["Certification"].Value)));
                }
            }
            return list;
        }

        private List<MovieCertification> GetCertifications(string page, string imdbid)
        {
            string _certifsPage = Helpers.GetPage(string.Format("{0}/title/{1}/parentalguide", m_CountryFactory.TargetHost, imdbid), null, m_CountryFactory.GetEncoding, "", true, false, m_CountryFactory.Language);
            if (!string.IsNullOrEmpty(_certifsPage))
            {
                List<MovieCertification> _results = DoGetCertifs(_certifsPage);
                return _results.Count == 0 ? DoGetCertifs(page) : _results;
            }
            else
            {
                return DoGetCertifs(page);
            }
        }

        private List<string> GetCountries(string page)
        {
            List<string> list = new List<string>();

            list = BaseCollector.GetItems(page, @"<a href=""/country/[^\?]+\?ref_=tt_dt_dt"" itemprop='url'>(?<Country>[^<]+)</a>", "Country");

            if (list.Count != 0)
            {
                return list;
            }
            else
            {

                Regex regex = new Regex(string.Format("<h5>{0}</h5>.*?info-content\">(.*?)</div>", m_CountryFactory.RE_Countries), RegexOptions.Multiline | RegexOptions.IgnoreCase);
                Match match = regex.Match(page.Replace("\n", "").Replace("\r", ""));
                if (match.Success)
                {
                    //regex = new Regex("<a\\shref=\"/Sections/Countries.*?>(.*?)</a>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    regex = new Regex("<a href=\"/country.*?>(.*?)</a>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    if (regex.Matches(match.Groups[1].Value).Count > 0)
                    {
                        foreach (Match match2 in regex.Matches(match.Groups[1].Value))
                        {
                            list.Add(HttpUtility.HtmlDecode(match2.Groups[1].Value));
                        }
                    }
                    else
                    {
                        regex = new Regex("<a\\shref=\"/Sections/Countries.*?>(.*?)</a>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                        if (regex.Matches(match.Groups[1].Value).Count > 0)
                        {
                            foreach (Match match2 in regex.Matches(match.Groups[1].Value))
                            {
                                list.Add(HttpUtility.HtmlDecode(match2.Groups[1].Value));
                            }
                        }
                        else
                        {
                            string[] _s = match.Groups[1].Value.Split('|');
                            foreach (string _item in _s)
                            {
                                list.Add(HttpUtility.HtmlDecode(_item.Trim().Trim('\r').Trim()));
                            }
                        }
                    }
                }
                return list;
            }
        }

        public string GetIMDbRating(string imdbId)
        {
            string _result = null;

            if (!string.IsNullOrEmpty(imdbId))
            {
                string _moviePage = Helpers.GetPage(string.Format("{0}/title/{1}/", m_CountryFactory.TargetHost, imdbId), null, m_CountryFactory.GetEncoding, "", true, false, m_CountryFactory.Language);
                if (!string.IsNullOrEmpty(_moviePage))
                {
                    _result = GetRating(_moviePage);
                }
            }

            return _result;
        }

        public MovieInfo GetMovieInfo(string imdbId, string countryCode)
        {
            MovieInfo _result = new MovieInfo();

            if (!string.IsNullOrEmpty(imdbId))
            {
                MovieInfo _originalInfo = new MovieInfo();
                if (this.Language != "com")
                {
                    _originalInfo = new IMDBMovieInfo("com").GetMovieInfo(imdbId, countryCode);
                }

                string _moviePage = Helpers.GetPage(string.Format("{0}/title/{1}/", m_CountryFactory.TargetHost, imdbId), null, m_CountryFactory.GetEncoding, "", true, false, m_CountryFactory.Language);
                if (!string.IsNullOrEmpty(_moviePage))
                {
                    _result.IMDBID = imdbId;
                    _result.Name = GetTitle(_moviePage);
                    _result.Name = string.IsNullOrEmpty(_result.Name) ? _originalInfo.Name : _result.Name;
                    _result.OriginalTitle = GetOriginalTitle(_moviePage);
                    _result.OriginalTitle = string.IsNullOrEmpty(_result.OriginalTitle) ? (string.IsNullOrEmpty(_originalInfo.OriginalTitle) ? _result.Name : _originalInfo.OriginalTitle) : _result.OriginalTitle;
                    _result.Year = GetYear(_moviePage);
                    _result.Year = string.IsNullOrEmpty(_result.Year) ? _originalInfo.Year : _result.Year;
                    _result.Runtime = GetRunTime(_moviePage);
                    _result.Runtime = string.IsNullOrEmpty(_result.Runtime) ? _originalInfo.Runtime : _result.Runtime;

                    DateTimeFormatInfo _dtfi = new DateTimeFormatInfo() { DateSeparator = " ", ShortDatePattern = "dd MMMM yyyy" };
                    if (string.IsNullOrEmpty(_originalInfo.ReleaseDate))
                    {
                        _result.SetReleaseDate(Helpers.GetFormattedDate(GetReleaseDate(imdbId), _dtfi));
                    }
                    else
                    {
                        _result.ReleaseDate = _originalInfo.ReleaseDate;
                    }
                    DateTime _out = DateTime.MinValue;
                    if (DateTime.TryParse(_result.ReleaseDate, out _out) && _out != DateTime.MinValue)
                    {
                        _result.ReleaseDate = Helpers.GetFormattedDate(_out);
                    }
                    _result.Rating = GetRating(_moviePage);
                    _result.Rating = string.IsNullOrEmpty(_result.Rating) ? _originalInfo.Rating : _result.Rating;
                    _result.MPAA = GetMPAA(_moviePage);
                    _result.MPAA = string.IsNullOrEmpty(_result.MPAA) ? _originalInfo.MPAA : _result.MPAA;
                    _result.Tagline = GetTagline(_moviePage);
                    _result.Tagline = string.IsNullOrEmpty(_result.Tagline) ? _originalInfo.Tagline : _result.Tagline;
                    _result.Metascore = GetMetascore(_moviePage);
                    _result.Metascore = string.IsNullOrEmpty(_result.Metascore) ? _originalInfo.Metascore : _result.Metascore;
                    _result.Trailer = GetTrailerLink(_moviePage);
                    _result.Trailer = string.IsNullOrEmpty(_result.Trailer) ? _originalInfo.Trailer : _result.Trailer;
                    if (!string.IsNullOrEmpty(_result.Trailer) && !_result.Trailer.StartsWith("http"))
                    {
                        _result.Trailer = "http://www.imdb.com" + _result.Trailer;
                    }
                    _result.Overview = GetOverview(imdbId);
                    if (string.IsNullOrEmpty(_result.Overview))
                    {
                        _result.Overview = GetPlot(_moviePage);
                    }
                    _result.Overview = string.IsNullOrEmpty(_result.Overview) ? _originalInfo.Overview : _result.Overview;
                    _result.Genre = GetGenres(_moviePage);
                    _result.Genre = _result.Genre.Count == 0 ? _originalInfo.Genre : _result.Genre;
                    _result.Cast = GetActors(_moviePage).ToTrimmedList();
                    _result.Cast = _result.Cast.Count == 0 ? _originalInfo.Cast : _result.Cast;
                    _result.Director = GetDirectors(_moviePage);
                    _result.Director = _result.Director.Count == 0 ? _originalInfo.Director : _result.Director;
                    _result.Countries = GetCountries(_moviePage);
                    _result.Countries = _result.Countries.Count == 0 ? _originalInfo.Countries : _result.Countries;
                    _result.Certification = GetCertification(countryCode, _moviePage, imdbId);
                    _result.Certification = string.IsNullOrEmpty(_result.Certification) ? _originalInfo.Certification : _result.Certification;
                    _result.Studios = GetCompanies(_moviePage);
                    _result.Studios = _result.Studios.Count == 0 ? _originalInfo.Studios : _result.Studios;

                }
            }

            return _result;
        }

        private IEnumerable<MovieInfo> AnalyzeSearchResults(string page)
        {
            var matches = new Regex(ResultsRegexPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase).Matches(page);
            return matches.Cast<Match>().Select(ProcessItem).Where(item => item != null).ToList();
        }

        private MovieInfo ProcessItem(Match match)
        {
            if (match != null)
            {
                var title = match.Groups["Title"].Value;
                if (title.Contains("(VG)")) return null;

                var item = new MovieInfo
                    {
                        IMDBID = match.Groups["Id"].Value,
                        Name = HttpUtility.HtmlDecode(title)
                    };
                if (string.IsNullOrEmpty(item.Name))
                {
                    return null;
                }
                int year;
                if (Int32.TryParse(match.Groups["Year"].Value, out year))
                {
                    item.Year = year.ToString(CultureInfo.InvariantCulture);
                }
                return item;
            }
            return null;
        }

        private List<MovieInfo> LimitResults(List<MovieInfo> candidates, int maxCount)
        {
            if (candidates.Count > maxCount)
            {
                candidates.RemoveRange(maxCount, candidates.Count - maxCount);
            }
            return candidates;
        }

        public List<MovieInfo> GetMovies(string keywords, string year, int maxCount)
        {
            Loggy.Logger.Debug("Prepare query: {0} [{1}]", keywords, maxCount);

            List<MovieInfo> _result = new List<MovieInfo>();

            if (!string.IsNullOrEmpty(keywords))
            {
                if (!string.IsNullOrEmpty(year))
                {
                    keywords = string.Format("{0} ({1})", keywords, year);
                }
                if (keywords.Length > 1)
                {
                    // capitalize letters for better matching in IMDb (weird but it is really better)
                    TextInfo _UsaTextInfo = CultureInfo.GetCultureInfo("en-US").TextInfo;
                    keywords = _UsaTextInfo.ToTitleCase(keywords);
                }

                string _query = FileManager.Configuration.Options.IMDBOptions.UseFeelingLuckyMode && FileManager.Configuration.Options.IMDBOptions.Country == "com" ?
                      string.Format("http://www.google.com/search?hl=en&btnI=I%27m+Feeling+Lucky&q={0}", HttpUtility.UrlEncode(Encoding.Default.GetBytes(keywords)))
                    : string.Format("{0}/find?s=tt&q={1}&x=0&y=0&exact=true", m_CountryFactory.TargetHost, HttpUtility.UrlEncode(Encoding.Default.GetBytes(keywords)));

                string page = Helpers.GetPage(_query, null, m_CountryFactory.GetEncoding, "", true, false, m_CountryFactory.Language);
                //if (!string.IsNullOrEmpty(page) && (((page.IndexOf(m_CountryFactory.Popular) > 0) || (page.IndexOf(m_CountryFactory.Exact) > 0)) || (page.IndexOf(m_CountryFactory.Approx) > 0)))
                if (!string.IsNullOrEmpty(page) && Regex.IsMatch(page, ResultsRegexPattern))
                {
                    _result.AddRange(AnalyzeSearchResults(page));
                    return LimitResults(_result, maxCount);
                }
                if (!string.IsNullOrEmpty(page))
                {
                    Regex regex = new Regex("/title/(tt\\d{7})/fullcredits\"", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    Match match = regex.Match(page);
                    if (!match.Success)
                    {
                        return LimitResults(_result, maxCount);
                    }

                    MovieInfo _item = new MovieInfo();
                    _item.IMDBID = match.Groups[1].Value.Trim();
                    _item.Name = GetTitle(page);
                    // get the year too
                    Match _m = Regex.Match(page, "<a href=\"/year/([0-9]*)/", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    if (_m.Success)
                    {
                        _item.Year = _m.Groups[1].Value.Trim();
                    }
                    if (!string.IsNullOrEmpty(_item.Name) && !string.IsNullOrEmpty(_item.Name))
                    {
                        _result.Add(_item);
                    }
                }
            }

            return LimitResults(_result, maxCount);
        }

        public IMDBMovieInfo()
            : this(FileManager.Configuration.Options.IMDBOptions.Country)
        {

        }

        public IMDBMovieInfo(string langCode)
        {
            this.Language = langCode;
            m_CountryFactory = new IMDBCountryFactory(this.Language);
        }
    }
}
