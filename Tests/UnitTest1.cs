using MyCryptLib;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Tests
{
    public class Tests
    {

        public int r1 = 0;
        public string testString;

        [SetUp]
        public void Setup()
        {
            //Cryptography.CreateKeysForRSA(false, out string publicKey, out string privateKey);
            //using StreamWriter k1 = new StreamWriter(".\\k1.key");
            //using StreamWriter k2 = new StreamWriter(".\\k2.key");
            //k1.Write(publicKey);
            //k2.Write(privateKey);
            testString = "Test string with random text to open it. Well, let's encrypt and decrypt it.";
        }

        //[Test]
        //public void RSAKeyTest()
        //{
        //    StreamReader r1 = new StreamReader(".\\k1.key");
        //    StreamReader r2 = new StreamReader(".\\k2.key");
        //    string publicKey = r1.ReadToEnd();
        //    string privateKey = r2.ReadToEnd();
        //    byte[] test = { 0, 1, 2, 3 };
        //    byte[] enc = Cryptography.EncryptWithRSA(test, publicKey);
        //    byte[] dec = Cryptography.DecryptWithRSA(enc, privateKey);
        //    Assert.That(dec, Is.EqualTo(test));
        //}

        [Test]
        public async Task TCPTest()
        {
            //await Task.Delay(10000);
            TCPManager mgr = new TCPManager();
            long fileSize = 0;
            LinkedList<FileSendData> tmp = new LinkedList<FileSendData>();
            mgr.FileStatusUpdated = (s, e) =>
            {
                fileSize = e.FileSize;
                tmp.AddLast(e);
            };
            await mgr.RunEncryptedReceivingClient(IPAddress.Loopback, 8005, "Ok.test", "Cypher");
            StringBuilder sb = new StringBuilder().AppendJoin('\n', tmp);
            TestContext.WriteLine(sb.ToString());
            var inf = new FileInfo("Ok.test");
            Assert.That(inf.Length, Is.EqualTo(fileSize));
        }

        //[Test]
        //public void Test1()
        //{
        //    Cryptography.OnEncryptionFailure += (sender, e) =>
        //    {
        //        Assert.Fail(e?.ToString());
        //    };
        //    Cryptography.OnDecryptionFailure += (sender, e) =>
        //    {
        //        Assert.Fail(e?.ToString());
        //    };
        //    //FileStream input = new FileStream(".\\MyTest1.txt", FileMode.Open);
        //    //FileStream encrypt = new FileStream(".\\MyTest1.aes", FileMode.Create);
        //    //string key = "TestPassword";
        //    //try
        //    //{
        //    //    Cryptography.EncryptStreamAsync(input, encrypt, key).GetAwaiter().GetResult();
        //    //}
        //    //finally
        //    //{
        //    //    input.Close();
        //    //    encrypt.Close();
        //    //}
        //    //encrypt = new FileStream(".\\MyTest1.aes", FileMode.Open);
        //    //FileStream t2 = new FileStream(".\\MyTest1_enc.txt", FileMode.Create);
        //    //try
        //    //{
        //    //    Cryptography.DecryptStreamAsync(encrypt, t2, key).GetAwaiter().GetResult();
        //    //}
        //    //finally
        //    //{
        //    //    encrypt.Close();
        //    //    t2.Close();
        //    //}
        //    //using StreamReader r1 = new StreamReader(".\\MyTest1.txt");
        //    //using StreamReader r2 = new StreamReader(".\\MyTest1_enc.txt");
        //    //string s1 = r1.ReadToEnd();
        //    //string s2 = r2.ReadToEnd();
        //    //Assert.That(s1, Is.EqualTo(s2));
        //    Cryptography.EncryptFileAsync(".\\MyTest1.txt", ".\\MyTest1.enc", "TestPassword").GetAwaiter().GetResult();
        //    Cryptography.DecryptFileAsync(".\\MyTest1.enc", ".\\MyTest1_enc", "TestPassword").GetAwaiter().GetResult();
        //    using StreamReader r1 = new StreamReader(".\\MyTest1.txt");
        //    using StreamReader r2 = new StreamReader(".\\MyTest1_enc.txt");
        //    string s1 = r1.ReadToEnd();
        //    string s2 = r2.ReadToEnd();
        //    Assert.That(s1, Is.EqualTo(s2));
        //}

        //[Test]
        //public void Test2()
        //{
        //    string s1 = "Hello, world!";
        //    string enc = Cryptography.EncryptWithCaesar(s1);
        //    string s2 = Cryptography.DecryptWithCaesar(enc);
        //    Assert.That(s2, Is.EqualTo(s1));
        //}

        //[Test]
        //public void Test3()
        //{
        //    string s1 = "Hello, world!";
        //    byte[] enc = Encoding.UTF8.GetBytes(s1);
        //    byte[] crypt = Cryptography.EncryptInitiallyWithRSA(enc, out string publicKey, out string privateKey);
        //    byte[] crypt2 = Cryptography.EncryptWithRSA(enc, publicKey);
        //    byte[] enc2 = Cryptography.DecryptWithRSA(crypt, privateKey);
        //    byte[] enc3 = Cryptography.DecryptWithRSA(crypt2, privateKey);
        //    //byte[] enc4 = CryptographyManager.DecryptWithRSA(crypt, publicKey);
        //    string s2 = Encoding.UTF8.GetString(enc2);
        //    string s3 = Encoding.UTF8.GetString(enc3);
        //    //string s4 = Encoding.UTF8.GetString(enc4);
        //    Assert.That(s1, Is.EqualTo(s2).And.EqualTo(s3));
        //}

        //[Test]
        //public void Test4()
        //{
        //    string s1 = "Hello, world!";
        //    string key = "Key";
        //    string enc = Cryptography.EncryptWithVigenere(s1, key, 2000);
        //    string s2 = Cryptography.DecryptWithVigenere(enc, key, 2000);
        //    Assert.That(s2, Is.EqualTo(s1));
        //}

        //[Test]
        //public void Test5()
        //{
        //    var config = Cryptography.CreateKeysForRSA(false, out string pub, out string pri);
        //    string test = "Hello, world!";
        //    byte[] enc = Encoding.UTF8.GetBytes(test);
        //    byte[] crypto = enc.EncryptWithRSA(config);
        //    byte[] encRes = crypto.DecryptWithRSA(pri);
        //    string s1 = Encoding.UTF8.GetString(encRes);
        //    config = Cryptography.CreateKeysForRSA(true, out pub, out pri);
        //    crypto = enc.EncryptWithRSA(pub);
        //    encRes = crypto.DecryptWithRSA(config);
        //    string s2 = Encoding.UTF8.GetString(encRes);
        //    Assert.That(test, Is.EqualTo(s1).And.EqualTo(s2));
        //}

        //[Test]
        //public void Test6()
        //{
        //    string s1 = "Hello, world!";
        //    byte[] enc = Encoding.UTF8.GetBytes(s1);
        //    byte[] crypt = Cryptography.EncryptInitiallyWithRSA(enc, out string publicKey, out string privateKey);
        //    byte[] crypt2 = Cryptography.EncryptWithRSA(enc, privateKey);
        //    byte[] enc2 = Cryptography.DecryptWithRSA(crypt, privateKey);
        //    byte[] enc3 = Cryptography.DecryptWithRSA(crypt2, privateKey);
        //    //byte[] enc4 = CryptographyManager.DecryptWithRSA(crypt, publicKey);
        //    string s2 = Encoding.UTF8.GetString(enc2);
        //    string s3 = Encoding.UTF8.GetString(enc3);
        //    //string s4 = Encoding.UTF8.GetString(enc4);
        //    Assert.That(s1, Is.EqualTo(s2).And.EqualTo(s3));
        //}

        //[Test]
        //public void Test7()
        //{
        //    DateTime time = new DateTime(2011, 10, 5, 12, 1, 3);
        //    string s = $"{time:yyyyMMddHHmmss}";
        //    Assert.That(s, Is.EqualTo("20111005120103"));
        //}

        //private static byte[] ReadFully(Stream input)   
        //{
        //    byte[] buffer = new byte[16*1024];
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        int read;
        //        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
        //        {
        //            ms.Write(buffer, 0, read);
        //        }
        //        return ms.ToArray();
        //    }
        //}
    }
}