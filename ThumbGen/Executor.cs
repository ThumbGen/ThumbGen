using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ThumbGen.MovieSheets;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;

namespace ThumbGen
{
    public class Executor
    {
        public string MoviePath { get; set; }

        public Executor(string moviePath)
        {
            MoviePath = moviePath;
        }

        public bool CreateThumbnail(string sourcePath, string targetPath)
        {
            // generate thumbnail
            return Helpers.CreateThumbnailImage(sourcePath, targetPath, FileManager.Configuration.Options.KeepAspectRatio);
        }

        public bool CreateThumbnail(string sourcePath)
        {
            string _thumbPath = Helpers.GetCorrectThumbnailPath(MoviePath, true);
            return CreateThumbnail(sourcePath, _thumbPath);
        }

        public bool CreateExtraThumbnail(string sourcePath)
        {
            string _folderJPGPath = FileManager.Configuration.GetFolderjpgPath(MoviePath, true);
            return Helpers.CreateExtraThumbnailImage(sourcePath, _folderJPGPath);
        }

        public void CreateMovieInfoFile(MediaInfoData mediainfo, MovieInfo movieinfo)
        {
            MediaInfoData _mediainfo = mediainfo == null ? MediaInfoManager.GetMediaInfoData(MoviePath) : mediainfo;
            nfoHelper.GenerateNfoFile(MoviePath, movieinfo, _mediainfo);
        }

        public static BaseCollector GetNewCollectorObject(string collectorName)
        {
            // be careful here coz due to obfuscation u can't rely on type name
            Type t = BaseCollector.GetCollectorType(collectorName);

            //Create the object, cast it and return it to the caller
            return (BaseCollector)Activator.CreateInstance(t);
        }

        public MovieInfo QueryPreferredCollector(string imdbid, string keywords)
        {
            MovieInfo _result = null;

            // find the type of the preferred collector
            BaseCollector _tmp = BaseCollector.GetMovieCollector(FileManager.Configuration.Options.PreferedInfoCollector);
            if (_tmp != null)
            {
                // create a new collector (to not affect the old one)
                BaseCollector _prefCollector = GetNewCollectorObject(FileManager.Configuration.Options.PreferedInfoCollector); //BaseCollector.GetMovieCollector(FileManager.Configuration.Options.PreferedInfoCollector);
                Loggy.Logger.Debug("Detected prefinfocollector: " + _prefCollector.CollectorName);
                //BaseCollector _prefCollector = BaseCollector.GetMovieCollectorObject(FileManager.Configuration.Options.PreferedInfoCollector);
                if (_prefCollector != null)
                {
                    _prefCollector.IMDBID = imdbid;
                    _prefCollector.CurrentMovie = new MovieItem(MoviePath);
                    _result = _prefCollector.QueryMovieInfo(_prefCollector.IMDBID);
                    if ((_result == null) || (string.IsNullOrEmpty(_result.Name)))
                    {
                        // ask user
                        _prefCollector.GetResults(keywords, _prefCollector.IMDBID, true);
                        BindingList<MovieInfo> _candidates = new BindingList<MovieInfo>();
                        foreach (ResultItemBase _rib in _prefCollector.ResultsList)
                        {
                            if (!string.IsNullOrEmpty(imdbid) && !string.IsNullOrEmpty(_rib.MovieInfo.IMDBID) && imdbid != _rib.MovieInfo.IMDBID)
                            {
                                continue; // IMDBID does not match with ours
                            }
                            if (!_candidates.Contains(_rib.MovieInfo, new MovieInfoComparer()))
                            {
                                _candidates.Add(_rib.MovieInfo);
                            }
                        }
                        ChooseMovieDialogResult _dresult = ChooseMovieFromIMDb.GetCorrectMovie(null, _candidates, "", false);
                        _result = _dresult != null ? _dresult.MovieInfo : null;
                    }
                }
            }
            return _result;
        }

        public MovieInfo QueryIMDB(string imdbid, string keywords)
        {
            MovieInfo _temp = null;
            try
            {

                if (!string.IsNullOrEmpty(imdbid))
                {
                    //Country _country = _form.SelectedCollector != null ? _form.SelectedCollector.Country : Country.International;
                    string _country = FileManager.Configuration.Options.IMDBOptions.CertificationCountry;
                    string _cacheKey = imdbid + _country;

                    if (ResultsListBox.IMDbMovieInfoCache.ContainsKey(_cacheKey))
                    {
                        IMDBMovieInfoCacheItem _cacheItem = ResultsListBox.IMDbMovieInfoCache[_cacheKey];
                        if (_cacheItem != null && string.Compare(_cacheItem.CountryCode, _country, true) == 0)
                        {
                            _temp = _cacheItem.MovieInfo;
                        }
                    }
                    if (_temp == null)
                    {
                        _temp = new IMDBMovieInfo().GetMovieInfo(imdbid, _country);
                        ResultsListBox.IMDbMovieInfoCache.Add(_cacheKey, new IMDBMovieInfoCacheItem(_country, _temp));
                    }
                }
            }
            catch { }

            return _temp;
        }

        private bool ExportImage(bool doSelect, string mask, string sourcePath, Size targetSize, bool isMaxQuality, double maxfilesize, bool overwriteExisting, bool keepAspectRatio)
        {
            bool _result = false;

            // check if must populate from masked existing file
            if (doSelect && !string.IsNullOrEmpty(mask) && !string.IsNullOrEmpty(sourcePath))
            {
                string _jpgPath = null;
                if (ConfigHelpers.CheckIfFileNeedsCreation(MoviePath, mask, out _jpgPath))
                {
                    if (overwriteExisting || !File.Exists(_jpgPath))
                    {
                        double _maxFileSize = isMaxQuality ? double.PositiveInfinity : maxfilesize;
                        _result = Helpers.CreateThumbnailImage(sourcePath, _jpgPath, keepAspectRatio, false, targetSize, true, _maxFileSize);
                    }
                }
            }

            return _result;
        }

        public bool ExportCover(string sourcePath)
        {
            ThumbGen.UserOptions.ExportImages _exportOptions = FileManager.Configuration.Options.ExportImagesOptions;
            Size _size = new Size(0, 0);
            if (_exportOptions.IsResizingCover)
            {
                _size.Width = _exportOptions.CoverSizeWidth;
                _size.Height = _exportOptions.CoverSizeHeight;
            }
            return this.ExportImage(_exportOptions.AutoExportFolderjpgAsCover, _exportOptions.AutoExportFolderjpgAsCoverName + _exportOptions.CoverExtension,
                                    sourcePath, _size,
                                    _exportOptions.IsMaxQuality, _exportOptions.MaxFilesize, _exportOptions.OverwriteExistingCover, _exportOptions.PreserveAspectRatioCover);
        }

        public bool ExportBackdrop(string sourcePath, MoviesheetImageType imgType)
        {
            bool _doIt = false;
            string _name = null;
            Size _size = new Size(0, 0);
            bool _isMaxQuality = true;
            double _maxFilesize = double.PositiveInfinity;
            bool _overwriteExisting = true;
            bool _preserveAR = true;

            ThumbGen.UserOptions.ExportImages _exportOptions = FileManager.Configuration.Options.ExportImagesOptions;
            switch (imgType)
            {
                case MoviesheetImageType.Background:
                    _doIt = _exportOptions.AutoExportFanartjpgAsBackground;
                    _name = _exportOptions.AutoExportFanartjpgAsBackgroundName + _exportOptions.BackgroundExtension;
                    if (_exportOptions.IsResizingBackground)
                    {
                        _size.Width = _exportOptions.BackgroundSizeWidth;
                        _size.Height = _exportOptions.BackgroundSizeHeight;
                    }
                    _isMaxQuality = _exportOptions.IsMaxQualityBackground;
                    _maxFilesize = _exportOptions.MaxFilesizeBackground;
                    _overwriteExisting = _exportOptions.OverwriteExistingBackground;
                    _preserveAR = _exportOptions.PreserveAspectRatioBackground;
                    break;
                case MoviesheetImageType.Fanart1:
                    _doIt = _exportOptions.AutoExportFanart1jpgAsBackground;
                    _name = _exportOptions.AutoExportFanart1jpgAsBackgroundName + _exportOptions.Fanart1Extension;
                    if (_exportOptions.IsResizingFanart1)
                    {
                        _size.Width = _exportOptions.Fanart1SizeWidth;
                        _size.Height = _exportOptions.Fanart1SizeHeight;
                    }
                    _isMaxQuality = _exportOptions.IsMaxQualityFanart1;
                    _maxFilesize = _exportOptions.MaxFilesizeFanart1;
                    _overwriteExisting = _exportOptions.OverwriteExistingFanart1;
                    _preserveAR = _exportOptions.PreserveAspectRatioFanart1;
                    break;
                case MoviesheetImageType.Fanart2:
                    _doIt = _exportOptions.AutoExportFanart2jpgAsBackground;
                    _name = _exportOptions.AutoExportFanart2jpgAsBackgroundName + _exportOptions.Fanart2Extension;
                    if (_exportOptions.IsResizingFanart2)
                    {
                        _size.Width = _exportOptions.Fanart2SizeWidth;
                        _size.Height = _exportOptions.Fanart2SizeHeight;
                    }
                    _isMaxQuality = _exportOptions.IsMaxQualityFanart2;
                    _maxFilesize = _exportOptions.MaxFilesizeFanart2;
                    _overwriteExisting = _exportOptions.OverwriteExistingFanart2;
                    _preserveAR = _exportOptions.PreserveAspectRatioFanart2;
                    break;
                case MoviesheetImageType.Fanart3:
                    _doIt = _exportOptions.AutoExportFanart3jpgAsBackground;
                    _name = _exportOptions.AutoExportFanart3jpgAsBackgroundName + _exportOptions.Fanart3Extension;
                    if (_exportOptions.IsResizingFanart3)
                    {
                        _size.Width = _exportOptions.Fanart3SizeWidth;
                        _size.Height = _exportOptions.Fanart3SizeHeight;
                    }
                    _isMaxQuality = _exportOptions.IsMaxQualityFanart3;
                    _maxFilesize = _exportOptions.MaxFilesizeFanart3;
                    _overwriteExisting = _exportOptions.OverwriteExistingFanart3;
                    _preserveAR = _exportOptions.PreserveAspectRatioFanart3;
                    break;
            }

            return this.ExportImage(_doIt, _name, sourcePath, _size, _isMaxQuality, _maxFilesize, _overwriteExisting, _preserveAR);
        }
    }

}
