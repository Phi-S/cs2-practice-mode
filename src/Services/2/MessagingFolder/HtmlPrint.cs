namespace Cs2PracticeMode.Services._2.MessagingFolder;

public class HtmlPrint
{
    private readonly object _contentLock = new();
    private string _content;

    public HtmlPrint(string content)
    {
        _content = content;
    }

    public string Content
    {
        get
        {
            lock (_contentLock)
            {
                return _content;
            }
        }
        set
        {
            lock (_contentLock)
            {
                _content = value;
            }
        }
    }
}