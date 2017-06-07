using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global

namespace HomeMediaApp.Interfaces
{
    public interface IGetDeviceIPAddress
    {
        string GetDeviceIP();
    }
    public interface IMediaPlayer
    {
        bool PlayFromUri(Uri FileUri);
        bool PlayFromFile(string FilePath);
        void Pause();
        void Play();
        /// <summary>
        /// Setzt die aktuelle Wiedergabeposition auf Position-Wert in sec.
        /// </summary>
        /// <param name="Position"></param>
        void SeekTo(int Position);
        PlayingState GetPlayingState();
    }

    public class PlayingState
    {
        public int Max;
        public int Current;
    }

    public interface IMediaPlayerControl : IMediaPlayer
    {
    }

    public interface ICloseApplication
    {
        void Close();
    }

    public interface IPhotoViewer
    {
        void ShowPhotoFromUri(Uri FileURI);
        void ShowPhoto(string FilePath);
    }

    public interface IVideoViewer
    {
        void ShowVideoFromUri(Uri FileUri);
        void ShowVideoFromPath(string FilePath);
        void Play();
        void Pause();
        PlayingState GetPlayingState();
        void SeekTo(int Position);
    }

    public interface IGetFileImageSource
    {
        FileImageSource GetPlaySource();
        FileImageSource GetPauseSource();
    }
}
