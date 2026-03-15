namespace Server.Items;

public class BulletinMessagePersistence : GenericEntityPersistence<BulletinMessage>
{
    private static BulletinMessagePersistence _persistence;

    public static void Configure()
    {
        _persistence = new BulletinMessagePersistence();
    }

    public BulletinMessagePersistence() : base("BulletinMessages", 3, 0x80000000, 0xFFFFFFFE)
    {
    }

    public static Serial NewMessage => _persistence.NewEntity;
    public static void Add(BulletinMessage msg) => _persistence.AddEntity(msg);
    public static void Remove(BulletinMessage msg) => _persistence.RemoveEntity(msg);
    public static BulletinMessage Find(Serial serial) => _persistence.FindEntity<BulletinMessage>(serial);
}
