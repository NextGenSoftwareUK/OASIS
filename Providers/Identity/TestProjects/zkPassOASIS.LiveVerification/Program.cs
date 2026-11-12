using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Providers.zkPassOASIS;

const string Transaction = "0xa3a9ee047af5fa7540f48ea9b4f815dbfb83f80c9351aef70cbfaa73408823ad";
const string Contract = "0x8c18c0436A8d6ea44C87Bf5853F8D11B55CF0302";
const string Allocator = "0x19a567b3b212a5b35bA0E3B600FbEd5c2eE9083d";

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
using var response = await http.GetAsync($"https://eth-sepolia.blockscout.com/api/v2/transactions/{Transaction}");
response.EnsureSuccessStatusCode();
using var receipt = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
var tx = receipt.RootElement;
Require(tx.GetProperty("hash").GetString() == Transaction && tx.GetProperty("status").GetString() == "ok" &&
    string.Equals(tx.GetProperty("to").GetProperty("hash").GetString(), Contract, StringComparison.OrdinalIgnoreCase),
    "The expected successful Sepolia attestation was not returned.");

var fields = tx.GetProperty("decoded_input").GetProperty("parameters")[0].GetProperty("value");
Require(fields.GetArrayLength() == 8, "The attestation tuple shape changed.");
string Field(int index) => fields[index].GetString() ?? throw new InvalidDataException("Missing attestation field.");
static string TextId(string hex) => Encoding.ASCII.GetString(Convert.FromHexString(hex.AsSpan(2))).TrimEnd('\0');

var schema = TextId(Field(1));
var recipient = Field(3);
var proof = new ZkPassEvmProof(TextId(Field(0)), Field(2), Field(4), recipient, Allocator, Field(6), Field(5), Field(7));
var provider = new zkPassOASIS();
Require(provider.ActivateProvider().Result, "zkPass activation failed.");
var accepted = provider.Verify(proof, schema, recipient);
Require(!accepted.IsError && accepted.Result, "The genuine TransGate proof was rejected.");
Require(provider.Verify(proof, schema + "x", recipient).IsError, "A different schema was accepted.");
Require(provider.Verify(proof, schema, "0x0000000000000000000000000000000000000001").IsError,
    "A different recipient was accepted.");
Require(provider.Verify(proof with { UserHash = "0x" + new string('0', 64) }, schema, recipient).IsError,
    "A modified signed user hash was accepted.");
Require(provider.DeActivateProvider().Result, "zkPass deactivation failed.");
Console.WriteLine("Verified genuine Sepolia TransGate attestation and three rejection controls.");

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
