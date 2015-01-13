namespace GarrisonButler.Libraries
{
    internal class FreshDesk
    {
        public void SendBugReport(string title, string body)
        {
            //   // Command line argument must the the SMTP host.
            //GarrisonButler.Diagnostic("aaaaaaaaaaaaaa");
            //SmtpClient client = new SmtpClient("smtp.bifrost.ws");
            //NetworkCredential basicCredential =
            //    new NetworkCredential("InAppSupport", "InAppSupport");
            //client.UseDefaultCredentials = false;
            //client.Credentials = basicCredential; 
            //// Specify the e-mail sender. 
            //// Create a mailing address that includes a UTF8 character 
            //// in the display name.
            //GarrisonButler.Diagnostic("bbbbbbbbbbbbbb");
            //MailAddress from = new MailAddress("InAppSupport@Bifrost.ws", 
            //   "In " + (char)0xD8+ " Soft", 
            //System.Text.Encoding.UTF8);
            //// Set destinations for the e-mail message.
            //GarrisonButler.Diagnostic("ccccccccccc");
            //MailAddress to = new MailAddress("support@bifrost.ws");
            //// Specify the message content.
            //MailMessage message = new MailMessage(from, to);
            //message.Body = body;
            //// Include some non-ASCII characters in body and subject. 
            //GarrisonButler.Diagnostic("ddddddddddd");
            //message.Body += Environment.NewLine;
            //message.BodyEncoding =  System.Text.Encoding.UTF8;
            //message.Subject = title;
            //message.SubjectEncoding = System.Text.Encoding.UTF8;
            ////message.Attachments.Add(new Attachment(Styx.Common.Logging.LogFilePath));
            //// Set the method that is called back when the send operation ends.
            //GarrisonButler.Diagnostic("eeeeeeeeeeeee");
            //client.SendCompleted += new 
            //SendCompletedEventHandler(SendCompletedCallback);
            //// The userState can be any object that allows your callback  
            //// method to identify this send operation. 
            //// For this example, the userToken is a string constant. 
            //GarrisonButler.Diagnostic("fffffffffffff");
            //string userState = "test message1";
            //client.SendAsync(message, userState);
            ////Console.WriteLine("Sending message... press c to cancel mail. Press any other key to exit.");
            ////string answer = Console.ReadLine();
            //// If the user canceled the send, and mail hasn't been sent yet, 
            //// then cancel the pending operation. 
            ////if (answer.StartsWith("c") && mailSent == false)
            ////{
            ////    client.SendAsyncCancel();
            ////}
            //// Clean up.
            //message.Dispose();
            GarrisonButler.Diagnostic("Goodbye.");
        }
    }
}

//class CreateTicketWithAttachment
//{
//    private const string _APIKey = "YOUR_API_KEY";
//    private const string _Url = "http://YOUR_DOMAIN.freshdesk.com/helpdesk/tickets.json"; // verify if you have to use http or https for your account

//    private static void writeCRLF(Stream o)
//    {
//        byte[] crLf = Encoding.ASCII.GetBytes("\r\n");
//        o.Write(crLf, 0, crLf.Length);
//    }

//    private static void writeBoundaryBytes(Stream o, string b, bool isFinalBoundary)
//    {
//        string boundary = isFinalBoundary == true ? "--" + b + "--" : "--" + b + "\r\n";
//        byte[] d = Encoding.ASCII.GetBytes(boundary);
//        o.Write(d, 0, d.Length);
//    }

//    private static void writeContentDispositionFormDataHeader(Stream o, string name)
//    {
//        string data = "Content-Disposition: form-data; name=\"" + name + "\"\r\n\r\n";
//        byte[] b = Encoding.ASCII.GetBytes(data);
//        o.Write(b, 0, b.Length);
//    }

//    private static void writeContentDispositionFileHeader(Stream o, string name, string fileName, string contentType)
//    {
//        string data = "Content-Disposition: form-data; name=\"" + name + "\"; filename=\"" + fileName + "\"\r\n";
//        data += "Content-Type: " + contentType + "\r\n\r\n";
//        byte[] b = Encoding.ASCII.GetBytes(data);
//        o.Write(b, 0, b.Length);
//    }

//    private static void writeString(Stream o, string data)
//    {
//        byte[] b = Encoding.ASCII.GetBytes(data);
//        o.Write(b, 0, b.Length);
//    }

//    static void Main(string[] args)
//    {
//        Console.WriteLine("Application starting...");

//        // Define boundary:
//        string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");

//        // Web Request:
//        HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(_Url);

//        wr.Headers.Clear();

//        // Method and headers:
//        wr.ContentType = "multipart/form-data; boundary=" + boundary;
//        wr.Method = "POST";
//        wr.KeepAlive = true;

//        // Basic auth:
//        string login = _APIKey + ":X";
//        string credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(login));
//        wr.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;

//        // Body:
//        using (var rs = wr.GetRequestStream())
//        {
//            // Email:
//            writeBoundaryBytes(rs, boundary, false);
//            writeContentDispositionFormDataHeader(rs, "helpdesk_ticket[email]");
//            writeString(rs, "example@example.com");
//            writeCRLF(rs);

//            // Subject:
//            writeBoundaryBytes(rs, boundary, false);
//            writeContentDispositionFormDataHeader(rs, "helpdesk_ticket[subject]");
//            writeString(rs, "Ticket Title");
//            writeCRLF(rs);

//            // Description:
//            writeBoundaryBytes(rs, boundary, false);
//            writeContentDispositionFormDataHeader(rs, "helpdesk_ticket[description]");
//            writeString(rs, "Ticket description.");
//            writeCRLF(rs);

//            // Attachment:
//            writeBoundaryBytes(rs, boundary, false);
//            writeContentDispositionFileHeader(rs, "helpdesk_ticket[attachments][][resource]", "x.txt", "text/plain");
//            FileStream fs = new FileStream("x.txt", FileMode.Open, FileAccess.Read);
//            byte[] data = new byte[fs.Length];
//            fs.Read(data, 0, data.Length);
//            fs.Close();
//            rs.Write(data, 0, data.Length);
//            writeCRLF(rs);

//            // End marker:
//            writeBoundaryBytes(rs, boundary, true);

//            rs.Close();
//        }

//        // Response processing:
//        try
//        {
//            Console.WriteLine("Submitting Request");
//            var response = (HttpWebResponse)wr.GetResponse();
//            Stream resStream = response.GetResponseStream();
//            string resJson = new StreamReader(resStream, Encoding.ASCII).ReadToEnd();
//            Console.WriteLine(resJson);
//        }
//        catch(Exception ex)
//        {
//            Console.WriteLine("ERROR");
//            Console.WriteLine(ex.Message);
//        }
//        finally
//        {
//            Console.WriteLine(Environment.NewLine);
//            Console.WriteLine(Environment.NewLine);
//        }

//    }

//}