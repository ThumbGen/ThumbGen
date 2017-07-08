using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;
using System.IO;
using System.ComponentModel;
using System.IO.Compression;
using System.Windows;

namespace ThumbGen.Subtitles
{
    internal class SubtitlesManager : IDisposable
    {
        private string m_Token;
        private IOSDb m_osdbProxy;

        public static List<string> SubtitlesSupported = new List<string>()
            {
                "*.srt", "*.ass", "*.ssa", "*.sub", "*.smi", "*.idx"
            };

        public static string GetImdbId(string movieFilename)
        {
            string _result = null;
            if (FileManager.DisableOpenSubtitles)
            {
                return null;
            }
            using (SubtitlesManager _subMan = new SubtitlesManager())
            {
                _result = _subMan.GetImdbIdByMovieHash(movieFilename);
            }
            return _result;
        }

        public static string FixImdbId(string imdbId)
        {
            return string.IsNullOrEmpty(imdbId) ? null : imdbId.Replace("t", string.Empty);
        }

        public static MovieInfo GetIMDbData(string movieFilename, string imdbId)
        {
            MovieInfo _result = new MovieInfo();

            if (FileManager.DisableOpenSubtitles)
            {
                return null;
            }

            imdbId = FixImdbId(imdbId);
            using (SubtitlesManager _subMan = new SubtitlesManager())
            {
                string _imdbId = string.IsNullOrEmpty(imdbId) ? _subMan.GetImdbIdByMovieHash(movieFilename) : imdbId;
                if (!string.IsNullOrEmpty(_imdbId))
                {
                    imdbdata _data = _subMan.GetImdbData(_imdbId);
                    if (_data != null)
                    {
                        if (_data.cast != null && _data.cast.Count > 0)
                        {
                            _result.Cast = _data.cast.Values.Cast<string>().ToList<string>();
                        }
                        if (_data.directors != null && _data.directors.Count > 0)
                        {
                            _result.Director = _data.directors.Values.Cast<string>().ToList<string>();
                        }
                        if (_data.genres != null && _data.genres.Length > 0)
                        {
                            _result.Genre = _data.genres.ToList<string>();
                        }
                        if (_data.country != null && _data.country.Length > 0)
                        {
                            _result.Countries = _data.country.ToList<string>();
                        }
                        if (!string.IsNullOrEmpty(_data.id))
                        {
                            _result.IMDBID = "tt" + _data.id.PadLeft(7, '0');
                        }
                        _result.Name = _data.title;

                        _result.Overview = _data.plot.Replace("full summary | add synopsis", "").Trim();
                        _result.Rating = _data.rating;
                        _result.ReleaseDate = _data.year;
                        _result.Runtime = _data.duration;
                        _result.Year = _data.year;
                    }
                }
            }
            return _result;
        }

        public bool Ready
        {
            get
            {
                return m_osdbProxy != null && m_Token != null;
            }
        }

        public SubtitlesManager()
        {
            try
            {
                if (!FileManager.DisableOpenSubtitles)
                {
                    m_osdbProxy = XmlRpcProxyGen.Create<IOSDb>();
                    m_osdbProxy.Url = "http://api.opensubtitles.org/xml-rpc";
                    m_osdbProxy.KeepAlive = false;
                    m_osdbProxy.Timeout = 30000;

                    m_Token = Login();
                }
            }
            catch { }
        }

        private string Login()
        {
            if (!FileManager.DisableOpenSubtitles)
            {
                return m_osdbProxy.LogIn("", "", "en", "ThumbGen")["token"].ToString();
            }
            else
            {
                return null;
            }
        }

        public static string GetMovieHash(string movieFilename)
        {
            string _result = string.Empty;
            if (!string.IsNullOrEmpty(movieFilename) && File.Exists(movieFilename))
            {
                _result = Helpers.ToHexadecimal(Helpers.ComputeMovieHash(movieFilename));
            }
            return _result;
        }

        public imdbdata GetImdbData(string imdbId)
        {
            imdbdata _result = null;
            if (FileManager.DisableOpenSubtitles)
            {
                return null;
            }

            imdbId = FixImdbId(imdbId);

            if (!string.IsNullOrEmpty(imdbId))
            {
                imdbheader _header = null;
                try
                {
                    _header = m_osdbProxy.GetIMDBMovieDetails(m_Token, imdbId);
                }
                catch { }
                if (_header != null)
                {
                    _result = _header.data;
                }
            }

            return _result;
        }

        public string GetImdbIdByMovieHash(string movieFilename)
        {
            string _result = null;

            if (FileManager.DisableOpenSubtitles)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(movieFilename) && File.Exists(movieFilename) && m_osdbProxy != null && m_Token != null)
            {
                subInfo[] subs = new subInfo[1];
                subs[0] = new subInfo();
                subs[0].imdbid = string.Empty;
                subs[0].sublanguageid = "";
                subs[0].moviehash = GetMovieHash(movieFilename);
                subs[0].moviebytesize = new FileInfo(movieFilename).Length.ToString();
                subs[0].query = string.Empty;

                subrt subrt = null;
                try
                {
                    subrt = m_osdbProxy.SearchSubtitles(m_Token, subs);
                }
                catch { }
                if (subrt != null && subrt.data.Length != 0)
                {
                    _result = "tt" + subrt.data[0].IDMovieImdb.PadLeft(7, '0');
                }
            }
            return _result;
        }

        public enum OSSearchType
        {
            MovieHashAndSize,
            IMDbId,
            Query
        }

        private subrt DoSearch(string movieFilename, string sumCD, string language, string imdbid, string query, OSSearchType searchType)
        {
            subrt _result = null;

            if (!string.IsNullOrEmpty(movieFilename) && File.Exists(movieFilename) && m_osdbProxy != null && m_Token != null)
            {
                subInfo[] subs = new subInfo[1];
                subs[0] = new subInfo();
                subs[0].sublanguageid = language;

                switch (searchType)
                {
                    case OSSearchType.MovieHashAndSize:
                        subs[0].moviehash = GetMovieHash(movieFilename);
                        subs[0].moviebytesize = new FileInfo(movieFilename).Length.ToString();
                        subs[0].imdbid = string.Empty;
                        subs[0].query = string.Empty;
                        break;
                    case OSSearchType.IMDbId:
                        subs[0].moviehash = string.Empty;
                        subs[0].moviebytesize = string.Empty;
                        subs[0].imdbid = string.IsNullOrEmpty(imdbid) ? string.Empty : imdbid;
                        subs[0].query = string.Empty;
                        break;
                    case OSSearchType.Query:
                        subs[0].moviehash = string.Empty;
                        subs[0].moviebytesize = string.Empty;
                        subs[0].imdbid = string.Empty;
                        subs[0].query = string.IsNullOrEmpty(query) ? string.Empty : query;
                        break;
                    default:
                        break;
                }

                try
                {
                    _result = m_osdbProxy.SearchSubtitles(m_Token, subs);
                }
                catch { }
            }

            return _result;
        }

        public delegate subRes NeedUserConfirmationHandler(BindingList<subRes> candidates);
        public NeedUserConfirmationHandler NeedUserConfirmation;

        public bool GetSubtitle(string movieFilename, string sumCD, string language, string imdbid, string query)
        {
            bool _result = false;

            imdbid = FixImdbId(imdbid);

            if (FileManager.DisableOpenSubtitles)
            {
                return false;
            }

            if (m_Token == null)
            {
                m_Token = Login();
            }

            if (!string.IsNullOrEmpty(movieFilename) && File.Exists(movieFilename) && m_osdbProxy != null && m_Token != null)
            {
                // search by moviehash and size
                subrt subrt = DoSearch(movieFilename, sumCD, language, imdbid, query, OSSearchType.MovieHashAndSize);
                if (subrt == null)
                {
                    // search by imdbid (if any)
                    subrt = DoSearch(movieFilename, sumCD, language, imdbid, query, OSSearchType.IMDbId);
                    if (subrt == null)
                    {
                        // search by keywords (if any)
                        subrt = DoSearch(movieFilename, sumCD, language, imdbid, query, OSSearchType.Query);
                    }
                }

                if (subrt != null && subrt.data != null && subrt.data.Length != 0)
                {
                    BindingList<subRes> _results = new BindingList<subRes>(subrt.data);
                    subRes _winner = null;
                    BindingList<subRes> _candidates = new BindingList<subRes>();
                    foreach (subRes _subRes in _results)
                    {
                        if (_subRes.SubSumCD == sumCD)
                        {
                            _candidates.Add(_subRes);
                        }
                    }
                    // if the candidates list has only one match, declare it a winner - TOO DANGEROUS
                    //if (_candidates.Count == 1)
                    //{
                    //    _winner = _candidates[0];
                    //}
                    // several candidates available => must show dialog and ask user to choose (if application wants this)
                    if (_winner == null && _candidates.Count >= 1 && NeedUserConfirmation != null)
                    {
                        // ask user 
                        _winner = NeedUserConfirmation(_candidates);
                    }
                    if (_winner != null)
                    {
                        subdata _subdata = null;
                        try
                        {
                            _subdata = m_osdbProxy.DownloadSubtitles(m_Token, new string[] { _winner.IDSubtitleFile });
                        }
                        catch { }
                        if (_subdata != null)
                        {
                            foreach (subtitle _subtitle in _subdata.data)
                            {
                                if (_winner.IDSubtitleFile == _subtitle.idsubtitlefile)
                                {
                                    string _targetName = Path.ChangeExtension(movieFilename, Path.GetExtension(_winner.SubFileName));
                                    byte[] _data = DecodeAndDecompress(_subtitle.data);
                                    // maybe check here if subtitle exists...
                                    SaveSubtitle(_targetName, true, _data);
                                    _data = null;
                                    _result = true;
                                }
                            }
                        }
                    }
                }
            }
            return _result;
        }

        private static byte[] DecodeAndDecompress(string str)
        {
            return Decompress(Decode(str));
        }

        private static byte[] Decode(string str)
        {
            return Convert.FromBase64String(str);
        }

        private static byte[] Decompress(byte[] b)
        {
            MemoryStream stream = new MemoryStream(b.Length);
            stream.Write(b, 0, b.Length);
            stream.Seek(-4L, SeekOrigin.Current);
            byte[] buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            int count = BitConverter.ToInt32(buffer, 0);
            stream.Seek(0L, SeekOrigin.Begin);
            byte[] buffer2 = new byte[count];
            new GZipStream(stream, CompressionMode.Decompress).Read(buffer2, 0, count);
            return buffer2;
        }

        private void SaveSubtitle(string subtitleFilename, bool overwrite, byte[] subtitle)
        {
            try
            {
                if (File.Exists(subtitleFilename))
                {
                    if (!overwrite)
                    {
                        return;
                    }
                    File.Delete(subtitleFilename);
                }
                FileStream output = new FileStream(subtitleFilename, FileMode.Create);
                BinaryWriter writer = new BinaryWriter(output);
                writer.Write(subtitle);
                writer.Close();
                output.Close();
            }
            catch (Exception uae)
            {
                MessageBox.Show(uae.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }







        #region IDisposable Members

        public void Dispose()
        {
            if (m_osdbProxy != null)
            {
                try
                {
                    if (m_Token != null)
                    {
                        m_osdbProxy.LogOut(m_Token);
                    }
                }
                catch { }
            }
        }

        #endregion
    }

}
