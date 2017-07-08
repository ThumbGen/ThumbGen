using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThumbGen.MovieSheets;
using System.IO;
using System.Windows.Input;

namespace ThumbGen
{
    public class ImagesProcessor
    {
        public string MoviePath { get; set; }

        public string CoverPath { get; set; }
        public string DefaultCoverPath { get; set; }

        public IEnumerable<BackdropBase> Backdrops { get; set; }
        public IEnumerable<BackdropBase> OwnBackdrops { get; set; }

        public MovieSheetsGenerator MainGenerator { get; set; }
        public MovieSheetsGenerator ExtraGenerator { get; set; }
        public MovieSheetsGenerator SpareGenerator { get; set; }
        public MoviesheetsUpdateManager MetadataManager { get; set; }
        public MoviesheetsUpdateManager SpareMetadataManager { get; set; }

        public bool IsMyOwnThumbnailFromDiskImageRequired { get; private set; }

        private List<BackdropBase> m_TakenBackdrops = new List<BackdropBase>();

        public ImagesProcessor(string moviePath)
        {
            MoviePath = moviePath;
            IsMyOwnThumbnailFromDiskImageRequired = false;
            Backdrops = new List<BackdropBase>();
            OwnBackdrops = new List<BackdropBase>();

            string _metadataFilename = FileManager.Configuration.GetMoviesheetMetadataPath(MoviePath, false);
            if (!string.IsNullOrEmpty(_metadataFilename) && File.Exists(_metadataFilename))
            {
                MetadataManager = MoviesheetsUpdateManager.CreateManagerForMovie(MoviePath);
            }
            string _spareMetadataFilename = FileManager.Configuration.GetParentFolderMetadataPath(MoviePath, false);
            if (!string.IsNullOrEmpty(_spareMetadataFilename) && File.Exists(_spareMetadataFilename))
            {
                SpareMetadataManager = MoviesheetsUpdateManager.CreateManagerForParentFolder(MoviePath);
            }
        }

        private void AddBackdropToLists(BackdropItem item, bool asFirst)
        {
            if (item != null)
            {
                (Backdrops as List<BackdropBase>).Insert(asFirst ? 0 : Math.Max(0, Backdrops.Count() - 1), item);
                (OwnBackdrops as List<BackdropBase>).Insert(asFirst ? 0 : Math.Max(0, OwnBackdrops.Count() - 1), item);
            }
        }

        private bool AutoloadBackdrop(MovieSheetsGenerator generator, bool loadFromMetadata, MoviesheetsUpdateManager metadatamanager, bool doSelect, 
                                            string mask, MoviesheetImageType imgType)
        {
            bool _result = false;

            // check if must populate from masked existing file
            if (doSelect && !string.IsNullOrEmpty(mask))
            {
                string _jpgPath = null;
                if (ConfigHelpers.CheckIfFileExists(MoviePath, mask, out _jpgPath))
                {
                    generator.UpdateBackdrop(imgType, _jpgPath);
                    // add it to the pool too
                    BackdropItem _item = PrepareBackdropItem(_jpgPath, false);
                    AddBackdropToLists(_item, true);
                    _result = true;
                }
            }

            // check if must populate from metadata
            if (!_result && loadFromMetadata && metadatamanager != null)
            {
                string _itemType = null;
                string _path = null;
                switch (imgType)
                {
                    case MoviesheetImageType.Background:
                        _itemType = MoviesheetsUpdateManager.BACKGROUND_STREAM_NAME;
                        _path = generator.BackdropTempPath;
                        break;
                    case MoviesheetImageType.Fanart1:
                        _itemType = MoviesheetsUpdateManager.FANART1_STREAM_NAME;
                        _path = generator.Fanart1TempPath;
                        break;
                    case MoviesheetImageType.Fanart2:
                        _itemType = MoviesheetsUpdateManager.FANART2_STREAM_NAME;
                        _path = generator.Fanart2TempPath;
                        break;
                    case MoviesheetImageType.Fanart3:
                        _itemType = MoviesheetsUpdateManager.FANART3_STREAM_NAME;
                        _path = generator.Fanart3TempPath;
                        break;
                }

                if (!string.IsNullOrEmpty(_itemType))
                {
                    _result = metadatamanager.GetImage(_itemType, _path);
                    if (_result && File.Exists(_path))
                    {
                        generator.UpdateBackdrop(imgType, _path);
                        // add it to the pool too
                        BackdropItem _item = PrepareBackdropItem(_path, false);
                        AddBackdropToLists(_item, true);
                    }
                }

            }

            return _result;
        }

        private BackdropItem PrepareBackdropItem(string filePath, bool isScreenshot)
        {
            // create the item
            BackdropItem _result = new BackdropItem(null, null, string.Empty, filePath, filePath);
            // mark it as screenshot
            _result.IsScreenshot = isScreenshot;
            // detect imagesize
            System.Drawing.Size _size = Helpers.GetImageSize(filePath);
            if (_size.Height != 0 && _size.Width != 0)
            {
                _result.Width = _size.Width.ToString();
                _result.Height = _size.Height.ToString();
            }
            return _result;
        }

        public BackdropItem GetRandomBackdrop()
        {
            BackdropItem _result = null;

            if (FileManager.Configuration.Options.IsMTNPathSpecified)
            {
                string _filePath = Helpers.GetUniqueFilename(".jpg");
                try
                {
                    if (VideoScreenShot.MakeBackdropSnapshot(MoviePath, _filePath))
                    {
                        if (File.Exists(_filePath))
                        {
                            _result = PrepareBackdropItem(_filePath, true);
                        }
                    }
                }
                finally
                {
                    FileManager.AddToGarbageFiles(_filePath);
                }
            }

            return _result;
        }

        private void AddRandomBackdrop()
        {
            BackdropItem _item = GetRandomBackdrop();
            AddBackdropToLists(_item, false);
        }

        private string ChooseARandomBackdrop(bool allowWideBanners)
        {
            if (Backdrops != null && Backdrops.Count() > 0)
            {
                IEnumerable<BackdropBase> _tempPool = null;
                // if I have taken backdrops then remove them from the temporary pool
                if (m_TakenBackdrops != null && m_TakenBackdrops.Count() < Backdrops.Count())
                {
                    _tempPool = from c in Backdrops
                                from d in m_TakenBackdrops
                                where c.OriginalUrl != d.OriginalUrl
                                select c;
                }
                if(_tempPool == null || _tempPool.Count() == 0)
                {
                    _tempPool = Backdrops;
                }

                // remove from the candidates the banners if the AllowWideBanners is false
                if (!allowWideBanners)
                {
                    _tempPool = from c in _tempPool
                                where !c.IsBanner
                                select c;
                }

                if (_tempPool != null && _tempPool.Count() != 0)
                {
                    BackdropBase _backdrop = null;
                    Random _rand = new Random();
                    do
                    {
                        int _idx = _rand.Next(0, _tempPool.Count() - 1);
                        _backdrop = _tempPool.ElementAt(_idx);
                    } while (_backdrop == null);

                    m_TakenBackdrops.Add(_backdrop); // remember it as taken ;)
                    return _backdrop.OriginalUrl;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private BackdropBase GetSpecialBackdrop(bool isScreenshot, bool isWideBanner)
        {
            BackdropBase _result = null;

            foreach (BackdropBase _b in Backdrops)
            {
                if (isScreenshot && _b.IsScreenshot)
                {
                    _result = _b;
                    break;
                }
                if (isWideBanner && _b.IsBanner)
                {
                    _result = _b;
                    break;
                }
            }

            return _result;
        }

        public void ImportCover(MovieSheetsGenerator generator, bool IsLoadFromMetadata, MoviesheetsUpdateManager metadatamanager)
        {
            if (!string.IsNullOrEmpty(this.CoverPath))
            {
                generator.UpdateCover(this.CoverPath);
                return;
            }

            bool _coverDone = false;

            //process cover
            // if import is selected and mask is not empty
            if (FileManager.Configuration.Options.MovieSheetsOptions.AutoSelectFolderjpgAsCover &&
                   !string.IsNullOrEmpty(FileManager.Configuration.Options.MovieSheetsOptions.AutoSelectFolderjpgAsCoverName))
            {
                string _coverPath = null;
                if (ConfigHelpers.CheckIfFileExists(MoviePath, FileManager.Configuration.Options.MovieSheetsOptions.AutoSelectFolderjpgAsCoverName, out _coverPath))
                {
                    CoverPath = _coverPath;
                    // signal that MyOwnThumbnailFromDiskImage should be updated
                    IsMyOwnThumbnailFromDiskImageRequired = true;
                    // update the sheets generators
                    if (generator != null)
                    {
                        generator.UpdateCover(_coverPath);
                    }
                    _coverDone = true;
                }
            }

            if (metadatamanager == null)
            {
                metadatamanager = MetadataManager;
            }

            if (!_coverDone && IsLoadFromMetadata && metadatamanager != null)
            {
                string _tmpCover = Helpers.GetUniqueFilename(".jpg");
                FileManager.AddToGarbageFiles(_tmpCover);
                _coverDone = metadatamanager.GetImage(MoviesheetsUpdateManager.COVER_STREAM_NAME, _tmpCover);
                if (generator != null)
                {
                    generator.UpdateCover(_tmpCover);
                }
                CoverPath = _tmpCover;
                IsMyOwnThumbnailFromDiskImageRequired = true;

                _coverDone = true;
            }

            if (!_coverDone && generator != null)
            {
                generator.UpdateCover(DefaultCoverPath);
            }
        }

        private void ImportFromMetadata(MoviesheetsUpdateManager metadatamanager)
        {
            // if there is a .tgmd file add images from inside to the own backdrops pool
            if (metadatamanager != null && File.Exists(metadatamanager.TargetFilename))
            {
                string _path = Helpers.GetUniqueFilename(".jpg");
                if (metadatamanager.GetImage(MoviesheetsUpdateManager.BACKGROUND_STREAM_NAME, _path))
                {
                    FileManager.AddToGarbageFiles(_path);
                    // add it to the pool too
                    BackdropItem _item = PrepareBackdropItem(_path, false);
                    AddBackdropToLists(_item, true);
                }
                _path = Helpers.GetUniqueFilename(".jpg");
                if (metadatamanager.GetImage(MoviesheetsUpdateManager.FANART1_STREAM_NAME, _path))
                {
                    FileManager.AddToGarbageFiles(_path);
                    // add it to the pool too
                    BackdropItem _item = PrepareBackdropItem(_path, false);
                    AddBackdropToLists(_item, true);
                }
                _path = Helpers.GetUniqueFilename(".jpg");
                if (metadatamanager.GetImage(MoviesheetsUpdateManager.FANART2_STREAM_NAME, _path))
                {
                    FileManager.AddToGarbageFiles(_path);
                    // add it to the pool too
                    BackdropItem _item = PrepareBackdropItem(_path, false);
                    AddBackdropToLists(_item, true);
                }
                _path = Helpers.GetUniqueFilename(".jpg");
                if (metadatamanager.GetImage(MoviesheetsUpdateManager.FANART3_STREAM_NAME, _path))
                {
                    FileManager.AddToGarbageFiles(_path);
                    // add it to the pool too
                    BackdropItem _item = PrepareBackdropItem(_path, false);
                    AddBackdropToLists(_item, true);
                }
            }
        }

        private void ImportBackground(MovieSheetsGenerator generator, MoviesheetsUpdateManager metadatamanager)
        {
            if (!FileManager.Configuration.Options.MovieSheetsOptions.DoNotAutopopulateBackdrop)
            {
                bool _backgrDone = AutoloadBackdrop(generator, FileManager.Configuration.Options.MovieSheetsOptions.AutopopulateFromMetadata,
                                                    metadatamanager,
                                                    FileManager.Configuration.Options.MovieSheetsOptions.AutoSelectFanartjpgAsBackground,
                                                    FileManager.Configuration.Options.MovieSheetsOptions.AutoSelectFanartjpgAsBackgroundName,
                                                    MoviesheetImageType.Background);
                if (!_backgrDone)
                {
                    if (Backdrops != null && Backdrops.Count() != 0)
                    {
                        //update backdrop (if no backdrop selected then choose first one)
                        if (!File.Exists(generator.BackdropTempPath))
                        {
                            string _backPath = ChooseARandomBackdrop(false);
                            if (!string.IsNullOrEmpty(_backPath))
                            {
                                generator.UpdateBackdrop(MoviesheetImageType.Background, _backPath);
                            }
                        }
                    }
                }

            }
        }

        private void ImportFanarts(MovieSheetsGenerator generator, MoviesheetsUpdateManager metadatamanager)
        {
            if (!FileManager.Configuration.Options.MovieSheetsOptions.DoNotAutopopulateFanart)
            {
                bool _f1done = AutoloadBackdrop(generator, FileManager.Configuration.Options.MovieSheetsOptions.AutopopulateFromMetadata,
                                                metadatamanager,
                                                FileManager.Configuration.Options.MovieSheetsOptions.AutoSelectFanart1jpgAsBackground,
                                                FileManager.Configuration.Options.MovieSheetsOptions.AutoSelectFanart1jpgAsBackgroundName,
                                                MoviesheetImageType.Fanart1);
                bool _f2done = AutoloadBackdrop(generator, FileManager.Configuration.Options.MovieSheetsOptions.AutopopulateFromMetadata,
                                                metadatamanager,
                                                FileManager.Configuration.Options.MovieSheetsOptions.AutoSelectFanart2jpgAsBackground,
                                                FileManager.Configuration.Options.MovieSheetsOptions.AutoSelectFanart2jpgAsBackgroundName,
                                                MoviesheetImageType.Fanart2);
                bool _f3done = AutoloadBackdrop(generator, FileManager.Configuration.Options.MovieSheetsOptions.AutopopulateFromMetadata,
                                                metadatamanager,
                                                FileManager.Configuration.Options.MovieSheetsOptions.AutoSelectFanart3jpgAsBackground,
                                                FileManager.Configuration.Options.MovieSheetsOptions.AutoSelectFanart3jpgAsBackgroundName,
                                                MoviesheetImageType.Fanart3);


                if (Backdrops != null && Backdrops.Count() != 0)
                {
                    if (!_f1done)
                    {
                        string _s = ChooseARandomBackdrop(false);
                        if (!string.IsNullOrEmpty(_s))
                        {
                            generator.UpdateBackdrop(MoviesheetImageType.Fanart1, _s);
                        }
                    }
                    if (!_f2done)
                    {
                        string _s = null;
                        if (FileManager.Configuration.Options.GetBannerAsFanart2)
                        {
                            BackdropBase _banner = GetSpecialBackdrop(false, true);
                            if (_banner != null)
                            {
                                _s = _banner.OriginalUrl;
                            }
                        }
                        if (string.IsNullOrEmpty(_s))
                        {
                            _s = ChooseARandomBackdrop(false);
                        }
                        if (!string.IsNullOrEmpty(_s))
                        {
                            generator.UpdateBackdrop(MoviesheetImageType.Fanart2, _s);
                        }

                    }
                    if (!_f3done)
                    {
                        string _s = null;
                        if (FileManager.Configuration.Options.RetrieveEpisodeScreenshots)
                        {
                            BackdropBase _screenshot = GetSpecialBackdrop(true, false);
                            if (_screenshot != null)
                            {
                                _s = _screenshot.OriginalUrl;
                            }
                        }
                        if (string.IsNullOrEmpty(_s))
                        {
                            _s = ChooseARandomBackdrop(false);
                        }
                        if (!string.IsNullOrEmpty(_s))
                        {
                            generator.UpdateBackdrop(MoviesheetImageType.Fanart3, _s);
                        }
                    }
                }
            }
        }
        
        public void ImportImages()
        {
            if (FileManager.Configuration.Options.MovieSheetsOptions.InsertInPoolFromMetadata)
            {
                Loggy.Logger.Debug("Importing from metadata");
                ImportFromMetadata(MetadataManager);
                ImportFromMetadata(SpareMetadataManager);
                Loggy.Logger.Debug("Importing from metadata done");
            }

            if (FileManager.Configuration.Options.MovieSheetsOptions.AutoTakeScreenshots)
            {
                Loggy.Logger.Debug("Preparing to take snapshots");
                AddRandomBackdrop();
                AddRandomBackdrop();
                AddRandomBackdrop();
                Loggy.Logger.Debug("Snapshots taken");
            }
            else
            {
                Loggy.Logger.Debug("AutoTakeScreenshots off");
            }

            Loggy.Logger.Debug("Importing cover");
            ImportCover(MainGenerator, FileManager.Configuration.Options.MovieSheetsOptions.AutopopulateFromMetadata, MetadataManager);
            ImportCover(SpareGenerator, FileManager.Configuration.Options.MovieSheetsOptions.AutopopulateFromMetadata, SpareMetadataManager);
            Loggy.Logger.Debug("Importing cover done");

            m_TakenBackdrops.Clear();

            Loggy.Logger.Debug("Importing background");
            ImportBackground(MainGenerator, MetadataManager);
            ImportBackground(SpareGenerator, SpareMetadataManager);
            Loggy.Logger.Debug("Importing background done");

            Loggy.Logger.Debug("Importing fanarts");
            ImportFanarts(MainGenerator, MetadataManager);
            ImportFanarts(SpareGenerator, SpareMetadataManager);
            Loggy.Logger.Debug("Importing fanarts done");
        }
        
    }
}
