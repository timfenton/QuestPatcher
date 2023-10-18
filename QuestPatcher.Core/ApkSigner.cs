/*
 * All-C# APK signing code was taken from emulamer's Apkifier library: https://github.com/emulamer/Apkifier/blob/master/Apkifier.cs
MIT License

Copyright (c) 2019 emulamer

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.IO.Pem;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;
using QuestPatcher.Core.Apk;
using QuestPatcher.Core.Patching;
using Serilog;
using PemReader = Org.BouncyCastle.OpenSsl.PemReader;

namespace QuestPatcher.Core
{
    public class ApkSigner
    {
        private const string PatchingCertificatePem = @"-----BEGIN CERTIFICATE-----
MIICpjCCAY6gAwIBAgIIcmOVkuI/DbUwDQYJKoZIhvcNAQELBQAwEjEQMA4GA1UE
AwwHVW5rbm93bjAgFw0xMTA5MjkwMDAwMDBaGA8yMDcxMDkyOTAwMDAwMFowEjEQ
MA4GA1UEAwwHVW5rbm93bjCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEB
AIb3U/N3xLTsmde+nX2Z7ABc+EMsK92w5N0rn/ynAE+3Qyvb4nY6qFBsP1LqP1uN
ZyMoZOm+8MqSW0zFsEABEzkWmu1Ahecl6JIDqjSbamj+IkZZLFICUg00UIw5XJ1/
uq2jX2hknc/qPrGNsvltgZ5NDwIc/eNJ7Sb1b8PYD1rHWMEvxkdDmW41EUP35j5N
ATJX2NjQC3QZAslbkT890TrlwNWbexa1YypSSe31hjaTYVc8ubsoacGq/dSxAkOf
EKYf1+U+z0Vdxu76wSnfO7H/SXPYc4ToNzzoqk0ko9LBzjTqle1sHEJBCKRBbMKt
Qylz4rjyMobvgIFkPqFy6d8CAwEAATANBgkqhkiG9w0BAQsFAAOCAQEAMNTQo9lg
bvHnp1Ot4g1UgjpSDu52BKdAB0eaeR/3Rtm+E0E+jUMXSI70im4PxbN+eOmTG3NC
o0nO/FLQUw3j3o3kmON4VlPapGsDpKe2rHbL+5HySPbSjkGpwTTGPVzzfhv9dUD6
l97QIB5cmvRH3T9CP/8c+erOARBF2kGitdNTtyUxvQsl/xaiKAnuaE7Ub0YmpsZQ
e1EiJ9LNwF92YvK3dWP9cBKOKnxQEAcSgugGWWIbiCWF9KHLUWYvT2Gv1tgl+kvE
/ZUie++OqnFEjPeWDTsbpiJXD1sKFUp3iCf970mgLMfXYwkiRxwicYFny0tu90wF
Nbzwy1zKhUC80w==
-----END CERTIFICATE-----
-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEAhvdT83fEtOyZ176dfZnsAFz4Qywr3bDk3Suf/KcAT7dDK9vi
djqoUGw/Uuo/W41nIyhk6b7wypJbTMWwQAETORaa7UCF5yXokgOqNJtqaP4iRlks
UgJSDTRQjDlcnX+6raNfaGSdz+o+sY2y+W2Bnk0PAhz940ntJvVvw9gPWsdYwS/G
R0OZbjURQ/fmPk0BMlfY2NALdBkCyVuRPz3ROuXA1Zt7FrVjKlJJ7fWGNpNhVzy5
uyhpwar91LECQ58Qph/X5T7PRV3G7vrBKd87sf9Jc9hzhOg3POiqTSSj0sHONOqV
7WwcQkEIpEFswq1DKXPiuPIyhu+AgWQ+oXLp3wIDAQABAoIBAATKc2dutpkkK5Mr
R/81CdnlcvE8zdMmZqlXKsGsJ+hXKAI+4ZCyz6td0aL0KmA/P7b8GxY/rzUcRt4N
x7jYkOxzhKJWqlTPKqyW3AwsAXX392gp3YHiZTPkydXVv2zeeNZAY1rwSg3Jpy9k
SUMYrljb1rmLepOpLyA1PCIRNvz7jXreXVTZlaOmUUXT4tGeJNXgMseJ41szzBmB
5Ouro4goqx2jTkGx6qX/RnvIo/hp0ykHSQOtg3F1tmcYi5qlKwmoFln3pRhN27Ed
JUOaAV7adv09hyM6yRosg0A47abFHIBBCqgvdjqNk+orboUjhfPHabwt6x0imZ1k
v4iwUNkCgYEA9dO7l1IqGJ1fSlQwwy5HRB8mb5s488G1sUTmxR6ByPr2gf7h6FmX
vG1yQZ3A6OLVFHkFRa++bGdZ3ngW6vnQhmfpAVaAjg3EkdlAN1eVyEfACkkoYV20
D4PmM07B9aOGKXK0KpvwYh8GlZDKuKIC8QymLlDxOw/UJImFn/4daIsCgYEAjI0l
OQ7SlKvdalYs0fpPo3gZmygFeosfNFJ1xwDxhzoBvUhkHfHho6qBj7wb2E4UjHQC
67QgnQe60SQnpB/LfmCk9HvQ4dB0kCGhoHHuhUH9Kf9PX6auKGySNZ6p7gG8xOy9
dBjMH75gJK3H/LsW7LERCDHmaefS53f1QpfCWn0CgYBxxy8TKa9cNzKMl4z+OaQ4
jmZez6w7fhPXWXmqEKWnXSjNICh1P0pwpwN0BUztPVe8IwtipqXvTKKWymRpG3j9
TIjW2q+jkBHEI5aKRtqHmVX0LMoozpLxf24Dn1c8lxQYiQOEmSpYb92/SgXaEPpl
kSI1W7dbS8c3pgMX+yinYwKBgBhnTWY5x6BesuQKsF+I+ZjlenSxHzpmu3VHOAHk
jQswrCqkThXQ8J+NNE+zlpYZAIJehj9MmDkLpYk4oNVjW97Ggv2cHemHWyXHYRvN
jF+A1KcdGDgAZc7JAx3iPZkAnjkG7eIhiBee42ya69Va2qEgIVft6hbLVJgyANie
JvW1AoGBANX/7ZpHZO6UKb8KBs81aMn1mw478p3R4BrjaBh/9Cmh98UjxzgNo9+K
QwA97QgLhZd5HjLpZlEzV45gO4VakAAnXDtCEWEMPy2Pp/Oo+kw5sznsUe9Dk6A0
llAY8xXVMiYeyHboXxDPOCH8y1TgEW0Nc2cnnCKOuji2waIwrVwR
-----END RSA PRIVATE KEY-----";
        
        private static readonly Encoding Encoding = new UTF8Encoding();
        private static readonly SHA256 Sha = SHA256.Create();

        /// <summary>
        /// Stores the hash of a file within an APK, scraped from the signature before patching.
        /// </summary>
        public struct PrePatchHash
        {
            internal string Hash { get; }
            
            internal DateTimeOffset LastModified { get; }

            internal PrePatchHash(string hash, DateTimeOffset lastModified)
            {
                Hash = hash;
                LastModified = lastModified;
            }
        }

        /// <summary>
        /// Parses the META-INF/MANIFEST.MF file within <paramref name="apkArchive"/> and uses it to collect
        /// the hashes of the entries within the given APK.
        /// </summary>
        /// <param name="apkArchive">The archive to get the entry hashes of</param>
        /// <returns>A dictionary of the full entry names and entry hashes, or null if parsing the manifest failed.</returns>
        public async Task<Dictionary<string, PrePatchHash>?> CollectPrePatchHashes(ZipArchive apkArchive)
        {
            var manifestEntry = apkArchive.GetEntry("META-INF/MANIFEST.MF");
            // Fallback failure if the APK isn't signed
            if(manifestEntry == null)
            {
                return null;
            }

            await using var manifestStream = manifestEntry.Open();
            using var manifestReader = new StreamReader(manifestStream);

            // Fallback failure if the manifest version isn't what we're expecting.
            if((await manifestReader.ReadLineAsync()) != "Manifest-Version: 1.0")
            {
                return null;
            }
            
            // Read the remaining lines of the MANIFEST.MF header, when we reach a blank line, the header is over
            // This skips information such as the piece of software that was doing the signing.
            while(await manifestReader.ReadLineAsync() != "") {}
            
            var result = new Dictionary<string, PrePatchHash>();
            while(true)
            {
                // Sometimes the names of files within a hash are formatted with multiple lines
                // In this case, the files will be formatted like:
                // |Name: myFileNameIsReallyReally
                // | LongItIsVeryLong.txt
                // So, each newline and space indicates an extension of the file name.
                var nameBuilder = new StringBuilder();
                string? firstLineOfName = await manifestReader.ReadLineAsync();
                // We have reached the end of the file, or there is a formatting issue, so we quit parsing
                if(firstLineOfName == null)
                {
                    return result;
                }
                // Skip the "Name: " prefix.
                nameBuilder.Append(firstLineOfName[6..]);

                string digest;
                // Now we will parse the remaining lines within the name of the file
                while(true)
                {
                    string? nextLineOfName = await manifestReader.ReadLineAsync();
                    if(nextLineOfName == null)
                    {
                        // We have reached the end of the file, or there is a formatting issue, so we quit parsing
                        return result;
                    }
                    if(nextLineOfName.StartsWith(" "))
                    {
                        // A space at the beginning of the line indicates that it is a continuation of the current file's name
                        nameBuilder.Append(nextLineOfName[1..]);
                    }
                    else if(nextLineOfName.StartsWith("SHA-256-Digest: "))
                    {
                        // We have now reached the end of the name of the file, and the start of the SHA-256 digest.
                        // Skip the "SHA-256-Digest: " prefix.
                        digest = nextLineOfName[16..];
                        break;
                    }
                    else
                    {
                        // If the next line does not start with a space, and is not a SHA-256 digest, then this manifest
                        // format/hash type is unsupported, so we will quit parsing here.
                        return result;
                    }
                }
                string entryName = nameBuilder.ToString();

                // Make sure that an entry actually exits with the given name
                ZipArchiveEntry? entry = apkArchive.GetEntry(entryName);
                if(entry == null)
                {
                    continue;
                }

                result[entryName] = new PrePatchHash(digest, entry.LastWriteTime);
                Log.Debug($"Added hash {digest} for {entryName}");

                // Skip the newline after each entry.
                await manifestReader.ReadLineAsync();
            }
        }

        /// <summary>
        /// Signs the signature file's content using the given certificate, and returns the RSA signature.
        /// </summary>
        /// <param name="signatureFileData">Content of the signature file to be signed</param>
        /// <param name="pemCertData">PEM data of the certificate and private key for signing</param>
        /// <returns>The RSA signature</returns>
        private byte[] GetSignature(byte[] signatureFileData, string pemCertData)
        {
            var (cert, privateKey) = LoadCertificate(pemCertData);
            
            var certStore = X509StoreFactory.Create("Certificate/Collection", new X509CollectionStoreParameters(new List<X509Certificate> { cert }));
            CmsSignedDataGenerator dataGen = new();
            dataGen.AddCertificates(certStore);
            dataGen.AddSigner(privateKey, cert, CmsSignedGenerator.EncryptionRsa, CmsSignedGenerator.DigestSha256);

            // Content is detached - i.e. not included in the signature block itself
            CmsProcessableByteArray detachedContent = new(signatureFileData);
            var signedContent = dataGen.Generate(detachedContent, false);

            // Get the signature in the proper ASN.1 structure for java to parse it properly.  Lots of trial and error
            var signerInfos = signedContent.GetSignerInfos();
            var signer = signerInfos.GetSigners().Cast<SignerInformation>().First();
            SignerInfo signerInfo = signer.ToSignerInfo();
            Asn1EncodableVector digestAlgorithmsVector = new();
            digestAlgorithmsVector.Add(new AlgorithmIdentifier(new DerObjectIdentifier("2.16.840.1.101.3.4.2.1"), DerNull.Instance));
            ContentInfo encapContentInfo = new(new DerObjectIdentifier("1.2.840.113549.1.7.1"), null);
            Asn1EncodableVector asnVector = new()
            {
                X509CertificateStructure.GetInstance(Asn1Object.FromByteArray(cert.GetEncoded()))
            };
            Asn1EncodableVector signersVector = new() {signerInfo.ToAsn1Object()};
            SignedData signedData = new(new DerSet(digestAlgorithmsVector), encapContentInfo, new BerSet(asnVector), null, new DerSet(signersVector));
            ContentInfo contentInfo = new(new DerObjectIdentifier("1.2.840.113549.1.7.2"), signedData);
            return contentInfo.GetDerEncoded();
        }

        /// <summary>
        /// Loads the certificate and private key from the given PEM data
        /// </summary>
        /// <param name="pemData"></param>
        /// <returns>The loaded certificate and private key</returns>
        /// <exception cref="System.Security.SecurityException">If the certificate or private key failed to load</exception>
        private (X509Certificate certificate, AsymmetricKeyParameter privateKey) LoadCertificate(string pemData)
        {
            X509Certificate? cert = null;
            AsymmetricKeyParameter? privateKey = null;
            using (var reader = new StringReader(pemData))
            {
                // Iterate through the PEM objects until we find the public or private key
                var pemReader = new PemReader(reader);
                object pemObject;
                while ((pemObject = pemReader.ReadObject()) != null)
                {
                    cert ??= pemObject as X509Certificate;
                    privateKey ??= (pemObject as AsymmetricCipherKeyPair)?.Private;
                }
            }
            if (cert == null)
                throw new System.Security.SecurityException("Certificate could not be loaded from PEM data.");

            if (privateKey == null)
                throw new System.Security.SecurityException("Private Key could not be loaded from PEM data.");

            return (cert, privateKey);
        }

        /// <summary>
        /// Signs the given APK with the QuestPatcher patching certificate.
        /// </summary>
        /// <param name="path">Path to the APK to sign</param>
        /// <param name="knownHashes">Optionally, the hashes of the files within the APK at some earlier point.
        /// Using existing hashes reduces signing time, since only the files within the APK that have actually changed have to get signed.</param>
        public void SignApkWithPatchingCertificate(string path, Dictionary<string, PrePatchHash>? knownHashes = null)
        {
            SignApk(path, PatchingCertificatePem, knownHashes);
        }

        /// <summary>
        /// Signs the archive with the given PEM certificate and private key. 
        /// </summary>
        /// <param name="path">Path to the APK to sign</param>
        /// <param name="pemData">PEM of the certificate and private key</param>
        /// <param name="knownHashes">Optionally, the hashes of the files within the APK at some earlier point.
        /// Using existing hashes reduces signing time, since only the files within the APK that have actually changed have to get signed.</param>
        public void SignApk(string path, string pemData, Dictionary<string, PrePatchHash>? knownHashes = null)
        {
            Log.Information("Generating jar signature");
            using Stream manifestFile = new MemoryStream();
            using Stream sigFileBody = new MemoryStream();
            using (StreamWriter manifestWriter = OpenStreamWriter(manifestFile))
            {
                manifestWriter.WriteLine("Manifest-Version: 1.0");
                manifestWriter.WriteLine("Created-By: QuestPatcher");
                manifestWriter.WriteLine();
            }

            // Temporarily open the archive in order to calculate these hashes
            // This is done because opening all of the entries will cause them all to be recompressed if using ZipArchiveMode.Update, thus causing a long dispose time
            using (ZipArchive apkArchive = ZipFile.OpenRead(path))
            {
                foreach (ZipArchiveEntry entry in apkArchive.Entries
                            .Where(entry => !entry.FullName.StartsWith("META-INF"))) // Skip other signature related files
                {
                    WriteEntryHash(entry, manifestFile, sigFileBody, knownHashes);
                }
            }

            ZipArchive writingArchive = ZipFile.Open(path, ZipArchiveMode.Update);
            try
            {
                // Delete existing signature related files
                foreach (ZipArchiveEntry entry in writingArchive.Entries.Where(entry => entry.FullName.StartsWith("META-INF")).ToList())
                {
                    entry.Delete();
                }

                using Stream signaturesFile = writingArchive.CreateAndOpenEntry("META-INF/BS.SF");
                using Stream rsaFile = writingArchive.CreateAndOpenEntry("META-INF/BS.RSA");
                using Stream manifestStream = writingArchive.CreateAndOpenEntry("META-INF/MANIFEST.MF");

                // Find the hash of the manifest
                manifestFile.Position = 0;
                byte[] manifestHash = Sha.ComputeHash(manifestFile);

                // Finally, copy it to the output file
                manifestFile.Position = 0;
                manifestFile.CopyTo(manifestStream);

                // Write the signature information
                using (StreamWriter signatureWriter = OpenStreamWriter(signaturesFile))
                {
                    signatureWriter.WriteLine("Signature-Version: 1.0");
                    signatureWriter.WriteLine($"SHA-256-Digest-Manifest: {Convert.ToBase64String(manifestHash)}");
                    signatureWriter.WriteLine("Created-By: QuestPatcher");
                    signatureWriter.WriteLine("X-Android-APK-Signed: 2");
                    signatureWriter.WriteLine();
                }

                // Copy the body of signatures for each file into the signature file
                sigFileBody.Position = 0;
                sigFileBody.CopyTo(signaturesFile);
                signaturesFile.Position = 0;

                // Get the bytes in the signature file for signing
                using MemoryStream sigFileMs = new();
                signaturesFile.CopyTo(sigFileMs);

                // Sign the signature file, and save the signature
                byte[] keyFile = GetSignature(sigFileMs.ToArray(), pemData);
                rsaFile.Write(keyFile);
            }
            finally
            {
                // Dispose in Task.Run, to avoid UI freezes
                Log.Information("Disposing signed archive");
                writingArchive.Dispose();
            }
            Log.Information("Aligning APK");
            ApkAligner.AlignApk(path);

            Log.Information("Generating V2 signature");

            using TempFile tempFile = new TempFile();
            using (FileStream outFs = new FileStream(tempFile.Path, FileMode.Open))
            {
                using FileStream fs = new FileStream(path, FileMode.Open);
                using FileMemory memory = new FileMemory(fs);
                using FileMemory outMemory = new FileMemory(outFs);
                memory.Position = memory.Length() - 22;
                while (memory.ReadInt() != EndOfCentralDirectory.SIGNATURE)
                {
                    memory.Position -= 4 + 1;
                }
                memory.Position -= 4;
                var eocdPosition = memory.Position;
                EndOfCentralDirectory eocd = new EndOfCentralDirectory(memory);
                if (eocd == null)
                    return;
                var cd = eocd.OffsetOfCD;
                memory.Position = cd - 16 - 8;
                var d = memory.ReadULong();
                var d2 = memory.ReadString(16);
                var section1 = GetSectionDigests(fs, 0, cd);
                var section3 = GetSectionDigests(fs, cd, eocdPosition);
                var section4 = GetSectionDigests(fs, eocdPosition, fs.Length);

                var digestChunks = section1.Concat(section3).Concat(section4).ToList();

                byte[] bytes = new byte[1 + 4];
                bytes[0] = 0x5a;
                byte[] sizeBytes = BitConverter.GetBytes((uint) digestChunks.Count);
                bytes[1] = sizeBytes[0];
                bytes[2] = sizeBytes[1];
                bytes[3] = sizeBytes[2];
                bytes[4] = sizeBytes[3];
                var digest = Sha.ComputeHash(bytes.Concat(digestChunks.Aggregate((a, b) => a.Concat(b).ToArray())).ToArray());

                uint algorithm = 0x0103;

                APKSignatureSchemeV2 block = new APKSignatureSchemeV2();
                var signer = new APKSignatureSchemeV2.Signer();

                using MemoryStream signedDataMs = new MemoryStream();
                using FileMemory memorySignedData = new FileMemory(signedDataMs);
                var signedData = new APKSignatureSchemeV2.Signer.BlockSignedData();
                signedData.Digests.Add(new APKSignatureSchemeV2.Signer.BlockSignedData.Digest(algorithm, digest));
                var (cert, privateKey) = LoadCertificate(pemData);

                signedData.Certificates.Add(cert.GetEncoded());

                signedData.Write(memorySignedData);
                signer.SignedData = signedDataMs.ToArray();
                ISigner signerType = SignerUtilities.GetSigner("SHA256WithRSA");
                signerType.Init(true, privateKey);
                signerType.BlockUpdate(signer.SignedData, 0, signer.SignedData.Length);

                signer.Signatures.Add(new APKSignatureSchemeV2.Signer.BlockSignature(algorithm, signerType.GenerateSignature()));
                signer.PublicKey = cert.CertificateStructure.SubjectPublicKeyInfo.GetDerEncoded();
                block.Signers.Add(signer);

                APKSigningBlock signingBlock = new APKSigningBlock();
                signingBlock.Values.Add(block.ToIDValuePair());

                fs.Position = 0;
                outMemory.WriteBytes(memory.ReadBytes(cd));
                signingBlock.Write(outMemory);
                eocd.OffsetOfCD = (int) outFs.Position;
                outMemory.WriteBytes(memory.ReadBytes((int) (eocdPosition - cd)));
                eocd.Write(outMemory);
            }

            File.Delete(path);
            File.Move(tempFile.Path, path);
        }

        private List<byte[]> GetSectionDigests(FileStream fs, long startOffset, long endOffset)
        {
            var digests = new List<byte[]>();
            int chunkSize = 1024 * 1024;
            for(long i = startOffset; i < endOffset; i+= chunkSize)
            {
                fs.Position = i;
                var size = Math.Min(endOffset - i, chunkSize);
                byte[] bytes = new byte[1 + 4 + size];
                bytes[0] = 0xa5;
                byte[] sizeBytes = BitConverter.GetBytes((uint) size);
                bytes[1] = sizeBytes[0];
                bytes[2] = sizeBytes[1];
                bytes[3] = sizeBytes[2];
                bytes[4] = sizeBytes[3];
                fs.Read(bytes, 5, (int)size);
                digests.Add(Sha.ComputeHash(bytes));
            }
            return digests;
        }

        /// <summary>
        /// Writes the MANIFEST.MF and signature file hashes for the given entry
        /// </summary>
        private void WriteEntryHash(ZipArchiveEntry entry, Stream manifestStream, Stream signatureStream, Dictionary<string, PrePatchHash>? prePatchHashes)
        {
            string hash;
            if(prePatchHashes != null && 
               prePatchHashes.TryGetValue(entry.FullName, out var prePatchHash) &&
               entry.LastWriteTime == prePatchHash.LastModified)
            {
                Log.Verbose("Using existing hash for " + entry.FullName);
                hash = prePatchHash.Hash;
            }
            else
            {
                Log.Verbose("Hashing " + entry.FullName);
                using Stream sourceStream = entry.Open();
                hash = Convert.ToBase64String(Sha.ComputeHash(sourceStream));
            }

            // First write the digest of the file to a section of the manifest file
            using MemoryStream sectStream = new();
            using(StreamWriter sectWriter = OpenStreamWriter(sectStream))
            {
                sectWriter.WriteLine($"Name: {entry.FullName}");
                sectWriter.WriteLine($"SHA-256-Digest: {hash}");
                sectWriter.WriteLine();
            }

            // Then write the hash for the section of the manifest file to the signature file
            sectStream.Position = 0;
            string sectHash = Convert.ToBase64String(Sha.ComputeHash(sectStream));
            using(StreamWriter signatureWriter = OpenStreamWriter(signatureStream))
            {
                signatureWriter.WriteLine($"Name: {entry.FullName}");
                signatureWriter.WriteLine($"SHA-256-Digest: {sectHash}");
                signatureWriter.WriteLine(); 
            }

            sectStream.Position = 0;
            sectStream.CopyTo(manifestStream);
        }

        private StreamWriter OpenStreamWriter(Stream stream)
        {
            return new(stream, Encoding, 1024, true);
        }
        
        /// <summary>
        /// Creates a new X509 certificate and returns its data in PEM format.
        ///
        /// <see cref="PatchingCertificatePem"/> is generated using this method.
        /// </summary>
        public string GenerateNewCertificatePem()
        {
            
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);
            var certificateGenerator = new X509V3CertificateGenerator();
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);
            
            // TODO: Figure out ISignatureFactory to avoid these deprecated methods
#pragma warning disable 618
            certificateGenerator.SetSignatureAlgorithm("SHA256WithRSA");
#pragma warning restore 618
            var subjectDn = new X509Name("cn=Unknown");
            var issuerDn = subjectDn;
            certificateGenerator.SetIssuerDN(issuerDn);
            certificateGenerator.SetSubjectDN(subjectDn);
            certificateGenerator.SetNotBefore(DateTime.UtcNow.Date.AddYears(-10));
            certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(50));
            var keyGenerationParameters = new KeyGenerationParameters(random, 2048);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            // TODO: Figure out ISignatureFactory to avoid these deprecated methods
#pragma warning disable 618
            X509Certificate cert = certificateGenerator.Generate(subjectKeyPair.Private);
#pragma warning restore 618

            using var writer = new StringWriter();
            var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(writer);
                                
            pemWriter.WriteObject(new PemObject("CERTIFICATE", cert.GetEncoded()));
            pemWriter.WriteObject(subjectKeyPair.Private);
            return writer.ToString();
        }
    }
}
