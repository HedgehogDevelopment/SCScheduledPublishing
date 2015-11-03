<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MailTester.aspx.cs" Inherits="ScheduledPublish.sitecore.admin.custom.MailTester" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link href="/sitecore/shell/themes/standard/default/WebFramework.css" rel="stylesheet" />
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div>
            <asp:TextBox runat="server" ID="PublishedItem" Text="/sitecore/content" />
            <asp:Button runat="server" Text="Test" />
        </div>
        <asp:TextBox runat="server" ID="EmailList" ReadOnly="true" TextMode="MultiLine" Width="300px" Height="150px" />
    </div>
    </form>
</body>
</html>
