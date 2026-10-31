using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

// Pure wire integrity tests: no native write, encryption or persistence claim.
[TestClass]
public class SolanaMemoFramesTests
{
    private static string Signature(int index) => Encoders.Base58.EncodeData(
        Enumerable.Repeat((byte)(index+1),64).ToArray());

    private static (SolanaMemoFrames.Commit Commit,List<(string Signature,string Memo)> Rows,string Ciphertext) Fixture()
    {
        var ciphertext=SolanaMemoFrames.CipherPrefix+new string('a',1800);
        var frames=new SolanaMemoFrames(ciphertext);
        var rows=new List<(string Signature,string Memo)>();
        for(var index=0;index<frames.Count;index++)
            rows.Add((Signature(index),frames.ChunkMemo(index,index==0?null!:rows[^1].Signature)));
        return (SolanaMemoFrames.ParseCommit(frames.CommitMemo(rows[^1].Signature)),rows,ciphertext);
    }

    [TestMethod]
    public void FullCiphertextRoundTripsAcrossBoundedFrames()
    {
        var fixture=Fixture();
        Assert.AreEqual(4,fixture.Rows.Count);
        Assert.IsTrue(fixture.Rows.All(row=>row.Memo.Length<=SolanaMemoFrames.MaximumMemoBytes));
        Assert.AreEqual(fixture.Ciphertext,SolanaMemoFrames.Join(fixture.Commit,fixture.Rows));
    }

    [DataTestMethod]
    [DataRow("missing")][DataRow("order")][DataRow("duplicate")]
    [DataRow("payload")][DataRow("terminal")][DataRow("owner")]
    public void CorruptOrIncompleteChainIsRejected(string corruption)
    {
        var fixture=Fixture();var rows=fixture.Rows;var commit=fixture.Commit;
        switch(corruption)
        {
            case "missing":rows.RemoveAt(1);break;
            case "order":(rows[0],rows[1])=(rows[1],rows[0]);break;
            case "duplicate":rows[1]=(rows[0].Signature,rows[1].Memo);break;
            case "payload":rows[0]=(rows[0].Signature,rows[0].Memo[..^1]+"b");break;
            case "terminal":commit=commit with{LastSignature=Signature(8)};break;
            case "owner":commit=commit with{WriteId=Guid.NewGuid()};break;
        }
        Assert.ThrowsException<InvalidDataException>(()=>SolanaMemoFrames.Join(commit,rows));
    }

    [DataTestMethod]
    [DataRow("count")][DataRow("integer")][DataRow("version")][DataRow("signature")]
    public void NoncanonicalCommitIsRejectedBeforeNativeReads(string corruption)
    {
        var fixture=Fixture();var commit=fixture.Commit;
        var fields=new[]{"oasis-solana-commit-v1",commit.WriteId.ToString("N"),"4",commit.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),commit.Hash,commit.LastSignature};
        switch(corruption){case "count":fields[2]="257";break;case "integer":fields[2]="04";break;case "version":fields[0]="oasis-solana-commit-v2";break;case "signature":fields[5]="not-a-signature";break;}
        Assert.ThrowsException<InvalidDataException>(()=>SolanaMemoFrames.ParseCommit(string.Join('|',fields)));
    }

    [TestMethod]
    public void OversizeCiphertextIsRejectedBeforeFraming()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(()=>new SolanaMemoFrames(
            SolanaMemoFrames.CipherPrefix+new string('a',SolanaMemoFrames.PartCharacters*SolanaMemoFrames.MaximumParts)));
    }
}
