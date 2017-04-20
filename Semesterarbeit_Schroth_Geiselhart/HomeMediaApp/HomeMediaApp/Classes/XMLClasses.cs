using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xamarin.Forms.Xaml;

namespace HomeMediaApp.Classes
{


    public abstract class UPnPObject
    {
        public string id = "";
        public string parentID = "";
        public string Title = "";
        public string Class = "";
        public bool restricted = false;

        public T Create<T>(XElement Element, T ReturnElement) where T : UPnPObject
        {
            List<XAttribute> ObjectAttributes = Element.Attributes().ToList();
            ReturnElement.id = ObjectAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "id").Value;
            ReturnElement.parentID = ObjectAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "parentid").Value;
            ReturnElement.restricted = Boolean.Parse(ObjectAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "restricted").Value);
            List<XElement> Descendants = Element.Descendants().ToList();
            XElement DescElement = Descendants.Find(Desc => Desc.Name.LocalName.ToLower() =="title");
            ReturnElement.Title = DescElement.Value;
            DescElement = Descendants.Find(Desc => Desc.Name.LocalName.ToLower() == "class");
            ReturnElement.Class = DescElement.Value;
            return ReturnElement;
        }
    }

    public class UPnPItem : UPnPObject
    {
        public string RefID = "";

        public new T Create<T>(XElement Element, T ReturnElement) where T : UPnPItem
        {
            ReturnElement = base.Create(Element, ReturnElement);
            XAttribute Val = Element.Attributes().ToList().Find(Attrib => Attrib.Name.LocalName.ToLower() == "refid");
            if (Val != null) ReturnElement.RefID = Val.Value;
            return ReturnElement;
        }
    }

    public class UPnPContainer : UPnPObject
    {
        public new T Create<T>(XElement Element, T ReturnElement) where T : UPnPContainer
        {
            ReturnElement = base.Create(Element, ReturnElement);
            List<XAttribute> ContainerAttributes = Element.Attributes().ToList();
            XAttribute Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "searchable");
            if (Value != null) ReturnElement.Searchable = Boolean.Parse(Value.Value);
            Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "childcount");
            if (Value != null) ReturnElement.ChildCount = int.Parse(Value.Value);
            Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "createclass");
            if (Value != null) ReturnElement.CreateClass = Value.Value;
            Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "searchclass");
            if (Value != null) ReturnElement.SearchClass = Value.Value;
            return ReturnElement;
        }

        //public UPnPContainer GenerateContainer(XElement ContainerElement)
        //{
        //    UPnPContainer ReturnContainer = new UPnPContainer();
        //    List<XAttribute> ContainerAttributes = ContainerElement.Attributes().ToList();
        //    ReturnContainer = base.Create(ContainerElement, ReturnContainer);
        //    XAttribute Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "searchable");
        //    if (Value != null) ReturnContainer.Searchable = Boolean.Parse(Value.Value);
        //    Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "childcount");
        //    if (Value != null) ReturnContainer.ChildCount = int.Parse(Value.Value);
        //    Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "createclass");
        //    if (Value != null) ReturnContainer.CreateClass = Value.Value;
        //    Value = ContainerAttributes.Find(e => e.Name.LocalName.ToLower() == "searchclass");
        //    if (Value != null) ReturnContainer.SearchClass = Value.Value;

        //    return ReturnContainer;
        //}

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

        new public T Create<T>(XElement Element, T ReturnElement) where T : UPnPAudioItem
        {
            ReturnElement = base.Create(Element, ReturnElement);
            List<XAttribute> ElementAttributes = Element.Attributes().ToList();
            XAttribute Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "genre");
            if (Val != null) ReturnElement.Genre = Val.Value;
            Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "longdescription");
            if (Val != null) ReturnElement.LongDescription = Val.Value;
            return ReturnElement;
        }
    }

    public class UPnPMusicTrack : UPnPAudioItem
    {
        public new T Create<T>(XElement Element, T ReturnElement) where T : UPnPMusicTrack
        {
            ReturnElement = base.Create(Element, ReturnElement);
            List<XElement> Descendants = Element.Descendants().ToList();
            XElement Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "artist");
            if (Value != null) ReturnElement.Artist = Value.Value;
            Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "album");
            if (Value != null) ReturnElement.Album = Value.Value;
            Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "originaltracknumber");
            if (Value != null) ReturnElement.OriginalTrackNumber = Value.Value;
            Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "playlist");
            if (Value != null) ReturnElement.PlayList = Value.Value;
            Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "storagemedium");
            if (Value != null) ReturnElement.StorageMedium = Value.Value;
            Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "res");
            if (Value != null) ReturnElement.Res = Value.Value;
            Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "albumarturi");
            if (Value != null) ReturnElement.AlbumArtURI = Value.Value;
            return ReturnElement;
        }

        //public UPnPMusicTrack CreateTrack(XElement MusicElement)
        //{
        //    UPnPMusicTrack ReturnTrack = new UPnPMusicTrack();
        //    List<XAttribute> Attributes = MusicElement.Attributes().ToList();
        //    ReturnTrack.id = Attributes.Find(e => e.Name.LocalName.ToLower() == "id").Value;
        //    ReturnTrack.parentID = Attributes.Find(e => e.Name.LocalName.ToLower() == "parentid").Value;
        //    ReturnTrack.restricted = Boolean.Parse(Attributes.Find(e => e.Name.LocalName.ToLower() == "restricted").Value);
        //    ReturnTrack.Title = MusicElement.Elements().Where(e => e.Name.LocalName.ToLower() == "title").ToList()[0].Value;
        //    ReturnTrack.Class = MusicElement.Elements().Where(e => e.Name.LocalName.ToLower() == "class").ToList()[0].Value;
        //    // Nicht notwendige Properties
        //    List<XElement> Descendants = MusicElement.Descendants().ToList();
        //    XElement Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "artist");
        //    if (Value != null) ReturnTrack.Artist = Value.Value;
        //    Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "album");
        //    if (Value != null) ReturnTrack.Album = Value.Value;
        //    Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "originaltracknumber");
        //    if (Value != null) ReturnTrack.OriginalTrackNumber = Value.Value;
        //    Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "playlist");
        //    if (Value != null) ReturnTrack.PlayList = Value.Value;
        //    Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "storagemedium");
        //    if (Value != null) ReturnTrack.StorageMedium = Value.Value;
        //    Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "res");
        //    if (Value != null) ReturnTrack.Res = Value.Value;
        //    Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "albumarturi");
        //    if (Value != null) ReturnTrack.AlbumArtURI = Value.Value;
        //    return ReturnTrack;
        //}

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
        public string Res = "";

        public new T Create<T>(XElement Element, T ReturnElement) where T : UPnPVideoItem
        {
            ReturnElement = base.Create(Element, ReturnElement);
            List<XAttribute> ElementAttributes = Element.Attributes().ToList();
            List<XElement> Descendants = Element.Descendants().ToList();
            XAttribute Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "genre");
            if (Val != null) ReturnElement.Genre = Val.Value;
            Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "longdescription");
            if (Val != null) ReturnElement.LongDescription = Val.Value;
            Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "producer");
            if (Val != null) ReturnElement.Producer = Val.Value;
            Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "rating");
            if (Val != null) ReturnElement.Rating = Val.Value;
            Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "actor");
            if (Val != null) ReturnElement.Actor = Val.Value;
            Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "director");
            if (Val != null) ReturnElement.Director = Val.Value;
            XElement Value = Descendants.Find(e => e.Name.LocalName.ToLower() == "res");
            if (Value != null) ReturnElement.Res = Value.Value;
            return ReturnElement;
        }
    }

    public class UPnPMovie : UPnPVideoItem
    {
        public string StorageMedium = "";
        public string DVDRegionCode = "";
        public string ChannelName = "";
        public string ScheduledStartTime = "";
        public string ScheduledEndTime = "";

        public new T Create<T>(XElement Element, T ReturnElement) where T : UPnPMovie
        {
            ReturnElement = base.Create(Element, ReturnElement);
            List<XAttribute> ElementAttributes = Element.Attributes().ToList();
            XAttribute Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "storagemedium");
            if (Val != null) ReturnElement.StorageMedium = Val.Value;
            Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "dvdregioncode");
            if (Val != null) ReturnElement.DVDRegionCode = Val.Value;
            Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "channelname");
            if (Val != null) ReturnElement.ChannelName = Val.Value;
            Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "scheduledstarttime");
            if (Val != null) ReturnElement.ScheduledStartTime = Val.Value;
            Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "scheduledendtim");
            if (Val != null) ReturnElement.ScheduledEndTime = Val.Value;
            return ReturnElement;
        }
    }

    public class UPnPImageItem : UPnPItem
    {
        public string LongDescription = "";
        public string StorageMedium = "";
        public string Rating = "";

        public new T Create<T>(XElement Element, T ReturnElement) where T : UPnPImageItem
        {
            ReturnElement = base.Create(Element, ReturnElement);
            List<XAttribute> ElementAttributes = Element.Attributes().ToList();
            XAttribute Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "longdesciption");
            if (Val != null) ReturnElement.LongDescription = Val.Value;
            Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "storagemedium");
            if (Val != null) ReturnElement.StorageMedium = Val.Value;
            Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "rating");
            if (Val != null) ReturnElement.Rating = Val.Value;
            return ReturnElement;
        }
    }

    public class UPnPPhoto : UPnPImageItem
    {
        public string Album = "";
        public string AlbumArtURI = "";
        public string Res = "";

        public new T Create<T>(XElement Element, T ReturnElement) where T : UPnPPhoto
        {
            ReturnElement = base.Create(Element, ReturnElement);
            List<XElement> Descendants = Element.Descendants().ToList();
            XElement ValTemp = Descendants.Find(Attrib => Attrib.Name.LocalName.ToLower() == "res");
            if (ValTemp != null) ReturnElement.Res = ValTemp.Value;
            ValTemp = Descendants.Find(Attrib => Attrib.Name.LocalName.ToLower() == "album");
            if (ValTemp != null) ReturnElement.Album = ValTemp.Value;
            ValTemp = Descendants.Find(Attrib => Attrib.Name.LocalName.ToLower() == "albumarturi");
            if (ValTemp != null) ReturnElement.AlbumArtURI = ValTemp.Value;
            return ReturnElement;
        }
    }

    public class UPnPStorageFolder : UPnPContainer
    {
        public string storageUsed { get; set; } = "";

        public new T Create<T>(XElement Element, T ReturnElement) where T : UPnPStorageFolder
        {
            ReturnElement = base.Create(Element, ReturnElement);
            List<XAttribute> ElementAttributes = Element.Attributes().ToList();
            XAttribute Val = ElementAttributes.Find(Attrib => Attrib.Name.LocalName.ToLower() == "storageused");
            if (Val != null) ReturnElement.storageUsed = Val.Value;
            return ReturnElement;
        }
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
