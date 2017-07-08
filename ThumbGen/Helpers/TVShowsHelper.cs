using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThumbGen
{
    internal static class TVShowsHelper
    {
        public static string GetCurrentSeriesRootFolder(string currentMoviePath)
        {
            // 2 = parentfolder, 3= folder of the parentfolder
            int _level = FileManager.Configuration.Options.TVShowsFiltersOptions.UseEachEpisodeInOwnFolder ? 3 : 2;
            //return Helpers.GetMovieParentFolderName(currentMoviePath, "");
            return Helpers.GetMovieFolderNameByLevel(currentMoviePath, "", _level);
        }

        public static bool IsSameSeriesBeingProcessed(string currentMoviePath, EpisodeData episodeData)
        {
            // do the trick only if you have detected current season and episode
            if (!string.IsNullOrEmpty(episodeData.Episode)/* && !string.IsNullOrEmpty(m_EpisodeData.Season)*/)
            {
                // check if maybe we are processing a known series
                // decide if it is a new series or the currently processed one
                if (string.Compare(CurrentSeriesHelper.SeriesRootFolder, TVShowsHelper.GetCurrentSeriesRootFolder(currentMoviePath), true) == 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
