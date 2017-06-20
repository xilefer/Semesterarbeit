using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace HomeMediaApp.Interfaces
{
    //Hier befinden sich die Interfaces zur plattformspezifischen Implementierung von Funktionen

    public interface IGetDeviceIPAddress
    {
        /// <summary>
        /// Gibt die aktuelle Geräte-IP-Adresse zurück
        /// </summary>
        /// <returns>Geräte IP-Adresse</returns>
        string GetDeviceIP();
    }
    public interface IMediaPlayer
    {
        /// <summary>
        /// Ermöglicht die Wiedergabe einer Datei aus einer Netzwerkquelle
        /// </summary>
        /// <param name="FileUri">Netzwerkpfad</param>
        /// <returns>True: Pfad erfolgreich gesetzt</returns>
        bool PlayFromUri(Uri FileUri);
        /// <summary>
        /// Ermöglicht die Wiedergabe einer Datei auf dem lokalen System
        /// </summary>
        /// <param name="FilePath">Lokaler Dateipfad</param>
        /// <returns>True: Pfad erfolgreich gesetzt</returns>
        bool PlayFromFile(string FilePath);
        /// <summary>
        /// Pausiert die Wiedergabe
        /// </summary>
        void Pause();
        /// <summary>
        /// Startet die Wiedergabe
        /// </summary>
        void Play();
        /// <summary>
        /// Setzt die aktuelle Wiedergabeposition auf Position-Wert in sec.
        /// </summary>
        /// <param name="Position">Wiedergabeposition</param>
        void SeekTo(int Position);
        /// <summary>
        /// Gibt den aktuellen Wiedergabestatus zurück
        /// </summary>
        /// <returns>Wiedergabestatus</returns>
        PlayingState GetPlayingState();
        /// <summary>
        /// Setzt den Name in der Wiedergabeanzeige
        /// </summary>
        /// <param name="ItemName">Wiedergabename</param>
        void SetName(string ItemName);
    }

    /// <summary>
    /// Klasse zum speichern des Wiedergabestatus
    /// </summary>
    public class PlayingState
    {
        public int Max;
        public int Current;
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
