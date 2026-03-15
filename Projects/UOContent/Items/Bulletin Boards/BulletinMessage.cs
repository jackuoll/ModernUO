using System;
using ModernUO.Serialization;
using Server.Collections;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BulletinMessage : ISerializable
{
    public BulletinMessage(BaseBulletinBoard board, Mobile poster, BulletinMessage thread, string subject, string[] lines)
    {
        Serial = BulletinMessagePersistence.NewMessage;

        _board = board;
        _poster = poster;
        _subject = subject;
        _time = Core.Now;
        _lastPostTime = _time;
        _thread = thread;
        _postedName = poster.Name;
        _postedBody = poster.Body;
        _postedHue = poster.Hue;
        _lines = lines;

        using var list = PooledRefQueue<BulletinEquip>.Create(poster.Items.Count);

        for (var i = 0; i < poster.Items.Count; ++i)
        {
            var item = poster.Items[i];

            if (item.Layer >= Layer.OneHanded && item.Layer <= Layer.Mount)
            {
                list.Enqueue(new BulletinEquip(item.ItemID, item.Hue));
            }
        }

        _postedEquip = list.ToArray();

        BulletinMessagePersistence.Add(this);
    }

    public DateTime Created { get; set; } = Core.Now;
    public Serial Serial { get; }
    public byte SerializedThread { get; set; }
    public int SerializedPosition { get; set; }
    public int SerializedLength { get; set; }
    public bool Deleted { get; private set; }

    [SerializableField(0)]
    private BaseBulletinBoard _board;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster, readOnly: true)]
    private Mobile _poster;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster, readOnly: true)]
    private string _subject;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster, readOnly: true)]
    private DateTime _time;

    [SerializableField(4)]
    private DateTime _lastPostTime;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster, readOnly: true)]
    private BulletinMessage _thread;

    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster, readOnly: true)]
    private string _postedName;

    [SerializableField(7)]
    private int _postedBody;

    [SerializableField(8)]
    private int _postedHue;

    [SerializableField(9)]
    private BulletinEquip[] _postedEquip;

    [SerializableField(10)]
    private string[] _lines;

    // TODO: Memoize
    public string GetTimeAsString() => Time.ToString("MMM dd, yyyy");

    public void Delete()
    {
        Deleted = true;
        _board?.Messages.Remove(this);
        BulletinMessagePersistence.Remove(this);
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (_board == null || _board.Deleted)
        {
            Delete();
            return;
        }

        _board.Messages.Add(this);
    }
}
