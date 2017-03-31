using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeMediaApp.Interfaces
{
    public interface IMediaPlayer
    {
        bool PlayFromUri(Uri FileUri);
        bool PlayFromFile(string FilePath);
        void Pause();
        void Play();
        Action OnFinishedPlaying { get; set; }
    }
}
