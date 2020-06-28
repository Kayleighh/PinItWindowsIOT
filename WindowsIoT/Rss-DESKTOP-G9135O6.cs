using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Web.Syndication;
using Windows.System.Threading;


namespace RSS
{
    public class rss
    {
        private ThreadPoolTimer RssTimer;
        private async void load(ItemsControl list, Uri uri)
        {
            SyndicationClient client = new SyndicationClient();
            SyndicationFeed feed = await client.RetrieveFeedAsync(uri);
            if (feed != null)
            {
                foreach (SyndicationItem item in feed.Items)
                {
                    list.Items.Add(item);
                }
            }
        }

        public void Go(ref ItemsControl list, string value)
        {
            try
            {
                load(list, new Uri(value));

            }
            catch
            {

            }
            list.Focus(FocusState.Keyboard);
        }
    }
}



