using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace HomeMediaApp.Classes
{


    public abstract class UPnPObject
    {
        [XmlAttribute]
        public string id = "";
        [XmlAttribute]
        public string parentID = "";
        public string Title = "";
        public string Class = "";
        [XmlAttribute]
        public bool restricted = false;
    }

    public class UPnPItem : UPnPObject
    {
        public string RefID = "";
    }

    public class UPnPContainer : UPnPObject
    {
        public static UPnPContainer GenerateContainer(XElement ContainerElement)
        {
            UPnPContainer ReturnContainer = new UPnPContainer();
            List<XAttribute> ContainerAttributes = ContainerElement.Attributes().ToList();
            ReturnContainer.id = ContainerAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "id").Value;
            ReturnContainer.parentID = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "parentid").Value;
            ReturnContainer.restricted = Boolean.Parse(ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "restricted").Value);
            ReturnContainer.Title = ContainerElement.Elements().Where(e => e.Name.LocalName.ToLower() == "title").ToList()[0].Value;
            ReturnContainer.Class = ContainerElement.Elements().Where(e => e.Name.LocalName.ToLower() == "class").ToList()[0].Value;
            XAttribute Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "searchable");
            if (Value != null) ReturnContainer.Searchable = Boolean.Parse(Value.Value);
            Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "childcount");
            if (Value != null) ReturnContainer.ChildCount = int.Parse(Value.Value);
            Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "createclass");
            if (Value != null) ReturnContainer.CreateClass = Value.Value;
            Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "searchclass");
            if (Value != null) ReturnContainer.SearchClass = Value.Value;

            return ReturnContainer;
        }

        public static UPnPContainer GenerateRootContainer(XDocument MetaDocument)
        {
            List<XElement> ResultNodes = MetaDocument.Descendants().Where(Node => Node.Name.LocalName.ToLower() == "result").ToList();
            if (ResultNodes.Count == 0 || ResultNodes.Count > 1)
            {
                throw new Exception("Der Vorgang kann nicht durchgeführt werden!");
            }
            XDocument Result = XDocument.Parse(ResultNodes[0].Value);
            // Handelt es sich um den äußersten Ordner?
            bool bRootContainer = false;
            List<XElement> ContainerElements = Result.Root.Descendants().Where(Node => Node.Name.LocalName.ToLower() == "container").ToList();
            if (ContainerElements.Count > 0)
            {
                List<XAttribute> ParentIDs =
                    ContainerElements[0].Attributes()
                        .Where(Attrib => Attrib.Name.LocalName.ToLower() == "parentid")
                        .ToList();
                if (ParentIDs.Count > 0)
                {
                    if (ParentIDs[0].Value == "-1")
                    {
                        bRootContainer = true;
                    }
                }
            }
            UPnPContainer ReturnContainer = new UPnPContainer();
            if (bRootContainer)
            {
                XElement ContainerElement = ContainerElements[0];
                List<XAttribute> ContainerAttributes = ContainerElement.Attributes().ToList();
                // Benötigte Parameter zuweisen
                ReturnContainer.id = ContainerAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "id").Value;
                ReturnContainer.parentID = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "parentid").Value;
                ReturnContainer.restricted = Boolean.Parse(ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "restricted").Value);
                ReturnContainer.Title = ContainerElement.Elements().Where(e => e.Name.LocalName.ToLower() == "title").ToList()[0].Value;
                ReturnContainer.Class = ContainerElement.Elements().Where(e => e.Name.LocalName.ToLower() == "class").ToList()[0].Value;
                // Optionale Parameter zuweisen
                XAttribute Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "searchable");
                if (Value != null) ReturnContainer.Searchable = Boolean.Parse(Value.Value);
                Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "childcount");
                if (Value != null) ReturnContainer.ChildCount = int.Parse(Value.Value);
                Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "createclass");
                if (Value != null) ReturnContainer.CreateClass = Value.Value;
                Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "searchclass");
                if (Value != null) ReturnContainer.SearchClass = Value.Value;
                // Keine Descendants auswerten, weil ganz aussen sind
            }
            else
            {
                    // TODO: Browse von Dateien
            }
            return ReturnContainer;
        }

        public int ChildCount = 0;
        public string CreateClass = "";
        public string SearchClass = "";
        public bool Searchable = false;
        public List<UPnPMusicTrack> MusicTracks = new List<UPnPMusicTrack>();
        public List<UPnPMovie> Movies = new List<UPnPMovie>();
        public List<UPnPPhoto> Photos = new List<UPnPPhoto>();
        public List<UPnPContainer> ChildContainers = new List<UPnPContainer>();
    }

    public class UPnPAudioItem : UPnPItem
    {
        public string Genre = "";
        public string LongDescription = "";
    }

    public class UPnPMusicTrack : UPnPAudioItem
    {
        public static UPnPMusicTrack CreateTrack(XElement MusicElement)
        {
            UPnPMusicTrack ReturnTrack = new UPnPMusicTrack();
            List<XAttribute> Attributes = MusicElement.Attributes().ToList();
            ReturnTrack.id = Attributes.Find(e => e.Name.LocalName.ToLower() == "id").Value;
            ReturnTrack.parentID = Attributes.Find(e => e.Name.LocalName.ToLower() == "parentid").Value;
            ReturnTrack.restricted = Boolean.Parse(Attributes.Find(e => e.Name.LocalName.ToLower() == "restricted").Value);
            ReturnTrack.Title = MusicElement.Elements().Where(e => e.Name.LocalName.ToLower() == "title").ToList()[0].Value;
            ReturnTrack.Class = MusicElement.Elements().Where(e => e.Name.LocalName.ToLower() == "class").ToList()[0].Value;
            // Nicht notwendige Properties
            List<XElement> Descendants = MusicElement.Descendants().ToList();
            XElement Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "artist");
            if (Value != null) ReturnTrack.Artist = Value.Value;
            Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "album");
            if (Value != null) ReturnTrack.Album = Value.Value;
            Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "originaltracknumber");
            if (Value != null) ReturnTrack.OriginalTrackNumber = Value.Value;
            Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "playlist");
            if (Value != null) ReturnTrack.PlayList = Value.Value;
            Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "storagemedium");
            if (Value != null) ReturnTrack.StorageMedium = Value.Value;
            Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "res");
            if (Value != null) ReturnTrack.Res = Value.Value;
            Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "albumarturi");
            if (Value != null) ReturnTrack.AlbumArtURI = Value.Value;
            return ReturnTrack;
        }
        public string Artist = "";
        public string Album = "";
        public string OriginalTrackNumber = "";
        public string PlayList = "";
        public string StorageMedium = "";
        public string Res = "";
        public string AlbumArtURI = "";
    }

    public class UPnPVideoItem : UPnPItem
    {
        public string Genre = "";
        public string LongDescription = "";
        public string Producer = "";
        public string Rating = "";
        public string Actor = "";
        public string Director = "";
    }

    public class UPnPMovie : UPnPVideoItem
    {
        public string StorageMedium = "";
        public string DVDRegionCode = "";
        public string ChannelName = "";
        public string ScheduledStartTime = "";
        public string ScheduledEndTime = "";
    }

    public class UPnPImageItem : UPnPItem
    {
        public string LongDescription = "";
        public string StorageMedium = "";
        public string Rating = "";
    }

    public class UPnPPhoto : UPnPImageItem
    {
        public static UPnPPhoto CreatePhoto(XElement PhotoElement)
        {
            UPnPPhoto ReturnPhoto = new UPnPPhoto();
            List<XAttribute> Attributes = PhotoElement.Attributes().ToList();
            ReturnPhoto.id = Attributes.Find(e => e.Name.LocalName.ToLower() == "id").Value;
            ReturnPhoto.parentID = Attributes.Find(e => e.Name.LocalName.ToLower() == "parentid").Value;
            ReturnPhoto.restricted = Boolean.Parse(Attributes.Find(e => e.Name.LocalName.ToLower() == "restricted").Value);
            ReturnPhoto.Title = PhotoElement.Elements().Where(e => e.Name.LocalName.ToLower() == "title").ToList()[0].Value;
            ReturnPhoto.Class = PhotoElement.Elements().Where(e => e.Name.LocalName.ToLower() == "class").ToList()[0].Value;
            List<XElement> Descendants = PhotoElement.Descendants().ToList();
            XElement value = Descendants.Find(e => e.Name.LocalName.ToLower() == "albumarturi");
            if (value != null) ReturnPhoto.AlbumArtURI = value.Value;
            return ReturnPhoto;
        }
        public string Album = "";
        public string AlbumArtURI = "";
    }

    public class UPnPStorageFolder : UPnPContainer
    {
        public string storageUsed { get; set; } = "";
    }

    public static class UPnPBrowseFlag
    {
        public static string BrowseMetadata = "BrowseMetadata";
        public static string BrowseDirectChildren = "BrowseDirectChildren";
    }

    public static class UPnPStateVariables
    {
        private static int mA_ARG_TYPE_ObjectID = 0;
        public static string A_ARG_TYPE_ObjectID
        {
            get
            {
                return mA_ARG_TYPE_ObjectID.ToString();
            }
            set
            {
                int Temp = 0;
                if (int.TryParse(value, out Temp))
                {
                    mA_ARG_TYPE_ObjectID = Temp;
                }
            }
        }

        public static string A_ARG_TYPE_BrowseFlag { get; set; } = UPnPBrowseFlag.BrowseMetadata;

        public static string A_ARG_TYPE_Filter { get; set; } = "*";

        private static int mA_ARG_TYPE_Index = 0;
        public static string A_ARG_TYPE_Index
        {
            get
            {
                return mA_ARG_TYPE_Index.ToString();
            }
            set
            {
                int nTemp = 0;
                if (int.TryParse(value, out nTemp))
                {
                    mA_ARG_TYPE_Index = nTemp;
                }
            }
        }

        private static int mA_ARG_TYPE_Count = 0;
        public static string A_ARG_TYPE_Count
        {
            get { return mA_ARG_TYPE_Count.ToString(); }
            set
            {
                int nTemp = 0;
                if (int.TryParse(value, out nTemp))
                {
                    mA_ARG_TYPE_Count = nTemp;
                }
            }
        }

        public static string A_ARG_TYPE_SortCriteria { get; set; } = "";
    }
}
