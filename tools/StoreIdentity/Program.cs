using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Xml.Linq;

if (args is ["--self-test"]) { IdentitySelection.SelfTest(); return 0; }
if (args.Length != 2) { Console.Error.WriteLine("Use StoreIdentity 'certificate name' decoded-profile.plist"); return 2; }
try
{
    var start = new ProcessStartInfo("/usr/bin/security") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    foreach (var arg in new[] { "find-identity", "-v", "-p", "codesigning" }) start.ArgumentList.Add(arg);
    using var process = Process.Start(start) ?? throw new IOException("Could not read signing identities.");
    string identities = process.StandardOutput.ReadToEnd();
    string error = process.StandardError.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0) throw new IOException("Could not read signing identities: " + error);
    string selected = IdentitySelection.Select(args[0], identities, IdentitySelection.Certificates(XDocument.Load(args[1])));
    Console.Error.WriteLine("Selected profile-authorized signing identity: " + args[0] + " [" + selected + "]");
    Console.WriteLine(selected); // stdout is consumed by codesign; keep it fingerprint-only.
    return 0;
}
catch (Exception e) { Console.Error.WriteLine("Signing identity selection failed: " + e.Message); return 1; }

internal static class IdentitySelection
{
    public static List<(string Hash, DateTime Expires)> Certificates(XDocument profile)
    {
        var key = profile.Root?.Element("dict")?.Elements("key").SingleOrDefault(k => k.Value == "DeveloperCertificates");
        var array = key?.ElementsAfterSelf().FirstOrDefault();
        if (array?.Name != "array") throw new InvalidDataException("Profile has no DeveloperCertificates array.");
        var hashes = new List<(string Hash, DateTime Expires)>();
        foreach (var data in array.Elements("data"))
        {
            using var certificate = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(data.Value));
            hashes.Add((certificate.GetCertHashString(HashAlgorithmName.SHA1), certificate.NotAfter.ToUniversalTime()));
        }
        return hashes;
    }

    public static string Select(string name, string identities, IEnumerable<(string Hash, DateTime Expires)> certificates)
    {
        var active = Regex.Matches(identities, "^\\s*\\d+\\)\\s+([0-9a-fA-F]{40})\\s+\"([^\"]+)\"", RegexOptions.Multiline)
            .Cast<Match>().Where(m => string.Equals(m.Groups[2].Value, name, StringComparison.Ordinal))
            .Select(m => m.Groups[1].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selected = certificates.Where(c => active.Contains(c.Hash))
            .OrderByDescending(c => c.Expires).ThenBy(c => c.Hash, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        if (selected.Hash == null)
            throw new InvalidOperationException("No valid private-key identity with this name is authorized by the selected profile. Choose the matching profile or install its certificate with its private key.");
        return selected.Hash.ToUpperInvariant();
    }

    public static void SelfTest()
    {
        const string name = "Apple Distribution: Test Owner (TESTTEAM)";
        string oldHash = new('A', 40), newHash = new('B', 40), otherHash = new('C', 40);
        string listing = $"  1) {oldHash} \"{name}\"\n  2) {newHash} \"{name}\"\n  3) {otherHash} \"Other signing identity\"\n  4) {oldHash} \"{name}\"\n     4 valid identities found\n";
        var earlier = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var later = earlier.AddYears(1);
        void Check(bool value, string reason) { if (!value) throw new Exception(reason); }
        Check(Select(name, listing, [(oldHash, earlier)]) == oldHash, "Must choose the older identity if only it is authorized by the profile.");
        Check(Select(name, listing, [(newHash, later)]) == newHash, "Must choose the newer identity if only it is authorized.");
        Check(Select(name, listing, [(oldHash, earlier), (newHash, later)]) == newHash, "When both are authorized and active, prefer the later expiry.");
        Check(Select(name, listing, [(oldHash.ToLowerInvariant(), earlier)]) == oldHash, "Fingerprints ignore case and duplicate keychain rows.");
        foreach (var profile in new[] { new[] { (otherHash, later) }, new[] { (new string('D',40), later) }, Array.Empty<(string,DateTime)>() })
        {
            bool rejected = false;
            try { Select(name, listing, profile); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Reject a missing private key, a different identity name, or an empty profile.");
        }
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=Public fixture only", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var plist = new XDocument(new XElement("plist", new XElement("dict", new XElement("key", "DeveloperCertificates"),
            new XElement("array", new XElement("data", Convert.ToBase64String(cert.Export(X509ContentType.Cert)))))));
        var parsed = Certificates(plist);
        Check(parsed.Count == 1 && parsed[0].Hash == cert.GetCertHashString(HashAlgorithmName.SHA1), "Decode the public DER certificate from the profile plist.");
        Check(parsed[0].Expires == cert.NotAfter.ToUniversalTime(), "Read certificate expiry from the profile.");
        Console.WriteLine("PASS: decoded profile certificates, duplicate certificate names, profile-authorized old/new choice, duplicate rows, expiry preference and missing/mismatched identities.");
    }
}
