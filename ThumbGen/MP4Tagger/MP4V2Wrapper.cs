using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ThumbGen.MP4Tagger
{
    internal sealed class MP4V2Wrapper
    {
        // Methods
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4Close(IntPtr file);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern IntPtr MP4Modify([MarshalAs(UnmanagedType.VBByRefStr)] ref string filename, int verbosity, int flags);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern IntPtr MP4Read([MarshalAs(UnmanagedType.VBByRefStr)] ref string filename, int verbosity);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsRemoveArtwork(IntPtr tags, int flags);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetAlbum(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetArtist(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetCategory(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetCNID(IntPtr tags, ref int id);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetComments(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetCompilation(IntPtr tags, ref byte id);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetContentRating(IntPtr tags, ref byte id);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetCopyright(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetDescription(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetDisk(IntPtr tags, ref trackDisk id);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetEncodingTool(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetGapless(IntPtr tags, ref byte id);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetGenre(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetGrouping(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetHDVideo(IntPtr tags, ref byte id);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetKeywords(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetLongDescription(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetMediaType(IntPtr tags, ref byte id);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetName(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetPodcast(IntPtr tags, ref byte id);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetReleaseDate(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetSortArtist(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetSortName(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetSortTVShow(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetTrack(IntPtr tags, ref trackDisk id);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetTVEpisode(IntPtr tags, ref int id);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetTVEpisodeID(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetTVNetwork(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetTVSeason(IntPtr tags, ref int id);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsSetTVShow(IntPtr tags, byte[] name);
        [DllImport("libMP4V2.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void MP4TagsStore(IntPtr tags, IntPtr file);
        [DllImport("MP4V2Wrapper.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern string VBMP4GetChapterData(IntPtr file);
        [DllImport("MP4V2Wrapper.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void VBMP4GetChapterInfo(IntPtr file, ref VBChapterInfo ci);
        [DllImport("MP4V2Wrapper.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int VBMP4GetCoverArt(byte[] buff);
        [DllImport("MP4V2Wrapper.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern bool VBMP4SetChapterData(IntPtr file, string[] ci, long[] dur, int numChaps);
        [DllImport("MP4V2Wrapper.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void VBMP4SetCoverArt(byte[] buff, int len);
        [DllImport("MP4V2Wrapper.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void VBMP4SetKind(byte[] buff, IntPtr file);
        [DllImport("MP4V2Wrapper.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void VBMP4SetMOVI(byte[] buff, IntPtr file);
        [DllImport("MP4V2Wrapper.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void VBMP4SetRating(byte[] buff, IntPtr file);
        [DllImport("MP4V2Wrapper.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern IntPtr VBMP4TagsAlloc();
        [DllImport("MP4V2Wrapper.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void VBMP4TagsFetch(ref VBMP4Tags vb, IntPtr file);
        [DllImport("MP4V2Wrapper.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern void VBMP4TagsFree(ref VBMP4Tags vb);
    }

    // Nested Types
    [StructLayout(LayoutKind.Sequential)]
    internal struct VBChapterData : IComparable
    {
        internal int number;
        internal string title;
        internal TimeSpan duration;
        public int CompareTo(object x)
        {
            //Global.chapterData data = (Global.chapterData)x;
            //if (this.number > data.number)
            //{
            //    return 1;
            //}
            //if (this.number < data.number)
            //{
            //    return -1;
            //}
            return 0;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VBChapterInfo
    {
        public int numSamples;
        public int timeScale;
        public long duration;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VBChapters
    {
        public int numSamples;
        public int timeScale;
        public long movieDuration;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VBMP4Tags
    {
        public string name;
        public string artist;
        public string grouping;
        public string comment;
        public string genre;
        public string releaseDate;
        public string kind;
        public trackDisk track;
        public trackDisk disk;
        public byte compilation;
        public byte advisory;
        public string tvShow;
        public string tvNetwork;
        public string tvEpisodeID;
        public int tvSeason;
        public int tvEpisode;
        public string description;
        public string longDescription;
        public string contentRating;
        public string MOVI;
        public string sortName;
        public string sortArtist;
        public string sortTVShow;
        public string copyright;
        public string encodingTool;
        public byte podcast;
        public string keywords;
        public string category;
        public byte gapless;
        public byte HD;
        public int CNID;
    }

    [StructLayout(LayoutKind.Sequential)/*, OptionText*/]
    public struct trackDisk
    {
        public short index;
        public short total;
    }


}
