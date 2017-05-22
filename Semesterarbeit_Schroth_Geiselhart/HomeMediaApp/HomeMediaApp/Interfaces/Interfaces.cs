using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        Action OnFinishedPlaying { get; set; }
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
        void ShwoVideoFromPath(string FilePath);
        void Play();

    }
}
