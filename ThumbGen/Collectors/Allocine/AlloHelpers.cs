
namespace AlloCine
{
    internal static class AlloHelpers
    {
        internal static string ReviewTypesGetValue(ReviewTypes type)
        {
            switch (type)
            {
                case ReviewTypes.DeskPress: return "desk-press";
                case ReviewTypes.Public: return "public";
                default:
                    return "";
            }
        }

        internal static string MediaFormatsGetValue(MediaFormat format)
        {
            switch (format)
            {
                case MediaFormat.Flv: return "flv";
                case MediaFormat.Mp4Lc: return "mp4-lc";
                case MediaFormat.Mp4Hip: return "mp4-hip";
                case MediaFormat.Mp4Archive: return "mp4-archive";
                case MediaFormat.Mpeg2Theater: return "mpeg2-theater";
                case MediaFormat.Mpeg2: return "mpeg2";
                default:
                    return "";
            }
        }
    }
}
