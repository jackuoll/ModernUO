using Server;
using Server.Items;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BulletinBoardPacketTests
{
    [Theory]
    [InlineData("Test Name")]
    [InlineData(null)]
    [InlineData("🅵🅰🅽🅲🆈 🆃🅴🆇🆃")]
    public void TestSendBBDisplayBoard(string boardName)
    {
        var bb = new TestBulletinBoard(0x234) { BoardName = boardName };

        var expected = new BBDisplayBoard(bb).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendBBDisplayBoard(bb);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Theory]
    [InlineData("The Subject", false, "First Line", "Second Line", "Third Line")]
    [InlineData("", false, "")]
    [InlineData("🅵🅰🅽🅲🆈 🆃🅴🆇🆃", false, "First Line", "Second Line")]
    [InlineData("The Subject", true, "First Line", "Second Line", "Third Line")]
    [InlineData("", true, "")]
    [InlineData("🅵🅰🅽🅲🆈 🆃🅴🆇🆃", true, "First Line", "Second Line")]
    public void TestSendBBHeaderMessage(string subject, bool content, params string[] lines)
    {
        var poster = new Mobile((Serial)0x1024u) { Name = "Kamron" };
        poster.DefaultMobileInit();

        var bb = new TestBulletinBoard(0x234);
        bb.PostMessage(poster, null, subject, lines);

        var msg = bb.Messages[0];

        var expected = (content ?
            (Packet)new BBMessageContent(bb, msg) : new BBMessageHeader(bb, msg)).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendBBMessage(bb, msg, content);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}

internal class TestBulletinBoard : BaseBulletinBoard
{
    public TestBulletinBoard(int itemID) : base(itemID)
    {
    }

    public TestBulletinBoard(Serial serial) : base(serial)
    {
    }
}
